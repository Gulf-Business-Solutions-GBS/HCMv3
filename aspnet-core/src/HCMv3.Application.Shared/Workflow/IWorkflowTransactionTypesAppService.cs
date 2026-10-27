using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

/// <summary>
/// Manages the <c>WorkflowTransactionType</c> catalog -- which transactions run through approval, which
/// appear in Employee Self Service, and which approval chain each one falls back to. This is the screen
/// that replaces editing a 28-case switch statement in C# and redeploying.
/// </summary>
public interface IWorkflowTransactionTypesAppService : IApplicationService
{
    Task<PagedResultDto<WorkflowTransactionTypeDto>> GetAll(GetAllWorkflowTransactionTypesInput input);

    Task<GetWorkflowTransactionTypeForEditOutput> GetWorkflowTransactionTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWorkflowTransactionTypeDto input);

    Task Delete(EntityDto input);

    /// <summary>
    /// The types the current user may raise themselves, ordered for display. Drives the ESS menu and the
    /// landing page's quick actions, so that adding a self-service transaction is a catalog row plus a form
    /// rather than a menu code change.
    /// </summary>
    Task<List<WorkflowTransactionTypeDto>> GetEssEnabledTypes();

    List<string> GetSubTypeSources();

    Task<List<WorkflowSubTypeOptionDto>> GetSubTypeOptions(string source);
}
