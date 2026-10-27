using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Administration.Dto;

/// <summary>
/// Filter input for the Execution Log analysis grid report (legacy
/// <c>uscRptExecutionLogReport.ascx</c>). Row source is the SAME <see cref="SystemSecurity.ExecutionLog"/>
/// entity <see cref="GridReportAppServiceBase.WriteExecutionLogAsync"/> writes to for every OTHER report in
/// this family -- this screen is simply a reader over that table, exactly as legacy was.
///
/// Legacy required BOTH dates (returns an empty table otherwise, uscRptExecutionLogReport.ascx.cs) rather
/// than defaulting blanks to today; see <c>GetAuditReportInput</c> remarks for why that is not
/// special-cased here (the Angular screen always sends both dates).
/// </summary>
public class GetExecutionLogReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbUserName</c>. Blank means "all users".</summary>
    public string LogUserName { get; set; }

    protected override string DefaultSorting => "ActionDateTime ASC";
}

/// <summary>
/// One grid row. LEGACY COLUMN SET: OID, UserName, ActionDate, ActionTime, Action, Module, ScreenCaption,
/// Parameters, ExecutionStatus, TotalRecords, Successfull, Failed.
/// </summary>
public class ExecutionLogReportRowDto : EntityDto<int>
{
    public string UserName { get; set; }

    public DateTime ActionDate { get; set; }

    /// <summary>Legacy formatted this as a separate "hh:mm tt"-style text column.</summary>
    public string ActionTime { get; set; }

    /// <summary>
    /// Raw timestamp carried through the query so ActionDate/ActionTime can be derived in-memory after
    /// materialization -- <c>DateTime.ToString(format)</c> does not translate to SQL.
    /// </summary>
    public DateTime ActionDateTime { get; set; }

    public SystemSecurity.ExecutionLogAction Action { get; set; }

    public SystemSecurity.ExecutionLogModule Module { get; set; }

    public string ScreenCaption { get; set; }

    public string Parameters { get; set; }

    public SystemSecurity.ExecutionLogStatus ExecutionStatus { get; set; }

    public int TotalRecords { get; set; }

    public int Successfull { get; set; }

    public int Failed { get; set; }
}
