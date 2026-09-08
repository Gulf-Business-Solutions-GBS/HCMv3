using System.Collections.Generic;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// One screenful of the Position Org Chart: three rows, exactly as the Employee Org Chart draws them --
/// the parent position above, the position in focus, and the positions directly beneath it.
/// </summary>
public class PositionOrgChartDto
{
    /// <summary>The card in the middle. Null when no position has a parent set anywhere.</summary>
    public PositionOrgChartNodeDto Focus { get; set; }

    /// <summary>The focus position's parent, or null when it is the top of its tree.</summary>
    public PositionOrgChartNodeDto Parent { get; set; }

    /// <summary>Positions reporting directly to <see cref="Focus"/>, ordered by name.</summary>
    public List<PositionOrgChartNodeDto> Children { get; set; } = new List<PositionOrgChartNodeDto>();
}
