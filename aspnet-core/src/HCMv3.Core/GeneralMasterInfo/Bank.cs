using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Bank master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.Bank</c>).
/// Extends the Code/NameEn/NameAr shape with <see cref="ExternalCode"/>/<see cref="SWIFTCode"/>/<see cref="CompanyCode"/>
/// and a <see cref="BankFile"/> FK, so it can't derive from <see cref="SimpleLookupEntity"/>
/// (see <see cref="City"/> for the same bespoke pattern). The legacy aggregated <c>PayeeBankDetail</c>
/// collection is ported separately as its own entity (<see cref="PayeeBankDetail"/>).
/// </summary>
public class Bank : FullAuditedEntity, IMustHaveTenant, IBilingualLookup
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
    public virtual string ExternalCode { get; set; }

    [StringLength(256)]
    public virtual string SWIFTCode { get; set; }

    [StringLength(256)]
    public virtual string CompanyCode { get; set; }

    public virtual int? BankFileId { get; set; }

    public virtual BankFile BankFile { get; set; }
}
