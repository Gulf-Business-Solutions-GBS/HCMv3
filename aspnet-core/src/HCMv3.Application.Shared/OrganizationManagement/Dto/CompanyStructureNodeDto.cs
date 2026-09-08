using System.Collections.Generic;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// One screenful of the Company Structure Overview: the focus node's own card, the cards for its immediate
/// children, and the breadcrumb trail back to the root.
///
/// The screen is a drill-down explorer rather than a whole-tree render -- it asks for one node at a time
/// and the user clicks a child to descend -- so this deliberately does NOT nest grandchildren. That keeps
/// the response flat and cheap regardless of how large the tenant's tree is.
/// </summary>
public class CompanyStructureNodeDto
{
    /// <summary>The card at the top. Null only when the tenant has no organization units at all.</summary>
    public OrgChartNodeDto Focus { get; set; }

    /// <summary>Root-first trail of every ancestor above <see cref="Focus"/>; empty when focus is a root.</summary>
    public List<OrgChartNodeSummaryDto> Ancestors { get; set; } = new List<OrgChartNodeSummaryDto>();

    /// <summary>Immediate children of <see cref="Focus"/>, in tree order.</summary>
    public List<OrgChartNodeDto> Children { get; set; } = new List<OrgChartNodeDto>();
}
