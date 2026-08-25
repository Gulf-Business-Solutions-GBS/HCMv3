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
/// Annual Leave Sheet analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptAnnualLeaveSheet.ascx(.cs)</c>,
/// <c>Table1()</c> (lines 514-731). The drill-down <c>Table2</c> (per-employee transaction ledger) is NOT
/// ported -- see <c>EmployeeContractReportAppService</c> remarks for the "single Table1 grid" precedent
/// this whole wave follows.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_AnnualLeaveSheet)]
public class AnnualLeaveSheetReportAppService : GridReportAppServiceBase, IAnnualLeaveSheetReportAppService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<TimeTransfer> _timeTransferRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeAbsAdjustment> _employeeAbsAdjustmentRepository;
    private readonly IRepository<EmployeeAbsQuotaForfeit> _employeeAbsQuotaForfeitRepository;
    private readonly IRepository<EmployeeSubGroupAbsence> _employeeSubGroupAbsenceRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly QuotaProvisionOrgContextResolver _orgContextResolver;

    public AnnualLeaveSheetReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<TimeTransfer> timeTransferRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeAbsAdjustment> employeeAbsAdjustmentRepository,
        IRepository<EmployeeAbsQuotaForfeit> employeeAbsQuotaForfeitRepository,
        IRepository<EmployeeSubGroupAbsence> employeeSubGroupAbsenceRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        QuotaProvisionOrgContextResolver orgContextResolver)
    {
        _employeeRepository = employeeRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _timeTransferRepository = timeTransferRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _employeeAbsAdjustmentRepository = employeeAbsAdjustmentRepository;
        _employeeAbsQuotaForfeitRepository = employeeAbsQuotaForfeitRepository;
        _employeeSubGroupAbsenceRepository = employeeSubGroupAbsenceRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgContextResolver = orgContextResolver;
    }

    protected override string LegacyScreenName => "uscRptAnnualLeaveSheet";

    protected override string ScreenCaption => "Annual Leave Sheet";

    [HttpPost]
    public async Task<PagedResultDto<AnnualLeaveSheetReportRowDto>> GetAnnualLeaveSheetReport(GetAnnualLeaveSheetReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var query = rows.AsQueryable();
        var totalCount = query.Count();

        var pagedRows = query.OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<AnnualLeaveSheetReportRowDto>(totalCount, pagedRows);
    }

    [HttpPost]
    public async Task<FileDto> GetAnnualLeaveSheetReportToExcel(GetAnnualLeaveSheetReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var orderedRows = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync("AnnualLeaveSheet.xlsx", orderedRows, GetExportColumns(), BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c>: for each Active employee (optionally filtered by payroll area/code),
    /// computes opening balance (TimeTransfer type "1"), accrued (TimeRegister.AnnualLeaveAccrualPerDay minus
    /// opening balance -- legacy line 701), used (EmployeeAbsence.TotalLeaveDays), adjusted
    /// (EmployeeAbsAdjustment), forfeited (EmployeeAbsQuotaForfeit, only under the Monthly forfeit rule) and
    /// the resulting balance, capped at the employee sub-group's MaxLimit when the forfeit rule is NOT
    /// Monthly (legacy lines 707-717).
    /// </summary>
    private async Task<List<AnnualLeaveSheetReportRowDto>> BuildRowsAsync(GetAnnualLeaveSheetReportInput input)
    {
        var asOnDate = input.ResolveFromDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var forfeitMonthly = timeSettings != null && timeSettings.ForfeitRule == ForfeitRule.Monthly;
        var earliestEvaluationDate = timeSettings?.EarliestEvaluationDate ?? System.DateTime.MinValue;

        var employeeQuery = _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        if (!string.IsNullOrWhiteSpace(input.PayrollAreaCode))
        {
            var payrollAreaEmployeeIds = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.StartDate <= asOnDate && o.EndDate >= asOnDate
                            && o.PayrollArea != null && o.PayrollArea.Code == input.PayrollAreaCode)
                .Select(o => o.EmployeeId);
            employeeQuery = employeeQuery.Where(e => payrollAreaEmployeeIds.Contains(e.Id));
        }

        var employees = await employeeQuery
            .Select(e => new { e.Id, e.Code, EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn })
            .OrderBy(e => e.Code)
            .ToListAsync();

        var employeeIds = employees.Select(e => e.Id).ToList();

        var annualLeaveType = await _absenceTypeRepository.GetAll()
            .Where(a => a.Code == "1")
            .Select(a => new { a.Id })
            .FirstOrDefaultAsync();

        var absenceTypeIds = await _absenceTypeRepository.GetAll()
            .Where(a => a.Code == "1" || a.QuotaType == QuotaType.AnnualLeave)
            .Select(a => a.Id)
            .ToListAsync();

        var openingBalances = await _timeTransferRepository.GetAll()
            .Where(t => employeeIds.Contains(t.EmployeeId) && t.TransferType.Code == "1")
            .GroupBy(t => t.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(t => t.TransferCount) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

        var accruals = await _timeRegisterRepository.GetAll()
            .Where(t => employeeIds.Contains(t.EmployeeId) && t.DayDate <= asOnDate)
            .GroupBy(t => t.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(t => t.AnnualLeaveAccrualPerDay) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

        var used = await _employeeAbsenceRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId) && a.EndDate >= earliestEvaluationDate && a.StartDate <= asOnDate
                        && absenceTypeIds.Contains(a.AbsenceTypeId))
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(a => a.TotalLeaveDays) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

        var adjusted = await _employeeAbsAdjustmentRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId) && a.EffectiveDate <= asOnDate
                        && absenceTypeIds.Contains(a.AbsenceTypeId) && a.AdjustmentType != AdjustmentType.NoImpactOnQuota)
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(a => a.QuotaAdjusted) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

        // Legacy lines 638-641: forfeit is summed up to the END of the As-On-Date month, to include the
        // current month's forfeit run.
        var lastAsOnDate = new System.DateTime(asOnDate.Year, asOnDate.Month, System.DateTime.DaysInMonth(asOnDate.Year, asOnDate.Month));
        var forfeited = await _employeeAbsQuotaForfeitRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId) && a.EffectiveDate <= lastAsOnDate && absenceTypeIds.Contains(a.AbsenceTypeId))
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(a => a.QuotaForfeited) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

        var orgContexts = await _orgContextResolver.BuildAsync(employeeIds, asOnDate, useArabicNames);

        var subGroupIds = orgContexts is null
            ? new List<int>()
            : employeeIds.Select(id => orgContexts.Get(id).EmployeeSubGroupId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();

        var subGroupAbsences = annualLeaveType == null
            ? new List<EmployeeSubGroupAbsence>()
            : await _employeeSubGroupAbsenceRepository.GetAll()
                .Where(a => subGroupIds.Contains(a.EmployeeSubGroupId) && a.AbsenceTypeId == annualLeaveType.Id
                            && a.StartDate <= asOnDate && a.EndDate >= asOnDate)
                .ToListAsync();

        const int leaveRoundoff = 2;
        var rows = new List<AnnualLeaveSheetReportRowDto>();
        foreach (var employee in employees)
        {
            var orgContext = orgContexts.Get(employee.Id);
            openingBalances.TryGetValue(employee.Id, out var openingBalance);
            accruals.TryGetValue(employee.Id, out var totalAccrual);
            used.TryGetValue(employee.Id, out var usedDays);
            adjusted.TryGetValue(employee.Id, out var adjustedDays);
            forfeited.TryGetValue(employee.Id, out var forfeitedDays);

            var subGroupAbs = orgContext.EmployeeSubGroupId.HasValue
                ? subGroupAbsences.FirstOrDefault(a => a.EmployeeSubGroupId == orgContext.EmployeeSubGroupId.Value)
                : null;

            var maxLimit = subGroupAbs?.MaxLimit ?? 0;

            double balanceQuota;
            var quotaForfeited = 0d;
            if (forfeitMonthly)
            {
                quotaForfeited = System.Math.Round(forfeitedDays, leaveRoundoff);
                balanceQuota = System.Math.Round(totalAccrual - (usedDays + adjustedDays + forfeitedDays), leaveRoundoff);
            }
            else
            {
                balanceQuota = System.Math.Round(totalAccrual - (usedDays + adjustedDays), leaveRoundoff);
                if (maxLimit != 0 && balanceQuota > maxLimit)
                {
                    balanceQuota = maxLimit;
                }
            }

            rows.Add(new AnnualLeaveSheetReportRowDto
            {
                Id = employee.Id,
                EmployeeCode = employee.Code,
                EmployeeName = employee.EmployeeName,
                OrgLevel1 = orgContext.OrgLevel1,
                OrgLevel2 = orgContext.OrgLevel2,
                OrgLevel3 = orgContext.OrgLevel3,
                OrgLevel4 = orgContext.OrgLevel4,
                OrgLevel5 = orgContext.OrgLevel5,
                OrgLevel6 = orgContext.OrgLevel6,
                OrgLevel7 = orgContext.OrgLevel7,
                JobTitleName = orgContext.JobTitleName,
                CostCenterName = orgContext.CostCenterName,
                AsOnDate = asOnDate,
                PerYearQuota = subGroupAbs?.TotalDays ?? 0,
                MaxLimit = maxLimit,
                OpeningBalance = System.Math.Round(openingBalance, leaveRoundoff),
                QuotaAccrued = System.Math.Round(totalAccrual - openingBalance, leaveRoundoff),
                QuotaUsed = System.Math.Round(usedDays, leaveRoundoff),
                QuotaAdjusted = System.Math.Round(adjustedDays, leaveRoundoff),
                QuotaForfeited = quotaForfeited,
                QuotaBalance = balanceQuota
            });
        }

        return rows;
    }

    private static IReadOnlyList<GridReportColumn<AnnualLeaveSheetReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<AnnualLeaveSheetReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("Jobtitle", r => r.JobTitleName),
            new("CostCenter", r => r.CostCenterName),
            new("AsOnDate", r => r.AsOnDate),
            new("PerYearQuota", r => r.PerYearQuota),
            new("MaxLimit", r => r.MaxLimit),
            new("OpeningBalance", r => r.OpeningBalance),
            new("QuotaAccrued", r => r.QuotaAccrued),
            new("QuotaUsed", r => r.QuotaUsed),
            new("QuotaAdjusted", r => r.QuotaAdjusted),
            new("QuotaForfeited", r => r.QuotaForfeited),
            new("QuotaBalance", r => r.QuotaBalance)
        };
    }
}
