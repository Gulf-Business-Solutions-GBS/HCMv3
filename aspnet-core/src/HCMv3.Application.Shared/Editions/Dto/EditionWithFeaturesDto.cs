using System.Collections.Generic;
using Abp.Application.Services.Dto;

namespace HCMv3.Editions.Dto;

public class EditionWithFeaturesDto
{
    public EditionWithFeaturesDto()
    {
        FeatureValues = new List<NameValueDto>();
    }

    public EditionSelectDto Edition { get; set; }

    public List<NameValueDto> FeatureValues { get; set; }
}

