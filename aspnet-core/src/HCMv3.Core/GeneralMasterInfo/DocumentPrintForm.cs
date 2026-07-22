using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// DocumentPrintForm (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.DocumentPrintForm</c>).
/// Pure detail record: no Code/NameEn/NameAr. Owned by a <see cref="DocumentType"/>.
/// The legacy <c>ReportDataV2</c> field is not ported (DevExpress reporting infrastructure, no equivalent).
/// </summary>
public class DocumentPrintForm : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? DocumentTypeId { get; set; }

    public virtual DocumentType DocumentType { get; set; }
}
