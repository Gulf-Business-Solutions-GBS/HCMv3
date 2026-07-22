using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Organizations;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Enforces a hard cap on how deep the ASP.NET Zero organization-unit tree may nest, independent of the
/// <see cref="OrganizationUnitType"/> level classification. Zero's own <c>OrganizationUnitManager</c> has
/// no built-in max-depth concept, so this is checked before every create/move, regardless of whether the
/// operation came through Zero's own built-in tree admin screen or this app's HR-specific screen -- both
/// call into this same helper so the cap can't be bypassed via either entry point.
///
/// Depth is derived from Zero's own auto-maintained hierarchical <c>OrganizationUnit.Code</c>
/// (dot-separated, e.g. "00001.00002.00003" = depth 3); no separate tracking is needed.
/// </summary>
public static class OrganizationUnitDepthHelper
{
    public const int MaxDepth = 10;

    public static int GetDepth(string organizationUnitCode)
    {
        return organizationUnitCode.Split('.').Length;
    }

    /// <summary>
    /// Computes what depth a new unit under <paramref name="parentId"/> (or a unit being moved,
    /// identified by <paramref name="movingUnitId"/>) would end up at, accounting for the moved unit's
    /// own descendants (since moving a subtree deeper pushes every descendant deeper by the same
    /// amount). Returns the resulting depth so the caller can decide how to report a violation with a
    /// properly localized message (this helper has no access to <c>L()</c>) — compare the result against
    /// <see cref="MaxDepth"/> and throw a <c>UserFriendlyException</c> if it's exceeded.
    /// </summary>
    public static async Task<int> ComputeResultingDepthAsync(
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        long? parentId,
        long? movingUnitId = null)
    {
        var newParentDepth = 0;
        if (parentId.HasValue)
        {
            var parent = await organizationUnitRepository.GetAsync(parentId.Value);
            newParentDepth = GetDepth(parent.Code);
        }

        var subtreeHeight = 0;
        if (movingUnitId.HasValue)
        {
            var movingUnit = await organizationUnitRepository.GetAsync(movingUnitId.Value);
            var movingDepth = GetDepth(movingUnit.Code);

            var descendants = await organizationUnitRepository.GetAll()
                .Where(u => u.Code.StartsWith(movingUnit.Code + ".") || u.Id == movingUnit.Id)
                .Select(u => u.Code)
                .ToListAsync();

            if (descendants.Count > 0)
            {
                subtreeHeight = descendants.Max(GetDepth) - movingDepth;
            }
        }

        return newParentDepth + 1 + subtreeHeight;
    }
}
