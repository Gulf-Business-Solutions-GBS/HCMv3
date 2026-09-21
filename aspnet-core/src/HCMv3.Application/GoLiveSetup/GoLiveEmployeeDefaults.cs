using System;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Organizations;
using HCMv3.OrganizationManagement;
using HCMv3.OrganizationManagement.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GoLiveSetup;

/// <summary>
/// What every employee in "Load Employee Data" is attached to, found or created: the organisation unit, and
/// an employee group and sub-group set up as Kuwait labour law says. Each is looked up first, so a second
/// load reuses what the first one made -- and a tenant that already has its own is not given a duplicate.
///
/// <para>The Kuwait values are the Default tenant's own "Benefits_Normal" grade, migrated from the legacy
/// system, less its tickets (a management benefit, not law). Its sick-leave deduction rule has no slabs
/// there; here it gets art. 69's.</para>
/// </summary>
public class GoLiveEmployeeDefaults : AbpServiceBase, ITransientDependency
{
    public const string EmployeeGroupName = "Default Normal Staff";
    public const string EmployeeSubGroupName = "Default Grade";

    /// <summary>Kuwait Labour Law art. 51: indemnity is capped at a year and a half's pay.</summary>
    private const double IndemnityCapMonths = 18;

    /// <summary>Annual and sick leave -- art. 70 and art. 69 -- by the leave-type codes the payroll pack seeds.</summary>
    private static readonly (string Code, int Days)[] LeaveEntitlements = { ("1", 30), ("3", 15) };

    /// <summary>
    /// Art. 51: 15 days' pay for each of the first five years, 26 days' pay for each year after. A day's pay is
    /// a 26th of the month, matching wage type 1010's fixed days. The last slab ends at year 99, as legacy's do.
    /// </summary>
    private static readonly (double ChangeYear, double GratuityDays, double BasisDays)[] GratuitySlabs =
    {
        (5, 15, 26),
        (99, 26, 26),
    };

    /// <summary>
    /// What share of the accrued indemnity is PAID, by how the employee left and their years of service -- the
    /// legacy Full &amp; Final screen's suggestion reads these. Without a rule for a separation type, that type is
    /// suggested nothing at all.
    ///
    /// <para>Copied from the legacy CCH database, the newest Kuwait client, whose termination scale was corrected
    /// to 100 % throughout in August 2025. (RCF and ASF still pay 50 % on termination under 5 years.) The bands
    /// stop at .99 so they never overlap: legacy compares a year count rounded to 2 decimals, inclusive both
    /// ends.</para>
    /// </summary>
    private static readonly (SeparationType Type, (double Min, double Max, double Percentage)[] Bands)[] IndemnityPayRules =
    {
        (SeparationType.Resignation, new[] { (0.0, 2.99, 0.0), (3.0, 4.99, 50.0), (5.0, 9.99, 66.67), (10.0, 99.0, 100.0) }),
        (SeparationType.Termination, new[] { (0.0, 2.99, 100.0), (3.0, 4.99, 100.0), (5.0, 9.99, 100.0), (10.0, 99.0, 100.0) }),
    };

    public const string SickLeaveRuleName = "Kuwait Art.69 Sick Leave";

    /// <summary>Leave-type code of paid sick leave, whose entitlement carries the deduction rule.</summary>
    private const string SickLeaveCode = "3";

    /// <summary>
    /// Art. 69, after the 15 days on full pay: 10 days at three quarters, 10 at half, 10 at a quarter, then 30
    /// unpaid. Each slab is (last day of the slab, % of pay deducted), counted in "Sick Leave Unpaid" (type 5)
    /// days in the calendar year -- the engine picks the first slab whose day count covers the running count.
    /// </summary>
    private static readonly (double DaysCount, double DeductionPercent)[] SickLeaveSlabs =
    {
        (10, 25),
        (20, 50),
        (30, 75),
        (60, 100),
    };

