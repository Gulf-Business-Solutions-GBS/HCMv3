using System.Collections.Generic;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// Everything the Company Structure Overview toolbar needs to populate its two dropdowns in one round
/// trip: the "Company Structure" root picker and the type filter beside the node search.
/// </summary>
public class CompanyStructureChartFilterOptionsDto
{
    /// <summary>Top-level units (no parent) -- the "Company Structure" dropdown.</summary>
    public List<OrgChartNodeSummaryDto> Roots { get; set; } = new List<OrgChartNodeSummaryDto>();

    public List<OrgChartTypeOptionDto> OrganizationUnitTypes { get; set; } = new List<OrgChartTypeOptionDto>();
}
