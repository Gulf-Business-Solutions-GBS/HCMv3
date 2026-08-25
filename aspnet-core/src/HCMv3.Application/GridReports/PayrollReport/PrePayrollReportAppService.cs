using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Pre-Payroll Report grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPrePayrollReport.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 560-748). A per-employee payroll-readiness checklist, all columns
/// booleans/dates rather than amounts.
///
/// LEGACY QUIRK REPRODUCED: the Run Type and Payroll Area combos are logged into the parameter string but
/// never applied to the query -- see <see cref="GetPrePayrollReportInput"/> remarks.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-down session stack, the
/// ColumnChooser JSON persistence and the callback-panel state machine.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PrePayrollReport)]
public class PrePayrollReportAppService : GridReportAppServiceBase, IPrePayrollReportAppService
{
    /// <summary>Legacy line 638: the Basic Pay checklist item only looks at wage type 1000.</summary>
    private const string BasicPayWageTypeCode = "1000";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _basicPayDetailRepository;
    private readonly IRepository<EmployeeContract> _contractRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeTimePolicy> _timePolicyRepository;
    private readonly IRepository<EmployeeDutyPeriod> _dutyPeriodRepository;
    private readonly IRepository<PayrollManagement.EmployeePayrollStatus> _payrollStatusRepository;
    private readonly IRepository<EmployeeLoan> _loanRepository;
    private readonly IRepository<EmployeeLoanRecovery> _loanRecoveryRepository;
    private readonly IRepository<EmployeeLoanExemption> _loanExemptionRepository;

