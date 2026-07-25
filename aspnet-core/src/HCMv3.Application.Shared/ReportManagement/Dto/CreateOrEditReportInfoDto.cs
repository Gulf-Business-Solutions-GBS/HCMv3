using System.ComponentModel.DataAnnotations;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Create/Update input DTO for <see cref="ReportInfo"/>. <c>Id == null</c> means create.</summary>
public class CreateOrEditReportInfoDto
{
    public int? Id { get; set; }

    [StringLength(256)]
    public string ReportName { get; set; }

    [Required]
    public int ReportTypeId { get; set; }

    [Required]
    public int DomainTypeId { get; set; }

    [StringLength(512)]
    public string Description { get; set; }

    [StringLength(1500)]
    public string ReportQuery { get; set; }

    public bool IsActive { get; set; }
}
