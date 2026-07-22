using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IOvertimeTypeDetailsAppService : IApplicationService
{
    Task<PagedResultDto<OvertimeTypeDetailDto>> GetAll(GetAllOvertimeTypeDetailsInput input);

    Task<GetOvertimeTypeDetailForEditOutput> GetOvertimeTypeDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditOvertimeTypeDetailDto input);

    Task Delete(EntityDto input);
}
