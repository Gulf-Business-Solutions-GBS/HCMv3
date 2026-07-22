using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IDependantTypesAppService : IApplicationService
{
    Task<PagedResultDto<DependantTypeDto>> GetAll(GetAllDependantTypesInput input);

    Task<GetDependantTypeForEditOutput> GetDependantTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditDependantTypeDto input);

    Task Delete(EntityDto input);
}
