using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Rejoin analysis grid report (legacy
/// <c>uscRptEmployeeRejoin.ascx</c>). Legacy exposes exactly the four common filters and nothing
/// report-specific.
/// </summary>
public class GetEmployeeRejoinReportInput : GridReportInputBase
{
    /// <summary>Legacy applied no default sort; employee code ascending keeps paging deterministic.</summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeRejoin.ascx.cs:462-629</c>: OID, Code, Name, StartDate, OrgLevel1..7,
/// CreateNewEmployeeCode, RejoinWithPrevLeaveAccrual, RejoinWithPrevIndemnityAccrual,
/// RejoinWithPrevTicketAccrual, RejoinWithPrevLoan, NewEmpCode.
///
/// DEVIATIONS forced by the HCMv3 entity model (never by choice):
/// <list type="bullet">
/// <item><description>OrgLevel1..7 collapse into the single <see cref="OrganizationUnitName"/> column --
/// HCMv3's <c>EmployeeOrgAssignment</c> replaced those seven FKs with one FK to ABP's hierarchical
/// <c>OrganizationUnit</c> tree.</description></item>
/// <item><description>Legacy <c>StartDate</c> is HCMv3's <c>EmployeeRejoin.RejoinDate</c> (renamed in the
/// entity port); the column keeps the legacy name/meaning.</description></item>
/// <item><description>The legacy boolean columns <c>RejoinWithPrev*Accrual</c>/<c>RejoinWithPrevLoan</c> are
/// HCMv3's <c>CarryForward*Balance</c> flags, same semantics under a new name.</description></item>
/// <item><description>The legacy <c>CreateNewEmployeeCode</c> column is DROPPED: <c>EmployeeRejoin</c> in
/// HCMv3 has no such field, and there is no other source for it.</description></item>
/// </list>
/// </summary>
public class EmployeeRejoinReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    /// <summary>Legacy "StartDate" column -- HCMv3 <c>EmployeeRejoin.RejoinDate</c>.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Replaces the legacy OrgLevel1..7 block. Resolved from the assignment current TODAY
    /// (legacy line 549 used <c>DateTime.Now</c>, not the report dates).
    /// </summary>
    public string OrganizationUnitName { get; set; }

    /// <summary>Legacy "RejoinWithPrevLeaveAccrual" -- HCMv3 <c>CarryForwardLeaveBalance</c>.</summary>
    public bool RejoinWithPrevLeaveAccrual { get; set; }

    /// <summary>Legacy "RejoinWithPrevIndemnityAccrual" -- HCMv3 <c>CarryForwardIndemnityBalance</c>.</summary>
    public bool RejoinWithPrevIndemnityAccrual { get; set; }

    /// <summary>Legacy "RejoinWithPrevTicketAccrual" -- HCMv3 <c>CarryForwardTicketBalance</c>.</summary>
    public bool RejoinWithPrevTicketAccrual { get; set; }

    /// <summary>Legacy "RejoinWithPrevLoan" -- HCMv3 <c>CarryForwardLoanBalance</c>.</summary>
    public bool RejoinWithPrevLoan { get; set; }

    /// <summary>
    /// Legacy "NewEmpCode" column (line 587): the code of the employee record whose
    /// <c>PreviousEmpCode</c> points back at this row's employee, i.e. the new record created for the
    /// rejoin. Null when the rejoin reused the same employee code.
    /// </summary>
    public string NewEmployeeCode { get; set; }
}
