using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.Transaction.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Social Security analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeSocialSecurity.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 437-807): the full PIFSS contribution calculation, computed PER ROW
/// exactly as legacy did (marital/education/child allowances, earnings-tier lookup, capped contribution
/// amounts). See <see cref="EmployeeSocialSecurityReportRowDto"/> for the one documented scope reduction
/// (per-education pivot columns folded into the EducationAllowance total).
///
/// STRUCTURAL NOTE: like the Leave Salary report, this cannot be one SQL projection -- legacy's own
/// calculation is a per-row imperative walk against several master tables. <see cref="BuildRowsAsync"/> is
/// the single source of truth, shared unpaged by both the read and the export.
///
/// NOT PORTED: the ColumnChooser JSON persistence and the callback-panel state machine.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeSocialSecurity)]
public class EmployeeSocialSecurityReportAppService : GridReportAppServiceBase, IEmployeeSocialSecurityReportAppService
{
    private readonly IRepository<EmployeeSocialSecurity> _employeeSocialSecurityRepository;
    private readonly IRepository<SocialSecurity> _socialSecurityRepository;
    private readonly IRepository<SocialSecurityDetail> _socialSecurityDetailRepository;
    private readonly IRepository<SocialSecurityMaritalAllowance> _maritalAllowanceRepository;
    private readonly IRepository<SocialSecurityEducationAllowance> _educationAllowanceRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly IRepository<EmployeeEducation> _employeeEducationRepository;
    private readonly IRepository<EmployeeDependant> _employeeDependantRepository;

    public EmployeeSocialSecurityReportAppService(
        IRepository<EmployeeSocialSecurity> employeeSocialSecurityRepository,
        IRepository<SocialSecurity> socialSecurityRepository,
        IRepository<SocialSecurityDetail> socialSecurityDetailRepository,
        IRepository<SocialSecurityMaritalAllowance> maritalAllowanceRepository,
        IRepository<SocialSecurityEducationAllowance> educationAllowanceRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        IRepository<EmployeeEducation> employeeEducationRepository,
        IRepository<EmployeeDependant> employeeDependantRepository)
    {
        _employeeSocialSecurityRepository = employeeSocialSecurityRepository;
        _socialSecurityRepository = socialSecurityRepository;
        _socialSecurityDetailRepository = socialSecurityDetailRepository;
        _maritalAllowanceRepository = maritalAllowanceRepository;
        _educationAllowanceRepository = educationAllowanceRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _employeeSponsorRepository = employeeSponsorRepository;
        _employeeEducationRepository = employeeEducationRepository;
        _employeeDependantRepository = employeeDependantRepository;
    }

    protected override string LegacyScreenName => "uscRptEmployeeSocialSecurity";

