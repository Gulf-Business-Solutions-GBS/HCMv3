using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.Dashboards.Workflow.Dto;
using HCMv3.Workflow;

namespace HCMv3.Dashboards.Workflow;

/// <summary>
/// One request, reduced to what the dashboard counts. <paramref name="DecidedAt"/> is when the final approval or
/// rejection was logged; <paramref name="LastActionAt"/> is the latest action that moved the request (reads and
/// remarks do not); <paramref name="DueInDays"/> comes from the request's approval chain, for open requests only.
/// </summary>
public record WorkflowItem(
    int Id,
    string RequestNumber,
    string RequestType,
    int? EmployeeId,
    DateTime CreationTime,
    RequestStatus Status,
    int WorkflowStep,
    DateTime? DecidedAt,
    DateTime LastActionAt,
    int? DueInDays);

/// <summary>An approver an open request is waiting with at its current step.</summary>
public record WorkflowWaiting(int RequestId, int ApproverId);

public record WorkflowPerson(int Id, string Code, string NameEn, string NameAr, long? OrganizationUnitId);

/// <summary>
/// Every number on the Workflow Requests Dashboard, computed in memory from requests the app service has already
/// scoped. Kept free of repositories so the rules can be tested directly.
///
/// <para><b>Overdue</b> is an open request older than its approval chain's "due in days"; a chain with no due days
/// never makes a request overdue (legacy counted every such request as overdue). <b>Time to decide</b> runs from
/// raising the request to its final approval or rejection in the request log -- not <c>ApprovalProcessTime</c>, which
/// is measured from the last edit and only on approval.</para>
/// </summary>
public class WorkflowDashboardCalculator
{
    public const int TopCount = 10;

    /// <summary>Lower bounds, in whole days waited, of the waiting bands.</summary>
    public static readonly int[] WaitingBandBounds = { 0, 3, 8, 15, 31 };

    private static readonly RequestStatus[] StatusOrder =
    {
        RequestStatus.Open, RequestStatus.Approved, RequestStatus.Rejected, RequestStatus.Closed, RequestStatus.Transferred
    };

    private readonly DateTime _from;
    private readonly DateTime _to;
    private readonly DateTime _now;
    private readonly DashboardOrgTree _tree;
    private readonly IReadOnlyDictionary<int, WorkflowPerson> _people;

    /// <param name="people">Requesters and approvers, by employee id.</param>
    public WorkflowDashboardCalculator(DateTime from, DateTime to, DateTime now, DashboardOrgTree tree, IEnumerable<WorkflowPerson> people)
    {
        _from = from.Date;
        _to = to.Date;
        _now = now;
        _tree = tree;
        _people = people.GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
    }

    public static DateTime MonthOf(DateTime date) => new(date.Year, date.Month, 1);

    // ---------------------------------------------------------------- one request

    public static DateTime? DueDateOf(WorkflowItem item) =>
        item.DueInDays is > 0 ? item.CreationTime.AddDays(item.DueInDays.Value) : null;

    public bool IsOverdue(WorkflowItem item) =>
        item.Status == RequestStatus.Open && DueDateOf(item) is DateTime due && due < _now;

    public static double? DaysToDecide(WorkflowItem item) =>
        item.Status is RequestStatus.Approved or RequestStatus.Rejected && item.DecidedAt is DateTime decided
            ? Math.Max(0, (decided - item.CreationTime).TotalDays)
            : null;

    public double DaysWaiting(WorkflowItem item) => Math.Max(0, (_now - item.LastActionAt).TotalDays);

    public static int BandOf(double days)
    {
        var whole = (int)Math.Floor(days);
        for (var i = WaitingBandBounds.Length - 1; i > 0; i--)
        {
            if (whole >= WaitingBandBounds[i])
            {
                return i;
            }
        }

        return 0;
    }

