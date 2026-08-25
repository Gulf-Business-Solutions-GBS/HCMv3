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
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Leave Salary analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeLeaveSalary.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 479-734).
///
/// STRUCTURAL DEVIATION FROM THE SIMPLE-REPORT TEMPLATE: legacy computes, PER ROW, a month-boundary
/// leave/salary split against <c>TimeRegister</c> and the employee's basic-pay rate -- correlated
/// per-row logic that cannot be expressed as one SQL projection. <see cref="BuildRowsAsync"/> is
/// therefore the single source of truth (filtering AND the per-row calculation), materializing the base
/// rows once and computing the derived columns in a C# loop, exactly mirroring legacy's own approach.
/// Both the paged read and the export call it and get an identical, unpaged row set; the read applies
/// sorting/paging in memory afterwards.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine, and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeLeaveSalary)]
public class EmployeeLeaveSalaryReportAppService : GridReportAppServiceBase, IEmployeeLeaveSalaryReportAppService
{
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<EmployeeAbsenceQuota> _employeeAbsenceQuotaRepository;
    private readonly IRepository<WageTypeLeave> _wageTypeLeaveRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;

    public EmployeeLeaveSalaryReportAppService(
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<EmployeeAbsenceQuota> employeeAbsenceQuotaRepository,
        IRepository<WageTypeLeave> wageTypeLeaveRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository)
    {
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _employeeAbsenceQuotaRepository = employeeAbsenceQuotaRepository;
        _wageTypeLeaveRepository = wageTypeLeaveRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
    }

    protected override string LegacyScreenName => "uscRptEmployeeLeaveSalary";

