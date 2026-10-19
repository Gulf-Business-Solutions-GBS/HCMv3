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
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.Transaction.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Monthly Payroll Transaction analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptMonthlyPayrollTransaction.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 447-625): a UNION of four transaction sources for the requested
/// calendar month (leave-pay estimate, one-off payment, overtime, loan).
///
/// LEGACY QUIRK REPRODUCED (not a data-loss defect -- reproduced faithfully, see the pinning test): the
/// leave-pay branch's "wage types eligible for paid-leave rate" filter (line 517-520) does not actually
/// filter by the CURRENT row's absence type the way the sibling Employee Leave Salary report's identical
/// looking filter does -- it keeps ANY wage type that has SOME WageTypeLeave mapping to SOME absence type
/// with a non-null WageTypeId, i.e. effectively "all wage types with any leave mapping at all". Reproduced
/// exactly rather than silently fixed.
///
/// The overtime branch's ExpectedAmount is legacy's per-row <c>EmployeeOvertime.ExpectedOvertimeAmount</c> (legacy
/// line 580), calculated by <see cref="IExpectedOvertimeAmountService"/>.
///
/// NOT PORTED: the drill-up/drill-down session stack, the ColumnChooser JSON persistence, and the PDF
/// export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_MonthlyPayrollTransaction)]
public class MonthlyPayrollTransactionReportAppService : GridReportAppServiceBase, IMonthlyPayrollTransactionReportAppService
{
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeePayment> _employeePaymentRepository;
    private readonly IRepository<EmployeeOvertime> _employeeOvertimeRepository;
    private readonly IRepository<EmployeeLoan> _employeeLoanRepository;
    private readonly IRepository<WageTypeLeave> _wageTypeLeaveRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IExpectedOvertimeAmountService _expectedOvertimeAmountService;

    public MonthlyPayrollTransactionReportAppService(
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeePayment> employeePaymentRepository,
        IRepository<EmployeeOvertime> employeeOvertimeRepository,
        IRepository<EmployeeLoan> employeeLoanRepository,
        IRepository<WageTypeLeave> wageTypeLeaveRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IExpectedOvertimeAmountService expectedOvertimeAmountService)
    {
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _employeePaymentRepository = employeePaymentRepository;
        _employeeOvertimeRepository = employeeOvertimeRepository;
        _employeeLoanRepository = employeeLoanRepository;
        _wageTypeLeaveRepository = wageTypeLeaveRepository;
        _wageTypeRepository = wageTypeRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _expectedOvertimeAmountService = expectedOvertimeAmountService;
    }

    protected override string LegacyScreenName => "uscRptMonthlyPayrollTransaction";

    protected override string ScreenCaption => "Monthly Payroll Transaction";

