using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.OrganizationManagement.Dto;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Read-only feed for the Company Structure Overview chart. Every method here answers a question about
/// the existing <c>Abp.Organizations.OrganizationUnit</c> tree -- nothing on this service mutates it;
/// creating and editing units stays with <see cref="IOrgUnitsAppService"/>.
/// </summary>
public interface ICompanyStructureChartAppService : IApplicationService
{
    /// <summary>
    /// Returns one focus node, its immediate children and its breadcrumb trail, with all headcounts
    /// resolved as of <c>GetCompanyStructureNodeInput.AsOfDate</c>.
    /// </summary>
    Task<CompanyStructureNodeDto> GetNode(GetCompanyStructureNodeInput input);

    /// <summary>Populates the toolbar's root picker and organization unit type filter in one call.</summary>
    Task<CompanyStructureChartFilterOptionsDto> GetFilterOptions();

    /// <summary>Type-ahead over the tree, so the user can jump to a node without drilling down to it.</summary>
    Task<List<OrgChartNodeSummaryDto>> SearchNodes(SearchCompanyStructureNodesInput input);
}
