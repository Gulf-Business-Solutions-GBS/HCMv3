using System;
using Abp.Application.Services.Dto;

namespace HCMv3.JobProfileManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="JobDescription"/>.
/// </summary>
public class JobDescriptionDto : EntityDto<int>
{
    public int JobTitleId { get; set; }

    public string JobTitleNameEn { get; set; }

    public string ShortDescriptionEn { get; set; }

    public string ShortDescriptionAr { get; set; }

    public string LongDescriptionEn { get; set; }

    public string LongDescriptionAr { get; set; }

    public string Version { get; set; }

    public DateTime VersionChangedOn { get; set; }
}
