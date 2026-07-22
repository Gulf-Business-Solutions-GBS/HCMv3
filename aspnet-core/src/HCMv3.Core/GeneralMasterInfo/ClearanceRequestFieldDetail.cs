using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// ClearanceRequestFieldDetail (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.ClearanceRequestFieldDetail</c>).
/// Pure detail record: no Code/NameEn/NameAr. Owned by a <see cref="ClearanceRequestField"/>.
/// The legacy <c>WorkflowRole</c> FK is not ported (Workflow module out of scope).
/// </summary>
public class ClearanceRequestFieldDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? ClearanceRequestFieldId { get; set; }

    public virtual ClearanceRequestField ClearanceRequestField { get; set; }
}
