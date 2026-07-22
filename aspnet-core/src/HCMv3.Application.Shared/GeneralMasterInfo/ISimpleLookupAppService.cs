using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Shared contract for pure bilingual lookup app services. Each concrete lookup
/// (e.g. ICountriesAppService) extends this with an empty body — the distinct interface
/// per entity is what gives each one its own ABP dynamic-API route and NSwag proxy class.
/// </summary>
public interface ISimpleLookupAppService : IApplicationService
{
    Task<PagedResultDto<SimpleLookupDto>> GetAll(GetAllSimpleLookupsInput input);

    Task<CreateOrEditSimpleLookupDto> GetForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSimpleLookupDto input);

    Task Delete(EntityDto input);
}