    protected override string ScreenCaption => "Employee Leave Salary";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeLeaveSalaryReportRowDto>> GetEmployeeLeaveSalaryReport(
        GetEmployeeLeaveSalaryReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var totalCount = rows.Count;

        var paged = rows
            .AsQueryable()
            .OrderBy(input.Sorting)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeeLeaveSalaryReportRowDto>(totalCount, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeLeaveSalaryReportToExcel(GetEmployeeLeaveSalaryReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        rows = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync("EmployeeLeaveSalary.xlsx", rows, GetExportColumns(), BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND the per-row leave/salary calculation. Ports legacy
    /// <c>Table1()</c> lines 560-725. Returns every matching row, unpaged -- the caller pages/sorts.
    /// </summary>
    private async Task<List<EmployeeLeaveSalaryReportRowDto>> BuildRowsAsync(GetEmployeeLeaveSalaryReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var leaveTypeCode = input.LeaveTypeCode;

        var query = _employeeAbsenceRepository.GetAll()
            // Legacy lines 560-561: NOT an overlap test -- StartDate/EndDate must both fall inside the window.
            .Where(a => a.StartDate >= fromDate && a.EndDate <= toDate)
            .WhereIf(!leaveTypeCode.IsNullOrWhiteSpace(), a => a.AbsenceType.Code == leaveTypeCode)
            .WhereIf(employeeCodes.Count > 0, a => employeeCodes.Contains(a.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, a => rlsEmployeeIds.Contains(a.EmployeeId));

        var baseRows = await query
            .Select(a => new
            {
                a.Id,
                a.EmployeeId,
                EmployeeCode = a.Employee.Code,
                EmployeeName = useArabicNames ? a.Employee.FullNameAr : a.Employee.FullNameEn,
                AbsenceTypeName = a.AbsenceType == null
                    ? string.Empty
                    : (useArabicNames ? a.AbsenceType.NameAr : a.AbsenceType.NameEn),
                AbsenceTypeNameEn = a.AbsenceType.NameEn,
                a.StartDate,
                a.EndDate,
                a.StartTime,
                a.EndTime,
                a.AbsenceHours,
                a.CalendarDays,
                a.TotalLeaveDays
            })
            .OrderBy(a => a.EmployeeCode).ThenBy(a => a.StartDate).ThenBy(a => a.EndDate)
            .ToListAsync();

        var rows = new List<EmployeeLeaveSalaryReportRowDto>(baseRows.Count);

        foreach (var rec in baseRows)
        {
            var startOfMonth = new DateTime(rec.StartDate.Year, rec.StartDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            var monthStartDaysCount = await _timeRegisterRepository.GetAll()
                .Where(t => t.EmployeeId == rec.EmployeeId && t.DayDate >= startOfMonth && t.DayDate <= endOfMonth)
                .SumAsync(t => t.AnnualLeaveDay + t.LeaveAdjDay);

            var endMonthStart = new DateTime(rec.EndDate.Year, rec.EndDate.Month, 1);
            var endMonthEnd = endMonthStart.AddMonths(1).AddDays(-1);
            var monthEndDaysCount = await _timeRegisterRepository.GetAll()
                .Where(t => t.EmployeeId == rec.EmployeeId && t.DayDate >= endMonthStart && t.DayDate <= endMonthEnd)
                .SumAsync(t => t.AnnualLeaveDay + t.LeaveAdjDay);

            // Legacy QuotaBalance is a computed XPO property (accrued - used - forfeited + adjusted); the
            // ported entity stores the raw components instead, so it is computed here.
            var quotaBalance = await _employeeAbsenceQuotaRepository.GetAll()
                .Where(q => q.EmployeeId == rec.EmployeeId && q.AbsenceType.NameEn == rec.AbsenceTypeNameEn)
                .Select(q => q.QuotaAccrued - q.QuotaUsed - q.QuotaForfeited + q.QuotaAdjusted)
                .FirstOrDefaultAsync();

            var includeInPaidLeaveWageTypeIds = await _wageTypeLeaveRepository.GetAll()
                .Where(l => l.AbsenceType.NameEn == rec.AbsenceTypeNameEn)
                .Select(l => l.WageTypeId)
                .Distinct()
                .ToListAsync();

            double leaveRate = 0;
            var employeeBasicPayId = await _employeeBasicPayRepository.GetAll()
                .Where(p => p.EmployeeId == rec.EmployeeId && p.StartDate <= startOfMonth)
                .OrderByDescending(p => p.StartDate)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();

            if (employeeBasicPayId.HasValue)
            {
                leaveRate = await _employeeBasicPayDetailRepository.GetAll()
                    .Where(d => d.EmployeeBasicPayId == employeeBasicPayId.Value
                                && includeInPaidLeaveWageTypeIds.Contains(d.WageTypeId))
                    .SumAsync(d => d.Amount);
            }

            var leaveSalaryMonthStart = monthStartDaysCount * (leaveRate / 26);
            var leaveSalaryMonthEnd = monthEndDaysCount * (leaveRate / 26);
            var empSalaryMonthStart = (26 - monthStartDaysCount) * (leaveRate / 26);
            var empSalaryMonthEnd = (26 - monthEndDaysCount) * (leaveRate / 26);

            // Legacy lines 673-707: count calendar months touched by [StartDate, EndDate], then a middle
            // month "salary" -- legacy's own middleMonthSalary formula ignores the loop's per-month days
            // and instead multiplies (monthCount-2)*26 by the daily rate every iteration, so only the LAST
            // iteration's value survives. Reproduced exactly, including that quirk.
            var monthCount = 0;
            var currentMonth = new DateTime(rec.StartDate.Year, rec.StartDate.Month, 1);
            while (currentMonth <= rec.EndDate)
            {
                monthCount++;
                currentMonth = currentMonth.AddMonths(1);
            }

            var middleMonthsCount = Math.Max(0, monthCount - 2);
            double middleMonthSalary = 0;
            if (monthCount > 2)
            {
                middleMonthSalary = middleMonthsCount * 26 * (leaveRate / 26);
            }

            double totalLeaveSalary;
            double totalEmployeeSalary;
            if (monthCount > 1)
            {
                totalLeaveSalary = leaveSalaryMonthEnd + leaveSalaryMonthStart + middleMonthSalary;
                totalEmployeeSalary = empSalaryMonthStart + empSalaryMonthEnd;
            }
            else
            {
                totalLeaveSalary = leaveSalaryMonthStart;
                totalEmployeeSalary = empSalaryMonthStart;
            }

            rows.Add(new EmployeeLeaveSalaryReportRowDto
            {
                Id = rec.Id,
                EmployeeCode = rec.EmployeeCode,
                EmployeeName = rec.EmployeeName,
                AbsenceTypeName = rec.AbsenceTypeName,
                StartDate = rec.StartDate,
                EndDate = rec.EndDate,
                StartTime = rec.StartTime.ToString("HH:mm"),
                EndTime = rec.EndTime.ToString("HH:mm"),
                AbsenceHours = rec.AbsenceHours,
                LeaveDaysInMonthStart = monthStartDaysCount,
                LeaveDaysInMonthEnd = monthEndDaysCount,
                CalendarDays = rec.CalendarDays,
                TotalLeaveDays = rec.TotalLeaveDays,
                QuotaBalance = quotaBalance,
                EmployeeSalaryInMonthStart = empSalaryMonthStart,
                EmployeeSalaryInMiddleMonth = monthCount > 2 ? middleMonthSalary : 0,
                EmployeeSalaryInMonthEnd = empSalaryMonthEnd,
                LeaveSalaryInMonthStart = leaveSalaryMonthStart,
                LeaveSalaryInMiddleMonth = monthCount > 2 ? middleMonthSalary : 0,
                LeaveSalaryInMonthEnd = leaveSalaryMonthEnd,
                TotalEmployeeSalary = totalEmployeeSalary,
                TotalLeaveSalary = totalLeaveSalary
            });
        }

        return rows;
    }

    private string BuildParameterString(GetEmployeeLeaveSalaryReportInput input)
    {
        var parts = new List<string>
        {
            "From Date : " + input.ResolveFromDate(Today).ToString("dd/MM/yyyy"),
            "To Date : " + input.ResolveToDate(Today).ToString("dd/MM/yyyy")
        };

        if (!input.LeaveTypeCode.IsNullOrWhiteSpace())
        {
            parts.Add("Leave Type : " + input.LeaveTypeCode);
        }

        if (!input.EmployeeCode.IsNullOrWhiteSpace())
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    private static IReadOnlyList<GridReportColumn<EmployeeLeaveSalaryReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeLeaveSalaryReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("AbsenceType", r => r.AbsenceTypeName),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate),
            new("StartTime", r => r.StartTime),
            new("EndTime", r => r.EndTime),
            new("AbsenceHours", r => r.AbsenceHours),
            new("LeaveDaysInMonthStart", r => r.LeaveDaysInMonthStart),
            new("LeaveDaysInMonthEnd", r => r.LeaveDaysInMonthEnd),
            new("CalendarDays", r => r.CalendarDays),
            new("TotalLeaveDays", r => r.TotalLeaveDays),
            new("QuotaBalance", r => r.QuotaBalance),
            new("EmployeeSalaryInMonthStart", r => r.EmployeeSalaryInMonthStart),
            new("EmployeeSalaryInMiddleMonth", r => r.EmployeeSalaryInMiddleMonth),
            new("EmployeeSalaryInMonthEnd", r => r.EmployeeSalaryInMonthEnd),
            new("LeaveSalaryInMonthStart", r => r.LeaveSalaryInMonthStart),
            new("LeaveSalaryInMiddleMonth", r => r.LeaveSalaryInMiddleMonth),
            new("LeaveSalaryInMonthEnd", r => r.LeaveSalaryInMonthEnd),
            new("TotalEmployeeSalary", r => r.TotalEmployeeSalary),
            new("TotalLeaveSalary", r => r.TotalLeaveSalary)
        };
    }
}
