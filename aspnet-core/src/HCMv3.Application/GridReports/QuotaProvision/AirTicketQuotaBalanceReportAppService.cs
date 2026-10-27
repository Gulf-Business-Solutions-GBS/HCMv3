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
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.QuotaProvision;

/// <summary>
/// Air Ticket Quota Balance analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptAirTicketQuotaBalance.ascx(.cs)</c>,
/// method <c>Table1()</c>.
///
/// LEDGER SIMPLIFICATION (per wave brief): legacy re-derived accrued/used/adjusted quota LIVE for every row
/// -- <c>EmployeeSubGroupTicket</c> lookup for the entitlement + <c>AccrualService.GetAirTicketAccrualStartEnd</c>
/// for the deduction window + summing raw <c>EmployeeTicket</c>/<c>EmployeeTktAdjustment</c> transactions
/// inside that window. HCMv3 already maintains that exact result as a ledger row,
/// <see cref="EmployeeTicketQuota"/> (identical shape to <see cref="HCMv3.PersonnelAdministration.EmployeeAbsenceQuota"/>,
/// which the wave brief explicitly says to reuse rather than re-deriving). This port reads the
/// <see cref="EmployeeTicketQuota"/> row covering the As-On-Date directly instead of re-running the live
/// computation, exactly as the brief's guidance for leave-quota-balance reports extends to the ticket-quota
/// analogue.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_AirTicketQuotaBalance)]
public class AirTicketQuotaBalanceReportAppService : GridReportAppServiceBase, IAirTicketQuotaBalanceReportAppService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<HCMv3.PayrollManagement.TicketType> _ticketTypeRepository;
    private readonly IRepository<EmployeeTicketQuota> _employeeTicketQuotaRepository;
    private readonly QuotaProvisionOrgContextResolver _orgContextResolver;

    public AirTicketQuotaBalanceReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<HCMv3.PayrollManagement.TicketType> ticketTypeRepository,
        IRepository<EmployeeTicketQuota> employeeTicketQuotaRepository,
        QuotaProvisionOrgContextResolver orgContextResolver)
    {
        _employeeRepository = employeeRepository;
        _ticketTypeRepository = ticketTypeRepository;
        _employeeTicketQuotaRepository = employeeTicketQuotaRepository;
        _orgContextResolver = orgContextResolver;
    }

    protected override string LegacyScreenName => "uscRptAirTicketQuotaBalance";

    protected override string ScreenCaption => "Air Ticket Quota Balance";

    [HttpPost]
    public async Task<PagedResultDto<AirTicketQuotaBalanceReportRowDto>> GetAirTicketQuotaBalanceReport(
        GetAirTicketQuotaBalanceReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var query = rows.AsQueryable();
        var totalCount = query.Count();

        var pagedRows = query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<AirTicketQuotaBalanceReportRowDto>(totalCount, pagedRows);
    }

    [HttpPost]
    public async Task<FileDto> GetAirTicketQuotaBalanceReportToExcel(GetAirTicketQuotaBalanceReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var orderedRows = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "AirTicketQuotaBalance.xlsx",
            orderedRows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> lines 476-657: cross-joins active employees against the (optionally
    /// single) ticket type, reads the <see cref="EmployeeTicketQuota"/> row covering the As-On-Date, and
    /// reproduces the "AsOnDate is Less then Joining Date" comment.
    /// </summary>
    private async Task<List<AirTicketQuotaBalanceReportRowDto>> BuildRowsAsync(GetAirTicketQuotaBalanceReportInput input)
    {
        var asOnDate = input.ResolveFromDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        // Legacy line 547: the employee set on this screen is hard-coded to Active (no status combo).
        var employeeQuery = _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        var employees = await employeeQuery
            .Select(e => new { e.Id, e.Code, EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn, e.JoiningDate })
            .OrderBy(e => e.Code)
            .ToListAsync();

        var ticketTypeQuery = _ticketTypeRepository.GetAll()
            .WhereIf(!string.IsNullOrWhiteSpace(input.TicketTypeCode), t => t.Code == input.TicketTypeCode);

        var ticketTypes = await ticketTypeQuery
            .Select(t => new { t.Id, TicketTypeName = useArabicNames ? t.NameAr : t.NameEn })
            .ToListAsync();

        var employeeIds = employees.Select(e => e.Id).ToList();

        var quotas = await _employeeTicketQuotaRepository.GetAll()
            .Where(q => employeeIds.Contains(q.EmployeeId) && q.EntitlementFrom <= asOnDate && q.EntitlementTo >= asOnDate)
            .Select(q => new
            {
                q.EmployeeId,
                q.TicketTypeId,
                q.DeductionFrom,
                q.DeductionTo,
                q.QuotaAccrued,
                q.QuotaUsed,
                q.QuotaAdjusted
            })
            .ToListAsync();

        var orgContexts = await _orgContextResolver.BuildAsync(employeeIds, asOnDate, useArabicNames);

        var rows = new List<AirTicketQuotaBalanceReportRowDto>();
        foreach (var employee in employees)
        {
            var orgContext = orgContexts.Get(employee.Id);

            foreach (var ticketType in ticketTypes)
            {
                var row = new AirTicketQuotaBalanceReportRowDto
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
                    TicketTypeName = ticketType.TicketTypeName,
                    AsOnDate = asOnDate
                };

                // Legacy lines 590-591/643-644: nothing is computed when the As-On-Date predates joining.
                if (asOnDate > employee.JoiningDate)
                {
                    var quota = quotas.FirstOrDefault(q => q.EmployeeId == employee.Id && q.TicketTypeId == ticketType.Id);
                    if (quota != null)
                    {
                        row.DeductionStart = quota.DeductionFrom;
                        row.DeductionEnd = quota.DeductionTo;
                        row.AccruedQuota = quota.QuotaAccrued;
                        row.UsedQuota = quota.QuotaUsed;
                        row.AdjustedQuota = quota.QuotaAdjusted;
                        row.BalanceQuota = quota.QuotaAccrued - (quota.QuotaUsed + quota.QuotaAdjusted);
                    }
                }
                else
                {
                    row.Comment = "AsOnDate is Less then Joining Date : " + employee.JoiningDate.ToString("dd-MM-yyyy");
                }

                rows.Add(row);
            }
        }

        return rows;
    }

    private static IReadOnlyList<GridReportColumn<AirTicketQuotaBalanceReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<AirTicketQuotaBalanceReportRowDto>>
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
            new("TicketType", r => r.TicketTypeName),
            new("AsOnDate", r => r.AsOnDate),
            new("DeductionStart", r => r.DeductionStart),
            new("DeductionEnd", r => r.DeductionEnd),
            new("AccruedQuota", r => r.AccruedQuota),
            new("UsedQuota", r => r.UsedQuota),
            new("AdjustedQuota", r => r.AdjustedQuota),
            new("BalanceQuota", r => r.BalanceQuota),
            new("Comment", r => r.Comment)
        };
    }
}
