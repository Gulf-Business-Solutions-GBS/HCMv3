using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Asset master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.Asset</c>).
/// Extends the Code/NameEn/NameAr shape with vehicle/equipment attributes and an <see cref="AssetType"/> FK,
/// so it can't derive from <see cref="SimpleLookupEntity"/> (see <see cref="AccessUnit"/> for the same
/// bespoke pattern). The legacy <c>Earning</c>/<c>Deduction</c> <c>WageType</c> FKs are ported as
/// <see cref="EarningWageTypeId"/>/<see cref="DeductionWageTypeId"/>. The legacy <c>DocAttachment</c> FK
/// is replaced by the generic per-tenant attachment store (attachment owner type <c>"Asset"</c>), so it is
/// not a column here.
/// </summary>
public class Asset : FullAuditedEntity, IMustHaveTenant
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

    [StringLength(256)]
    public virtual string Brand { get; set; }

    [StringLength(256)]
    public virtual string Model { get; set; }

    [StringLength(64)]
    public virtual string Color { get; set; }

    [StringLength(64)]
    public virtual string VIN_Number { get; set; }

    [StringLength(64)]
    public virtual string Serial { get; set; }

    [StringLength(64)]
    public virtual string PlateNumber { get; set; }

    [StringLength(64)]
    public virtual string Barcode { get; set; }

    public virtual int Year { get; set; }

    public virtual DateTime ExpiryDate { get; set; }

    public virtual double Value { get; set; }

    public virtual int? AssetTypeId { get; set; }

    public virtual AssetType AssetType { get; set; }

    public virtual AssetStatus Status { get; set; }

    public virtual AssetAllocationStatus AssetAllocationStatus { get; set; }

    /// <summary>Legacy <c>Asset.Earning</c>: the wage type paid for this asset. Optional.</summary>
    public virtual int? EarningWageTypeId { get; set; }

    public virtual WageType EarningWageType { get; set; }

    /// <summary>Legacy <c>Asset.Deduction</c>: the wage type deducted for this asset. Optional.</summary>
    public virtual int? DeductionWageTypeId { get; set; }

    public virtual WageType DeductionWageType { get; set; }
}