    [HttpPost]
    public async Task<PagedResultDto<MonthlyPayrollTransactionReportRowDto>> GetMonthlyPayrollTransactionReport(
        GetMonthlyPayrollTransactionReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var totalCount = rows.Count;

        var paged = rows.AsQueryable().OrderBy(input.Sorting).Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<MonthlyPayrollTransactionReportRowDto>(totalCount, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetMonthlyPayrollTransactionReportToExcel(GetMonthlyPayrollTransactionReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        rows = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync("MonthlyPayrollTransaction.xlsx", rows, GetExportColumns(), BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection across all four sources, shared by the paged
    /// read and the export. Ports legacy <c>Table1()</c> lines 451-616.
    /// </summary>
    private async Task<List<MonthlyPayrollTransactionReportRowDto>> BuildRowsAsync(GetMonthlyPayrollTransactionReportInput input)
    {
        var (start, end) = input.ResolvePeriod(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        // Legacy lines 517-520: reproduced exactly (see class remarks) -- NOT scoped to the row's own
        // absence type.
        var includeInPaidLeaveWageTypeIds = await _wageTypeLeaveRepository.GetAll()
            .Where(l => l.AbsenceType.WageTypeId != null)
            .Select(l => l.WageTypeId)
            .Distinct()
            .ToListAsync();

        var leaveRows = await _employeeAbsenceRepository.GetAll()
            .Where(a => a.StartDate >= start && a.EndDate <= end)
            .WhereIf(employeeCodes.Count > 0, a => employeeCodes.Contains(a.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, a => rlsEmployeeIds.Contains(a.EmployeeId))
            .Select(a => new
            {
                a.Id,
                a.EmployeeId,
                EmployeeCode = a.Employee.Code,
                EmployeeName = useArabicNames ? a.Employee.FullNameAr : a.Employee.FullNameEn,
                WageTypeId = a.AbsenceType.WageTypeId,
                a.StartDate
            })
            .ToListAsync();

        var wageTypeNamesById = await _wageTypeRepository.GetAll()
            .Select(w => new { w.Id, Name = useArabicNames ? w.NameAr : w.NameEn })
            .ToDictionaryAsync(w => w.Id, w => w.Name);

        var rows = new List<MonthlyPayrollTransactionReportRowDto>();

        foreach (var rec in leaveRows)
        {
            var daysInMonth = DateTime.DaysInMonth(rec.StartDate.Year, rec.StartDate.Month);
            var monthStart = new DateTime(rec.StartDate.Year, rec.StartDate.Month, 1);
            var monthEnd = new DateTime(rec.StartDate.Year, rec.StartDate.Month, daysInMonth);

            var employeeBasicPayId = await _employeeBasicPayRepository.GetAll()
                .Where(p => p.EmployeeId == rec.EmployeeId && p.StartDate <= rec.StartDate)
                .OrderByDescending(p => p.StartDate)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();

            double leaveRate = 0;
            if (employeeBasicPayId.HasValue)
            {
                leaveRate = await _employeeBasicPayDetailRepository.GetAll()
                    .Where(d => d.EmployeeBasicPayId == employeeBasicPayId.Value
                                && includeInPaidLeaveWageTypeIds.Contains(d.WageTypeId))
                    .SumAsync(d => d.Amount);
            }

            var leaveDays = await _timeRegisterRepository.GetAll()
                .Where(t => t.EmployeeId == rec.EmployeeId && t.DayDate >= monthStart && t.DayDate <= monthEnd)
                .SumAsync(t => t.LeaveDay + t.LeaveAdjDay);

            rows.Add(new MonthlyPayrollTransactionReportRowDto
            {
                Id = rec.Id,
                EmployeeCode = rec.EmployeeCode,
                EmployeeName = rec.EmployeeName,
                WageTypeName = rec.WageTypeId.HasValue && wageTypeNamesById.TryGetValue(rec.WageTypeId.Value, out var n) ? n : string.Empty,
                TransactionDate = rec.StartDate,
                ExpectedAmount = leaveDays * (leaveRate / daysInMonth)
            });
        }

        var paymentRows = await _employeePaymentRepository.GetAll()
            .Where(p => p.PaymentDate >= start && p.PaymentDate <= end)
            .WhereIf(employeeCodes.Count > 0, p => employeeCodes.Contains(p.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, p => rlsEmployeeIds.Contains(p.EmployeeId))
            .Select(p => new MonthlyPayrollTransactionReportRowDto
            {
                Id = p.Id,
                EmployeeCode = p.Employee.Code,
                EmployeeName = useArabicNames ? p.Employee.FullNameAr : p.Employee.FullNameEn,
                WageTypeName = p.WageType == null ? string.Empty : (useArabicNames ? p.WageType.NameAr : p.WageType.NameEn),
                TransactionDate = p.PaymentDate,
                ExpectedAmount = p.Amount
            })
            .ToListAsync();
        rows.AddRange(paymentRows);

        var overtimeRows = await _employeeOvertimeRepository.GetAll()
            .Where(o => o.EffectiveDate >= start && o.EffectiveDate <= end)
            .WhereIf(employeeCodes.Count > 0, o => employeeCodes.Contains(o.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, o => rlsEmployeeIds.Contains(o.EmployeeId))
            .Select(o => new MonthlyPayrollTransactionReportRowDto
            {
                Id = o.Id,
                EmployeeCode = o.Employee.Code,
                EmployeeName = useArabicNames ? o.Employee.FullNameAr : o.Employee.FullNameEn,
                WageTypeName = o.OvertimeType.WageType == null
                    ? string.Empty
                    : (useArabicNames ? o.OvertimeType.WageType.NameAr : o.OvertimeType.WageType.NameEn),
                TransactionDate = o.EffectiveDate,
                // Filled below: the legacy figure is computed, not stored.
                ExpectedAmount = 0
            })
            .ToListAsync();

        if (overtimeRows.Count > 0)
        {
            var overtimeIds = overtimeRows.Select(r => r.Id).ToList();
            var overtimes = await _employeeOvertimeRepository.GetAll()
                .Where(o => overtimeIds.Contains(o.Id))
                .ToListAsync();
            var expected = await _expectedOvertimeAmountService.GetExpectedOvertimeAmountsAsync(overtimes);
            foreach (var row in overtimeRows)
            {
                row.ExpectedAmount = expected.TryGetValue(row.Id, out var amount) ? amount : 0;
            }
        }

        rows.AddRange(overtimeRows);

        var loanRows = await _employeeLoanRepository.GetAll()
            .Where(l => l.StartDate >= start && l.EndDate <= end)
            .WhereIf(employeeCodes.Count > 0, l => employeeCodes.Contains(l.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, l => rlsEmployeeIds.Contains(l.EmployeeId))
            .Select(l => new MonthlyPayrollTransactionReportRowDto
            {
                Id = l.Id,
                EmployeeCode = l.Employee.Code,
                EmployeeName = useArabicNames ? l.Employee.FullNameAr : l.Employee.FullNameEn,
                WageTypeName = "Loan Pay",
                TransactionDate = l.StartDate,
                ExpectedAmount = l.ActualAmount
            })
            .ToListAsync();
        rows.AddRange(loanRows);

        return rows.OrderBy(r => r.EmployeeCode).ThenBy(r => r.TransactionDate).ToList();
    }

    private string BuildParameterString(GetMonthlyPayrollTransactionReportInput input)
    {
        var today = Today;
        var parts = new List<string>
        {
            "Month : " + (input.Month ?? today.Month),
            "Year : " + (input.Year ?? today.Year)
        };

        if (!input.EmployeeCode.IsNullOrWhiteSpace())
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    private static IReadOnlyList<GridReportColumn<MonthlyPayrollTransactionReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<MonthlyPayrollTransactionReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Name", r => r.EmployeeName),
            new("WageType", r => r.WageTypeName),
            new("TransactionDate", r => r.TransactionDate),
            new("ExpectedAmount", r => r.ExpectedAmount),
            new("SysCreator", r => r.SysCreator),
            new("SysModifier", r => r.SysModifier)
        };
    }
}
