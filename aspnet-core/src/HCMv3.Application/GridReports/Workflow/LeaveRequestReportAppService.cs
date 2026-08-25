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
/// Leave Request analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Workflow/uscRptLeaveRequest.ascx(.cs)</c> (<c>Table1()</c>,
/// lines 475-778), which UNIONS five concrete workflow-request subclasses (LeaveRequest,
/// LeaveAmendmentRequest, LeaveCancellationRequest, ReturnFromLeaveRequest, AbsenceRequest).
///
/// SCOPE NOTE: see <see cref="GetLeaveRequestReportInput"/> and <see cref="LeaveRequestReportRowDto"/> --
/// HCMv3's standalone <see cref="HCMv3.Workflow.WorkflowRequest"/> has no concrete-subclass hierarchy, so
/// the five-way union becomes a single filter on <see cref="WorkflowRequest.RequestType"/>, and
/// LeaveType/StartDate/EndDate/StartTime/EndTime/WorkflowComments/DelegatedEmployee are not ported (no
/// source field exists). This is an upstream scope decision inherited from the Workflow module port.
///
/// DEVIATION (documented, not a legacy defect): legacy's Table1() approver filter ran as a
/// case-insensitive SUBSTRING match against the already-joined approver display string
/// (<c>employees.Any(code =&gt; joinedNames.ToLower().Contains(code.ToLower()))</c>), a byproduct of
/// filtering an in-memory anonymous projection rather than the query. This port normalizes it to the same
/// exact-code-match query predicate used by the sibling <see cref="EmployeeRequestsReportAppService"/>
/// (<c>WorkFlowApprovers.Any(ap =&gt; approverCodes.Contains(ap.Approver.Code))</c>) for consistency and
/// reliable SQL translation.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine, and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_LeaveRequestAnalysis)]
public class LeaveRequestReportAppService : GridReportAppServiceBase, ILeaveRequestReportAppService
{
    private static readonly IReadOnlyDictionary<string, string> ActionTypeByRequestType =
        new Dictionary<string, string>
        {
            ["LeaveRequest"] = "Create",
            ["LeaveAmendmentRequest"] = "Amendment",
            ["LeaveCancellationRequest"] = "Cancellation",
            ["ReturnFromLeaveRequest"] = "ReturnFromLeave"
            // "AbsenceRequest" deliberately absent: legacy read the concrete row's own ActionType field,
            // which does not exist on the standalone WorkflowRequest -- see the class remarks.
        };

    private readonly IRepository<WorkflowRequest> _workflowRequestRepository;
    private readonly IRepository<WorkFlowApprover> _workFlowApproverRepository;

    public LeaveRequestReportAppService(
        IRepository<WorkflowRequest> workflowRequestRepository,
        IRepository<WorkFlowApprover> workFlowApproverRepository)
    {
        _workflowRequestRepository = workflowRequestRepository;
        _workFlowApproverRepository = workFlowApproverRepository;
    }

    protected override string LegacyScreenName => "uscRptLeaveRequest";

    protected override string ScreenCaption => "Leave Request";

    [HttpPost]
    public async Task<PagedResultDto<LeaveRequestReportRowDto>> GetLeaveRequestReport(GetLeaveRequestReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampApproverNamesAsync(rows);
        StampActionTypes(rows);

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildParameterString(input, ApproverParameterPart(input)),
            totalCount);

        return new PagedResultDto<LeaveRequestReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetLeaveRequestReportToExcel(GetLeaveRequestReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: export must return every filtered row, not just the current page.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampApproverNamesAsync(rows);
        StampActionTypes(rows);

        return await ExportToFileAsync(
            "LeaveRequest.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input, ApproverParameterPart(input)));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c>. ApproverNames and ActionType are stamped afterwards -- see
    /// <see cref="StampApproverNamesAsync"/> and <see cref="StampActionTypes"/> -- but the approver FILTER
    /// stays here as a real query predicate.
    /// </summary>
    private async Task<IQueryable<LeaveRequestReportRowDto>> BuildQueryAsync(GetLeaveRequestReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var approverCodes = ResolveApproverCodes(input.ApproverCode);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var requestStatus = input.RequestStatus;
        var leaveRequestTypes = GetLeaveRequestReportInput.LeaveRequestTypes;

        var query = _workflowRequestRepository.GetAll()
            // Legacy: five separate SysCreatedOn-range + Employee != null queries, unioned. Reproduced
            // here as one query over the RequestType set -- see the class remarks.
            .Where(r => r.EmployeeId != null
                        && leaveRequestTypes.Contains(r.RequestType)
                        && r.CreationTime.Date >= fromDate
                        && r.CreationTime.Date <= toDate)
            .WhereIf(requestStatus.HasValue, r => r.RequestStatus == requestStatus.Value)
            .WhereIf(approverCodes.Count > 0,
                r => r.WorkFlowApprovers.Any(ap => ap.Approver != null && approverCodes.Contains(ap.Approver.Code)))
            .WhereIf(employeeCodes.Count > 0, r => employeeCodes.Contains(r.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, r => rlsEmployeeIds.Contains(r.EmployeeId.Value));

        var today = Today;

        return query.Select(r => new LeaveRequestReportRowDto
        {
            Id = r.Id,
            RequestNumber = r.RequestNumber,
            CreatedDate = r.CreationTime,
            EmployeeCode = r.Employee.Code,
            EmployeeName = useArabicNames ? r.Employee.FullNameAr : r.Employee.FullNameEn,
            RequestType = r.RequestType,
            RequestStatus = r.RequestStatus,
            WorkflowStep = r.WorkflowStep,
            StalledDays = (int)EF.Functions.DateDiffDay((r.LastModificationTime ?? r.CreationTime).Date, today)
        });
    }

    /// <summary>Shared by the read and the export so the two cannot drift; see <see cref="EmployeeRequestsReportAppService.StampApproverNamesAsync"/> for the identical legacy quirk.</summary>
    private async Task StampApproverNamesAsync(List<LeaveRequestReportRowDto> rows)
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

    /// <summary>See <see cref="ActionTypeByRequestType"/> and the class remarks.</summary>
    private static void StampActionTypes(List<LeaveRequestReportRowDto> rows)
    {
        foreach (var row in rows)
        {
            row.ActionType = ActionTypeByRequestType.TryGetValue(row.RequestType, out var actionType)
                ? actionType
                : string.Empty;
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

    private static string ApproverParameterPart(GetLeaveRequestReportInput input)
    {
        return input.ApproverCode.IsNullOrWhiteSpace() ? null : "Approver Code : " + input.ApproverCode;
    }

    /// <summary>The exported column set, in legacy column order (OID omitted).</summary>
    private static IReadOnlyList<GridReportColumn<LeaveRequestReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<LeaveRequestReportRowDto>>
        {
            new("RequestNumber", r => r.RequestNumber),
            new("CreatedDate", r => r.CreatedDate),
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("Approver", r => r.ApproverNames),
            new("RequestType", r => r.RequestType),
            new("ActionType", r => r.ActionType),
            new("RequestStatus", r => r.RequestStatus.ToString()),
            new("WorkflowStep", r => r.WorkflowStep),
            new("StalledDays", r => r.StalledDays)
        };
    }
}
