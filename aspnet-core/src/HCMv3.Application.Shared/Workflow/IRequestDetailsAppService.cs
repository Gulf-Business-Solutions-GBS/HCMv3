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

    /// <summary>Puts the chain's steps in the order given and renumbers 1..n.</summary>
    Task ReorderSteps(ReorderRequestDetailsInput input);

    /// <summary>Duplicates a step onto the end of its chain, returning the new step's id.</summary>
    Task<EntityDto> CopyStep(EntityDto input);

    /// <summary>The request person fields a "Person on Request" step can name, and which transaction types fill each.</summary>
    Task<System.Collections.Generic.List<RequestPersonFieldOptionDto>> GetRequestPersonFieldOptions();
}
