using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Work location master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.OrganizationManagement.WorkLocation</c>).
/// Extends the Code/NameEn/NameAr shape with geo/location fields, so it can't derive from
/// <see cref="SimpleLookupEntity"/> (see <see cref="City"/> for the same bespoke pattern).
/// The legacy <c>Sponsor</c> FK is deferred (GovtRelationsManagement module not yet ported).
/// </summary>
public class WorkLocation : FullAuditedEntity, IMustHaveTenant, IBilingualLookup
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    public virtual double Latitude { get; set; }

    public virtual double Longitude { get; set; }

    public virtual double GeoRange { get; set; }

    [StringLength(1000)]
    public virtual string Address { get; set; }

    [StringLength(2000)]
    public virtual string GeoFenceCoordinates { get; set; }

    public virtual bool IsActive { get; set; }
}
