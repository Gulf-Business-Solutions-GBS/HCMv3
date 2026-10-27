using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IAccessUnitsAppService : IApplicationService
{
    Task<PagedResultDto<AccessUnitDto>> GetAll(GetAllAccessUnitsInput input);

    Task<GetAccessUnitForEditOutput> GetAccessUnitForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAccessUnitDto input);

    Task Delete(EntityDto input);
}
