using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Action Change analysis grid report (legacy
/// <c>uscRptEmployeeActionChange.ascx</c>).
///
/// ATYPICAL for this family: the legacy panel has ONLY dtFrom/dtTo/cmbPersonalAction -- there is no
/// <c>txtEmpCode</c> and no <c>cmbEmpStatus</c>. The inherited
/// <see cref="GridReportInputBase.EmployeeCode"/> / <see cref="GridReportInputBase.EmployeeStatus"/>
/// are therefore deliberately NOT applied by the app service, and the Angular screen hides both.
/// </summary>
public class GetEmployeeActionChangeReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbPersonalAction</c> (value = <c>PersonalAction.Code</c>, "1".."10"). Null/empty means
    /// "all actions" (uscRptEmployeeActionChange.ascx.cs:516-519).
    ///
    /// This filter also drives the legacy grid's COLUMN SET: for codes 7, 8, 1, 2, 5 and 6 the legacy
    /// DataTable omits the OriginalValue/AdjustedValue/FinalValue columns entirely (lines 498-508).
    /// A typed DTO cannot drop properties, so those three are always present here and the Angular grid
    /// hides them for exactly that set of codes.
    /// </summary>
    public string PersonalActionCode { get; set; }

    /// <summary>
    /// Legacy applied no sort -- rows are emitted in action order, then wage-type order within an
    /// action. Employee code then start date is used here so server-side paging is deterministic.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, StartDate ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeActionChange.ascx.cs:484-745</c>: OID, EmployeeCode, EmployeeName, ActionType,
/// StartDate, EndDate, Component, [OriginalValue, AdjustedValue, FinalValue], CreatedBy, CreatedOn,
/// ModifiedOn, ModifiedBy.
///
/// ATYPICAL, and the reason this report is not a single projection: an action does not map to one row.
/// The legacy code branches on <c>PersonalAction.Code</c> and, for CHANGE IN PAY / PROMOTION (codes 4
/// and 3), emits ONE ROW PER WAGE TYPE comparing the pay before the action with the pay in the action's
/// month. See the app service for the full branch table.
///
/// The three value columns are STRINGS because the legacy DataTable declared them untyped and then
/// stored amounts (doubles), job titles (strings) and contract end dates (formatted "dd/MM/yyyy"
/// strings) in them depending on the branch.
///
/// The inherited <c>Id</c> carries the legacy "OID" column, which stored
/// <c>action.Employee.Oid</c> -- the EMPLOYEE id, not the action id, and therefore NOT a unique row key
/// (lines 561, 590, ...). Reproduced as-is.
/// </summary>
public class EmployeeActionChangeReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "EmployeeCode" column -- <c>action.Employee.Code</c>.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>
    /// Legacy "EmployeeName" column -- always <c>FullNameEn</c>. Unlike every other report in this
    /// family this control never had an Arabic branch, so no Arabic name is resolved here either.
    /// </summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "ActionType" column -- always <c>PersonalAction.NameEn</c> (no Arabic branch).</summary>
    public string ActionTypeName { get; set; }

    public DateTime StartDate { get; set; }

    /// <summary>
    /// Legacy "EndDate" column. The legacy <c>EmployeeAction.EndDate</c> getter returns
    /// <c>StartDate</c>, so HCMv3 stores no EndDate and this always equals <see cref="StartDate"/>.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Legacy "Component" column: the wage type name for pay changes, otherwise a hardcoded English
    /// label ("Job Title", "Separation", "FnF", "Contract", "Joining", "Confirmation", "Suspension",
    /// "Transfer") -- or "Unknown" for a pay line whose wage type is missing.
    /// </summary>
    public string Component { get; set; }

    /// <summary>Legacy "OriginalValue" column. Null on branches that leave it unset. See class remarks.</summary>
    public string OriginalValue { get; set; }

    /// <summary>Legacy "AdjustedValue" column. For pay changes this is NEW minus OLD, not the new value.</summary>
    public string AdjustedValue { get; set; }

    /// <summary>Legacy "FinalValue" column.</summary>
    public string FinalValue { get; set; }

    /// <summary>Legacy <c>action.SysCreator</c> -- resolved from ABP's <c>CreatorUserId</c> here.</summary>
    public string CreatedBy { get; set; }

    /// <summary>Legacy <c>action.SysCreatedOn</c>. Null on the SEPARATION branch, which never set it.</summary>
    public DateTime? CreatedOn { get; set; }

    /// <summary>Legacy <c>action.SysModifiedOn</c>. Null on the SEPARATION branch and when never modified.</summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>Legacy <c>action.SysModifier</c> -- resolved from ABP's <c>LastModifierUserId</c> here.</summary>
    public string ModifiedBy { get; set; }
}