    /// <summary>Early enough to cover anyone's joining date: the time engine reads leave from the day they joined.</summary>
    private static readonly DateTime SlabStart = new(1900, 1, 1);

    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly IOrgUnitsAppService _orgUnits;
    private readonly IOrganizationUnitTypesAppService _orgUnitTypes;
    private readonly IEmployeeGroupsAppService _groups;
    private readonly IEmployeeSubGroupsAppService _subGroups;
    private readonly IEmployeeSubGroupGratuitiesAppService _gratuities;
    private readonly IEmployeeSubGroupAbsencesAppService _absences;
    private readonly IAbsenceDeductionRulesAppService _deductionRules;
    private readonly IAbsenceDedRuleDetailsAppService _deductionRuleDetails;
    private readonly IRepository<OrganizationUnit, long> _orgUnitRepository;
    private readonly IRepository<OrganizationUnitType> _orgUnitTypeRepository;
    private readonly IRepository<EmployeeGroup> _groupRepository;
    private readonly IRepository<EmployeeSubGroup> _subGroupRepository;
    private readonly IRepository<EmployeeSubGroupGratuity> _gratuityRepository;
    private readonly IRepository<EmployeeSubGroupAbsence> _absenceRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<AbsenceDeductionRule> _deductionRuleRepository;
    private readonly IRepository<AbsenceDedRuleDetail> _deductionRuleDetailRepository;
    private readonly IRepository<IndemnityPayRule> _indemnityPayRuleRepository;
    private readonly IRepository<IndemnityPayRuleDetail> _indemnityPayRuleDetailRepository;

    public GoLiveEmployeeDefaults(
        IOrgUnitsAppService orgUnits,
        IOrganizationUnitTypesAppService orgUnitTypes,
        IEmployeeGroupsAppService groups,
        IEmployeeSubGroupsAppService subGroups,
        IEmployeeSubGroupGratuitiesAppService gratuities,
        IEmployeeSubGroupAbsencesAppService absences,
        IAbsenceDeductionRulesAppService deductionRules,
        IAbsenceDedRuleDetailsAppService deductionRuleDetails,
        IRepository<OrganizationUnit, long> orgUnitRepository,
        IRepository<OrganizationUnitType> orgUnitTypeRepository,
        IRepository<EmployeeGroup> groupRepository,
        IRepository<EmployeeSubGroup> subGroupRepository,
        IRepository<EmployeeSubGroupGratuity> gratuityRepository,
        IRepository<EmployeeSubGroupAbsence> absenceRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<AbsenceDeductionRule> deductionRuleRepository,
        IRepository<AbsenceDedRuleDetail> deductionRuleDetailRepository,
        IRepository<IndemnityPayRule> indemnityPayRuleRepository,
        IRepository<IndemnityPayRuleDetail> indemnityPayRuleDetailRepository)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;

