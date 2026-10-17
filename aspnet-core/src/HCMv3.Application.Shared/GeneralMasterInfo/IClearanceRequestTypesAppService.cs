using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IClearanceRequestTypesAppService : ISimpleLookupAppService
{
    /// <summary>
    /// Decides whether employees may raise an exit clearance of this type. The ESS type dropdown lists only
    /// the types with it set, so this is what puts a clearance type in front of employees at all.
    /// </summary>
    Task SetIncludeInWorkflow(SetClearanceRequestTypeIncludeInWorkflowInput input);

    /// <summary>The ids of the types with Include in Workflow set, for the tick column on the types screen.</summary>
    Task<ListResultDto<int>> GetIdsIncludedInWorkflow();
}
