using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.ReportManagement;

/// <summary>
/// One grid column configuration of a <see cref="ReportSetting"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ReportManagement.ReportSettingDetail</c>, an aggregated child of
/// <see cref="ReportSetting"/> -- see its class remarks).
/// </summary>
public class ReportSettingDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int ReportSettingId { get; set; }

    public virtual ReportSetting ReportSetting { get; set; }

    [StringLength(256)]
    public virtual string FieldName { get; set; }

    [StringLength(256)]
    public virtual string CaptionEn { get; set; }

    [StringLength(256)]
    public virtual string CaptionAr { get; set; }

    public virtual int Order { get; set; }

    public virtual ReportColumnDataType ColumnDataType { get; set; }

    [StringLength(50)]
    public virtual string DisplayFormat { get; set; }

    public virtual double Width { get; set; }

    public virtual bool VisibleInEn { get; set; }

    public virtual bool VisibleInAr { get; set; }

    public virtual ReportColumnAlignment DataHAlignment { get; set; }
}
