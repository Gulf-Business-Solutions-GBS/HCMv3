using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

public interface IRequestDetailParallelsAppService : IApplicationService
{
    Task<PagedResultDto<RequestDetailParallelDto>> GetAll(GetAllRequestDetailParallelsInput input);

    Task<GetRequestDetailParallelForEditOutput> GetRequestDetailParallelForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditRequestDetailParallelDto input);

    Task Delete(EntityDto input);
}
