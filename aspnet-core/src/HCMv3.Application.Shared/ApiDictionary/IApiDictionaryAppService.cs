using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ApiDictionary.Dto;

namespace HCMv3.ApiDictionary;

/// <summary>
/// The API dictionary, modelled on SAP SuccessFactors' OData API Data Dictionary: for each documented record
/// type, the calls a third-party system can make and the fields it can read and write.
///
/// <para>
/// Generated from the live code -- the service, its DTOs and its entity -- so it cannot drift from what the
/// server actually does. These are the application's own endpoints, not a versioned public API; the screen says
/// so, because an integration built on them can break when a screen changes.
/// </para>
/// </summary>
public interface IApiDictionaryAppService : IApplicationService
{
    /// <summary>The record types the dictionary documents.</summary>
    Task<ListResultDto<ApiDictionaryEntitySummaryDto>> GetEntities();

    /// <summary>One record type's operations and fields.</summary>
    Task<ApiDictionaryEntityDto> GetEntity(string name);
}
