using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement;

/// <summary>
/// Employee membership row of a <see cref="RosterGroup"/>, for a date range (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.RosterGroupDetail</c>), listed separately and linked back
/// via <see cref="RosterGroupId"/>.
///
/// The legacy <c>PerformValidation</c> only checks RosterGroup/StartDate/EndDate presence and
/// EndDate &gt;= StartDate — no EndDate-auto-chaining logic exists in the legacy file, so none is ported
/// here. An overlap check (same RosterGroup + Employee, overlapping date range) is added, following the
/// same pattern used elsewhere in this codebase (e.g. EmployeeContractsAppService) for date-ranged child
/// entities, since the legacy behavior for this specific scenario isn't otherwise defined.
/// </summary>
public class RosterGroupDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int RosterGroupId { get; set; }

    public virtual RosterGroup RosterGroup { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
