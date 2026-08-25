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
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.QuotaProvision;

/// <summary>
/// Indemnity Provision analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptIndemnityProvision.ascx(.cs)</c>,
/// <c>Table1()</c> (lines 873-1234, the ACTIVE-employee path; the dead <c>Table1_Opt</c>/<c>Table1_Old</c>
/// and the separate <c>Table_InActive</c> summary path are NOT ported -- see
/// <see cref="Dto.GetIndemnityProvisionReportInput"/> remarks).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_IndemnityProvision)]
public class IndemnityProvisionReportAppService : GridReportAppServiceBase, IIndemnityProvisionReportAppService
{
    private static readonly string[] WageCodes = { "1000", "3000", "3003", "4001", "4024", "4029" };

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<EmployeeLoan> _employeeLoanRepository;
    private readonly IRepository<TimeTransfer> _timeTransferRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeeFnFPayment> _employeeFnFPaymentRepository;
    private readonly IRepository<EmployeeSubGroupGratuity> _employeeSubGroupGratuityRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly QuotaProvisionOrgContextResolver _orgContextResolver;

    public IndemnityProvisionReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<EmployeeLoan> employeeLoanRepository,
        IRepository<TimeTransfer> timeTransferRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<EmployeeFnFPayment> employeeFnFPaymentRepository,
        IRepository<EmployeeSubGroupGratuity> employeeSubGroupGratuityRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        QuotaProvisionOrgContextResolver orgContextResolver)
    {
        _employeeRepository = employeeRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _employeeLoanRepository = employeeLoanRepository;
        _timeTransferRepository = timeTransferRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeeFnFPaymentRepository = employeeFnFPaymentRepository;
        _employeeSubGroupGratuityRepository = employeeSubGroupGratuityRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgContextResolver = orgContextResolver;
    }

    protected override string LegacyScreenName => "uscRptIndemnityProvision";

    protected override string ScreenCaption => "Indemnity Provision";

    [HttpPost]
    public async Task<PagedResultDto<IndemnityProvisionReportRowDto>> GetIndemnityProvisionReport(GetIndemnityProvisionReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var query = rows.AsQueryable();
        var totalCount = query.Count();

        var pagedRows = query.OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<IndemnityProvisionReportRowDto>(totalCount, pagedRows);
    }

    [HttpPost]
    public async Task<FileDto> GetIndemnityProvisionReportToExcel(GetIndemnityProvisionReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var orderedRows = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync("IndemnityProvision.xlsx", orderedRows, GetExportColumns(), BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c>'s <c>monthYearList</c> loop (lines 979-1227): one row per Active
    /// employee per regular-run month-end between FromDate and ToDate.
    /// </summary>
    private async Task<List<IndemnityProvisionReportRowDto>> BuildRowsAsync(GetIndemnityProvisionReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(fromDate);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        var periods = GetMonthEndPeriods(fromDate, toDate);

        var employeeQuery = _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        var employees = await employeeQuery
            .Select(e => new { e.Id, e.Code, EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn })
            .OrderBy(e => e.Code)
            .ToListAsync();
        var employeeIds = employees.Select(e => e.Id).ToList();

        var latestSeparations = (await _employeeSeparationRepository.GetAll()
                .Where(s => employeeIds.Contains(s.EmployeeId) && s.RevokeDate == null)
                .Select(s => new { s.EmployeeId, s.SubmissionDate, s.LastDayInService })
                .ToListAsync())
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.LastDayInService).First());

        var latestFnfPayments = (await _employeeFnFPaymentRepository.GetAll()
                .Where(f => employeeIds.Contains(f.EmployeeId))
                .Select(f => new { f.EmployeeId, f.CompensationDate })
                .ToListAsync())
            .GroupBy(f => f.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.CompensationDate).First());

        var rows = new List<IndemnityProvisionReportRowDto>();

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
                .Select(d => new { d.SalaryRegister.EmployeeId, d.WageType.Code, d.Amount, d.Number, d.Rate })
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

            var timeTransferOpening = await _timeTransferRepository.GetAll()
                .Where(t => periodEmployeeIds.Contains(t.EmployeeId) && t.TransferType.Code == "5" && t.TransferDate < period)
                .GroupBy(t => t.EmployeeId)
                .Select(g => new { EmployeeId = g.Key, Total = g.Sum(t => t.TransferCount) })
                .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

            var totalAbsentDays = await _timeRegisterRepository.GetAll()
                .Where(t => periodEmployeeIds.Contains(t.EmployeeId))
                .GroupBy(t => t.EmployeeId)
                .Select(g => new { EmployeeId = g.Key, Total = g.Sum(t => t.AbsentDay) })
                .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

            var loans = await _employeeLoanRepository.GetAll()
                .Where(l => periodEmployeeIds.Contains(l.EmployeeId) && l.LoanType.Code == "12")
                .GroupBy(l => l.EmployeeId)
                .Select(g => new { EmployeeId = g.Key, Total = g.Sum(l => l.DueAmount) })
                .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

            var orgContexts = await _orgContextResolver.BuildAsync(periodEmployeeIds, period, useArabicNames);

            var subGroupIds = periodEmployeeIds.Select(id => orgContexts.Get(id).EmployeeSubGroupId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
            var gratuitySlabs = await _employeeSubGroupGratuityRepository.GetAll()
                .Where(g => subGroupIds.Contains(g.EmployeeSubGroupId) && g.StartDate <= period && g.EndDate >= period)
                .ToListAsync();

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

                var closingGratuity = NumberFor("3003");
                var indemnityPerYear = 0d;
                if (orgContext.EmployeeSubGroupId.HasValue)
                {
                    foreach (var slab in gratuitySlabs.Where(s => s.EmployeeSubGroupId == orgContext.EmployeeSubGroupId.Value))
                    {
                        if (slab.ChangeYear * slab.GratuityDays > closingGratuity)
                        {
                            indemnityPerYear = slab.GratuityDays;
                            break;
                        }
                    }
                }

                timeTransferOpening.TryGetValue(employeeId, out var openingAbsentUnpaid);
                totalAbsentDays.TryGetValue(employeeId, out var totalAbsent);
                loans.TryGetValue(employeeId, out var paidAmount);

                // Legacy lines 1215-1222: notice-period check wins, then pending/done from the FnF record.
                string fnfStatus;
                latestSeparations.TryGetValue(employeeId, out var separation);
                latestFnfPayments.TryGetValue(employeeId, out var fnfPayment);
                if (separation != null && separation.SubmissionDate <= Today && separation.LastDayInService >= Today)
                {
                    fnfStatus = "In Notice Period";
                }
                else if (fnfPayment == null || fnfPayment.CompensationDate > Today)
                {
                    fnfStatus = "Fnf Pending";
                }
                else
                {
                    fnfStatus = "Fnf Done";
                }

                var closingGratuityAmount = AmountFor("3003");

                rows.Add(new IndemnityProvisionReportRowDto
                {
                    Id = employeeId,
                    EmployeeCode = employee.Code,
                    EmployeeName = employee.EmployeeName,
                    OrgLevel1 = orgContext.OrgLevel1,
                    OrgLevel2 = orgContext.OrgLevel2,
                    OrgLevel3 = orgContext.OrgLevel3,
                    OrgLevel4 = orgContext.OrgLevel4,
                    OrgLevel5 = orgContext.OrgLevel5,
                    OrgLevel6 = orgContext.OrgLevel6,
                    OrgLevel7 = orgContext.OrgLevel7,
                    PayrollAreaName = orgContext.PayrollAreaName,
                    CostCenterName = orgContext.CostCenterName,
                    JobTitleName = orgContext.JobTitleName,
                    BasicSalary = basicSalary,
                    Allowance = allowance,
                    TotalSalary = totalSalaryRow?.TotalAmount ?? 0,
                    CurrencyName = totalSalaryRow?.CurrencyName,
                    SalaryInProvision = RateFor("3003"),
                    IndemnityPerYear = indemnityPerYear,
                    CalculationPeriod = period.ToString("MMM yyyy"),
                    OpeningBalance = NumberFor("3003") - NumberFor("3000"),
                    OpeningBalanceAmount = AmountFor("3003") - AmountFor("3000"),
                    AbsentUnpaidOpeningBalance = openingAbsentUnpaid,
                    CurrentMonthAccrual = NumberFor("3000"),
                    CurrentAccrualAmount = AmountFor("3000"),
                    CurrentMonthUnpaidLeave = NumberFor("4001"),
                    TotalUnpaidLeave = NumberFor("4024"),
                    AbsentDays = NumberFor("4029"),
                    TotalAbsentDays = totalAbsent,
                    ClosingBalance = closingGratuity,
                    ClosingBalanceAmount = closingGratuityAmount,
                    PaidAmount = paidAmount,
                    BalanceAmount = closingGratuityAmount - paidAmount,
                    FnfStatus = fnfStatus
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

    private static IReadOnlyList<GridReportColumn<IndemnityProvisionReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<IndemnityProvisionReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("EmployeeName", r => r.EmployeeName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("PayrollArea", r => r.PayrollAreaName),
            new("CostCenter", r => r.CostCenterName),
            new("JobTitle", r => r.JobTitleName),
            new("BasicSalary", r => r.BasicSalary),
            new("Allowance", r => r.Allowance),
            new("TotalSalary", r => r.TotalSalary),
            new("Currency", r => r.CurrencyName),
            new("SalaryInProvision", r => r.SalaryInProvision),
            new("IndemnityPerYear", r => r.IndemnityPerYear),
            new("IndemnityCalculationPeriod", r => r.CalculationPeriod),
            new("OpeningBalance", r => r.OpeningBalance),
            new("OpeningBalanceAmount", r => r.OpeningBalanceAmount),
            new("AbsentUnpaidOpeningBalance", r => r.AbsentUnpaidOpeningBalance),
            new("CurrentMonthAccrual", r => r.CurrentMonthAccrual),
            new("CurrentAccrualAmount", r => r.CurrentAccrualAmount),
            new("CurrentMonthUnpaidLeave", r => r.CurrentMonthUnpaidLeave),
            new("TotalUnpaidLeave", r => r.TotalUnpaidLeave),
            new("AbsentDays", r => r.AbsentDays),
            new("TotalAbsentDays", r => r.TotalAbsentDays),
            new("ClosingBalance", r => r.ClosingBalance),
            new("ClosingBalanceAmount", r => r.ClosingBalanceAmount),
            new("PaidAmount", r => r.PaidAmount),
            new("BalanceAmount", r => r.BalanceAmount),
            new("FnfStatus", r => r.FnfStatus)
        };
    }
}
