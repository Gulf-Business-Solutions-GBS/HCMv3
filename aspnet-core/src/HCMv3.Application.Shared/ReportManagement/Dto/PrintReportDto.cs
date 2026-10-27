using System;
using Abp.Application.Services.Dto;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Read/list DTO for <see cref="PrintReport"/>.</summary>
public class PrintReportDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string ReportName { get; set; }

    public bool IsActive { get; set; }

    public ReportCategory ReportCategory { get; set; }

    public decimal CustomPercentage { get; set; }

    public DateTime ReportCreatedOn { get; set; }

    /// <summary>Recomputes the legacy's <c>[NonPersistent] FullReportName</c> getter.</summary>
    public string FullReportName =>
        !string.IsNullOrEmpty(NameEn) && !string.IsNullOrEmpty(NameAr) ? NameEn + " - " + NameAr : "";
}