    private static double? Average(IEnumerable<double?> values)
    {
        var list = values.Where(v => v.HasValue).Select(v => v.Value).ToList();
        return list.Count == 0 ? null : Round(list.Average());
    }

    // ---------------------------------------------------------------- tiles

    public WorkflowKpisDto Kpis(IReadOnlyCollection<WorkflowItem> items)
    {
        int Count(RequestStatus status) => items.Count(i => i.Status == status);
        var approved = Count(RequestStatus.Approved);
        var rejected = Count(RequestStatus.Rejected);
        var open = items.Where(i => i.Status == RequestStatus.Open).ToList();

        return new WorkflowKpisDto
        {
            Raised = items.Count,
            Open = open.Count,
            Approved = approved,
            Rejected = rejected,
            Withdrawn = Count(RequestStatus.Closed),
            Transferred = Count(RequestStatus.Transferred),
            ApprovalRatePercent = approved + rejected == 0 ? null : Round(approved * 100.0 / (approved + rejected)),
            Overdue = open.Count(IsOverdue),
            AverageDaysToDecide = Average(items.Select(DaysToDecide)),
            LongestWaitDays = open.Count == 0 ? null : Round(open.Max(DaysWaiting))
        };
    }

    /// <summary>Requests raised each month of the period, by their status today.</summary>
    public List<WorkflowTrendPointDto> Trend(IReadOnlyCollection<WorkflowItem> items)
    {
        var byMonth = items.ToLookup(i => MonthOf(i.CreationTime));
        var points = new List<WorkflowTrendPointDto>();

        for (var month = MonthOf(_from); month <= _to; month = month.AddMonths(1))
        {
            points.Add(new WorkflowTrendPointDto
            {
                MonthStart = month,
                Counts = StatusOrder
                    .Select(s => new WorkflowStatusCountDto { Status = s, Count = byMonth[month].Count(i => i.Status == s) })
                    .Where(c => c.Count > 0)
                    .ToList()
            });
        }

        return points;
    }

    public List<WorkflowTypeBucketDto> ByType(IReadOnlyCollection<WorkflowItem> items)
    {
        return items
            .GroupBy(i => i.RequestType ?? string.Empty)
            .Select(g => new WorkflowTypeBucketDto
            {
                RequestType = g.Key,
                Raised = g.Count(),
                Open = g.Count(i => i.Status == RequestStatus.Open),
                Approved = g.Count(i => i.Status == RequestStatus.Approved),
                Rejected = g.Count(i => i.Status == RequestStatus.Rejected),
                Overdue = g.Count(IsOverdue),
                AverageDaysToDecide = Average(g.Select(DaysToDecide))
            })
            .OrderByDescending(b => b.Raised)
            .ThenBy(b => b.RequestType)
            .ToList();
    }

    public List<WorkflowOrgUnitBucketDto> ByOrgUnit(IReadOnlyCollection<WorkflowItem> items, long? selectedUnitId)
    {
        return items
            .Select(i => (Item: i, Bucket: i.EmployeeId is int id && _people.TryGetValue(id, out var p)
                ? _tree.BucketFor(p.OrganizationUnitId, selectedUnitId)
                : ((long? Id, bool Direct)?)null))
            .Where(x => x.Bucket != null)
            .GroupBy(x => x.Bucket.Value)
            .Select(g => new WorkflowOrgUnitBucketDto
            {
                OrganizationUnitId = g.Key.Id,
                IsDirectStaff = g.Key.Direct,
                Name = g.Key.Id.HasValue ? _tree.NameOf(g.Key.Id.Value) : null,
                HasChildren = !g.Key.Direct && g.Key.Id.HasValue && _tree.HasChildren(g.Key.Id.Value),
                Raised = g.Count(),
                Open = g.Count(x => x.Item.Status == RequestStatus.Open),
                Overdue = g.Count(x => IsOverdue(x.Item)),
                AverageDaysToDecide = Average(g.Select(x => DaysToDecide(x.Item)))
            })
            .OrderByDescending(b => b.Raised)
            .ThenBy(b => b.Name)
            .ToList();
    }

