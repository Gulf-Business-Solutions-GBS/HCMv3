using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.UI;
using HCMv3.ApiDictionary.Dto;
using HCMv3.Authorization;

namespace HCMv3.ApiDictionary;

/// <inheritdoc cref="IApiDictionaryAppService"/>
[AbpAuthorize(AppPermissions.Pages_Administration_ApiDictionary)]
public class ApiDictionaryAppService : HCMv3AppServiceBase, IApiDictionaryAppService
{
    private readonly ApiDictionaryBuilder _builder;

    public ApiDictionaryAppService(ApiDictionaryBuilder builder)
    {
        _builder = builder;
    }

    public Task<ListResultDto<ApiDictionaryEntitySummaryDto>> GetEntities()
    {
        var entities = ApiDictionaryCatalog.Entries
            .Select(entry => new ApiDictionaryEntitySummaryDto
            {
                Name = entry.Name,
                DisplayName = L(entry.DisplayNameKey)
            })
            .OrderBy(e => e.DisplayName)
            .ToList();

        return Task.FromResult(new ListResultDto<ApiDictionaryEntitySummaryDto>(entities));
    }

    public Task<ApiDictionaryEntityDto> GetEntity(string name)
    {
        var entry = ApiDictionaryCatalog.Entries
                        .FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase))
                    ?? throw new UserFriendlyException(L("ApiDictionaryEntityNotFound", name));

        return Task.FromResult(_builder.Build(entry));
    }
}
