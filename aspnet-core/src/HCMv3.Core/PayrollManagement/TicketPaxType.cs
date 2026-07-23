using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// TicketPaxType master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.TicketPaxType</c>). Extends the Code/NameEn/NameAr
/// shape with <see cref="AgeFrom"/>/<see cref="AgeTo"/> age-bucket bounds (used to bucket dependants by
/// age for ticket fare lookups), so it can't derive from <see cref="SimpleLookupEntity"/> -- same bespoke
/// pattern as <see cref="Nationality"/>. The legacy <c>IExcelImport</c> marker is an XAF UI concept and is
/// not ported.
/// </summary>
public class TicketPaxType : FullAuditedEntity, IMustHaveTenant
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

    public virtual double AgeFrom { get; set; }

    public virtual double AgeTo { get; set; }
}
