using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Organizations;
using HCMv3.Authorization;
using HCMv3.GridReports.Organization.Dto;
using HCMv3.OrganizationManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Organization;

/// <summary>
/// Organization Unit tree browse -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Organization/uscOrgUnitTree.ascx(.cs)</c>.
///
/// Legacy built the tree with one query PER NODE (<c>BuildChildNode</c> recursing into a fresh
/// <c>GetObjectsQuery&lt;OrganizationUnit&gt;().Where(r =&gt; r.ParentOrgUnit.Oid == parentOrgUnitOid)</c>
/// per level) -- an N+1 query pattern that is UI plumbing, not business logic, and is deliberately NOT
/// reproduced here: this port loads every org unit and every profile row in exactly two queries and
/// builds the tree in memory.
///
/// NOT PORTED: legacy's <c>txtSearch</c>/<c>btnSearch</c> client-side recursive text search and
/// expand-to-node (<c>TreeViewNode.FindAllRecursive</c>/<c>ExpandToNode</c>) -- pure DOM manipulation over
/// the already-rendered tree, with no server involvement to port.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_OrgUnitTree)]
public class OrgUnitTreeReportAppService : HCMv3AppServiceBase, IOrgUnitTreeReportAppService
{
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;

    public OrgUnitTreeReportAppService(
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository)
    {
        _organizationUnitRepository = organizationUnitRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
    }

    /// <summary>
    /// Legacy compared the XAF aspect against <c>"ar-KW"</c> (<c>uscRptOrganizationDocument.ascx.cs</c>
    /// pattern shared across this family); here it comes from the request culture, same convention as
    /// <see cref="GridReports.GridReportAppServiceBase.UseArabicNames"/>.
    /// </summary>
    private bool UseArabicNames => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

    public async Task<List<OrgUnitTreeNodeDto>> GetOrgUnitTree()
    {
        var units = await _organizationUnitRepository.GetAll()
            .Select(u => new { u.Id, u.ParentId, u.DisplayName })
            .ToListAsync();

        var profileNames = await _orgUnitProfileRepository.GetAll()
            .Select(p => new { p.OrganizationUnitId, p.NameAr })
            .ToDictionaryAsync(p => p.OrganizationUnitId, p => p.NameAr);

        var childrenByParent = units
            .GroupBy(u => u.ParentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var useArabicNames = UseArabicNames;

        List<OrgUnitTreeNodeDto> BuildNodes(long? parentId)
        {
            if (!childrenByParent.TryGetValue(parentId, out var children))
            {
                return new List<OrgUnitTreeNodeDto>();
            }

            return children.Select(u => new OrgUnitTreeNodeDto
            {
                Id = u.Id,
                Name = useArabicNames && profileNames.TryGetValue(u.Id, out var nameAr) && !string.IsNullOrEmpty(nameAr)
                    ? nameAr
                    : u.DisplayName,
                Children = BuildNodes(u.Id)
            }).ToList();
        }

        return BuildNodes(null);
    }
}
