using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.ManpowerPlanningManagement.Dto;
using HCMv3.PersonnelAdministration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ManpowerPlanningManagement;

/// <summary>
/// Read-only feed for the Position Org Chart -- the third of the three org-chart screens, sharing the
/// Angular card and viewport with the other two.
///
/// SHAPE: three rows, exactly as the Employee Org Chart draws them -- parent position above, focus in the
/// middle, direct children below -- with movement by re-centring rather than by expanding. What differs
/// is what a card means: a position, labelled with whoever currently holds it, or marked vacant when
/// nobody does.
///
/// HIERARCHY SOURCE: <see cref="Position.ParentPositionId"/>, added for this screen. It is a plain parent
/// pointer with no materialised path, so descendant counts are a traversal over the whole edge set held in
/// memory -- the same approach and the same reasoning as the Employee Org Chart. Positions number in the
/// hundreds here, so this is comfortably cheaper than it is there.
///
/// CYCLES: <c>PositionsAppService</c> rejects them on write, but a row predating that check, or one
/// written by an import, would still hang an unguarded traversal. Every walk below carries a visited set.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_PositionOrgChart)]
public class PositionOrgChartAppService : HCMv3AppServiceBase, IPositionOrgChartAppService
{
    private readonly IRepository<Position> _positionRepository;
    private readonly IRepository<PositionRelationship> _positionRelationshipRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public PositionOrgChartAppService(
        IRepository<Position> positionRepository,
        IRepository<PositionRelationship> positionRelationshipRepository,
        IRepository<Employee> employeeRepository)
    {
        _positionRepository = positionRepository;
        _positionRelationshipRepository = positionRelationshipRepository;
        _employeeRepository = employeeRepository;
    }

    /// <summary>
    /// ABP maps <c>Get*</c> to HTTP GET by default, which would make NSwag emit this custom input DTO as
    /// loose query-string parameters and never generate the class for the Angular client -- hence the
    /// explicit <c>[HttpPost]</c> (see the repository's application-layer rules).
    /// </summary>
    [HttpPost]
    public async Task<PositionOrgChartDto> GetNode(GetPositionOrgChartNodeInput input)
    {
        var hierarchy = await LoadHierarchyAsync();

        var focusPositionId = input.PositionId ?? hierarchy.FindLargestTreeRoot();
        if (focusPositionId == null)
        {
            // No position has a parent and the caller named none. An empty result, not an error: the
            // screen draws its own "nothing set up yet" state, which is the expected view until somebody
            // fills in the parent field on the position master.
            return new PositionOrgChartDto();
        }

        var parentId = hierarchy.GetParent(focusPositionId.Value);
        var childIds = hierarchy.GetChildren(focusPositionId.Value);

        var cardIds = new List<int> { focusPositionId.Value };
        if (parentId.HasValue)
        {
            cardIds.Add(parentId.Value);
        }

        cardIds.AddRange(childIds);

        var cards = await BuildCardsAsync(cardIds, hierarchy);

        return new PositionOrgChartDto
        {
            Focus = cards.TryGetValue(focusPositionId.Value, out var focusCard) ? focusCard : null,
            Parent = parentId.HasValue && cards.TryGetValue(parentId.Value, out var parentCard)
                ? parentCard
                : null,
            Children = childIds
                .Where(cards.ContainsKey)
                .Select(id => cards[id])
                .OrderBy(x => x.NameEn)
                .ToList()
        };
    }

    [HttpPost]
    public async Task<List<PositionOrgChartNodeDto>> SearchNodes(SearchPositionOrgChartNodesInput input)
    {
        var maxResultCount = input.MaxResultCount <= 0
            ? SearchPositionOrgChartNodesInput.DefaultResultCount
            : Math.Min(input.MaxResultCount, SearchPositionOrgChartNodesInput.MaxAllowedResultCount);

        var filter = input.Filter;

        // Searches every position, including those with no parent and no children yet. Somebody looking a
        // position up by name should find it and see a single card rather than be told it does not exist.
        var matchedIds = await _positionRepository.GetAll()
            .WhereIf(!filter.IsNullOrWhiteSpace(), x =>
                x.NameEn.Contains(filter) ||
                x.NameAr.Contains(filter) ||
                x.Code.Contains(filter))
            .OrderBy(x => x.NameEn)
            .Take(maxResultCount)
            .Select(x => x.Id)
            .ToListAsync();

        if (matchedIds.Count == 0)
        {
            return new List<PositionOrgChartNodeDto>();
        }

        var hierarchy = await LoadHierarchyAsync();
        var cards = await BuildCardsAsync(matchedIds, hierarchy);

        return matchedIds
            .Where(cards.ContainsKey)
            .Select(id => cards[id])
            .ToList();
    }

    /// <summary>
    /// Every parent pointer in one query, projected to two integers -- no entity materialisation.
    /// </summary>
    private async Task<PositionHierarchy> LoadHierarchyAsync()
    {
        var rows = await _positionRepository.GetAll()
            .Select(x => new { x.Id, x.ParentPositionId })
            .ToListAsync();

        return new PositionHierarchy(
            rows.Select(x => new PositionEdge { PositionId = x.Id, ParentPositionId = x.ParentPositionId })
                .ToList());
    }

    /// <summary>
    /// Turns a set of position ids into cards: the position's own fields, its current holder, and the two
    /// child counts from the in-memory graph.
    /// </summary>
    private async Task<Dictionary<int, PositionOrgChartNodeDto>> BuildCardsAsync(
        List<int> positionIds,
        PositionHierarchy hierarchy)
    {
        var distinctIds = positionIds.Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return new Dictionary<int, PositionOrgChartNodeDto>();
        }

