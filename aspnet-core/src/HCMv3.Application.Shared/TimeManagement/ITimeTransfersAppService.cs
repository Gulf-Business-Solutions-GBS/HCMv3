using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface ITimeTransfersAppService : IApplicationService
{
    Task<PagedResultDto<TimeTransferDto>> GetAll(GetAllTimeTransfersInput input);

    Task<GetTimeTransferForEditOutput> GetTimeTransferForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTimeTransferDto input);

    Task Delete(EntityDto input);
}
