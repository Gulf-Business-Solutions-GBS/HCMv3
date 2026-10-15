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

    /// <summary>
    /// Copies a chain with every one of its steps (and each step's parallel approvers) under a new code and
    /// name, and returns the new chain's id so the screen can open it for renaming.
    /// </summary>
    Task<EntityDto> Copy(EntityDto input);
}
