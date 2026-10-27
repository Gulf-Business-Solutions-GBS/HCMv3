using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee ticket-route (destination) assignment record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeTicketRoute</c>, Wave 3 deferred bucket).
///
/// The legacy overlap-validation against other <c>EmployeeTicketRoute</c> rows for the same employee is
/// ported at the app-service level, not on the entity (same approach as other Wave 1/2/3 ledger entities).
/// </summary>
public class EmployeeTicketRoute : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int TicketRouteId { get; set; }

    public virtual TicketRoute TicketRoute { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
