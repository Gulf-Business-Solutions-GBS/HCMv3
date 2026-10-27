using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditKpiByOrgUnitDto
{
    public int? Id { get; set; }

    [Required]
    public long OrganizationUnitId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }
}
