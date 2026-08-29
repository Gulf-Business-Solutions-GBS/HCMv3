using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// The approver's inbox: everything waiting on the current user, across every transaction type, and the
/// actions they can take on it.
///
/// <para>
/// One service for all types rather than one per type. That is only possible because the workflow tables are
/// generic and each request carries its own summary -- the alternative, which legacy took, was a separate
/// approval screen per request type, ~30 of them.
/// </para>
/// </summary>
public interface IEssInboxAppService : IApplicationService
{
    Task<PagedResultDto<EssInboxItemDto>> GetMyInbox(GetMyInboxInput input);

    /// <summary>Count only -- for the menu badge and the landing tile, which should not pay for the list.</summary>
    Task<int> GetMyPendingCount();

    Task Approve(ApproveRequestInput input);

    Task Reject(RejectRequestInput input);

    Task SendBack(SendBackRequestInput input);
}
