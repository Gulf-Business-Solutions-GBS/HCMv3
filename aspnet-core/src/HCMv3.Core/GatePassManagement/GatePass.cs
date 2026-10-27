using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GatePassManagement;

/// <summary>
/// Gate pass header (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GatePassManagement.GatePass</c>).
///
/// DELIBERATE SIMPLIFICATIONS:
/// <list type="bullet">
/// <item><description>Legacy <c>Location</c> (<see cref="WorkLocation"/>), <c>RequestedBy</c>
/// (<see cref="Employee"/>) and <c>Status</c> (<see cref="GatePassStatus"/>) were NOT included in the legacy
/// <c>PerformValidation</c> required-field checks (only GatePassType, Movement, IssueDate and ValidUpto were
/// validated), so they are ported as optional (nullable) FKs here, matching the legacy's actual enforced
/// behavior rather than what might be assumed from the UI.</description></item>
/// <item><description><c>GatePassNumber</c> is a plain persisted <c>int</c> field in the legacy object -- no
/// auto-increment/sequence logic was found for it (unlike the Code fields on the lookup masters in this
/// module, which use the shared <c>GetMaxCode</c>-equivalent auto-numbering), so it is ported as a
/// plain client-supplied field.</description></item>
/// <item><description>The legacy aggregated <c>XPCollection</c> child associations
/// (<see cref="GatePassPerson"/>/<see cref="GatePassMaterial"/>/<see cref="GatePassVehicle"/>) are not
/// declared as navigation collections here -- each child entity carries its own <c>GatePassId</c> FK and is
/// managed by its own app service, following the established Travel/TravelDetail precedent in this
/// codebase.</description></item>
/// </list>
/// </summary>
public class GatePass : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int GatePassNumber { get; set; }

    public virtual int GatePassTypeId { get; set; }

    public virtual GatePassType GatePassType { get; set; }

    public virtual int GatePassMovementId { get; set; }

    public virtual GatePassMovement Movement { get; set; }

    public virtual int? LocationId { get; set; }

    public virtual WorkLocation Location { get; set; }

    public virtual DateTime IssueDate { get; set; }

    public virtual DateTime ValidUpto { get; set; }

    public virtual DateTime StartTime { get; set; }

    public virtual DateTime EndTime { get; set; }

    public virtual int? RequestedByEmployeeId { get; set; }

    public virtual Employee RequestedBy { get; set; }

    public virtual int? GatePassStatusId { get; set; }

    public virtual GatePassStatus Status { get; set; }

    public virtual string Comments { get; set; }
}
