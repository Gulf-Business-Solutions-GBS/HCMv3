using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Administration.Dto;

/// <summary>
/// Filter input for the Audit Report analysis grid report (legacy <c>uscRptAuditReport.ascx</c>).
///
/// MODEL ADAPTATION: legacy's row source was raw SQL over DevExpress XPO's field-level audit tables
/// (<c>AuditDataItemPersistent</c>/<c>AuditedObjectWeakReference</c>) UNION'd with a bespoke, never-ported
/// <c>AuditLog</c>/<c>AuditLogDetail</c> pair. HCMv3 has no XPO audit trail and never ported that bespoke
/// pair; what it DOES have is ABP's own built-in Entity History
/// (<c>Abp.EntityHistory.EntityChangeSet</c>/<c>EntityChange</c>/<c>EntityPropertyChange</c>), which is
/// FIELD-LEVEL (PropertyName/OriginalValue/NewValue) and already wired into HCMv3 (see
/// <c>EntityChangeAppService</c>). That is the closest available successor and is used here as the sole
/// data source. The never-ported <c>AuditLog</c>/<c>AuditLogDetail</c> source has no HCMv3 equivalent --
/// this is a SCOPE GAP, not a data-loss defect (that table was a parallel, redundant audit mechanism in
/// legacy; nothing in HCMv3 ever wrote to an equivalent of it).
///
/// Legacy required BOTH dates to be supplied (uscRptAuditReport.ascx.cs: <c>Table1()</c> returns an empty
/// table when either is <c>default(DateTime)</c>) and did NOT default blank dates to today the way most
/// other reports in this family do (uscRptAuditReport.ascx.cs:132-143 explicitly comments out the
/// "default to Now" lines the other reports have). <see cref="GridReportInputBase"/>'s
/// <c>ResolveFromDate</c>/<c>ResolveToDate</c> still default null to today (there is no "unbounded" concept
/// in the shared base), so this behaviour is reproduced by requiring the Angular screen to always send both
/// dates (it always does -- both date pickers default to today) rather than by a null special-case here.
/// </summary>
public class GetAuditReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbUserName</c>. Blank means "all users".</summary>
    public string AuditUserName { get; set; }

    /// <summary>
    /// Legacy <c>cmbEntityObjectName</c> (a distinct list of entity type short names pulled from the audit
    /// tables at runtime). Here matched as a CONTAINS against <c>EntityTypeFullName</c> since HCMv3 has no
    /// equivalent distinct-name lookup query.
    /// </summary>
    public string EntityObjectName { get; set; }

    protected override string DefaultSorting => "ActionDate ASC";
}

/// <summary>
/// One grid row -- one property change, exactly the grain of legacy's <c>AuditDataItemPersistent</c> rows.
/// LEGACY COLUMN SET: UserName, ActionDate, ActionTime, ActionType, EntityObject, PropertyName, OldValue,
/// NewValue (legacy also had a Description column fed only by the never-ported second union branch; not
/// carried here).
/// </summary>
public class AuditReportRowDto : EntityDto<long>
{
    public string UserName { get; set; }

    public DateTime ActionDate { get; set; }

    /// <summary>Legacy formatted this as a separate "hh:mm tt" string column; kept as text here too.</summary>
    public string ActionTime { get; set; }

    /// <summary>Legacy's Insert/Update/Delete; here <c>EntityChange.ChangeType</c> (Created/Updated/Deleted).</summary>
    public string ActionType { get; set; }

    /// <summary>Legacy's short entity type name; here the last segment of <c>EntityTypeFullName</c>.</summary>
    public string EntityObject { get; set; }

    public string PropertyName { get; set; }

    public string OldValue { get; set; }

    public string NewValue { get; set; }
}
