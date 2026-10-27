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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.QuotaProvision;

/// <summary>
/// Provision Change analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptProvisionChange.ascx(.cs)</c>,
/// <c>Table1(IObjectSpace, string, string, DateTime, string)</c> (lines 470-871; the dead
/// <c>Table1_Old</c> overload is not ported). Legacy's call site
/// (<c>table = Table1(object_Space, "", "", default(DateTime), "")</c>) always passes the four
/// "JV drill-in" parameters empty/default, which routes every request through the "read filters from the
/// report's own Session values" branch (lines 588-620) -- so only that branch is ported.
///
/// For every employee with a basic-pay change since the earliest evaluation date, this walks each such
/// change's month and diffs that month's Annual Leave / Indemnity provision closing amounts against the
/// PREVIOUS month's, reproducing legacy's re-rating logic exactly (lines 809-853).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_ProvisionChange)]
public class ProvisionChangeReportAppService : GridReportAppServiceBase, IProvisionChangeReportAppService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<SalaryRegisterDaysDetail> _salaryRegisterDaysDetailRepository;
    private readonly IRepository<EmployeeFnFPayment> _employeeFnFPaymentRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly QuotaProvisionOrgContextResolver _orgContextResolver;

    public ProvisionChangeReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<SalaryRegisterDaysDetail> salaryRegisterDaysDetailRepository,
        IRepository<EmployeeFnFPayment> employeeFnFPaymentRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        QuotaProvisionOrgContextResolver orgContextResolver)
    {
        _employeeRepository = employeeRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _salaryRegisterDaysDetailRepository = salaryRegisterDaysDetailRepository;
        _employeeFnFPaymentRepository = employeeFnFPaymentRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgContextResolver = orgContextResolver;
    }

    protected override string LegacyScreenName => "uscRptProvisionChange";

    protected override string ScreenCaption => "Provision Change";

    [HttpPost]
    public async Task<PagedResultDto<ProvisionChangeReportRowDto>> GetProvisionChangeReport(GetProvisionChangeReportInput input)
    {
        var rows = await BuildRowsAsync(input);
        var query = rows.AsQueryable();
        var totalCount = query.Count();

        var pagedRows = query.OrderBy(input.Sorting ?? "EmployeeCode asc").PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<ProvisionChangeReportRowDto>(totalCount, pagedRows);
    }

    [HttpPost]
    public async Task<FileDto> GetProvisionChangeReportToExcel(GetProvisionChangeReportInput input)
    {
        var rows = await BuildRowsAsync(input);
        var orderedRows = rows.AsQueryable().OrderBy(input.Sorting ?? "EmployeeCode asc").ToList();

        return await ExportToFileAsync("ProvisionChange.xlsx", orderedRows, GetExportColumns(), BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c>.
    /// </summary>
    private async Task<List<ProvisionChangeReportRowDto>> BuildRowsAsync(GetProvisionChangeReportInput input)
    {
        var month = input.Month ?? Today.Month;
        var year = input.Year ?? Today.Year;
        var reportPeriod = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        var employeeQuery = _employeeRepository.GetAll()
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        var inactiveWithFnfIds = _employeeFnFPaymentRepository.GetAll().Select(f => f.EmployeeId).Distinct();

        var statusFilter = (input.EmployeeStatusFilter ?? "ALL").ToUpperInvariant();
        if (statusFilter == "ACTIVE")
        {
            employeeQuery = employeeQuery.Where(e => e.Status == EmployeeStatus.Active);
        }
        else if (statusFilter == "INACTIVE")
        {
            employeeQuery = employeeQuery.Where(e => e.Status == EmployeeStatus.Inactive && inactiveWithFnfIds.Contains(e.Id));
        }
        else
        {
            employeeQuery = employeeQuery.Where(e => e.Status == EmployeeStatus.Active
                                                      || (e.Status == EmployeeStatus.Inactive && inactiveWithFnfIds.Contains(e.Id)));
        }

        if (!string.IsNullOrWhiteSpace(input.PayrollAreaCode))
        {
            var payrollAreaEmployeeIds = _employeeOrgAssignmentRepository.GetAll().Where(o =>
                o.StartDate <= reportPeriod && o.EndDate >= reportPeriod
                && o.PayrollArea != null && o.PayrollArea.Code == input.PayrollAreaCode).Select(o => o.EmployeeId);
            employeeQuery = employeeQuery.Where(e => payrollAreaEmployeeIds.Contains(e.Id));
        }

        var employees = await employeeQuery
            .Select(e => new { e.Id, e.Code, EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn, e.Status, e.JoiningDate })
            .OrderBy(e => e.Code)
            .ToListAsync();
        var employeeIds = employees.Select(e => e.Id).ToList();

        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var earliestEvaluationDate = timeSettings?.EarliestEvaluationDate ?? DateTime.MinValue;

        var basicPayChanges = await _employeeBasicPayRepository.GetAll()
            .Where(b => employeeIds.Contains(b.EmployeeId) && b.StartDate > earliestEvaluationDate && b.StartDate <= reportPeriod)
            .Select(b => new { b.EmployeeId, b.StartDate })
            .ToListAsync();

        var basicPayDetails = await _employeeBasicPayDetailRepository.GetAll()
            .Where(d => employeeIds.Contains(d.EmployeeBasicPay.EmployeeId)
                        && d.EmployeeBasicPay.StartDate <= reportPeriod && d.EmployeeBasicPay.EndDate >= reportPeriod)
            .Select(d => new { EmployeeId = d.EmployeeBasicPay.EmployeeId, d.WageType.Code, d.Amount, TotalAmount = d.EmployeeBasicPay.Amount, CurrencyName = d.Currency.CurrencyShortFormEn })
            .ToListAsync();

        var orgContexts = await _orgContextResolver.BuildAsync(employeeIds, reportPeriod, useArabicNames);

        var rows = new List<ProvisionChangeReportRowDto>();
        foreach (var employee in employees)
        {
            var orgContext = orgContexts.Get(employee.Id);
            var basicSalary = basicPayDetails.FirstOrDefault(b => b.EmployeeId == employee.Id && b.Code == "1000")?.Amount ?? 0;
            var allowance = basicPayDetails.Where(b => b.EmployeeId == employee.Id && b.Code != "1000").Sum(b => b.Amount);
            var totalSalaryRow = basicPayDetails.FirstOrDefault(b => b.EmployeeId == employee.Id);

            double totalAlDifference = 0;
            double totalIndemnityDifference = 0;

            foreach (var change in basicPayChanges.Where(c => c.EmployeeId == employee.Id))
            {
                var currMonthEnd = new DateTime(change.StartDate.Year, change.StartDate.Month, DateTime.DaysInMonth(change.StartDate.Year, change.StartDate.Month));
                var prevMonthEnd = currMonthEnd.AddMonths(-1);
                prevMonthEnd = new DateTime(prevMonthEnd.Year, prevMonthEnd.Month, DateTime.DaysInMonth(prevMonthEnd.Year, prevMonthEnd.Month));

                var currRegister = await _salaryRegisterRepository.GetAll()
                    .Where(r => r.EmployeeId == employee.Id && r.ForPeriod == currMonthEnd && r.InPeriod == currMonthEnd && r.PayrollRunType == PayrollRunType.Regular)
                    .Select(r => new { r.Id })
                    .FirstOrDefaultAsync();
                if (currRegister == null)
                {
                    continue;
                }

                var prevRegister = await _salaryRegisterRepository.GetAll()
                    .Where(r => r.EmployeeId == employee.Id && r.ForPeriod == prevMonthEnd && r.InPeriod == prevMonthEnd && r.PayrollRunType == PayrollRunType.Regular)
                    .Select(r => new { r.Id })
                    .FirstOrDefaultAsync();
                if (prevRegister == null)
                {
                    continue;
                }

                var currDetails = await _salaryRegisterDetailRepository.GetAll()
                    .Where(d => d.SalaryRegisterId == currRegister.Id && (d.WageType.Code == "3004" || d.WageType.Code == "3000" || d.WageType.Code == "3003"))
                    .Select(d => new { d.Id, d.WageType.Code, d.Amount, d.Rate, d.Number })
                    .ToListAsync();

                var prevDetails = await _salaryRegisterDetailRepository.GetAll()
                    .Where(d => d.SalaryRegisterId == prevRegister.Id && (d.WageType.Code == "3004" || d.WageType.Code == "3000" || d.WageType.Code == "3003"))
                    .Select(d => new { d.WageType.Code, d.Amount, d.Rate, d.Number })
                    .ToListAsync();

                // Annual Leave Provision Difference (legacy lines 809-831).
                var currAlRate = currDetails.FirstOrDefault(d => d.Code == "3004")?.Rate ?? 0;
                var prevAlClosing = prevDetails.FirstOrDefault(d => d.Code == "3004");
                var prevAlClosingBalance = prevAlClosing?.Number ?? 0;
                var prevAlClosingAmountOldSalary = prevAlClosing?.Amount ?? 0;
                var alProvisionRateNewSalary = currAlRate != 0 ? Math.Round(currAlRate / 26, 6) : 0;
                var prevAlClosingAmountNewSalary = prevAlClosingBalance * alProvisionRateNewSalary;
                totalAlDifference += prevAlClosingAmountNewSalary - prevAlClosingAmountOldSalary;

                // Indemnity Provision Difference (legacy lines 833-853).
                var currIndemnityClosing = currDetails.FirstOrDefault(d => d.Code == "3003")?.Amount ?? 0;
                var currMonthDetail = currDetails.FirstOrDefault(d => d.Code == "3000");
                double currIndemnityMonthlyPosted;
                if (currMonthDetail != null)
                {
                    var daysDetailAmount = await _salaryRegisterDaysDetailRepository.GetAll()
                        .Where(dd => dd.SalaryRegisterDetailId == currMonthDetail.Id && dd.Period == currMonthEnd)
                        .Select(dd => (double?)dd.Amount)
                        .FirstOrDefaultAsync();
                    currIndemnityMonthlyPosted = daysDetailAmount ?? currMonthDetail.Amount;
                }
                else
                {
                    currIndemnityMonthlyPosted = 0;
                }

                var prevIndemnityClosing = prevDetails.FirstOrDefault(d => d.Code == "3003")?.Amount ?? 0;
                totalIndemnityDifference += currIndemnityClosing - currIndemnityMonthlyPosted - prevIndemnityClosing;
            }

            rows.Add(new ProvisionChangeReportRowDto
            {
                Id = employee.Id,
                EmployeeCode = employee.Code,
                EmployeeName = employee.EmployeeName,
                Status = employee.Status.ToString(),
                JoiningDate = employee.JoiningDate,
                PayrollAreaName = orgContext.PayrollAreaName,
                CostCenterName = orgContext.CostCenterName,
                JobTitleName = orgContext.JobTitleName,
                BasicSalary = basicSalary,
                Allowance = allowance,
                TotalSalary = totalSalaryRow?.TotalAmount ?? 0,
                CurrencyName = totalSalaryRow?.CurrencyName,
                OrgLevel1 = orgContext.OrgLevel1,
                OrgLevel2 = orgContext.OrgLevel2,
                OrgLevel3 = orgContext.OrgLevel3,
                OrgLevel4 = orgContext.OrgLevel4,
                OrgLevel5 = orgContext.OrgLevel5,
                OrgLevel6 = orgContext.OrgLevel6,
                OrgLevel7 = orgContext.OrgLevel7,
                TotalAlDifferenceAmount = totalAlDifference,
                TotalIndemnityDifferenceAmount = totalIndemnityDifference
            });
        }

        return rows;
    }

    private static IReadOnlyList<GridReportColumn<ProvisionChangeReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<ProvisionChangeReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("EmployeeName", r => r.EmployeeName),
            new("Status", r => r.Status),
            new("JoiningDate", r => r.JoiningDate),
            new("PayrollArea", r => r.PayrollAreaName),
            new("CostCenter", r => r.CostCenterName),
            new("Jobtitle", r => r.JobTitleName),
            new("BasicSalary", r => r.BasicSalary),
            new("Allowance", r => r.Allowance),
            new("TotalSalary", r => r.TotalSalary),
            new("Currency", r => r.CurrencyName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("TotalAlDifferenceAmount", r => r.TotalAlDifferenceAmount),
            new("TotalIndemnityDifferenceAmount", r => r.TotalIndemnityDifferenceAmount)
        };
    }
}