        _orgUnits = orgUnits;
        _orgUnitTypes = orgUnitTypes;
        _groups = groups;
        _subGroups = subGroups;
        _gratuities = gratuities;
        _absences = absences;
        _deductionRules = deductionRules;
        _deductionRuleDetails = deductionRuleDetails;
        _orgUnitRepository = orgUnitRepository;
        _orgUnitTypeRepository = orgUnitTypeRepository;
        _groupRepository = groupRepository;
        _subGroupRepository = subGroupRepository;
        _gratuityRepository = gratuityRepository;
        _absenceRepository = absenceRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _deductionRuleRepository = deductionRuleRepository;
        _deductionRuleDetailRepository = deductionRuleDetailRepository;
        _indemnityPayRuleRepository = indemnityPayRuleRepository;
        _indemnityPayRuleDetailRepository = indemnityPayRuleDetailRepository;
    }

    /// <summary>
    /// The company as an organisation unit, at the top of the tree, of type "Company". When that type is the one
    /// that carries payroll areas, the unit carries this one too -- a second route to it after each employee's
    /// org assignment, and the one the payroll uses to find the currency's decimals for rounding.
    /// </summary>
    public async Task<(long Id, bool Created)> EnsureOrganizationUnitAsync(string name, int payrollAreaId)
    {
        var existing = await _orgUnitRepository.FirstOrDefaultAsync(o => o.DisplayName == name);
        if (existing != null)
        {
            return (existing.Id, false);
        }

        var type = await EnsureCompanyTypeAsync();

        await _orgUnits.CreateOrEdit(new CreateOrEditOrgUnitDto
        {
            NameEn = name,
            NameAr = name,
            OrganizationUnitTypeId = type.Id,
            PayrollAreaId = type.AssignPayrollArea ? payrollAreaId : null,
        });
        await CurrentUnitOfWork.SaveChangesAsync();

        return ((await _orgUnitRepository.FirstOrDefaultAsync(o => o.DisplayName == name)).Id, true);
    }

    /// <summary>The group, its sub-group, and the sub-group's indemnity slabs and leave entitlements.</summary>
    public async Task<GoLiveEmployeeGrade> EnsureGradeAsync()
    {
        var grade = new GoLiveEmployeeGrade();

        var group = await _groupRepository.FirstOrDefaultAsync(g => g.NameEn == EmployeeGroupName);
        if (group == null)
        {
            // The code is left blank on purpose: the group service assigns the next one, as its screen does.
            await _groups.CreateOrEdit(new CreateOrEditEmployeeGroupDto
            {
                NameEn = EmployeeGroupName,
                NameAr = EmployeeGroupName,
                IndemnityMaxNumber = IndemnityCapMonths,
                IndemnityMaxUnit = Unit.Month,
                // 100%: the cap is the whole 18 months' pay. Left at 0, the engine caps indemnity to nothing.
                IndemnityMaxAccruePercentage = 100,
            });
            await CurrentUnitOfWork.SaveChangesAsync();

            group = await _groupRepository.FirstOrDefaultAsync(g => g.NameEn == EmployeeGroupName);
            grade.GroupCreated = true;
        }

        var subGroup = await _subGroupRepository.FirstOrDefaultAsync(s => s.EmployeeGroupId == group.Id && s.NameEn == EmployeeSubGroupName);
        if (subGroup == null)
        {
            await _subGroups.CreateOrEdit(new CreateOrEditEmployeeSubGroupDto
            {
                EmployeeGroupId = group.Id,
                NameEn = EmployeeSubGroupName,
                NameAr = EmployeeSubGroupName,
                PayOvertime = true,
            });
            await CurrentUnitOfWork.SaveChangesAsync();

            subGroup = await _subGroupRepository.FirstOrDefaultAsync(s => s.EmployeeGroupId == group.Id && s.NameEn == EmployeeSubGroupName);
            grade.SubGroupCreated = true;
        }

        grade.GroupId = group.Id;
        grade.SubGroupId = subGroup.Id;
        grade.GratuitySlabsCreated = await EnsureGratuitySlabsAsync(subGroup.Id);
        grade.LeaveEntitlementsCreated = await EnsureLeaveEntitlementsAsync(subGroup.Id, grade);

        return grade;
    }

    /// <summary>
    /// The Kuwait indemnity pay rules for <paramref name="countryId"/> -- the payroll area's country, which is
    /// how the Full &amp; Final screen finds them (<c>null</c> matches a payroll area with no country). Per
    /// separation type, all or nothing: a type that already has a rule for this country is someone's own.
    /// </summary>
    /// <returns>How many rules were created.</returns>
    public async Task<int> EnsureIndemnityPayRulesAsync(int? countryId)
    {
        var created = 0;

        foreach (var (type, bands) in IndemnityPayRules)
        {
            if (await _indemnityPayRuleRepository.GetAll().AnyAsync(r => r.SeparationType == type && r.CountryId == countryId))
            {
                continue;
            }

            var ruleId = await _indemnityPayRuleRepository.InsertAndGetIdAsync(new IndemnityPayRule
            {
                TenantId = CurrentUnitOfWork.GetTenantId() ?? 0,
                SeparationType = type,
                StartDate = SlabStart,
                EndDate = OpenEnded,
                MaxNumber = IndemnityCapMonths,
                MaxUnit = Unit.Month,
                MaxPayPercentage = 100,
                CountryId = countryId,
            });

            foreach (var (min, max, percentage) in bands)
            {
                await _indemnityPayRuleDetailRepository.InsertAsync(new IndemnityPayRuleDetail
                {
                    TenantId = CurrentUnitOfWork.GetTenantId() ?? 0,
                    IndemnityPayRuleId = ruleId,
                    MinYearOfService = min,
                    MaxYearOfService = max,
                    PayPercentage = percentage,
                });
            }

            await CurrentUnitOfWork.SaveChangesAsync();
            created++;
        }

        return created;
    }

    /// <summary>All or nothing: a sub-group with any slab of its own was set up by someone, and is theirs.</summary>
    private async Task<int> EnsureGratuitySlabsAsync(int subGroupId)
    {
        if (await _gratuityRepository.GetAll().AnyAsync(g => g.EmployeeSubGroupId == subGroupId))
        {
            return 0;
        }

        foreach (var slab in GratuitySlabs)
        {
            await _gratuities.CreateOrEdit(new CreateOrEditEmployeeSubGroupGratuityDto
            {
                EmployeeSubGroupId = subGroupId,
                StartDate = SlabStart,
                EndDate = OpenEnded,
                ChangeYear = slab.ChangeYear,
                GratuityDays = slab.GratuityDays,
                BasisDays = slab.BasisDays,
                ImpactFromDateOfJoining = false,
            });
            await CurrentUnitOfWork.SaveChangesAsync();
        }

        return GratuitySlabs.Length;
    }

    /// <summary>
    /// One entitlement per leave type the sub-group does not have yet. A leave type the tenant has not got --
    /// no payroll pack seeded -- is skipped rather than guessed at.
    /// </summary>
    private async Task<int> EnsureLeaveEntitlementsAsync(int subGroupId, GoLiveEmployeeGrade grade)
    {
        var created = 0;

        foreach (var (code, days) in LeaveEntitlements)
        {
            var absenceType = await _absenceTypeRepository.FirstOrDefaultAsync(a => a.Code == code);
            if (absenceType == null
                || await _absenceRepository.GetAll().AnyAsync(a => a.EmployeeSubGroupId == subGroupId && a.AbsenceTypeId == absenceType.Id))
            {
                continue;
            }

            int? deductionRuleId = null;
            if (code == SickLeaveCode)
            {
                var (ruleId, ruleCreated) = await EnsureSickLeaveRuleAsync();
                deductionRuleId = ruleId;
                grade.SickLeaveRuleCreated = ruleCreated;
            }

            await _absences.CreateOrEdit(new CreateOrEditEmployeeSubGroupAbsenceDto
            {
                EmployeeSubGroupId = subGroupId,
                AbsenceTypeId = absenceType.Id,
                AbsenceDeductionRuleId = deductionRuleId,
                StartDate = SlabStart,
                EndDate = OpenEnded,
                ChangeYear = 0,
                TotalDays = days,
                // No carry-over cap: the law sets none, and a cap forfeits days the customer may not expect to lose.
                MaxLimit = 0,
            });
            await CurrentUnitOfWork.SaveChangesAsync();
            created++;
        }

        return created;
    }

    /// <summary>
    /// "Company", at level 1 -- as the Default tenant has it -- since the type codes are a fixed list and level 0
    /// may not carry a payroll area. A new one carries payroll areas unless another type already does: only
    /// one type may.
    /// </summary>
    private async Task<OrganizationUnitType> EnsureCompanyTypeAsync()
    {
        var type = await _orgUnitTypeRepository.FirstOrDefaultAsync(t => t.Code == OrganizationUnitType.Level1Code);
        if (type != null)
        {
            return type;
        }

        var anotherCarriesPayrollAreas = await _orgUnitTypeRepository.GetAll().AnyAsync(t => t.AssignPayrollArea);

        await _orgUnitTypes.CreateOrEdit(new CreateOrEditOrganizationUnitTypeDto
        {
            Code = OrganizationUnitType.Level1Code,
            NameEn = "Company",
            NameAr = "شركة",
            AssignPayrollArea = !anotherCarriesPayrollAreas,
        });
        await CurrentUnitOfWork.SaveChangesAsync();

        return await _orgUnitTypeRepository.FirstOrDefaultAsync(t => t.Code == OrganizationUnitType.Level1Code);
    }

    /// <summary>
    /// The art. 69 rule, with its four slabs over all dates -- the engine reads the slab in force on the 1st of
    /// each pay month. A rule of this name that already has slabs is somebody's, and is left as it is.
    /// </summary>
    private async Task<(int Id, bool Created)> EnsureSickLeaveRuleAsync()
    {
        var created = false;
        var rule = await _deductionRuleRepository.FirstOrDefaultAsync(r => r.RuleName == SickLeaveRuleName);
        if (rule == null)
        {
            // The code is left blank on purpose: the rule service assigns the next one, as its screen does.
            await _deductionRules.CreateOrEdit(new CreateOrEditAbsenceDeductionRuleDto { RuleName = SickLeaveRuleName });
            await CurrentUnitOfWork.SaveChangesAsync();

            rule = await _deductionRuleRepository.FirstOrDefaultAsync(r => r.RuleName == SickLeaveRuleName);
            created = true;
        }

        if (!await _deductionRuleDetailRepository.GetAll().AnyAsync(d => d.AbsenceDeductionRuleId == rule.Id))
        {
            foreach (var (daysCount, deduction) in SickLeaveSlabs)
            {
                await _deductionRuleDetails.CreateOrEdit(new CreateOrEditAbsenceDedRuleDetailDto
                {
                    AbsenceDeductionRuleId = rule.Id,
                    StartDate = SlabStart,
                    EndDate = OpenEnded,
                    DaysCount = daysCount,
                    DeductionUnit = DeductionUnit.Percentage,
                    DeductionNumber = deduction,
                });
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            created = true;
        }

        return (rule.Id, created);
    }
}

/// <summary>The group and sub-group every loaded employee is put in, and what was created to get there.</summary>
public class GoLiveEmployeeGrade
{
    public int GroupId { get; set; }

    public int SubGroupId { get; set; }

    public bool GroupCreated { get; set; }

    public bool SubGroupCreated { get; set; }

    public int GratuitySlabsCreated { get; set; }

    public int LeaveEntitlementsCreated { get; set; }

    /// <summary>The art. 69 sick-leave deduction rule, or its slabs, had to be created.</summary>
    public bool SickLeaveRuleCreated { get; set; }
}
