using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

public interface IRequestDetailsAppService : IApplicationService
{
    Task<PagedResultDto<RequestDetailDto>> GetAll(GetAllRequestDetailsInput input);

    Task<GetRequestDetailForEditOutput> GetRequestDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditRequestDetailDto input);

    Task Delete(EntityDto input);
}
