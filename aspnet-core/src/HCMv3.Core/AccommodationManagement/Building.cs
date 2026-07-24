using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.AccommodationManagement;

/// <summary>
/// Building master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.AccommodationManagement.Building</c>). Code/NameEn/NameAr/Comments
/// lookup with a <see cref="LocationId"/> FK. Its <c>Contract</c> aggregated child collection is ported as
/// <see cref="BuildingContract"/>, listed/edited separately (same pattern as
/// <c>PayrollManagement.AbsenceDeductionRule</c>/<c>AbsenceDedRuleDetail</c>). The legacy
/// <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class Building : FullAuditedEntity, IMustHaveTenant
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

    public virtual int LocationId { get; set; }

    public virtual Location Location { get; set; }

    public virtual string Comments { get; set; }
}
