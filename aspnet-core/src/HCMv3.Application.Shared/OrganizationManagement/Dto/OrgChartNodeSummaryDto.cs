using Abp.Application.Services.Dto;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// Lightweight identification-only view of an <c>Abp.Organizations.OrganizationUnit</c> node, used where
/// the org-chart screens need to name a node but not draw a card for it -- the breadcrumb trail above the
/// focus node, the "Company Structure" root picker, and the node-search dropdown. Deliberately carries no
/// headcount figures: those cost an extra aggregate query per node and nothing in these three places
/// displays them (see <see cref="OrgChartNodeDto"/> for the card-bearing view that does).
/// </summary>
public class OrgChartNodeSummaryDto : EntityDto<long>
{
    /// <summary>Zero's own auto-maintained hierarchical tree-position code, e.g. "00001.00002".</summary>
    public string TreeCode { get; set; }

    /// <summary>User-assigned short business reference code from <c>OrgUnitProfile</c> (may be null).</summary>
    public string Code { get; set; }

    /// <summary>Zero's own <c>OrganizationUnit.DisplayName</c> -- serves as the English name.</summary>
    public string NameEn { get; set; }

    /// <summary>From <c>OrgUnitProfile.NameAr</c> (null when the unit has no profile row).</summary>
    public string NameAr { get; set; }

    public long? ParentId { get; set; }

    public string OrganizationUnitTypeNameEn { get; set; }

    public string OrganizationUnitTypeNameAr { get; set; }
}
