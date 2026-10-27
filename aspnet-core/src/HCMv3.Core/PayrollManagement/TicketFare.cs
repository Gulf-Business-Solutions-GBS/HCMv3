using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Effective-dated rate matrix cell for (TicketRoute, TicketPaxType, TicketClass) (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.TicketFare</c>). The EndDate auto-chaining (legacy
/// <c>SetEndDate</c>) is ported in <c>TicketFaresAppService</c>, mirroring
/// <c>OrgUnitWageAccGroupsAppService</c>'s pattern (scoped by (TicketRouteId, TicketPaxTypeId,
/// TicketClassId) instead of OrganizationUnitId). The legacy <c>IExcelImport</c> marker is an XAF UI
/// concept and is not ported.
/// </summary>
public class TicketFare : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int TicketRouteId { get; set; }

    public virtual TicketRoute TicketRoute { get; set; }

    public virtual int TicketPaxTypeId { get; set; }

    public virtual TicketPaxType TicketPaxType { get; set; }

    public virtual int TicketClassId { get; set; }

    public virtual TicketClass TicketClass { get; set; }

    /// <summary>"Effective Date" in the legacy UI.</summary>
    public virtual DateTime StartDate { get; set; }

    /// <summary>
    /// System-managed (auto-chained by <c>TicketFaresAppService</c>); defaults to 9999-12-31 for the
    /// latest record.
    /// </summary>
    public virtual DateTime EndDate { get; set; }

    public virtual double OneWayTicketFare { get; set; }

    public virtual double ReturnTicketFare { get; set; }
}
