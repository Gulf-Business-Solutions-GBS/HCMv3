using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee CTC (cost-to-company) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeCTC.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 1124-1687). With <c>uscRptEmployeeInfo</c> this is the largest control in the
/// family.
///
/// EVERY "as of" column is evaluated against TODAY -- legacy set <c>EndDate = DateTime.Now.Date</c> at the
/// top of Table1() (line 1129) and hid the date pickers.
///
/// NOT PORTED (DevExpress/WebForms plumbing with no data meaning): the drill-up/drill-down session stack
/// (single-level here -- <c>GetKey()</c>/<c>GetTable()</c> define only "Table1", lines 421-482), the
/// ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the dead
/// <c>Table1_Opt()</c> (lines 504-1123), an alternative formulation of the same sheet that the control
/// never calls.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeCTC)]
public class EmployeeCTCReportAppService : GridReportAppServiceBase, IEmployeeCTCReportAppService
{
    /// <summary>Legacy lines 1662-1666: salary register wage types read for the provision columns.</summary>
    private const string AbsentUnpaidWageTypeCode = "4024";

    private const string IndemnityProvisionWageTypeCode = "3003";

    private const string LeaveProvisionWageTypeCode = "3004";

    /// <summary>Legacy line 1462: the passport document type is matched by its ENGLISH name.</summary>
    private const string PassportDocumentTypeName = "Passport";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<EmployeeDocument> _employeeDocumentRepository;
    private readonly IRepository<EmployeeBank> _employeeBankRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly IRepository<EmployeeContract> _employeeContractRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeeBasicPay> _basicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _basicPayDetailRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeCTCReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<EmployeeDocument> employeeDocumentRepository,
        IRepository<EmployeeBank> employeeBankRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        IRepository<EmployeeContract> employeeContractRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<EmployeeBasicPay> basicPayRepository,
        IRepository<EmployeeBasicPayDetail> basicPayDetailRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _employeeDocumentRepository = employeeDocumentRepository;
        _employeeBankRepository = employeeBankRepository;
        _employeeSponsorRepository = employeeSponsorRepository;
        _employeeContractRepository = employeeContractRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _basicPayRepository = basicPayRepository;
        _basicPayDetailRepository = basicPayDetailRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeCTC";

    protected override string ScreenCaption => "Employee CTC";

    [HttpPost]
    public async Task<EmployeeCTCReportOutput> GetEmployeeCTCReport(GetEmployeeCTCReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var wageTypeNames = await FillWageColumnsAsync(rows);
        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildCtcParameterString(input), totalCount);

