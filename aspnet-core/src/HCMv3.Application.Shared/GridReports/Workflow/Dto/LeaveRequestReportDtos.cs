using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.Workflow;

namespace HCMv3.GridReports.Workflow.Dto;

/// <summary>
/// Filter input for the Leave Request analysis grid report (legacy <c>uscRptLeaveRequest.ascx</c>).
/// Legacy unioned five concrete workflow-request subclasses (<c>LeaveRequest</c>,
/// <c>LeaveAmendmentRequest</c>, <c>LeaveCancellationRequest</c>, <c>ReturnFromLeaveRequest</c>,
/// <c>AbsenceRequest</c>) -- see <see cref="LeaveRequestReportRowDto"/> remarks for how this port
/// identifies the same five categories on the standalone <see cref="WorkflowRequest"/> table. Filters
/// mirror <see cref="GetEmployeeRequestsReportInput"/>: legacy has no <c>cmbEmpStatus</c> on this screen
/// either, so the inherited <see cref="GridReportInputBase.EmployeeStatus"/> is unused.
/// </summary>
public class GetLeaveRequestReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy request-type discriminator strings for the five unioned subclasses (Table1() lines
    /// 543-566). <see cref="WorkflowRequest.RequestType"/> is a plain string carrying exactly this class
    /// name (see its class remarks), so filtering by this set reproduces the legacy union.
    /// </summary>
    public static readonly IReadOnlyList<string> LeaveRequestTypes = new[]
    {
        "LeaveRequest",
        "LeaveAmendmentRequest",
        "LeaveCancellationRequest",
        "ReturnFromLeaveRequest",
        "AbsenceRequest"
    };

    /// <summary>Legacy <c>cmbRequestStatus</c>.</summary>
    public RequestStatus? RequestStatus { get; set; }

    /// <summary>Legacy <c>txtApprover</c>: one approver code or several separated by commas.</summary>
    public string ApproverCode { get; set; }

    /// <summary>Legacy Table1() had no explicit ORDER BY; Id ASC gives a stable, reproducible order.</summary>
    protected override string DefaultSorting => "Id ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in <c>uscRptLeaveRequest.ascx.cs</c>
/// <c>Table1()</c> (lines 475-778): OID, RequestNumber, Created Date, Code, Name, Approver, RequestType,
/// ActionType, RequestStatus, LeaveType, StartDate, EndDate, StartTime, EndTime, WorkflowStep,
/// DelegatedEmployee, StalledDays, WorkflowComments.
///
/// SCOPE NOTE (see <see cref="GetLeaveRequestReportInput"/>): legacy unioned five concrete workflow-request
/// subclasses. HCMv3's <see cref="WorkflowRequest"/> is a standalone entity without that subclass
/// hierarchy (its own class remarks explain why -- an upstream scope decision from the Workflow module
/// port, not introduced here), so this row instead filters the single table by
/// <see cref="WorkflowRequest.RequestType"/>. <see cref="ActionType"/> is DERIVED from that same string
/// (RequestType "LeaveRequest" =&gt; Create, "LeaveAmendmentRequest" =&gt; Amendment,
/// "LeaveCancellationRequest" =&gt; Cancellation, "ReturnFromLeaveRequest" =&gt; ReturnFromLeave; legacy's
/// "AbsenceRequest" case read the concrete row's own <c>ActionType</c> field, which does not exist here and
/// is left blank).
///
/// NOT PORTED for the same reason: <c>LeaveType</c> (<c>AbsenceType.Name</c>), <c>StartDate</c>,
/// <c>EndDate</c>, <c>StartTime</c>, <c>EndTime</c>, <c>WorkflowComments</c> and the
/// <c>DelegatedEmployee</c> filter/column -- all sourced off the unported concrete subclasses.
/// </summary>
public class LeaveRequestReportRowDto : EntityDto<int>
{
    public string RequestNumber { get; set; }

    public DateTime CreatedDate { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    /// <summary>Same legacy quirk as <c>EmployeeRequestsReportRowDto.ApproverNames</c>: always English.</summary>
    public string ApproverNames { get; set; }

    public string RequestType { get; set; }

    /// <summary>Derived from <see cref="RequestType"/> -- see the class remarks.</summary>
    public string ActionType { get; set; }

    public RequestStatus RequestStatus { get; set; }

    public int WorkflowStep { get; set; }

    public int StalledDays { get; set; }
}
