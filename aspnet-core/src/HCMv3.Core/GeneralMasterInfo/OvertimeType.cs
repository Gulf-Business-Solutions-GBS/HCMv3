using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// OvertimeType master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.OvertimeType</c>).
/// Extends the Code/NameEn/NameAr shape with <see cref="OvertimeRate"/>, so it can't derive from
/// <see cref="SimpleLookupEntity"/> (see <see cref="City"/> for the same bespoke pattern).
/// The legacy <c>WageType</c> FK is not ported (Payroll module out of scope); the aggregated <c>Detail</c>
/// collection is ported separately as its own entity (<see cref="OvertimeTypeDetail"/>).
/// </summary>
public class OvertimeType : FullAuditedEntity, IMustHaveTenant
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

    public virtual double OvertimeRate { get; set; }
}
