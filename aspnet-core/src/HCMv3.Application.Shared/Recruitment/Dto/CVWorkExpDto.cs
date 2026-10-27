using System;
using Abp.Application.Services.Dto;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="CVWorkExp"/>.
/// </summary>
public class CVWorkExpDto : EntityDto<int>
{
    public int ApplicantCVId { get; set; }

    public string Company { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Position { get; set; }

    public string Responsibilities { get; set; }
}