        return new EmployeeCTCReportOutput
        {
            TotalCount = totalCount,
            Items = rows,
            WageTypeNames = wageTypeNames
        };
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeCTCReportToExcel(GetEmployeeCTCReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        var wageTypeNames = await FillWageColumnsAsync(rows);
        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeCTC.xlsx",
            rows,
            GetExportColumns(wageTypeNames),
            BuildCtcParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c>'s data regions (lines 1128-1158 and 1412-1600).
    ///
    /// The dynamic wage-type columns, the basic-pay totals, the salary-register provisions and the OrgLevel
    /// columns are stamped on afterwards (see <see cref="FillWageColumnsAsync"/> and
    /// <see cref="StampOrgLevelsAsync"/>), which both entry points also call.
    /// </summary>
    private async Task<IQueryable<EmployeeCTCReportRowDto>> BuildQueryAsync(GetEmployeeCTCReportInput input)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var employeeStatus = input.EmployeeStatus;
        var payrollAreaId = input.PayrollAreaId;
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var today = Today;

        var query = _employeeRepository.GetAll()
            // Legacy lines 1133-1140.
            .WhereIf(employeeStatus.HasValue, e => e.Status == employeeStatus.Value)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            // Legacy lines 1149-1157: the employee must have an org assignment current as of today on the
            // selected payroll area.
            .WhereIf(payrollAreaId.HasValue, e => _orgAssignmentRepository.GetAll().Any(
                o => o.EmployeeId == e.Id
                     && o.StartDate <= today
                     && o.EndDate >= today
                     && o.PayrollAreaId == payrollAreaId.Value))
            // Legacy line 1159 ApplyRowLevelSecurity -- an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        var currentAssignments = _orgAssignmentRepository.GetAll()
            .Where(o => o.StartDate <= today && o.EndDate >= today);

        return query.Select(e => new EmployeeCTCReportRowDto
        {
            // Legacy "OID" column (line 1538).
            Id = e.Id,
            EmployeeCode = e.Code,
            EmployeeName = e.FullNameEn,
            EmployeeNameAr = e.FullNameAr,
            Status = e.Status,
            BirthDate = e.BirthDate,
            ProbationDays = e.ProbationDays,
            JoiningDate = e.JoiningDate,
            ProbationEndDate = e.ProbationEndDate,
            NationalityName = e.Nationality == null
                ? string.Empty
                : (useArabicNames ? e.Nationality.NameAr : e.Nationality.NameEn),
            ReligionName = e.Religion == null
                ? string.Empty
                : (useArabicNames ? e.Religion.NameAr : e.Religion.NameEn),
            Gender = e.Gender,
            MaritalStatusName = e.MaritalStatus == null
                ? string.Empty
                : (useArabicNames ? e.MaritalStatus.NameAr : e.MaritalStatus.NameEn),
            RecruitmentTypeName = e.RecruitmentType == null
                ? string.Empty
                : (useArabicNames ? e.RecruitmentType.NameAr : e.RecruitmentType.NameEn),
            City = e.City,
            GovernorateName = e.Governorate == null
                ? string.Empty
                : (useArabicNames ? e.Governorate.NameAr : e.Governorate.NameEn),
            CountryName = e.Country == null
                ? string.Empty
                : (useArabicNames ? e.Country.NameAr : e.Country.NameEn),
            Telephone = e.Telephone,
            Mobile = e.Mobile,
            CurrentAddress = e.CurrentAddress,
            PermanentAddress = e.PermanentAddress,
            CivilID = e.CivilID,
            LoginID = e.LoginID,
            AccessID = e.AccessID,
            Email = e.Email,
            BirthPlaceName = e.BirthPlace == null
                ? string.Empty
                : (useArabicNames ? e.BirthPlace.NameAr : e.BirthPlace.NameEn),

            // Legacy lines 1459-1464: a passport document valid today. Legacy also required
            // IsActive == Active; HCMv3's EmployeeDocument has no such flag.
            PassportNumber = _employeeDocumentRepository.GetAll()
                .Where(d => d.EmployeeId == e.Id
                            && d.IssueDate <= today
                            && d.ExpiryDate >= today
                            && d.DocumentType != null
                            && d.DocumentType.NameEn == PassportDocumentTypeName)
                .Select(d => d.DocumentNum)
                .FirstOrDefault(),

            // Legacy lines 1512-1519 / 1570-1574: the sponsor record current as of today.
            ResidencyType = _employeeSponsorRepository.GetAll()
                .Where(s => s.EmployeeId == e.Id && s.StartDate <= today && s.EndDate >= today)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => (ResidencyType?)s.ResidencyType)
                .FirstOrDefault(),
            SponsorName = _employeeSponsorRepository.GetAll()
                .Where(s => s.EmployeeId == e.Id && s.StartDate <= today && s.EndDate >= today)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.Sponsor == null ? string.Empty : s.Sponsor.NameEn)
                .FirstOrDefault(),

            // Legacy lines 1495-1497: the contract in force today.
            ContractTypeName = _employeeContractRepository.GetAll()
                .Where(c => c.EmployeeId == e.Id && c.StartDate <= today && c.EndDate >= today)
                .OrderBy(c => c.StartDate).ThenBy(c => c.Id)
                .Select(c => c.ContractType == null
                    ? string.Empty
                    : (useArabicNames ? c.ContractType.NameAr : c.ContractType.NameEn))
                .FirstOrDefault(),
            ContractStartDate = _employeeContractRepository.GetAll()
                .Where(c => c.EmployeeId == e.Id && c.StartDate <= today && c.EndDate >= today)
                .OrderBy(c => c.StartDate).ThenBy(c => c.Id)
                .Select(c => (DateTime?)c.StartDate)
                .FirstOrDefault(),
            ContractEndDate = _employeeContractRepository.GetAll()
                .Where(c => c.EmployeeId == e.Id && c.StartDate <= today && c.EndDate >= today)
                .OrderBy(c => c.StartDate).ThenBy(c => c.Id)
                .Select(c => (DateTime?)c.EndDate)
                .FirstOrDefault(),

            // Legacy lines 1499-1510 / 1576-1582: the bank record current as of today.
            AccountNumber = _employeeBankRepository.GetAll()
                .Where(b => b.EmployeeId == e.Id && b.StartDate <= today && b.EndDate >= today)
                .OrderBy(b => b.StartDate).ThenBy(b => b.Id)
                .Select(b => b.AccountNum)
                .FirstOrDefault(),
            IBAN = _employeeBankRepository.GetAll()
                .Where(b => b.EmployeeId == e.Id && b.StartDate <= today && b.EndDate >= today)
                .OrderBy(b => b.StartDate).ThenBy(b => b.Id)
                .Select(b => b.IBAN)
                .FirstOrDefault(),
            BankBranch = _employeeBankRepository.GetAll()
                .Where(b => b.EmployeeId == e.Id && b.StartDate <= today && b.EndDate >= today)
                .OrderBy(b => b.StartDate).ThenBy(b => b.Id)
                .Select(b => b.BankBranch)
                .FirstOrDefault(),

            // Legacy lines 1453-1455: the manager current as of today, "code - full name".
            ManagerName = _employeeManagerRepository.GetAll()
                .Where(m => m.EmployeeId == e.Id && m.StartDate <= today && m.EndDate >= today)
                .OrderBy(m => m.StartDate).ThenBy(m => m.Id)
                .Select(m => m.Manager == null
                    ? string.Empty
                    : m.Manager.Code + " - " + m.Manager.FullNameEn)
                .FirstOrDefault(),

            // Legacy lines 1436-1451 / 1585-1600: the org assignment current as of today.
            JobTitle = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null
                    ? string.Empty
                    : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            PayrollAreaName = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.PayrollArea == null ? string.Empty : o.PayrollArea.Name)
                .FirstOrDefault(),
            EmployeeGroupName = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.EmployeeGroup == null
                    ? string.Empty
                    : (useArabicNames ? o.EmployeeGroup.NameAr : o.EmployeeGroup.NameEn))
                .FirstOrDefault(),
            EmployeeSubGroupName = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.EmployeeSubGroup == null
                    ? string.Empty
                    : (useArabicNames ? o.EmployeeSubGroup.NameAr : o.EmployeeSubGroup.NameEn))
                .FirstOrDefault(),
            WorkLocationCode = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null ? string.Empty : o.WorkLocation.Code)
                .FirstOrDefault(),
            WorkLocationName = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null
                    ? string.Empty
                    : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            CostCenterCode = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.CostCenter == null ? string.Empty : o.CostCenter.Code)
                .FirstOrDefault(),
            CostCenterName = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.CostCenter == null ? string.Empty : o.CostCenter.NameEn)
                .FirstOrDefault(),
            CostCenterGroupName = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.CostCenter == null || o.CostCenter.CostCenterGroup == null
                    ? string.Empty
                    : o.CostCenter.CostCenterGroup.NameEn)
                .FirstOrDefault(),
            PostingGroupName = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.PostingGroup == null ? string.Empty : o.PostingGroup.NameEn)
                .FirstOrDefault(),
            BudgetingGroupName = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.BudgetingGroup == null ? string.Empty : o.BudgetingGroup.NameEn)
                .FirstOrDefault(),
            OrganizationUnitId = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),

            // Legacy lines 1521-1531 / 1670-1677: the LATEST separation by submission date -- note that,
            // unlike uscRptEmployeeInfo, revoked separations are NOT excluded here.
            SeparationType = _employeeSeparationRepository.GetAll()
                .Where(s => s.EmployeeId == e.Id)
                .OrderByDescending(s => s.SubmissionDate)
                .Select(s => (SeparationType?)s.SeparationType)
                .FirstOrDefault(),
            SubmissionDate = _employeeSeparationRepository.GetAll()
                .Where(s => s.EmployeeId == e.Id)
                .OrderByDescending(s => s.SubmissionDate)
                .Select(s => (DateTime?)s.SubmissionDate)
                .FirstOrDefault(),
            LastDayInService = _employeeSeparationRepository.GetAll()
                .Where(s => s.EmployeeId == e.Id)
                .OrderByDescending(s => s.SubmissionDate)
                .Select(s => (DateTime?)s.LastDayInService)
                .FirstOrDefault(),
            RevokeDate = _employeeSeparationRepository.GetAll()
                .Where(s => s.EmployeeId == e.Id)
                .OrderByDescending(s => s.SubmissionDate)
                .Select(s => s.RevokeDate)
                .FirstOrDefault(),

            // Legacy lines 1604-1612: the basic pay in force today gives EffectiveDate and TotalSalary.
            EffectiveDate = _basicPayRepository.GetAll()
                .Where(b => b.EmployeeId == e.Id && b.StartDate <= today && b.EndDate >= today)
                .OrderBy(b => b.StartDate).ThenBy(b => b.Id)
                .Select(b => (DateTime?)b.StartDate)
                .FirstOrDefault(),
            TotalSalary = _basicPayRepository.GetAll()
                .Where(b => b.EmployeeId == e.Id && b.StartDate <= today && b.EndDate >= today)
                .OrderBy(b => b.StartDate).ThenBy(b => b.Id)
                .Select(b => b.Amount)
                .FirstOrDefault()
        });
    }

    /// <summary>
    /// Ports the dynamic basic-pay pivot (legacy lines 1159-1165 for the column set, 1604-1620 for the
    /// values) and the salary-register provision columns (lines 1650-1668). Runs after materialization
    /// because the column count is data-dependent, and is shared by the paged read and the export.
    /// </summary>
    private async Task<List<string>> FillWageColumnsAsync(List<EmployeeCTCReportRowDto> rows)
    {
        var today = Today;
        var useArabicNames = UseArabicNames;

        // Legacy lines 1159-1165: wage types of every basic pay detail whose parent pay is in force today,
        // distinct and ordered by PayrollOrder.
        var basicPayDetails = _basicPayDetailRepository.GetAll()
            .Where(d => d.EmployeeBasicPay.StartDate <= today
                        && d.EmployeeBasicPay.EndDate >= today
                        && d.WageType != null);

        var wageTypes = await basicPayDetails
            .Select(d => new
            {
                d.WageType.Code,
                Name = useArabicNames
                    ? (d.WageType.NameAr ?? d.WageType.NameEn)
                    : d.WageType.NameEn,
                d.WageType.PayrollOrder
            })
            .Distinct()
            .OrderBy(w => w.PayrollOrder)
            .ToListAsync();

        var wageTypeNames = wageTypes.Select(w => w.Name).ToList();

        if (rows.Count == 0)
        {
            return wageTypeNames;
        }

        var employeeIds = rows.Select(r => r.Id).ToList();

        var amounts = await basicPayDetails
            .Where(d => employeeIds.Contains(d.EmployeeBasicPay.EmployeeId))
            .Select(d => new
            {
                EmployeeId = d.EmployeeBasicPay.EmployeeId,
                WageTypeCode = d.WageType.Code,
                d.Amount
            })
            .ToListAsync();

        var amountsByEmployee = amounts.GroupBy(a => a.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        // Legacy lines 1650-1657: the LATEST regular, in-period salary register per employee.
        var latestRegisterIds = await _salaryRegisterRepository.GetAll()
            .Where(r => employeeIds.Contains(r.EmployeeId)
                        && r.InPeriod == r.ForPeriod
                        && r.PayrollRunType == PayrollRunType.Regular)
            .GroupBy(r => r.EmployeeId)
            .Select(g => new
            {
                EmployeeId = g.Key,
                RegisterId = g.OrderByDescending(r => r.InPeriod).Select(r => r.Id).FirstOrDefault()
            })
            .ToListAsync();

        var registerIdByEmployee = latestRegisterIds.ToDictionary(x => x.EmployeeId, x => x.RegisterId);
        var registerIds = registerIdByEmployee.Values.ToList();

        var registerDetails = registerIds.Count == 0
            ? new List<RegisterDetailInfo>()
            : await _salaryRegisterDetailRepository.GetAll()
                .Where(d => registerIds.Contains(d.SalaryRegisterId)
                            && (d.WageType.Code == AbsentUnpaidWageTypeCode
                                || d.WageType.Code == LeaveProvisionWageTypeCode
                                || d.WageType.Code == IndemnityProvisionWageTypeCode))
                .Select(d => new RegisterDetailInfo
                {
                    SalaryRegisterId = d.SalaryRegisterId,
                    WageTypeCode = d.WageType.Code,
                    Number = d.Number,
                    Amount = d.Amount
                })
                .ToListAsync();

        var detailsByRegister = registerDetails
            .GroupBy(d => d.SalaryRegisterId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var row in rows)
        {
            amountsByEmployee.TryGetValue(row.Id, out var employeeAmounts);

            row.WageAmounts = new List<EmployeeCTCWageAmountDto>(wageTypes.Count);

            foreach (var wageType in wageTypes)
            {
                // Legacy line 1615: 0 when the employee's basic pay has no detail for that wage type.
                var amount = employeeAmounts?
                    .Where(a => a.WageTypeCode == wageType.Code)
                    .Sum(a => a.Amount) ?? 0;

                row.WageAmounts.Add(new EmployeeCTCWageAmountDto
                {
                    WageTypeCode = wageType.Code,
                    WageTypeName = wageType.Name,
                    Amount = amount
                });
            }

            if (registerIdByEmployee.TryGetValue(row.Id, out var registerId)
                && detailsByRegister.TryGetValue(registerId, out var details))
            {
                // Legacy lines 1662-1666: FirstOrDefault per wage type, not a sum.
                row.TotalAbsentUnpaidDays =
                    details.FirstOrDefault(d => d.WageTypeCode == AbsentUnpaidWageTypeCode)?.Number ?? 0;
                row.LeaveBalance =
                    details.FirstOrDefault(d => d.WageTypeCode == LeaveProvisionWageTypeCode)?.Number ?? 0;
                row.LeaveProvisionAmount =
                    details.FirstOrDefault(d => d.WageTypeCode == LeaveProvisionWageTypeCode)?.Amount ?? 0;
                row.IndemnityBalance =
                    details.FirstOrDefault(d => d.WageTypeCode == IndemnityProvisionWageTypeCode)?.Number ?? 0;
                row.IndemnityProvisionAmount =
                    details.FirstOrDefault(d => d.WageTypeCode == IndemnityProvisionWageTypeCode)?.Amount ?? 0;
            }
        }

        return wageTypeNames;
    }

    /// <summary>
    /// Fills OrgLevel1..OrgLevel7 on already-materialized rows. Shared by the read and the export so the
    /// two cannot drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<EmployeeCTCReportRowDto> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(UseArabicNames);

        foreach (var row in rows)
        {
            var levels = lookup.GetLevelNames(row.OrganizationUnitId);
            row.OrgLevel1 = levels[0];
            row.OrgLevel2 = levels[1];
            row.OrgLevel3 = levels[2];
            row.OrgLevel4 = levels[3];
            row.OrgLevel5 = levels[4];
            row.OrgLevel6 = levels[5];
            row.OrgLevel7 = levels[6];
        }
    }

    /// <summary>
    /// Ports legacy <c>GetParameterStr</c> (lines 486-500): Payroll Area / Employee Status / Employee Code,
    /// with no dates -- this screen has no date range.
    /// </summary>
    private string BuildCtcParameterString(GetEmployeeCTCReportInput input)
    {
        var parts = new List<string>();

        if (input.PayrollAreaId.HasValue)
        {
            parts.Add("Payroll Area : " + input.PayrollAreaId.Value);
        }

        if (input.EmployeeStatus.HasValue)
        {
            parts.Add("Employee Status : " + input.EmployeeStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    /// <summary>The exported column set, in legacy column order. The technical OID key is omitted.</summary>
    private static IReadOnlyList<GridReportColumn<EmployeeCTCReportRowDto>> GetExportColumns(
        List<string> wageTypeNames)
    {
        var columns = new List<GridReportColumn<EmployeeCTCReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("FullNameAr", r => r.EmployeeNameAr),
            new("Status", r => r.Status.ToString()),
            new("BirthDate", r => r.BirthDate),
            new("ProbationDays", r => r.ProbationDays),
            new("JoiningDate", r => r.JoiningDate),
            new("ProbationEndDate", r => r.ProbationEndDate),
            new("Nationality", r => r.NationalityName),
            new("Religion", r => r.ReligionName),
            new("Gender", r => r.Gender.ToString()),
            new("MaritalStatus", r => r.MaritalStatusName),
            new("RecruitmentType", r => r.RecruitmentTypeName),
            new("City", r => r.City),
            new("Governorate", r => r.GovernorateName),
            new("Country", r => r.CountryName),
            new("Telephone", r => r.Telephone),
            new("Mobile", r => r.Mobile),
            new("CurrentAddress", r => r.CurrentAddress),
            new("PermanentAddress", r => r.PermanentAddress),
            new("CivilID", r => r.CivilID),
            new("LoginID", r => r.LoginID),
            new("AccessID", r => r.AccessID),
            new("PassportNumber", r => r.PassportNumber),
            new("Email", r => r.Email),
            new("BirthPlace", r => r.BirthPlaceName),
            new("ResidencyType", r => r.ResidencyType?.ToString()),
            new("ContractType", r => r.ContractTypeName),
            new("ContractStartDate", r => r.ContractStartDate),
            new("ContractEndDate", r => r.ContractEndDate),
            new("AccountNum", r => r.AccountNumber),
            new("IBAN", r => r.IBAN),
            new("BankBranch", r => r.BankBranch),
            new("Sponsor", r => r.SponsorName),
            new("Manager", r => r.ManagerName),
            new("JobTitle", r => r.JobTitle),
            new("PayrollArea", r => r.PayrollAreaName),
            new("EmployeeGroup", r => r.EmployeeGroupName),
            new("EmployeeSubGroup", r => r.EmployeeSubGroupName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("WorkLocationCode", r => r.WorkLocationCode),
            new("WorkLocation", r => r.WorkLocationName),
            new("CostCenterCode", r => r.CostCenterCode),
            new("CostCenter", r => r.CostCenterName),
            new("CostCenterGroup", r => r.CostCenterGroupName),
            new("PostingGroup", r => r.PostingGroupName),
            new("BudgetingGroup", r => r.BudgetingGroupName),
            new("EffectiveDate", r => r.EffectiveDate)
        };

        for (var i = 0; i < wageTypeNames.Count; i++)
        {
            var index = i;
            columns.Add(GridReportColumn<EmployeeCTCReportRowDto>.FromLiteralHeader(
                wageTypeNames[i],
                r => index < r.WageAmounts.Count ? r.WageAmounts[index].Amount : 0));
        }

        columns.AddRange(new List<GridReportColumn<EmployeeCTCReportRowDto>>
        {
            new("TotalSalary", r => r.TotalSalary),
            new("TotalAbsentUnpaidDays", r => r.TotalAbsentUnpaidDays),
            new("LeaveBalance", r => r.LeaveBalance),
            new("LeaveProvisionAmount", r => r.LeaveProvisionAmount),
            new("IndemnityBalance", r => r.IndemnityBalance),
            new("IndemnityProvisionAmount", r => r.IndemnityProvisionAmount),
            new("SeparationType", r => r.SeparationType?.ToString()),
            new("SubmissionDate", r => r.SubmissionDate),
            new("LastDayInService", r => r.LastDayInService),
            new("RevokeDate", r => r.RevokeDate)
        });

        return columns;
    }

    /// <summary>Named type so the salary-register detail projection stays a simple, translatable select.</summary>
    private class RegisterDetailInfo
    {
        public int SalaryRegisterId { get; set; }

        public string WageTypeCode { get; set; }

        public double Number { get; set; }

        public double Amount { get; set; }
    }
}
