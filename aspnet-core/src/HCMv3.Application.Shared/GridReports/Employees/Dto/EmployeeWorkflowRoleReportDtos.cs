using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Workflow Role analysis grid report (legacy
/// <c>uscRptEmployeeWorkflowRole.ascx</c>).
///
/// Legacy exposes exactly the four common filters and nothing report-specific, so this class adds no
/// fields of its own. NOTE the legacy control names its status combo <c>cmbRequestStatus</c>
/// (uscRptEmployeeWorkflowRole.ascx.cs:479) but binds it to <c>EmployeeStatus</c> and filters
/// <c>r.Employee.Status</c> with it (line 531) -- it is the ordinary employee-status filter under a
/// misleading control name, so it maps onto <see cref="GridReportInputBase.EmployeeStatus"/>.
/// </summary>
public class GetEmployeeWorkflowRoleReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy applied no explicit sort (it enumerated the LINQ result in provider order,
    /// uscRptEmployeeWorkflowRole.ascx.cs:556). Employee code ascending is used so server-side paging is
    /// deterministic.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeWorkflowRole.ascx.cs:494-570</c> column-for-column:
/// OID, Code, Name, WorkflowRole, StartDate, EndDate.
/// </summary>
public class EmployeeWorkflowRoleReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "Code" column -- <c>rec.Employee.Code</c> (line 543).</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Name" column -- <c>FullNameAr</c> when Arabic, else <c>FullNameEn</c> (line 544).</summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "WorkflowRole" column -- empty string (not null) when the row has no role (line 545).</summary>
    public string WorkflowRoleName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
