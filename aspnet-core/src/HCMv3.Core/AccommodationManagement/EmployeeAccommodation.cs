using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.AccommodationManagement;

/// <summary>
/// Employee accommodation assignment header (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.AccommodationManagement.EmployeeAccommodation</c>). Its <c>Services</c>
/// aggregated child collection is ported as <see cref="AccommodationServices"/>, listed/edited separately
/// (same pattern as <c>PayrollManagement.AbsenceDeductionRule</c>/<c>AbsenceDedRuleDetail</c>). The legacy
/// <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class EmployeeAccommodation : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual int AccommodationTypeId { get; set; }

    public virtual AccommodationType AccommodationType { get; set; }

    public virtual double DeductionAmount { get; set; }

    public virtual int LocationId { get; set; }

    public virtual Location Location { get; set; }

    public virtual int BuildingId { get; set; }

    public virtual Building Building { get; set; }

    public virtual int FloorId { get; set; }

    public virtual Floor Floor { get; set; }

    public virtual int FlatId { get; set; }

    public virtual Flat Flat { get; set; }

    public virtual int RoomId { get; set; }

    public virtual Room Room { get; set; }

    public virtual int BedId { get; set; }

    public virtual Bed Bed { get; set; }

    public virtual string Comments { get; set; }
}
