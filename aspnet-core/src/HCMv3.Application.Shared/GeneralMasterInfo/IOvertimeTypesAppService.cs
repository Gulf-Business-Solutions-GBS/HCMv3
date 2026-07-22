using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IOvertimeTypesAppService : IApplicationService
{
    Task<PagedResultDto<OvertimeTypeDto>> GetAll(GetAllOvertimeTypesInput input);

    Task<GetOvertimeTypeForEditOutput> GetOvertimeTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditOvertimeTypeDto input);

    Task Delete(EntityDto input);
}
