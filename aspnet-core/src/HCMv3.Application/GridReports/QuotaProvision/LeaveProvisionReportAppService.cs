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
using HCMv3.GridReports.QuotaProvision.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.QuotaProvision;

/// <summary>
/// Leave Provision analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptLeaveProvision.ascx(.cs)</c>,
/// <c>Table1()</c> (lines 928-1327; see <see cref="Dto.GetLeaveProvisionReportInput"/> remarks for what is
/// NOT ported).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_LeaveProvision)]
public class LeaveProvisionReportAppService : GridReportAppServiceBase, ILeaveProvisionReportAppService
{
    private static readonly string[] WageCodes =
    {
        "1000", "4021", "4010", "4000", "5000", "4030", "4031", "4022", "3001", "3004", "4001", "4024", "1003", "7003"
    };

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<TimeTransfer> _timeTransferRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly QuotaProvisionOrgContextResolver _orgContextResolver;

    public LeaveProvisionReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<TimeTransfer> timeTransferRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        QuotaProvisionOrgContextResolver orgContextResolver)
    {
        _employeeRepository = employeeRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _timeTransferRepository = timeTransferRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgContextResolver = orgContextResolver;
    }

    protected override string LegacyScreenName => "uscRptLeaveProvision";

    protected override string ScreenCaption => "Leave Provision";

    [HttpPost]
    public async Task<PagedResultDto<LeaveProvisionReportRowDto>> GetLeaveProvisionReport(GetLeaveProvisionReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var query = rows.AsQueryable();
        var totalCount = query.Count();

        var pagedRows = query.OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<LeaveProvisionReportRowDto>(totalCount, pagedRows);
    }

    [HttpPost]
    public async Task<FileDto> GetLeaveProvisionReportToExcel(GetLeaveProvisionReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var orderedRows = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync("LeaveProvision.xlsx", orderedRows, GetExportColumns(), BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c>'s <c>monthYearList</c> loop.
    /// </summary>
    private async Task<List<LeaveProvisionReportRowDto>> BuildRowsAsync(GetLeaveProvisionReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(fromDate);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var forfeitMonthly = timeSettings != null && timeSettings.ForfeitRule == ForfeitRule.Monthly;
        var enableTimeRegisterApproval = timeSettings != null && timeSettings.EnableTimeRegisterApproval;
        var earliestEvaluationDate = timeSettings?.EarliestEvaluationDate ?? DateTime.MinValue;

        var periods = GetMonthEndPeriods(fromDate, toDate);

        var employeeQuery = _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        var employees = await employeeQuery
            .Select(e => new { e.Id, e.Code, EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn, e.JoiningDate })
            .OrderBy(e => e.Code)
            .ToListAsync();
        var employeeIds = employees.Select(e => e.Id).ToList();

        var rows = new List<LeaveProvisionReportRowDto>();

        foreach (var period in periods)
        {
            var wageRowsQuery = _salaryRegisterDetailRepository.GetAll()
                .Where(d => employeeIds.Contains(d.SalaryRegister.EmployeeId)
                            && d.SalaryRegister.ForPeriod == period && d.SalaryRegister.InPeriod == period
                            && d.SalaryRegister.PayrollRunType == PayrollRunType.Regular
                            && WageCodes.Contains(d.WageType.Code));

            if (!string.IsNullOrWhiteSpace(input.PayrollAreaCode))
            {
                var payrollAreaEmployeeIds = _employeeOrgAssignmentRepository.GetAll()
                    .Where(o => o.StartDate <= period && o.EndDate >= period
                                && o.PayrollArea != null && o.PayrollArea.Code == input.PayrollAreaCode)
                    .Select(o => o.EmployeeId);
                wageRowsQuery = wageRowsQuery.Where(d => payrollAreaEmployeeIds.Contains(d.SalaryRegister.EmployeeId));
            }

            var wageRows = await wageRowsQuery
                .Select(d => new { d.SalaryRegister.EmployeeId, d.WageType.Code, d.Amount, d.Number, d.Rate, FixedDays = d.WageType.FixedDays })
                .ToListAsync();

            if (wageRows.Count == 0)
            {
                continue;
            }

            var periodEmployeeIds = wageRows.Select(r => r.EmployeeId).Distinct().ToList();

            var basicPayDetails = await _employeeBasicPayDetailRepository.GetAll()
                .Where(d => periodEmployeeIds.Contains(d.EmployeeBasicPay.EmployeeId)
                            && d.EmployeeBasicPay.StartDate <= period && d.EmployeeBasicPay.EndDate >= period)
                .Select(d => new { EmployeeId = d.EmployeeBasicPay.EmployeeId, d.WageType.Code, d.Amount, TotalAmount = d.EmployeeBasicPay.Amount, CurrencyName = d.Currency.CurrencyShortFormEn })
                .ToListAsync();

            var openingBalances = await _timeTransferRepository.GetAll()
                .Where(t => periodEmployeeIds.Contains(t.EmployeeId) && t.TransferType.Code == "1")
                .GroupBy(t => t.EmployeeId)
                .Select(g => new { EmployeeId = g.Key, Total = g.Sum(t => t.TransferCount) })
                .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

            var absentDaysByEmployee = enableTimeRegisterApproval
                ? await _timeRegisterRepository.GetAll()
                    .Where(t => periodEmployeeIds.Contains(t.EmployeeId) && t.UnpaidLeaveDay == 0)
                    .GroupBy(t => t.EmployeeId)
                    .Select(g => new { EmployeeId = g.Key, Total = g.Sum(t => t.ApprovedAbsentDay) })
                    .ToDictionaryAsync(g => g.EmployeeId, g => g.Total)
                : await _timeRegisterRepository.GetAll()
                    .Where(t => periodEmployeeIds.Contains(t.EmployeeId) && t.UnpaidLeaveDay == 0)
                    .GroupBy(t => t.EmployeeId)
                    .Select(g => new { EmployeeId = g.Key, Total = g.Sum(t => t.AbsentDay) })
                    .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

            var orgContexts = await _orgContextResolver.BuildAsync(periodEmployeeIds, period, useArabicNames);

            foreach (var employeeId in periodEmployeeIds)
            {
                var employee = employees.FirstOrDefault(e => e.Id == employeeId);
                if (employee == null)
                {
                    continue;
                }

                var orgContext = orgContexts.Get(employeeId);
                var employeeWageRows = wageRows.Where(r => r.EmployeeId == employeeId).ToList();
                double AmountFor(string code) => employeeWageRows.FirstOrDefault(r => r.Code == code)?.Amount ?? 0;
                double NumberFor(string code) => employeeWageRows.FirstOrDefault(r => r.Code == code)?.Number ?? 0;
                double RateFor(string code) => employeeWageRows.FirstOrDefault(r => r.Code == code)?.Rate ?? 0;

                var basicSalary = basicPayDetails.FirstOrDefault(b => b.EmployeeId == employeeId && b.Code == "1000")?.Amount ?? 0;
                var allowance = basicPayDetails.Where(b => b.EmployeeId == employeeId && b.Code != "1000").Sum(b => b.Amount);
                var totalSalaryRow = basicPayDetails.FirstOrDefault(b => b.EmployeeId == employeeId);

                openingBalances.TryGetValue(employeeId, out var systemOpeningBalance);
                absentDaysByEmployee.TryGetValue(employeeId, out var absentDays);

                var adjustmentRow = employeeWageRows.FirstOrDefault(r => r.Code == "4030");
                var adjustmentAmount = 0d;
                if (adjustmentRow != null)
                {
                    var fixedDays = adjustmentRow.FixedDays != 0 ? adjustmentRow.FixedDays : 1;
                    adjustmentAmount = RateFor("3004") / fixedDays * adjustmentRow.Number;
                }

                var currentPaidLeave = NumberFor("4000");
                var currentAdjustment = NumberFor("4030");

                rows.Add(new LeaveProvisionReportRowDto
                {
                    Id = employeeId,
                    EmployeeCode = employee.Code,
                    EmployeeName = employee.EmployeeName,
                    JoiningDate = employee.JoiningDate,
                    LeaveDaysCalculationPeriod = period.ToString("MMM yyyy"),
                    PayrollAreaName = orgContext.PayrollAreaName,
                    CostCenterName = orgContext.CostCenterName,
                    JobTitleName = orgContext.JobTitleName,
                    BasicSalary = basicSalary,
                    Allowance = allowance,
                    TotalSalary = totalSalaryRow?.TotalAmount ?? 0,
                    CurrencyName = totalSalaryRow?.CurrencyName,
                    SalaryInProvision = RateFor("3004"),
                    OrgLevel1 = orgContext.OrgLevel1,
                    OrgLevel2 = orgContext.OrgLevel2,
                    OrgLevel3 = orgContext.OrgLevel3,
                    OrgLevel4 = orgContext.OrgLevel4,
                    OrgLevel5 = orgContext.OrgLevel5,
                    OrgLevel6 = orgContext.OrgLevel6,
                    OrgLevel7 = orgContext.OrgLevel7,
                    SystemOpeningBalanceDays = systemOpeningBalance,
                    OpeningBalanceDays = NumberFor("4021"),
                    OpeningBalanceAmount = (RateFor("3004") != 0 ? RateFor("3004") / 26 : 0) * NumberFor("4021"),
                    CurrentMonthAccrual = NumberFor("4010"),
                    CurrentMonthAccrualAmount = AmountFor("3001"),
                    CurrentMonthPaidLeave = currentPaidLeave,
                    CurrentMonthPaidLeaveAmount = AmountFor("1003"),
                    CurrentMonthPaidLeaveArrear = NumberFor("5000"),
                    CurrentMonthPaidLeaveArrearAmount = AmountFor("7003"),
                    CurrentMonthAdjustment = currentAdjustment,
                    CurrentMonthAdjustmentAmount = adjustmentAmount,
                    CurrentMonthForfeit = forfeitMonthly ? NumberFor("4031") : 0,
                    LeaveUsage = currentPaidLeave + currentAdjustment,
                    CurrentMonthUnpaidLeave = NumberFor("4001"),
                    TotalUnpaidLeave = NumberFor("4024"),
                    AbsentDays = absentDays,
                    ClosingBalance = NumberFor("4022"),
                    ClosingBalanceAmount = AmountFor("3004")
                });
            }
        }

        return rows;
    }

    /// <summary>Legacy <c>SysService.GetMonthYearList</c>: the last day of every month from FromDate to ToDate inclusive.</summary>
    private static List<DateTime> GetMonthEndPeriods(DateTime fromDate, DateTime toDate)
    {
        var periods = new List<DateTime>();
        var cursor = new DateTime(fromDate.Year, fromDate.Month, 1);
        var end = new DateTime(toDate.Year, toDate.Month, 1);
        while (cursor <= end)
        {
            periods.Add(new DateTime(cursor.Year, cursor.Month, DateTime.DaysInMonth(cursor.Year, cursor.Month)));
            cursor = cursor.AddMonths(1);
        }

        return periods;
    }

    private static IReadOnlyList<GridReportColumn<LeaveProvisionReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<LeaveProvisionReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("EmployeeName", r => r.EmployeeName),
            new("JoiningDate", r => r.JoiningDate),
            new("LeaveDaysCalculationPeriod", r => r.LeaveDaysCalculationPeriod),
            new("PayrollArea", r => r.PayrollAreaName),
            new("CostCenter", r => r.CostCenterName),
            new("Jobtitle", r => r.JobTitleName),
            new("BasicSalary", r => r.BasicSalary),
            new("Allowance", r => r.Allowance),
            new("TotalSalary", r => r.TotalSalary),
            new("Currency", r => r.CurrencyName),
            new("SalaryInProvision", r => r.SalaryInProvision),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("SystemOpeningBalanceDays", r => r.SystemOpeningBalanceDays),
            new("OpeningBalanceDays", r => r.OpeningBalanceDays),
            new("OpeningBalanceAmount", r => r.OpeningBalanceAmount),
            new("CurrentMonthAccrual", r => r.CurrentMonthAccrual),
            new("CurrentMonthAccrualAmount", r => r.CurrentMonthAccrualAmount),
            new("CurrentMonthPaidLeave", r => r.CurrentMonthPaidLeave),
            new("CurrentMonthPaidLeaveAmount", r => r.CurrentMonthPaidLeaveAmount),
            new("CurrentMonthPaidLeaveArrear", r => r.CurrentMonthPaidLeaveArrear),
            new("CurrentMonthPaidLeaveArrearAmount", r => r.CurrentMonthPaidLeaveArrearAmount),
            new("CurrentMonthAdjustment", r => r.CurrentMonthAdjustment),
            new("CurrentMonthAdjustmentAmount", r => r.CurrentMonthAdjustmentAmount),
            new("CurrentMonthForfeit", r => r.CurrentMonthForfeit),
            new("LeaveUsage", r => r.LeaveUsage),
            new("CurrentMonthUnpaidLeave", r => r.CurrentMonthUnpaidLeave),
            new("TotalUnpaidLeave", r => r.TotalUnpaidLeave),
            new("AbsentDays", r => r.AbsentDays),
            new("ClosingBalance", r => r.ClosingBalance),
            new("ClosingBalanceAmount", r => r.ClosingBalanceAmount)
        };
    }
}
