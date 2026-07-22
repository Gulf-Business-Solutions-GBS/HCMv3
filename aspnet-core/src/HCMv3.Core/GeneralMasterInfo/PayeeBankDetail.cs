using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// PayeeBankDetail (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.PayeeBankDetail</c>).
/// Pure detail record: no Code/NameEn/NameAr. Owned by a <see cref="Bank"/> (legacy <c>[Association("PayeeBankDetail")]</c>)
/// with a second reference to another <see cref="Bank"/> (<see cref="PayeeBank"/>), plus <see cref="BankCode"/> and
/// a <see cref="BankFile"/> FK.
/// </summary>
public class PayeeBankDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? BankId { get; set; }

    public virtual Bank Bank { get; set; }

    public virtual int? PayeeBankId { get; set; }

    public virtual Bank PayeeBank { get; set; }

    [StringLength(256)]
    public virtual string BankCode { get; set; }

    public virtual int? BankFileId { get; set; }

    public virtual BankFile BankFile { get; set; }
}
