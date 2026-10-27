using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Date-range air-ticket entitlement for an <see cref="EmployeeSubGroup"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.EmployeeSubGroupTicket</c>), listed separately and
/// linked back via <see cref="EmployeeSubGroupId"/>. <see cref="TicketTypeId"/> is a nullable FK to the
/// not-yet-ported <c>TicketType</c> lookup (a later wave).
/// </summary>
public class EmployeeSubGroupTicket : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeSubGroupId { get; set; }

    public virtual EmployeeSubGroup EmployeeSubGroup { get; set; }

    public virtual int? TicketTypeId { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual int TicketCount { get; set; }

    public virtual double MaxAmount { get; set; }

    public virtual double FixAmount { get; set; }

    public virtual FareType FareType { get; set; }

    public virtual double AgeLimit { get; set; }

    public virtual AccrualPeriod AccrualPeriod { get; set; }
}
