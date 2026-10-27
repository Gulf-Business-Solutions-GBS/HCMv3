using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.ReportManagement;

/// <summary>
/// Printable report catalog entry (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ReportManagement.PrintReport</c>). The legacy's <c>[NonPersistent]
/// FullReportName</c> computed getter is dropped here -- it is recomputed on the DTO instead (see
/// <c>PrintReportDto.FullReportName</c>).
/// </summary>
public class PrintReport : FullAuditedEntity, IMustHaveTenant
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
    public virtual string ReportName { get; set; }

    public virtual bool IsActive { get; set; }

    public virtual ReportCategory ReportCategory { get; set; }

    public virtual decimal CustomPercentage { get; set; }

    public virtual DateTime ReportCreatedOn { get; set; }
}
