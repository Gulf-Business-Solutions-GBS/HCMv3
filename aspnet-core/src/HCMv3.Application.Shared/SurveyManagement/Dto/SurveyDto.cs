using System;
using Abp.Application.Services.Dto;

namespace HCMv3.SurveyManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="Survey"/>.
/// </summary>
public class SurveyDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
