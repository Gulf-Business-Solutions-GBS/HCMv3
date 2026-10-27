using System;
using Abp.Application.Services.Dto;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="ApplicantCV"/>. Legacy computed <c>EmailCVName</c> is reproduced here.
/// </summary>
public class ApplicantCVDto : EntityDto<int>
{
    public int ApplicantId { get; set; }

    public string ApplicantFullNameEn { get; set; }

    public int? JobTitleId { get; set; }

    public string JobTitleNameEn { get; set; }

    public string CVName { get; set; }

    public DateTime UploadDate { get; set; }

    public string EmailCVName { get; set; }
}