    /// <summary>Approvers with open requests waiting at their step, most waiting first.</summary>
    public List<WorkflowApproverBucketDto> Waiting(IReadOnlyCollection<WorkflowItem> items, IReadOnlyCollection<WorkflowWaiting> waiting)
    {
        var open = items.Where(i => i.Status == RequestStatus.Open).ToDictionary(i => i.Id);

        return waiting
            .Where(w => open.ContainsKey(w.RequestId))
            .Distinct()
            .GroupBy(w => w.ApproverId)
            .Select(g =>
            {
                _people.TryGetValue(g.Key, out var p);
                var requests = g.Select(w => open[w.RequestId]).ToList();
                return new WorkflowApproverBucketDto
                {
                    ApproverId = g.Key,
                    ApproverCode = p?.Code,
                    ApproverNameEn = p?.NameEn,
                    ApproverNameAr = p?.NameAr,
                    Waiting = requests.Count,
                    Overdue = requests.Count(IsOverdue),
                    OldestDays = Round(requests.Max(DaysWaiting))
                };
            })
            .OrderByDescending(b => b.Waiting)
            .ThenByDescending(b => b.OldestDays)
            .ThenBy(b => b.ApproverCode)
            .Take(TopCount)
            .ToList();
    }

    /// <summary>Open requests by how long they have waited since their last action; every band is returned.</summary>
    public List<WorkflowWaitingBandDto> WaitingBands(IReadOnlyCollection<WorkflowItem> items)
    {
        var counts = new int[WaitingBandBounds.Length];
        foreach (var item in items.Where(i => i.Status == RequestStatus.Open))
        {
            counts[BandOf(DaysWaiting(item))]++;
        }

        return WaitingBandBounds
            .Select((from, i) => new WorkflowWaitingBandDto
            {
                Index = i,
                FromDays = from,
                ToDays = i + 1 < WaitingBandBounds.Length ? WaitingBandBounds[i + 1] - 1 : null,
                Requests = counts[i]
            })
            .ToList();
    }

    // ---------------------------------------------------------------- lists

    public WorkflowRequestRowDto ToRow(WorkflowItem item, IEnumerable<int> waitingWith)
    {
        WorkflowPerson requester = null;
        if (item.EmployeeId is int employeeId)
        {
            _people.TryGetValue(employeeId, out requester);
        }

        var approvers = item.Status == RequestStatus.Open
            ? waitingWith.Distinct().Select(id => _people.TryGetValue(id, out var p) ? p : null).Where(p => p != null).ToList()
            : new List<WorkflowPerson>();

        return new WorkflowRequestRowDto
        {
            Id = item.Id,
            RequestNumber = item.RequestNumber,
            RequestType = item.RequestType,
            EmployeeId = item.EmployeeId,
            EmployeeCode = requester?.Code,
            EmployeeNameEn = requester?.NameEn,
            EmployeeNameAr = requester?.NameAr,
            OrganizationUnitName = requester?.OrganizationUnitId is long unitId ? _tree.NameOf(unitId) : null,
            CreationTime = item.CreationTime,
            Status = item.Status,
            WorkflowStep = item.WorkflowStep,
            WaitingWithEn = approvers.Count == 0 ? null : string.Join(", ", approvers.Select(p => p.NameEn ?? p.NameAr)),
            WaitingWithAr = approvers.Count == 0 ? null : string.Join("، ", approvers.Select(p => p.NameAr ?? p.NameEn)),
            Days = item.Status == RequestStatus.Open ? Round(DaysWaiting(item)) : DaysToDecide(item) is double d ? Round(d) : null,
            DueDate = item.Status == RequestStatus.Open ? DueDateOf(item) : null,
            IsOverdue = IsOverdue(item)
        };
    }

    private static double Round(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);
}
