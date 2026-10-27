using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GatePassManagement;

/// <summary>
/// Aggregated child of <see cref="GatePass"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GatePassManagement.GatePassPerson</c>), linked back via
/// <see cref="GatePassId"/>. Following the established Travel/TravelDetail precedent in this codebase, this
/// child is managed by its own app service rather than nested inside the <c>GatePass</c> create/edit DTO.
/// Legacy <c>PerformValidation</c> parity (no duplicate PersonType+Employee combination per gate pass, and
/// required PersonType/Employee/Vendor/PersonFullName) is ported in
/// <c>GatePassPersonsAppService</c>.
/// </summary>
public class GatePassPerson : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int GatePassId { get; set; }

    public virtual GatePass GatePass { get; set; }

    public virtual int PersonTypeId { get; set; }

    public virtual PersonType PersonType { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string PersonFullName { get; set; }

    [StringLength(64)]
    public virtual string PersonCivildID { get; set; }

    [StringLength(64)]
    public virtual string PersonPassportNumber { get; set; }

    public virtual int GatePassVendorId { get; set; }

    public virtual GatePassVendor Vendor { get; set; }
}
