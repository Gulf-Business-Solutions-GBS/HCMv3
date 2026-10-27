using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.TimeManagement;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Time Register Approval grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscTimeRegisterApproval.ascx</c>).
///
/// READ-ONLY DESPITE THE NAME: the legacy control has no save/commit path at all -- its only methods are
/// <c>Table1()</c>, the DevExpress callback plumbing and the two export buttons
/// (uscTimeRegisterApproval.ascx.cs:409/701/705). "Approval" refers to the APPROVED columns on
/// <c>TimeRegister</c> (<c>ApprovedAbsentDay</c>, <c>ApprovedDedLateInEarlyOutMins</c>), which the
/// report shows side by side with the raw ones so a reviewer can compare. It therefore fits the
/// read-only grid-report template unchanged.
///
/// FILTER PANEL (designer lines 61-94): <c>dtFrom</c>, <c>dtTo</c>, <c>cmbPayrollArea</c>,
/// <c>txtEmpCode</c>. There is NO employee-status combo, so the inherited
/// <see cref="GridReportInputBase.EmployeeStatus"/> is unused and the Angular screen hides it.
/// </summary>
public class GetTimeRegisterApprovalReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbPayrollArea</c> (value field = <c>PayrollArea.Code</c>, line 197). Null/empty means
    /// "all payroll areas". Legacy matched employees having an org assignment covering TODAY -- not the
    /// report window -- whose payroll area has this code (lines 419-425).
    /// </summary>
    public string PayrollAreaCode { get; set; }

    /// <summary>
    /// Legacy left the grid unsorted (rows arrived in database order). Employee code then day date is the
    /// natural reading order for a day-grain sheet and is deterministic, which server-side paging requires.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, DayDate ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (uscTimeRegisterApproval.ascx.cs:409-505).
///
/// Legacy column set, in order: <c>Oid</c>, <c>EmployeeCode</c>, <c>DayDate</c>, <c>WeekDay</c>,
/// <c>LeaveDay</c>, <c>LeaveType</c>, <c>DedLateInEarlyOutMinutes</c>, <c>AbsentDay</c>,
/// <c>ApprovedDedLateInEarlyOutMinutes</c>, <c>ApprovedAbsentDay</c>, <c>Status</c>, <c>Remarks</c>.
/// Note there is deliberately NO employee-name column -- legacy showed the code only.
/// </summary>
public class TimeRegisterApprovalReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "EmployeeCode" column -- <c>r.Employee.Code</c>.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "DayDate" column.</summary>
    public DateTime DayDate { get; set; }

    /// <summary>
    /// Legacy "WeekDay" column -- <c>r.DayDate.DayOfWeek.ToString()</c>, i.e. the INVARIANT English day
    /// name ("Monday"), never localized. Reproduced verbatim.
    /// </summary>
    public string WeekDay { get; set; }

    /// <summary>Legacy "LeaveDay" column. Declared <c>typeof(int)</c> on the legacy DataTable but fed
    /// from the <c>double</c> <c>TimeRegister.LeaveDay</c>; kept as <c>double</c> here so half days are
    /// not silently truncated.</summary>
    public double LeaveDay { get; set; }

    /// <summary>
    /// Legacy "LeaveType" column -- <c>r.LeaveType.NameEn</c>, ALWAYS the English name even in Arabic
    /// (line 437). Empty string, not null, when the row has no leave type.
    ///
    /// MODEL NOTE: HCMv3's <c>TimeRegister.LeaveTypeId</c> is a bare id with no navigation
    /// property, so the name is resolved through a correlated sub-select on <c>AbsenceType</c>.
    /// </summary>
    public string LeaveType { get; set; }

    /// <summary>Legacy "DedLateInEarlyOutMinutes" column -- <c>r.DedLateInEarlyOutMins</c>.</summary>
    public double DedLateInEarlyOutMinutes { get; set; }

    /// <summary>Legacy "AbsentDay" column.</summary>
    public double AbsentDay { get; set; }

    /// <summary>Legacy "ApprovedDedLateInEarlyOutMinutes" column -- <c>r.ApprovedDedLateInEarlyOutMins</c>.</summary>
    public double ApprovedDedLateInEarlyOutMinutes { get; set; }

    /// <summary>Legacy "ApprovedAbsentDay" column.</summary>
    public double ApprovedAbsentDay { get; set; }

    /// <summary>Legacy "Status" column -- the <c>TimeRegisterStatus</c> enum.</summary>
    public TimeRegisterStatus Status { get; set; }

    /// <summary>Legacy "Remarks" column.</summary>
    public string Remarks { get; set; }
}
