using System;
using Abp.Application.Services.Dto;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Read/list DTO for <see cref="CompetencyByJobTitle"/>.</summary>
public class CompetencyByJobTitleDto : EntityDto<int>
{
    public int JobTitleId { get; set; }

    public string JobTitleName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
