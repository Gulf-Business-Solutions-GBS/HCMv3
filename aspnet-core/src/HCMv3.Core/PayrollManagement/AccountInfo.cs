using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// AccountInfo master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.AccountInfo</c>).
///
/// DELIBERATE SIMPLIFICATION: the legacy <c>CodeWithNameEn</c> is a SQL-function-backed
/// <c>PersistentAlias</c> computed column with no clean EF Core equivalent. It is NOT ported as a stored
/// property — a combined display label can be computed client-side or in DTO mapping
/// (<c>$"{Code}-{NameEn}"</c>) if needed. The legacy <c>IExcelImport</c> marker is an XAF UI concept and is
/// not ported.
/// </summary>
public class AccountInfo : FullAuditedEntity, IMustHaveTenant
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

    public virtual int AccountTypeId { get; set; }

    public virtual AccountType AccountType { get; set; }
}
