using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Create/Update input DTO for <see cref="PrintReport"/>. <c>Id == null</c> means create.</summary>
public class CreateOrEditPrintReportDto
{
    public int? Id { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    [StringLength(256)]
    public string ReportName { get; set; }

    public bool IsActive { get; set; }

    public ReportCategory ReportCategory { get; set; }

    public decimal CustomPercentage { get; set; }

    public DateTime ReportCreatedOn { get; set; }
}