    public PrePayrollReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeBasicPayDetail> basicPayDetailRepository,
        IRepository<EmployeeContract> contractRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeTimePolicy> timePolicyRepository,
        IRepository<EmployeeDutyPeriod> dutyPeriodRepository,
        IRepository<PayrollManagement.EmployeePayrollStatus> payrollStatusRepository,
        IRepository<EmployeeLoan> loanRepository,
        IRepository<EmployeeLoanRecovery> loanRecoveryRepository,
        IRepository<EmployeeLoanExemption> loanExemptionRepository)
    {
        _employeeRepository = employeeRepository;
        _basicPayDetailRepository = basicPayDetailRepository;
        _contractRepository = contractRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _timePolicyRepository = timePolicyRepository;
        _dutyPeriodRepository = dutyPeriodRepository;
        _payrollStatusRepository = payrollStatusRepository;
        _loanRepository = loanRepository;
        _loanRecoveryRepository = loanRecoveryRepository;
        _loanExemptionRepository = loanExemptionRepository;
    }

    protected override string LegacyScreenName => "uscRptPrePayrollReport";

    protected override string ScreenCaption => "Pre Payroll Report";

    [HttpPost]
    public async Task<PagedResultDto<PrePayrollReportRowDto>> GetPrePayrollReport(GetPrePayrollReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var totalCount = rows.Count;

        var paged = rows.AsQueryable()
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<PrePayrollReportRowDto>(totalCount, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetPrePayrollReportToExcel(GetPrePayrollReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync("PrePayrollReport.xlsx", ordered, GetExportColumns(), BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection. Ports legacy <c>Table1()</c>.
    /// </summary>
    private async Task<List<PrePayrollReportRowDto>> BuildRowsAsync(GetPrePayrollReportInput input)
    {
        var endDate = ResolveEndDate(input);
        var startOfMonth = new DateTime(endDate.Year, endDate.Month, 1);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        // Legacy line 620: only Active employees, plus RLS.
        var employees = await _employeeRepository.GetAll()
            .Where(e => e.Status == PersonnelAdministration.EmployeeStatus.Active)
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id))
            .Select(e => new
            {
                e.Id,
                e.Code,
                Name = useArabicNames ? e.FullNameAr : e.FullNameEn,
                HasAccessId = !string.IsNullOrEmpty(e.AccessID)
            })
            .OrderBy(e => e.Code)
            .ToListAsync();

        if (employees.Count == 0)
        {
            return new List<PrePayrollReportRowDto>();
        }

        var employeeIds = employees.Select(e => e.Id).ToList();

        var hasBasicPay = (await _basicPayDetailRepository.GetAll()
                .Where(d => d.EmployeeBasicPay.StartDate <= endDate
                            && d.EmployeeBasicPay.EndDate >= endDate
                            && employeeIds.Contains(d.EmployeeBasicPay.EmployeeId)
                            && d.WageType.Code == BasicPayWageTypeCode)
                .Select(d => d.EmployeeBasicPay.EmployeeId)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        var hasContract = (await _contractRepository.GetAll()
                .Where(c => c.StartDate <= endDate && c.EndDate >= endDate && employeeIds.Contains(c.EmployeeId))
                .Select(c => c.EmployeeId)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        var orgAssignments = await _orgAssignmentRepository.GetAll()
            .Where(o => o.StartDate <= endDate && o.EndDate >= endDate && employeeIds.Contains(o.EmployeeId))
            .Select(o => new { o.EmployeeId, o.StartDate, o.Id, o.EmployeeSubGroupId })
            .ToListAsync();

        var orgAssignmentByEmployee = orgAssignments
            .GroupBy(o => o.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(o => o.StartDate).ThenBy(o => o.Id).First());

        var hasTimePolicy = (await _timePolicyRepository.GetAll()
                .Where(t => t.StartDate <= endDate && t.EndDate >= endDate && employeeIds.Contains(t.EmployeeId))
                .Select(t => t.EmployeeId)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        var hasDutyPeriod = (await _dutyPeriodRepository.GetAll()
                .Where(d => d.StartDate <= endDate && d.EndDate >= endDate && employeeIds.Contains(d.EmployeeId))
                .Select(d => d.EmployeeId)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        // Legacy line 651-652: EarliestMasterDataChange has no date scoping -- FirstOrDefault of ALL rows.
        var payrollStatusByEmployee = await _payrollStatusRepository.GetAll()
            .Where(p => employeeIds.Contains(p.EmployeeId))
            .Select(p => new { p.EmployeeId, p.EarliestMasterDataChange })
            .ToDictionaryAsync(p => p.EmployeeId, p => (DateTime?)p.EarliestMasterDataChange);

        var loanRecoveryByEmployee = await BuildLoanRecoveryLookupAsync(employeeIds, endDate);

        return employees.Select(e => new PrePayrollReportRowDto
        {
            Id = e.Id,
            EmployeeCode = e.Code,
            EmployeeName = e.Name,
            HasAccessId = e.HasAccessId,
            HasBasicPay = hasBasicPay.Contains(e.Id),
            HasContract = hasContract.Contains(e.Id),
            HasOrgAssignment = orgAssignmentByEmployee.ContainsKey(e.Id),
            HasSalaryProfile = orgAssignmentByEmployee.TryGetValue(e.Id, out var org) && org.EmployeeSubGroupId != null,
            HasTimePolicy = hasTimePolicy.Contains(e.Id),
            HasDutyPeriod = hasDutyPeriod.Contains(e.Id),
            EarliestMasterDataChange = payrollStatusByEmployee.TryGetValue(e.Id, out var earliest) ? earliest : null,
            IsBackDatedAmount = payrollStatusByEmployee.TryGetValue(e.Id, out var em) && em.HasValue && em.Value < startOfMonth,
            HasLoanRecovery = loanRecoveryByEmployee.Contains(e.Id)
        }).ToList();
    }

    /// <summary>
    /// Legacy lines 654/721-735: an employee counts as "Loan Recovery" when a loan current as of EndDate
    /// has either a positive balance due OR a recovery already posted THIS calendar month, AND no
    /// exemption was recorded this same month.
    /// </summary>
    private async Task<HashSet<int>> BuildLoanRecoveryLookupAsync(List<int> employeeIds, DateTime endDate)
    {
        var loans = await _loanRepository.GetAll()
            .Where(l => l.StartDate <= endDate && l.EndDate >= endDate && employeeIds.Contains(l.EmployeeId))
            .Select(l => new { l.Id, l.EmployeeId, l.BalanceDueAmount })
            .ToListAsync();

        if (loans.Count == 0)
        {
            return new HashSet<int>();
        }

        var loanIds = loans.Select(l => l.Id).ToList();

        var recoveredLoanIds = (await _loanRecoveryRepository.GetAll()
                .Where(r => loanIds.Contains(r.EmployeeLoanId)
                            && r.RecoveryDate.Month == endDate.Month
                            && r.RecoveryDate.Year == endDate.Year)
                .Select(r => r.EmployeeLoanId)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        var exemptLoanIds = (await _loanExemptionRepository.GetAll()
                .Where(x => loanIds.Contains(x.EmployeeLoanId)
                            && x.ExemptionDate.Month == endDate.Month
                            && x.ExemptionDate.Year == endDate.Year)
                .Select(x => x.EmployeeLoanId)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        return loans
            .Where(l => (l.BalanceDueAmount > 0 || recoveredLoanIds.Contains(l.Id)) && !exemptLoanIds.Contains(l.Id))
            .Select(l => l.EmployeeId)
            .ToHashSet();
    }

    /// <summary>Legacy <c>GetTable()</c> lines 488-492: blank month/year default to "now".</summary>
    private DateTime ResolveEndDate(GetPrePayrollReportInput input)
    {
        var month = input.Month ?? Today.Month;
        var year = input.Year ?? Today.Year;

        try
        {
            return new DateTime(year, month, DateTime.DaysInMonth(year, month)).Date;
        }
        catch (ArgumentOutOfRangeException)
        {
            return Today;
        }
    }

    /// <summary>Ports legacy <c>GetParameterStr</c> (lines 539-556).</summary>
    private string BuildParameterString(GetPrePayrollReportInput input)
    {
        var parts = new List<string>();

        if (input.PayrollAreaId.HasValue)
        {
            parts.Add("Payroll Area : " + input.PayrollAreaId.Value);
        }

        if (input.PayrollRunType.HasValue)
        {
            parts.Add("Run Type : " + input.PayrollRunType.Value);
        }

        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));

        return string.Join(" | ", parts);
    }

    private static IReadOnlyList<GridReportColumn<PrePayrollReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<PrePayrollReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("AccessId", r => r.HasAccessId),
            new("BasicPay", r => r.HasBasicPay),
            new("Contract", r => r.HasContract),
            new("OrgAssignment", r => r.HasOrgAssignment),
            new("SalaryProfile", r => r.HasSalaryProfile),
            new("TimePolicy", r => r.HasTimePolicy),
            new("DutyPeriod", r => r.HasDutyPeriod),
            new("EarliestMasterDataChange", r => r.EarliestMasterDataChange),
            new("BackDatedAmount", r => r.IsBackDatedAmount),
            new("LoanRecovery", r => r.HasLoanRecovery)
        };
    }
}
