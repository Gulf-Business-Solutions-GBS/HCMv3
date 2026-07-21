using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI.Inputs;
using HCMv3.DynamicEntityProperties.Dto;

namespace HCMv3.DynamicEntityProperties;

public interface IDynamicPropertyAppService
{
    Task<DynamicPropertyDto> Get(int id);

    Task<ListResultDto<DynamicPropertyDto>> GetAll();

    Task Add(DynamicPropertyDto dto);

    Task Update(DynamicPropertyDto dto);

    Task Delete(int id);

    IInputType FindAllowedInputType(string name);
}

