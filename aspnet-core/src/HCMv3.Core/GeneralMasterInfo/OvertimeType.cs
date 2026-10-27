using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// OvertimeType master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.OvertimeType</c>).
/// Extends the Code/NameEn/NameAr shape with <see cref="OvertimeRate"/>, so it can't derive from
/// <see cref="SimpleLookupEntity"/> (see <see cref="City"/> for the same bespoke pattern).
/// The aggregated <c>Detail</c> collection is ported separately as its own entity
/// (<see cref="OvertimeTypeDetail"/>).
///
/// WAVE P8: the legacy <c>WageType</c> FK -- originally dropped as "Payroll module out of scope" -- is
/// RESTORED as <see cref="WageTypeId"/>. Both the Project Cost Engine
/// (<c>uscProjectCosting.ascx.cs</c> lines 727-729 / 809-818) and the Payroll Engine
/// (<c>Program.cs</c> line 1932) identify overtime wage types through it; without it the overtime costing
/// branch never fires and money differs from legacy.
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

    /// <summary>
    /// Legacy <c>OvertimeType.WageType</c>. Nullable: legacy's own queries all guard with
    /// <c>Where(r =&gt; r.WageType != null)</c>, so an OvertimeType with no wage type is a legitimate state.
    /// </summary>
    public virtual int? WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }
}
