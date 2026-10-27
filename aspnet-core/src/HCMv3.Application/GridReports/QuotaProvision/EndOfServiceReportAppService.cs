using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
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
/// End Of Service analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptEndOfService.ascx(.cs)</c>, <c>Table1()</c>
/// (the dead, never-called <c>Table1_Old()</c> is not ported).
///
/// LEGACY FALLBACK REPRODUCED: legacy filled the wage-code figures from the register whose
/// <c>ForPeriod == InPeriod == ereportPeriod</c> (the Payroll Calendar month), but for any Active employee
/// with NO such register it fell back to that employee's most recent regular register instead of showing
/// zeros (lines 605-620). That fallback is reproduced here via <see cref="BuildRowsAsync"/>'s two-pass
/// lookup.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EndOfService)]
public class EndOfServiceReportAppService : GridReportAppServiceBase, IEndOfServiceReportAppService
{
    private static readonly string[] WageCodes = { "27", "29", "31", "3000", "3003" };

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<PayrollCalendar> _payrollCalendarRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<EmployeeLoan> _employeeLoanRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeeFnFPayment> _employeeFnFPaymentRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;

    public EndOfServiceReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<PayrollCalendar> payrollCalendarRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<EmployeeLoan> employeeLoanRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<EmployeeFnFPayment> employeeFnFPaymentRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository)
    {
        _employeeRepository = employeeRepository;
        _payrollCalendarRepository = payrollCalendarRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _employeeLoanRepository = employeeLoanRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeeFnFPaymentRepository = employeeFnFPaymentRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
    }

    protected override string LegacyScreenName => "uscRptEndOfService";

    protected override string ScreenCaption => "End Of Service";

    [HttpPost]
    public async Task<PagedResultDto<EndOfServiceReportRowDto>> GetEndOfServiceReport(GetEndOfServiceReportInput input)
    {
        var rows = await BuildRowsAsync(input);
        var query = rows.AsQueryable();
        var totalCount = query.Count();

        var pagedRows = query.OrderBy(input.Sorting ?? "EmployeeCode asc").PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EndOfServiceReportRowDto>(totalCount, pagedRows);
    }

    [HttpPost]
    public async Task<FileDto> GetEndOfServiceReportToExcel(GetEndOfServiceReportInput input)
    {
        var rows = await BuildRowsAsync(input);
        var orderedRows = rows.AsQueryable().OrderBy(input.Sorting ?? "EmployeeCode asc").ToList();

        return await ExportToFileAsync("EndOfService.xlsx", orderedRows, GetExportColumns(), BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (lines 471-709).
    /// </summary>
    private async Task<List<EndOfServiceReportRowDto>> BuildRowsAsync(GetEndOfServiceReportInput input)
    {
        var month = input.Month ?? Today.AddMonths(-1).Month;
        var year = input.Year ?? Today.AddMonths(-1).Year;

        var calendar = await _payrollCalendarRepository.GetAll()
            .Where(c => c.PayrollMonth == month && c.PayrollYear == year)
            .FirstOrDefaultAsync();

        if (calendar == null)
        {
            throw new UserFriendlyException($"Payroll Calendar not found for the Month {month} and Year {year}");
        }

        var reportPeriod = calendar.EndDate;
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        var employeeQuery = _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        if (!string.IsNullOrWhiteSpace(input.PayrollAreaCode))
        {
            var payrollAreaEmployeeIds = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.StartDate <= reportPeriod && o.EndDate >= reportPeriod
                            && o.PayrollArea != null && o.PayrollArea.Code == input.PayrollAreaCode)
                .Select(o => o.EmployeeId);
            employeeQuery = employeeQuery.Where(e => payrollAreaEmployeeIds.Contains(e.Id));
        }

        var employees = await employeeQuery
            .Select(e => new { e.Id, e.Code, EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn, e.JoiningDate })
            .OrderBy(e => e.Code)
            .ToListAsync();

        var employeeIds = employees.Select(e => e.Id).ToList();

        // Pass 1: the target period's register.
        var targetPeriodRows = await _salaryRegisterDetailRepository.GetAll()
            .Where(d => employeeIds.Contains(d.SalaryRegister.EmployeeId)
                        && d.SalaryRegister.ForPeriod == reportPeriod && d.SalaryRegister.InPeriod == reportPeriod
                        && WageCodes.Contains(d.WageType.Code))
            .Select(d => new { d.SalaryRegister.EmployeeId, d.WageType.Code, d.Amount })
            .ToListAsync();

        var employeesWithTargetPeriod = targetPeriodRows.Select(r => r.EmployeeId).Distinct().ToHashSet();
        var missingEmployeeIds = employeeIds.Where(id => !employeesWithTargetPeriod.Contains(id)).ToList();

        // Pass 2 (legacy fallback, lines 605-620): missing employees fall back to their latest regular register.
        var fallbackRows = new List<(int EmployeeId, string Code, double Amount)>();
        if (missingEmployeeIds.Count > 0)
        {
            var latestPeriods = await _salaryRegisterRepository.GetAll()
                .Where(r => missingEmployeeIds.Contains(r.EmployeeId) && r.ForPeriod == r.InPeriod)
                .GroupBy(r => r.EmployeeId)
                .Select(g => new { EmployeeId = g.Key, MaxInPeriod = g.Max(r => r.InPeriod) })
                .ToListAsync();

            var latestRows = await _salaryRegisterDetailRepository.GetAll()
                .Where(d => missingEmployeeIds.Contains(d.SalaryRegister.EmployeeId)
                            && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod
                            && WageCodes.Contains(d.WageType.Code))
                .Select(d => new { d.SalaryRegister.EmployeeId, d.SalaryRegister.InPeriod, d.WageType.Code, d.Amount })
                .ToListAsync();

            foreach (var period in latestPeriods)
            {
                foreach (var row in latestRows.Where(r => r.EmployeeId == period.EmployeeId && r.InPeriod == period.MaxInPeriod))
                {
                    fallbackRows.Add((row.EmployeeId, row.Code, row.Amount));
                }
            }
        }

        var allWageRows = targetPeriodRows.Select(r => (r.EmployeeId, r.Code, r.Amount)).Concat(fallbackRows).ToList();

        var basicSalaries = await _employeeBasicPayDetailRepository.GetAll()
            .Where(d => employeeIds.Contains(d.EmployeeBasicPay.EmployeeId)
                        && d.EmployeeBasicPay.StartDate.Date <= reportPeriod && d.EmployeeBasicPay.EndDate.Date >= reportPeriod
                        && d.WageType.Code == "1000")
            .Select(d => new { EmployeeId = d.EmployeeBasicPay.EmployeeId, d.Amount })
            .ToListAsync();

        var loans = await _employeeLoanRepository.GetAll()
            .Where(l => employeeIds.Contains(l.EmployeeId) && l.LoanType.Code == "12")
            .GroupBy(l => l.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(l => l.DueAmount) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

        var separations = await _employeeSeparationRepository.GetAll()
            .Where(s => employeeIds.Contains(s.EmployeeId) && s.RevokeDate == null)
            .Select(s => s.EmployeeId)
            .Distinct()
            .ToListAsync();

        var fnfPayments = await _employeeFnFPaymentRepository.GetAll()
            .Where(f => employeeIds.Contains(f.EmployeeId))
            .Select(f => f.EmployeeId)
            .Distinct()
            .ToListAsync();

        var daysInMonth = DateTime.DaysInMonth(reportPeriod.Year, reportPeriod.Month);

        var rows = new List<EndOfServiceReportRowDto>();
        foreach (var employee in employees)
        {
            var employeeWageRows = allWageRows.Where(r => r.EmployeeId == employee.Id).ToList();
            basicSalaries.Where(b => b.EmployeeId == employee.Id).Select(b => (double?)b.Amount).FirstOrDefault();

            double AmountFor(string code) => employeeWageRows.FirstOrDefault(r => r.Code == code).Amount;

            var totalIndemnityAmount = AmountFor("3003");
            loans.TryGetValue(employee.Id, out var loanAmount);

            rows.Add(new EndOfServiceReportRowDto
            {
                Id = employee.Id,
                EmployeeCode = employee.Code,
                EmployeeName = employee.EmployeeName,
                JoiningDate = employee.JoiningDate,
                Separation = separations.Contains(employee.Id),
                FnfPayment = fnfPayments.Contains(employee.Id),
                BasicSalary = basicSalaries.FirstOrDefault(b => b.EmployeeId == employee.Id)?.Amount ?? 0,
                AverageCommission = AmountFor("27"),
                AverageTicket = AmountFor("29"),
                AverageBonus = AmountFor("31"),
                MonthlyIndemnityAmount = AmountFor("3000"),
                DailyIndemnityAmount = daysInMonth != 0 ? AmountFor("3000") / daysInMonth : 0,
                GLProvisionAmount = totalIndemnityAmount,
                LoanAmount = loanAmount,
                NetAvailableBalance = totalIndemnityAmount - loanAmount
            });
        }

        return rows;
    }

    private static IReadOnlyList<GridReportColumn<EndOfServiceReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EndOfServiceReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("EmployeeName", r => r.EmployeeName),
            new("JoiningDate", r => r.JoiningDate),
            new("Separation", r => r.Separation),
            new("FnfPayment", r => r.FnfPayment),
            new("BasicSalary", r => r.BasicSalary),
            new("AverageCommission", r => r.AverageCommission),
            new("AverageTicket", r => r.AverageTicket),
            new("AverageBonus", r => r.AverageBonus),
            new("MonthlyIndemnityAmount", r => r.MonthlyIndemnityAmount),
            new("DailyIndemnityAmount", r => r.DailyIndemnityAmount),
            new("GLProvisionAmount", r => r.GLProvisionAmount),
            new("LoanAmount", r => r.LoanAmount),
            new("NetAvailableBalance", r => r.NetAvailableBalance)
        };
    }
}