        var positions = await _positionRepository.GetAll()
            .Where(x => distinctIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.NameEn,
                x.NameAr,
                x.ParentPositionId
            })
            .ToListAsync();

        var holders = await LoadHoldersAsync(distinctIds);

        return positions.ToDictionary(
            position => position.Id,
            position =>
            {
                holders.TryGetValue(position.Id, out var holder);

                return new PositionOrgChartNodeDto
                {
                    Id = position.Id,
                    Code = position.Code,
                    NameEn = position.NameEn,
                    NameAr = position.NameAr,
                    ParentPositionId = position.ParentPositionId,
                    IsVacant = holder == null,
                    HolderEmployeeId = holder?.EmployeeId,
                    HolderFullNameEn = holder?.FullNameEn,
                    HolderFullNameAr = holder?.FullNameAr,
                    HolderGender = holder?.Gender ?? default,
                    HolderHasPhoto = holder?.HasPhoto ?? false,
                    DirectChildCount = hierarchy.CountChildren(position.Id),
                    TotalChildCount = hierarchy.CountAllDescendants(position.Id)
                };
            });
    }

    /// <summary>
    /// The current holder of each position: the latest <see cref="PositionRelationship"/> on record.
    ///
    /// Not filtered to a date window, matching the Employee Org Chart -- the chart shows who holds the
    /// position now, and a lapsed row that nobody superseded is still the best answer available. Where a
    /// position genuinely has several concurrent holders (FTELimit above one), the latest-starting one is
    /// shown; the card is one person wide, and the child counts are unaffected either way.
    /// </summary>
    private async Task<Dictionary<int, PositionHolder>> LoadHoldersAsync(List<int> positionIds)
    {
        var relationships = await (from relationship in _positionRelationshipRepository.GetAll()
                                   join employee in _employeeRepository.GetAll()
                                       on relationship.EmployeeId equals employee.Id
                                   where positionIds.Contains(relationship.PositionId)
                                         && relationship.RelationType == PositionRelationType.Holder
                                   select new
                                   {
                                       relationship.PositionId,
                                       relationship.StartDate,
                                       EmployeeId = employee.Id,
                                       employee.FullNameEn,
                                       employee.FullNameAr,
                                       employee.Gender,
                                       HasPhoto = employee.Photo != null
                                   })
                                  .ToListAsync();

        return relationships
            .GroupBy(x => x.PositionId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(x => x.StartDate)
                    .Select(x => new PositionHolder
                    {
                        EmployeeId = x.EmployeeId,
                        FullNameEn = x.FullNameEn,
                        FullNameAr = x.FullNameAr,
                        Gender = x.Gender,
                        HasPhoto = x.HasPhoto
                    })
                    .First());
    }

    private class PositionHolder
    {
        public int EmployeeId { get; set; }

        public string FullNameEn { get; set; }

        public string FullNameAr { get; set; }

        public GeneralMasterInfo.Gender Gender { get; set; }

        public bool HasPhoto { get; set; }
    }

    private class PositionEdge
    {
        public int PositionId { get; set; }

        public int? ParentPositionId { get; set; }
    }

    /// <summary>
    /// The position hierarchy for the tenant, held in memory so the parent lookup and the descendant count
    /// are plain traversals. Cycle-guarded throughout -- a loop written before the create/edit check
    /// existed would otherwise never terminate.
    /// </summary>
    private class PositionHierarchy
    {
        private readonly Dictionary<int, int> _parentOf = new Dictionary<int, int>();
        private readonly Dictionary<int, List<int>> _childrenOf = new Dictionary<int, List<int>>();

        public PositionHierarchy(List<PositionEdge> edges)
        {
            foreach (var edge in edges)
            {
                // A position parented to itself is meaningless and would stall the walk on its own guard.
                if (edge.ParentPositionId == null || edge.ParentPositionId == edge.PositionId)
                {
                    continue;
                }

                _parentOf[edge.PositionId] = edge.ParentPositionId.Value;

                if (!_childrenOf.TryGetValue(edge.ParentPositionId.Value, out var children))
                {
                    children = new List<int>();
                    _childrenOf[edge.ParentPositionId.Value] = children;
                }

                children.Add(edge.PositionId);
            }
        }

        public int? GetParent(int positionId)
        {
            return _parentOf.TryGetValue(positionId, out var parentId) ? parentId : (int?)null;
        }

        public List<int> GetChildren(int positionId)
        {
            return _childrenOf.TryGetValue(positionId, out var children)
                ? new List<int>(children)
                : new List<int>();
        }

        public int CountChildren(int positionId)
        {
            return _childrenOf.TryGetValue(positionId, out var children) ? children.Count : 0;
        }

        /// <summary>Everything below this position at any depth, counted once each even if the graph loops.</summary>
        public int CountAllDescendants(int positionId)
        {
            var visited = new HashSet<int> { positionId };
            var pending = new Stack<int>();
            pending.Push(positionId);

            var total = 0;

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (!_childrenOf.TryGetValue(current, out var children))
                {
                    continue;
                }

                foreach (var child in children)
                {
                    if (!visited.Add(child))
                    {
                        continue;
                    }

                    total++;
                    pending.Push(child);
                }
            }

            return total;
        }

        /// <summary>
        /// The root of the biggest tree: among positions with children but no parent, the one with the most
        /// beneath it. That is the top of the main structure on any real dataset, and where the chart opens.
        /// </summary>
        public int? FindLargestTreeRoot()
        {
            int? bestRoot = null;
            var bestTotal = -1;

            foreach (var parentId in _childrenOf.Keys)
            {
                if (_parentOf.ContainsKey(parentId))
                {
                    continue;
                }

                var total = CountAllDescendants(parentId);
                if (total > bestTotal)
                {
                    bestTotal = total;
                    bestRoot = parentId;
                }
            }

            return bestRoot;
        }
    }
}