    protected override string ScreenCaption => "Employee Social Security";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSocialSecurityReportRowDto>> GetEmployeeSocialSecurityReport(
        GetEmployeeSocialSecurityReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var totalCount = rows.Count;

        var paged = rows.AsQueryable().OrderBy(input.Sorting).Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeeSocialSecurityReportRowDto>(totalCount, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeSocialSecurityReportToExcel(GetEmployeeSocialSecurityReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        rows = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync("EmployeeSocialSecurity.xlsx", rows, GetExportColumns(), BuildParameterString(input));
    }

    private async Task<List<EmployeeSocialSecurityReportRowDto>> BuildRowsAsync(GetEmployeeSocialSecurityReportInput input)
    {
        var asOf = input.ResolveAsOfDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var now = Today;

        var socialSecurities = await _socialSecurityRepository.GetAll()
            .Where(s => s.StartDate <= asOf && s.EndDate >= asOf)
            .ToListAsync();

        var baseRows = await _employeeSocialSecurityRepository.GetAll()
            .Where(r => r.StartDate <= asOf && r.EndDate >= asOf && r.SocialSecurityContrib)
            .WhereIf(employeeCodes.Count > 0, r => employeeCodes.Contains(r.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, r => rlsEmployeeIds.Contains(r.EmployeeId))
            .Select(r => new
            {
                r.Id,
                r.EmployeeId,
                EmployeeCode = r.Employee.Code,
                EmployeeName = r.Employee.FullNameEn,
                r.Employee.NationalityId,
                r.Employee.Gender,
                MaritalStatusId = r.Employee.MaritalStatusId,
                MaritalStatusName = r.Employee.MaritalStatus == null ? string.Empty : r.Employee.MaritalStatus.NameEn,
                MaritalStatusCode = r.Employee.MaritalStatus == null ? null : r.Employee.MaritalStatus.Code,
                r.StartDate
            })
            .ToListAsync();

        var rows = new List<EmployeeSocialSecurityReportRowDto>(baseRows.Count);

        foreach (var rec in baseRows)
        {
            var row = new EmployeeSocialSecurityReportRowDto
            {
                Id = rec.Id,
                EmployeeCode = rec.EmployeeCode,
                EmployeeName = rec.EmployeeName,
                EffectiveDate = rec.StartDate,
                Gender = rec.Gender.ToString(),
                MaritalStatusName = rec.MaritalStatusName
            };

            var socialSecurity = socialSecurities.FirstOrDefault(s => s.NationalityId == rec.NationalityId);
            if (socialSecurity == null)
            {
                rows.Add(row);
                continue;
            }

            var basicSalary = await _employeeBasicPayDetailRepository.GetAll()
                .Where(d => d.EmployeeBasicPay.EmployeeId == rec.EmployeeId
                            && d.EmployeeBasicPay.StartDate <= asOf && d.EmployeeBasicPay.EndDate >= asOf
                            && d.WageType.IncludeInSocialSecurity)
                .SumAsync(d => d.Amount);

            var contractSalary = await _employeeSponsorRepository.GetAll()
                .Where(s => s.EmployeeId == rec.EmployeeId && s.StartDate <= asOf && s.EndDate >= asOf)
                .Select(s => s.ContractSalary)
                .FirstOrDefaultAsync();

            row.BasicSalary = basicSalary;
            row.ContractSalary = contractSalary;

            var settings = await SysSettingsAccessor.GetPayrollSettingsAsync();
            var rate = settings.SocialSecurityRateRule == SocialSecurityRateRule.WageSalary ? basicSalary : contractSalary;
            var innerRate = rate;

            var employeeEducations = await _employeeEducationRepository.GetAll()
                .Where(e => e.EmployeeId == rec.EmployeeId && e.StartDate <= now && e.EndDate >= now)
                .Select(e => new { e.EducationId, EducationName = e.Education.NameEn, e.EducationBranchId, EducationBranchName = e.EducationBranch.NameEn })
                .ToListAsync();

            row.EducationLevelName = employeeEducations.Select(e => e.EducationName).FirstOrDefault();
            row.EducationBranchName = employeeEducations.Select(e => e.EducationBranchName).FirstOrDefault();

            var educationBranchIds = employeeEducations.Select(e => e.EducationBranchId).Distinct().ToList();

            double maritalAllowance = 0;
            if (!rec.MaritalStatusName.IsNullOrWhiteSpace())
            {
                var maritalAllowances = _maritalAllowanceRepository.GetAll()
                    .Where(m => m.SocialSecurityId == socialSecurity.Id && educationBranchIds.Contains(m.EducationBranchId));

                maritalAllowance = rec.Gender == Gender.Female
                    // Legacy: females always take the "not single" allowance regardless of actual status.
                    ? await maritalAllowances.Where(m => m.MaritalStatus.Code != "1").SumAsync(m => m.Amount)
                    : rec.MaritalStatusId.HasValue
                        ? await maritalAllowances.Where(m => m.MaritalStatusId == rec.MaritalStatusId.Value).SumAsync(m => m.Amount)
                        : 0;
            }

            row.MaritalAllowance = maritalAllowance;
            innerRate += maritalAllowance;

            double educationAllowance = 0;
            foreach (var edu in employeeEducations)
            {
                educationAllowance += await _educationAllowanceRepository.GetAll()
                    .Where(e => e.SocialSecurityId == socialSecurity.Id
                                && e.EducationId == edu.EducationId
                                && e.EducationBranchId == edu.EducationBranchId)
                    .Select(e => e.Amount)
                    .FirstOrDefaultAsync();
            }

            row.EducationAllowance = educationAllowance;
            innerRate += educationAllowance;

            double childAllowance = 0;
            double specialChildAllowance = 0;
            if (rec.Gender != Gender.Female)
            {
                var dependants = await _employeeDependantRepository.GetAll()
                    .Where(d => d.EmployeeId == rec.EmployeeId && d.IncludeInSocialSecurity
                                && d.StartDate <= now && d.EndDate >= now)
                    .ToListAsync();

                var childCount = dependants.Count(d => !d.SpecialNeed);
                var specialChildCount = dependants.Count(d => d.SpecialNeed);
                childAllowance = childCount * socialSecurity.ChildAllowance;
                specialChildAllowance = specialChildCount * socialSecurity.SpecialNeedChildAllowance;
            }

            row.ChildAllowance = childAllowance;
            row.SpecialChildAllowance = specialChildAllowance;
            innerRate += childAllowance + specialChildAllowance;

            row.GrossAmount = maritalAllowance + educationAllowance + childAllowance + specialChildAllowance;
            row.TotalAmount = innerRate;

            var details = await _socialSecurityDetailRepository.GetAll()
                .Where(d => d.SocialSecurityId == socialSecurity.Id)
                .ToListAsync();

            if (details.Count > 0)
            {
                var lowestTier = details.OrderBy(d => d.MaxEarnings).First();
                var highestTier = details.OrderByDescending(d => d.MaxEarnings).First();

                double caps = innerRate;
                double extraCaps = 0;
                if (innerRate > highestTier.MaxEarnings)
                {
                    caps = lowestTier.MaxEarnings;
                    extraCaps = highestTier.MaxEarnings - caps;
                }
                else if (innerRate > lowestTier.MaxEarnings && innerRate < highestTier.MaxEarnings)
                {
                    caps = lowestTier.MaxEarnings;
                    extraCaps = innerRate - caps;
                    var difference = highestTier.MaxEarnings - lowestTier.MaxEarnings;
                    if (extraCaps > difference)
                    {
                        extraCaps = difference;
                    }
                }

                row.Caps = caps;
                row.ExtraCaps = extraCaps;

                var tier = details.FirstOrDefault(d => d.MinEarnings <= innerRate && d.MaxEarnings >= innerRate);
                if (tier == null)
                {
                    var maxEarnings = details.Max(d => d.MaxEarnings);
                    tier = details.FirstOrDefault(d => d.MaxEarnings == maxEarnings);
                    innerRate = maxEarnings;
                }

                if (tier != null)
                {
                    double employeeDed = 0;
                    row.EmployeeSharePer = tier.EmployeeContribution;
                    if (tier.MinEarningsEmployeeContribution != 0)
                    {
                        employeeDed += (tier.MinEarnings - 1) * tier.MinEarningsEmployeeContribution / 100;
                    }

                    if (tier.EmployeeContribution != 0)
                    {
                        employeeDed += innerRate * tier.EmployeeContribution / 100;
                    }

                    if (socialSecurity.EarningsCap > 0 && employeeDed > socialSecurity.EarningsCap)
                    {
                        employeeDed = socialSecurity.EarningsCap;
                    }

                    row.EmployeeShareAmount = employeeDed;

                    double companyDed = 0;
                    row.CompanySharePer = tier.EmployerContribution;
                    if (tier.EmployeeContribution != 0)
                    {
                        companyDed += innerRate * tier.EmployerContribution / 100;
                    }

                    if (socialSecurity.EarningsCap > 0 && companyDed > socialSecurity.EarningsCap)
                    {
                        companyDed = socialSecurity.EarningsCap;
                    }

                    row.CompanyShareAmount = companyDed;
                }
            }

            rows.Add(row);
        }

        return rows;
    }

    private string BuildParameterString(GetEmployeeSocialSecurityReportInput input)
    {
        var parts = new List<string> { "From Date : " + input.ResolveAsOfDate(Today).ToString("dd/MM/yyyy") };

        if (!input.EmployeeCode.IsNullOrWhiteSpace())
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    private static IReadOnlyList<GridReportColumn<EmployeeSocialSecurityReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeSocialSecurityReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("EffectiveDate", r => r.EffectiveDate),
            new("Gender", r => r.Gender),
            new("MaritalStatus", r => r.MaritalStatusName),
            new("BasicSalary", r => r.BasicSalary),
            new("ContractSalary", r => r.ContractSalary),
            new("EducationBranch", r => r.EducationBranchName),
            new("EducationLevel", r => r.EducationLevelName),
            new("MaritalAllowance", r => r.MaritalAllowance),
            new("ChildAllowance", r => r.ChildAllowance),
            new("SpecialChildAllowance", r => r.SpecialChildAllowance),
            new("EducationAllowance", r => r.EducationAllowance),
            new("GrossAmount", r => r.GrossAmount),
            new("TotalAmount", r => r.TotalAmount),
            new("Caps", r => r.Caps),
            new("ExtraCaps", r => r.ExtraCaps),
            new("CompanySharePer", r => r.CompanySharePer),
            new("EmployeeSharePer", r => r.EmployeeSharePer),
            new("CompanyShareAmount", r => r.CompanyShareAmount),
            new("EmployeeShareAmount", r => r.EmployeeShareAmount)
        };
    }
}
