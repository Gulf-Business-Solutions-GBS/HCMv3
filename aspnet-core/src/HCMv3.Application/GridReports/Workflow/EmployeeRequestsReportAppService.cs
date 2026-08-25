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
using HCMv3.GridReports.Workflow.Dto;
using HCMv3.SystemSecurity;
using HCMv3.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Workflow;

/// <summary>
/// Employee Requests analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Workflow/uscRptEmployeeRequests.ascx(.cs)</c>, whose entire
/// data contract is <c>Table1()</c> (lines 474-681).
///
/// SCOPE NOTE: legacy's <c>WorkflowRequest</c> is an XAF table-per-hierarchy base class with ~20 concrete
/// subclasses (<c>LeaveRequest</c>, <c>AssetRequest</c>, ...) that supply the LeaveType and
/// WorkflowComments columns via per-subtype lookups. HCMv3's <see cref="HCMv3.Workflow.WorkflowRequest"/>
/// is a standalone entity WITHOUT that subclass hierarchy (see its class remarks) -- an upstream scope
/// decision from the Workflow module port, not introduced here -- so those two columns and the
/// DelegatedEmployee filter/column cannot be sourced and are not ported; see the row DTO remarks.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine, and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeRequests)]
public class EmployeeRequestsReportAppService : GridReportAppServiceBase, IEmployeeRequestsReportAppService
{
    private readonly IRepository<WorkflowRequest> _workflowRequestRepository;
    private readonly IRepository<WorkFlowApprover> _workFlowApproverRepository;

    public EmployeeRequestsReportAppService(
        IRepository<WorkflowRequest> workflowRequestRepository,
        IRepository<WorkFlowApprover> workFlowApproverRepository)
    {
        _workflowRequestRepository = workflowRequestRepository;
        _workFlowApproverRepository = workFlowApproverRepository;
    }

    protected override string LegacyScreenName => "uscRptEmployeeRequests";

    protected override string ScreenCaption => "Employee Requests";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeRequestsReportRowDto>> GetEmployeeRequestsReport(
        GetEmployeeRequestsReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampApproverNamesAsync(rows);

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildParameterString(input, ApproverParameterPart(input)),
            totalCount);

        return new PagedResultDto<EmployeeRequestsReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeRequestsReportToExcel(GetEmployeeRequestsReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: export must return every filtered row, not just the current page.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampApproverNamesAsync(rows);

        return await ExportToFileAsync(
            "EmployeeRequests.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input, ApproverParameterPart(input)));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeRequests.ascx.cs:474-681). ApproverNames is stamped
    /// separately afterwards -- see <see cref="StampApproverNamesAsync"/> -- but the APPROVER FILTER
    /// itself stays here, as a real query predicate.
    /// </summary>
    private async Task<IQueryable<EmployeeRequestsReportRowDto>> BuildQueryAsync(GetEmployeeRequestsReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var approverCodes = ResolveApproverCodes(input.ApproverCode);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var requestStatus = input.RequestStatus;

        var query = _workflowRequestRepository.GetAll()
            // Legacy lines 528-530: r.SysCreatedOn.Date >= dtFrom && <= dtTo && r.Employee != null.
            .Where(r => r.EmployeeId != null
                        && r.CreationTime.Date >= fromDate
                        && r.CreationTime.Date <= toDate)
            // Legacy lines 532-537.
            .WhereIf(requestStatus.HasValue, r => r.RequestStatus == requestStatus.Value)
            // Legacy lines 539-544: r.WorkFlowApprover.Any(ap => employees.Contains(ap.Approver.Code)).
            .WhereIf(approverCodes.Count > 0,
                r => r.WorkFlowApprovers.Any(ap => ap.Approver != null && approverCodes.Contains(ap.Approver.Code)))
            // Legacy lines 546-551: employees.Contains(r.Employee.Code).
            .WhereIf(employeeCodes.Count > 0, r => employeeCodes.Contains(r.Employee.Code))
            // Legacy line 553 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, r => rlsEmployeeIds.Contains(r.EmployeeId.Value));

        var today = Today;

        return query.Select(r => new EmployeeRequestsReportRowDto
        {
            Id = r.Id,
            RequestNumber = r.RequestNumber,
            CreatedDate = r.CreationTime,
            EmployeeCode = r.Employee.Code,
            EmployeeName = useArabicNames ? r.Employee.FullNameAr : r.Employee.FullNameEn,
            RequestType = r.RequestType,
            RequestStatus = r.RequestStatus,
            WorkflowStep = r.WorkflowStep,
            // Legacy line 579-581: SysModifiedOn if it was ever set, otherwise SysCreatedOn.
            StalledDays = (today - (r.LastModificationTime ?? r.CreationTime).Date).Days
        });
    }

    /// <summary>
    /// Fills <see cref="EmployeeRequestsReportRowDto.ApproverNames"/> on already-materialized rows.
    /// Legacy line 570: <c>string.Join(", ", w.Approver.CodeWithFullNameEn)</c> -- always the English name
    /// regardless of aspect, reproduced verbatim (Should_Reproduce_The_Legacy_ApproverAlwaysEnglish_Behavior).
    /// Shared by the read and the export so the two cannot drift.
    /// </summary>
    private async Task StampApproverNamesAsync(List<EmployeeRequestsReportRowDto> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var requestIds = rows.Select(r => r.Id).ToList();

        var approversByRequest = await _workFlowApproverRepository.GetAll()
            .Where(w => requestIds.Contains(w.WorkflowRequestId) && w.Approver != null)
            .Select(w => new
            {
                w.WorkflowRequestId,
                Name = w.Approver.Code + " " + w.Approver.FullNameEn
            })
            .ToListAsync();

        var lookup = approversByRequest
            .GroupBy(a => a.WorkflowRequestId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(a => a.Name)));

        foreach (var row in rows)
        {
            row.ApproverNames = lookup.TryGetValue(row.Id, out var names) ? names : string.Empty;
        }
    }

    private static List<string> ResolveApproverCodes(string approverCode)
    {
        if (approverCode.IsNullOrWhiteSpace())
        {
            return new List<string>();
        }

        return approverCode
            .Split(',')
            .Select(c => c.Trim())
            .Where(c => !c.IsNullOrWhiteSpace())
            .Distinct()
            .ToList();
    }

    private static string ApproverParameterPart(GetEmployeeRequestsReportInput input)
    {
        return input.ApproverCode.IsNullOrWhiteSpace() ? null : "Approver Code : " + input.ApproverCode;
    }

    /// <summary>
    /// The exported column set, in legacy column order (OID omitted, same convention as
    /// <c>EmployeeContractReportAppService</c>).
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeeRequestsReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeRequestsReportRowDto>>
        {
            new("RequestNumber", r => r.RequestNumber),
            new("CreatedDate", r => r.CreatedDate),
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("Approver", r => r.ApproverNames),
            new("RequestType", r => r.RequestType),
            new("RequestStatus", r => r.RequestStatus.ToString()),
            new("WorkflowStep", r => r.WorkflowStep),
            new("StalledDays", r => r.StalledDays)
        };
    }
}
