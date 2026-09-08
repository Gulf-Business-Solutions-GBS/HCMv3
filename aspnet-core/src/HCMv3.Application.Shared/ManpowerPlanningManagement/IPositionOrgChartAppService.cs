using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.ManpowerPlanningManagement.Dto;

namespace HCMv3.ManpowerPlanningManagement;

/// <summary>
/// Read-only feed for the Position Org Chart. Nothing here changes the structure; creating positions and
/// re-parenting them stays with <see cref="IPositionsAppService"/>.
/// </summary>
public interface IPositionOrgChartAppService : IApplicationService
{
    /// <summary>Returns one position, its parent, and the positions directly beneath it.</summary>
    Task<PositionOrgChartDto> GetNode(GetPositionOrgChartNodeInput input);

    /// <summary>Type-ahead over positions, so the user can jump straight to one.</summary>
    Task<List<PositionOrgChartNodeDto>> SearchNodes(SearchPositionOrgChartNodesInput input);
}
