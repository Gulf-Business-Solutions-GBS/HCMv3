using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

public interface IRequestMastersAppService : IApplicationService
{
    Task<PagedResultDto<RequestMasterDto>> GetAll(GetAllRequestMastersInput input);

    Task<GetRequestMasterForEditOutput> GetRequestMasterForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditRequestMasterDto input);

    Task Delete(EntityDto input);
}
