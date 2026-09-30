using System.Collections.Generic;
using System.Linq;
using HCMv3.Dashboards.Dto;

namespace HCMv3.Dashboards;

/// <summary>An org unit as a dashboard needs it: no repositories, so the tree logic can be tested directly.</summary>
public record DashboardOrgUnit(long Id, long? ParentId, string Name);

/// <summary>
/// The organisation tree every dashboard filters and drills with. Shared so that "department", "units below"
/// and "one level down" mean the same thing on every dashboard.
///
/// Walks parent links rather than Zero's dotted <c>Code</c>, and every walk is capped, so a damaged tree with a
/// cycle cannot hang a request.
/// </summary>
public class DashboardOrgTree
{
    private const int MaxDepth = 1000;

    private readonly Dictionary<long, DashboardOrgUnit> _units;
    private readonly ILookup<long?, DashboardOrgUnit> _childrenByParent;

    public DashboardOrgTree(IEnumerable<DashboardOrgUnit> units)
    {
        _units = units.ToDictionary(u => u.Id);
        _childrenByParent = _units.Values.ToLookup(u => u.ParentId);
    }

    public bool Contains(long unitId) => _units.ContainsKey(unitId);

    public string NameOf(long unitId) => _units.TryGetValue(unitId, out var unit) ? unit.Name : null;

    public bool HasChildren(long unitId) => _childrenByParent[unitId].Any();

    /// <summary>The unit itself and every unit below it.</summary>
    public HashSet<long> SelfAndDescendants(long unitId)
    {
        var result = new HashSet<long>();
        var queue = new Queue<long>();
        queue.Enqueue(unitId);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!result.Add(id))
            {
                continue;
            }

            foreach (var child in _childrenByParent[id])
            {
                queue.Enqueue(child.Id);
            }
        }

        return result;
    }

    /// <summary>Top-down breadcrumb to <paramref name="unitId"/>. Empty for null.</summary>
    public List<DashboardOrgUnitOptionDto> PathTo(long? unitId)
    {
        var path = new List<DashboardOrgUnitOptionDto>();
        var guard = 0;

        while (unitId.HasValue && _units.TryGetValue(unitId.Value, out var unit) && guard++ < MaxDepth)
        {
            path.Insert(0, new DashboardOrgUnitOptionDto { Id = unit.Id, Name = unit.Name });
            unitId = unit.ParentId;
        }

        for (var i = 0; i < path.Count; i++)
        {
            path[i].Depth = i;
        }

        return path;
    }

    /// <summary>
    /// The units a filter dropdown should offer: those in <paramref name="unitIds"/> (the units the in-scope
    /// employees sit in) plus their ancestors so the tree reads properly, in tree order. A manager is not shown
    /// the rest of the company.
    /// </summary>
    public List<DashboardOrgUnitOptionDto> OptionsFor(IEnumerable<long?> unitIds)
    {
        var visible = new HashSet<long>();
        foreach (var unitId in unitIds.Where(id => id.HasValue).Select(id => id.Value).Distinct())
        {
            foreach (var step in PathTo(unitId))
            {
                visible.Add(step.Id);
            }
        }

        var options = new List<DashboardOrgUnitOptionDto>();
        AddChildren(null, 0, visible, options);
        return options;
    }

    private void AddChildren(long? parentId, int depth, HashSet<long> visible, List<DashboardOrgUnitOptionDto> options)
    {
        if (depth > 50)
        {
            return;
        }

        foreach (var child in _childrenByParent[parentId].Where(u => visible.Contains(u.Id)).OrderBy(u => u.Name))
        {
            options.Add(new DashboardOrgUnitOptionDto { Id = child.Id, Name = child.Name, Depth = depth });
            AddChildren(child.Id, depth + 1, visible, options);
        }
    }

    /// <summary>
    /// Which "by department" bar an employee in <paramref name="unitId"/> belongs to: the unit one level below
    /// <paramref name="selectedUnitId"/> that contains them (a top-level unit when nothing is selected), or the
    /// selected unit itself as <c>Direct</c> for its own staff. Null when they fall outside the selected unit.
    /// An employee with no current assignment only appears at the top level, as an unassigned bar (null id).
    /// </summary>
    public (long? Id, bool Direct)? BucketFor(long? unitId, long? selectedUnitId)
    {
        if (!unitId.HasValue || !_units.ContainsKey(unitId.Value))
        {
            return selectedUnitId.HasValue ? null : (null, false);
        }

        if (selectedUnitId.HasValue && unitId.Value == selectedUnitId.Value)
        {
            return (selectedUnitId, true);
        }

        var current = _units[unitId.Value];
        var guard = 0;
        while (guard++ < MaxDepth)
        {
            if (current.ParentId == selectedUnitId)
            {
                return (current.Id, false);
            }

            if (!current.ParentId.HasValue || !_units.TryGetValue(current.ParentId.Value, out var parent))
            {
                return null;
            }

            current = parent;
        }

        return null;
    }
}
