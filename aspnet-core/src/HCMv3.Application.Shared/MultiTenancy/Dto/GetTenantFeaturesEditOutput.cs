using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.Editions.Dto;

namespace HCMv3.MultiTenancy.Dto;

public class GetTenantFeaturesEditOutput
{
    public List<NameValueDto> FeatureValues { get; set; }

    public List<FlatFeatureDto> Features { get; set; }
}

