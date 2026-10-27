using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.Workflow;

namespace HCMv3.GridReports.Workflow.Dto;

/// <summary>
/// Filter input for the Employee Requests analysis grid report (legacy
/// <c>uscRptEmployeeRequests.ascx</c>). Legacy filtered on <c>dtFrom</c>/<c>dtTo</c> (against
/// <c>SysCreatedOn</c>), <c>cmbRequestStatus</c>, <c>txtApprover</c> and <c>txtEmpCode</c>. The first three
/// map onto <see cref="GridReportInputBase"/> as usual; legacy's <c>cmbEmpStatus</c> equivalent does not
/// exist on this screen at all, so the inherited <see cref="GridReportInputBase.EmployeeStatus"/> exists
/// but is simply never referenced here (same convention as
/// <c>OrganizationDocumentReportAppService</c>'s unused inherited fields).
/// </summary>
public class GetEmployeeRequestsReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbRequestStatus</c>.</summary>
    public RequestStatus? RequestStatus { get; set; }

    /// <summary>Legacy <c>txtApprover</c>: one approver code or several separated by commas.</summary>
    public string ApproverCode { get; set; }

    /// <summary>
    /// Legacy Table1() had no explicit ORDER BY (provider-dependent row order); Id ASC is chosen here for
    /// a stable, reproducible order.
    /// </summary>
    protected override string DefaultSorting => "Id ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in <c>uscRptEmployeeRequests.ascx.cs</c>
/// <c>Table1()</c> (lines 474-681): OID, RequestNumber, Created Date, Code, Name, Approver, RequestType,
/// RequestStatus, WorkflowStep, StalledDays.
///
/// NOT PORTED: legacy's <c>LeaveType</c> and <c>WorkflowComments</c> columns, and the <c>DelegatedEmployee</c>
/// filter/column. Legacy sourced all three off the CONCRETE workflow-request subclasses
/// (<c>LeaveRequest</c>, <c>AbsenceRequest</c>, ...) that <see cref="WorkflowRequest"/>'s own class remarks
/// explain were deliberately NOT ported -- this standalone port has no <c>AbsenceType</c>/
/// <c>WorkflowComments</c>/<c>DelegatedEmployee</c> fields on the generic request row to read them from.
/// This is an upstream scope decision inherited from the Workflow module port, not introduced here.
/// </summary>
public class EmployeeRequestsReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "RequestNumber" column. Kept as a string: the ported entity stores it as one.</summary>
    public string RequestNumber { get; set; }

    /// <summary>Legacy "Created Date" column -- <c>rec.SysCreatedOn</c>.</summary>
    public DateTime CreatedDate { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    /// <summary>
    /// Legacy "Approver" column: <c>string.Join(", ", WorkFlowApprover.Select(w =&gt;
    /// w.Approver.CodeWithFullNameEn))</c> -- always the English name regardless of aspect, a legacy quirk
    /// reproduced verbatim (see <c>EmployeeRequestsReportAppService.StampApproverNamesAsync</c>).
    /// </summary>
    public string ApproverNames { get; set; }

    public string RequestType { get; set; }

    public RequestStatus RequestStatus { get; set; }

    public int WorkflowStep { get; set; }

    /// <summary>
    /// Legacy: <c>DateTime.Now.Date.Subtract((SysModifiedOn != default ? SysModifiedOn : SysCreatedOn).Date).TotalDays</c>.
    /// </summary>
    public int StalledDays { get; set; }
}
