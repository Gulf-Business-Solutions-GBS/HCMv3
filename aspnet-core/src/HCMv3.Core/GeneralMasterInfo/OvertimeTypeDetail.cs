using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// OvertimeTypeDetail (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.OvertimeTypeDetail</c>).
/// Pure detail record: no Code/NameEn/NameAr. Owned by an <see cref="OvertimeType"/>.
/// </summary>
public class OvertimeTypeDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? OvertimeTypeId { get; set; }

    public virtual OvertimeType OvertimeType { get; set; }

    public virtual System.DateTime StartDate { get; set; }

    public virtual System.DateTime EndDate { get; set; }

    public virtual OvertimeRateType OvertimeRateType { get; set; }

    public virtual double Rate { get; set; }
}
