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
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.QuotaProvision;

/// <summary>
/// Leave Quota Balance analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptLeaveQuotaBalance.ascx(.cs)</c>,
/// <c>Table1()</c> (lines 499-735; the dead <c>Table1_Old</c> is not ported).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_LeaveQuotaBalance)]
public class LeaveQuotaBalanceReportAppService : GridReportAppServiceBase, ILeaveQuotaBalanceReportAppService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<EmployeeAbsenceQuota> _employeeAbsenceQuotaRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<TimeTransfer> _timeTransferRepository;
    private readonly IRepository<EmployeeSubGroupAbsence> _employeeSubGroupAbsenceRepository;
    private readonly QuotaProvisionOrgContextResolver _orgContextResolver;
    private readonly IAccrualService _accrualService;

    public LeaveQuotaBalanceReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<EmployeeAbsenceQuota> employeeAbsenceQuotaRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<TimeTransfer> timeTransferRepository,
        IRepository<EmployeeSubGroupAbsence> employeeSubGroupAbsenceRepository,
        QuotaProvisionOrgContextResolver orgContextResolver,
        IAccrualService accrualService)
    {
        _employeeRepository = employeeRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _employeeAbsenceQuotaRepository = employeeAbsenceQuotaRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _timeTransferRepository = timeTransferRepository;
        _employeeSubGroupAbsenceRepository = employeeSubGroupAbsenceRepository;
        _orgContextResolver = orgContextResolver;
        _accrualService = accrualService;
    }

    protected override string LegacyScreenName => "uscRptLeaveQuotaBalance";

    protected override string ScreenCaption => "Leave Quota Balance";

    [HttpPost]
    public async Task<PagedResultDto<LeaveQuotaBalanceReportRowDto>> GetLeaveQuotaBalanceReport(GetLeaveQuotaBalanceReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var query = rows.AsQueryable();
        var totalCount = query.Count();

        var pagedRows = query.OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<LeaveQuotaBalanceReportRowDto>(totalCount, pagedRows);
    }

    [HttpPost]
    public async Task<FileDto> GetLeaveQuotaBalanceReportToExcel(GetLeaveQuotaBalanceReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var orderedRows = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync("LeaveQuotaBalance.xlsx", orderedRows, GetExportColumns(), BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c>. Legacy REQUIRES a leave type (line 611-614: no code -> empty table).
    /// </summary>
    private async Task<List<LeaveQuotaBalanceReportRowDto>> BuildRowsAsync(GetLeaveQuotaBalanceReportInput input)
    {
        if (string.IsNullOrWhiteSpace(input.LeaveTypeCode))
        {
            return new List<LeaveQuotaBalanceReportRowDto>();
        }

        var asOnDate = Today;
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        var absenceType = await _absenceTypeRepository.GetAll().Where(a => a.Code == input.LeaveTypeCode).FirstOrDefaultAsync();
        if (absenceType == null)
        {
            return new List<LeaveQuotaBalanceReportRowDto>();
        }

        var leaveTypeName = useArabicNames ? absenceType.NameAr : absenceType.NameEn;

        var employeeQuery = _employeeRepository.GetAll()
            .WhereIf(input.EmployeeStatus.HasValue, e => e.Status == input.EmployeeStatus.Value)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        var employees = await employeeQuery
            .Select(e => new { e.Id, e.Code, EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn })
            .OrderBy(e => e.Code)
            .ToListAsync();
        var employeeIds = employees.Select(e => e.Id).ToList();

        var orgContexts = await _orgContextResolver.BuildAsync(employeeIds, asOnDate, useArabicNames);

        var quotaRows = absenceType.HaveQuotaAccrual
            ? await _employeeAbsenceQuotaRepository.GetAll()
                .Where(q => employeeIds.Contains(q.EmployeeId) && q.AbsenceTypeId == absenceType.Id)
                .Select(q => new LeaveQuotaBalanceLedgerRow
                {
                    EmployeeId = q.EmployeeId,
                    EntitlementTo = q.EntitlementTo,
                    DeductionFrom = q.DeductionFrom,
                    DeductionTo = q.DeductionTo,
                    QuotaAccrued = q.QuotaAccrued,
                    QuotaUsed = q.QuotaUsed,
                    QuotaAdjusted = q.QuotaAdjusted,
                    QuotaForfeited = q.QuotaForfeited
                })
                .ToListAsync()
            : new List<LeaveQuotaBalanceLedgerRow>();

        var quotas = quotaRows
            .GroupBy(q => q.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(q => q.EntitlementTo).First());

        var subGroupIds = employeeIds.Select(id => orgContexts.Get(id).EmployeeSubGroupId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
        var subGroupAbsences = await _employeeSubGroupAbsenceRepository.GetAll()
            .Where(a => subGroupIds.Contains(a.EmployeeSubGroupId) && a.AbsenceTypeId == absenceType.Id
                        && a.StartDate <= asOnDate && a.EndDate >= asOnDate)
            .ToListAsync();

        const int leaveRoundoff = 2;
        var rows = new List<LeaveQuotaBalanceReportRowDto>();
        foreach (var employee in employees)
        {
            var orgContext = orgContexts.Get(employee.Id);

            var subGroupAbs = orgContext.EmployeeSubGroupId.HasValue
                ? subGroupAbsences.FirstOrDefault(a => a.EmployeeSubGroupId == orgContext.EmployeeSubGroupId.Value)
                : null;

            var row = new LeaveQuotaBalanceReportRowDto
            {
                Id = employee.Id,
                EmployeeCode = employee.Code,
                EmployeeName = employee.EmployeeName,
                JobTitleName = orgContext.JobTitleName,
                OrgLevel1 = orgContext.OrgLevel1,
                OrgLevel2 = orgContext.OrgLevel2,
                OrgLevel3 = orgContext.OrgLevel3,
                OrgLevel4 = orgContext.OrgLevel4,
                OrgLevel5 = orgContext.OrgLevel5,
                OrgLevel6 = orgContext.OrgLevel6,
                OrgLevel7 = orgContext.OrgLevel7,
                LeaveTypeName = leaveTypeName,
                TotalDaysAllowed = subGroupAbs?.TotalDays ?? absenceType.TotalDaysAllowed,
                WorkLocationName = orgContext.WorkLocationName
            };

            if (absenceType.HaveQuotaAccrual)
            {
                if (quotas.TryGetValue(employee.Id, out var quota))
                {
                    row.DeductionStart = quota.DeductionFrom;
                    row.DeductionEnd = quota.DeductionTo;
                    row.AccruedQuota = quota.QuotaAccrued;
                    row.UsedQuota = quota.QuotaUsed;
                    row.AdjustedQuota = quota.QuotaAdjusted;
                    row.ForfeitedQuota = quota.QuotaForfeited;
                    row.BalanceQuota = Math.Round((double)(quota.QuotaAccrued - (quota.QuotaUsed + quota.QuotaAdjusted + quota.QuotaForfeited)), leaveRoundoff);
                }
            }
            else
            {
                var (deductionStart, deductionEnd) = await _accrualService.GetLeaveAccrualStartEndAsync(employee.Id, absenceType.Id, asOnDate);
                row.DeductionStart = deductionStart;
                row.DeductionEnd = deductionEnd;

                double openingBalance = 0;
                if (absenceType.Code == "3")
                {
                    openingBalance = await _timeTransferRepository.GetAll()
                        .Where(t => t.EmployeeId == employee.Id && t.TransferType.Code == "2"
                                    && t.TransferDate >= deductionStart && t.TransferDate <= deductionEnd)
                        .SumAsync(t => (double?)t.TransferCount) ?? 0;
                }

                var usedDays = await _employeeAbsenceRepository.GetAll()
                    .Where(a => a.EmployeeId == employee.Id && a.AbsenceTypeId == absenceType.Id
                                && !(a.StartDate > deductionEnd || a.EndDate < deductionStart))
                    .SumAsync(a => (double?)a.TotalLeaveDays) ?? 0;

                usedDays += openingBalance;

                row.UsedQuota = usedDays;
                row.AdjustedQuota = 0;
                row.BalanceQuota = Math.Round(row.TotalDaysAllowed - usedDays, leaveRoundoff);
            }

            rows.Add(row);
        }

        return rows;
    }

    private static IReadOnlyList<GridReportColumn<LeaveQuotaBalanceReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<LeaveQuotaBalanceReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Name", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("LeaveType", r => r.LeaveTypeName),
            new("DeductionStart", r => r.DeductionStart),
            new("DeductionEnd", r => r.DeductionEnd),
            new("TotalDaysAllowed", r => r.TotalDaysAllowed),
            new("AccruedQuota", r => r.AccruedQuota),
            new("UsedQuota", r => r.UsedQuota),
            new("AdjustedQuota", r => r.AdjustedQuota),
            new("ForfeitedQuota", r => r.ForfeitedQuota),
            new("BalanceQuota", r => r.BalanceQuota),
            new("Comment", r => r.Comment),
            new("WorkLocation", r => r.WorkLocationName)
        };
    }

    /// <summary>Projection shape for an <see cref="EmployeeAbsenceQuota"/> ledger row, materialized once per report run.</summary>
    private class LeaveQuotaBalanceLedgerRow
    {
        public int EmployeeId { get; set; }
        public System.DateTime EntitlementTo { get; set; }
        public System.DateTime DeductionFrom { get; set; }
        public System.DateTime DeductionTo { get; set; }
        public double QuotaAccrued { get; set; }
        public double QuotaUsed { get; set; }
        public double QuotaAdjusted { get; set; }
        public double QuotaForfeited { get; set; }
    }
}
