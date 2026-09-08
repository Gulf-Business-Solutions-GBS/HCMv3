using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Read-only feed for the Employee Org Chart -- the second of the three org-chart screens, sharing the
/// Angular card and viewport with the Company Structure Overview.
///
/// SHAPE: a drill-down view. It returns the person in focus, the managers stacked above them, and one
/// level of direct reports; clicking a report re-centres the chart on them. Only one level is ever loaded,
/// so the response size follows one person's span of control rather than the size of the company.
///
/// WHY THE WHOLE EDGE SET IS LOADED: <see cref="EmployeeManager"/> is a plain employee-to-manager row with
/// no materialised path, so there is nothing to prefix-match on -- the trick that makes the descendant
/// counts on the Company Structure chart a single grouped query does not transfer. Counting everyone below
/// a person therefore needs a traversal. Rather than recurse in SQL (which would put raw queries in the
/// application layer) or issue a query per level, this loads every current reporting edge -- two integers
/// per employee, a few thousand rows at this tenant's scale -- and walks it in memory. If a tenant ever
/// grows to where that is too much, the fix is a recursive CTE behind a repository method in the
/// EntityFrameworkCore project, not a change here.
///
/// NO DATE FILTER: the chart always shows the present picture, taking each employee's latest reporting
/// line and latest job title. An earlier version took an "as of" date; it was removed because the chart
/// answers "who reports to whom now", and dating it silently emptied the whole chart whenever the
/// underlying rows had lapsed.
///
/// CYCLES: the data allows A to manage B while B manages A, and a cycle would otherwise hang both the
/// chain walk and the descendant count. Every traversal below carries a visited set and stops.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOrgChart)]
public class EmployeeOrgChartAppService : HCMv3AppServiceBase, IEmployeeOrgChartAppService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<JobTitle> _jobTitleRepository;

    public EmployeeOrgChartAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<JobTitle> jobTitleRepository)
    {
        _employeeRepository = employeeRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _jobTitleRepository = jobTitleRepository;
    }

    /// <summary>
    /// ABP maps <c>Get*</c> to HTTP GET by default, which would make NSwag emit this custom input DTO as
    /// loose query-string parameters and never generate the class for the Angular client -- hence the
    /// explicit <c>[HttpPost]</c> (see the repository's application-layer rules).
    /// </summary>
    [HttpPost]
    public async Task<EmployeeOrgChartDto> GetNode(GetEmployeeOrgChartNodeInput input)
    {
        var reportingLines = await LoadReportingLinesAsync();

        var focusEmployeeId = ResolveFocusEmployeeId(input.EmployeeId, reportingLines);
        if (focusEmployeeId == null)
        {
            // Nobody has a reporting line at all and the caller named nobody. An empty result, not an
            // error: the screen draws its own "nothing set up yet" state.
            return new EmployeeOrgChartDto();
        }

        var managerChainIds = reportingLines.BuildManagerChain(focusEmployeeId.Value);
        var directReportIds = reportingLines.GetDirectReports(focusEmployeeId.Value);

        var cardIds = new List<int>(managerChainIds) { focusEmployeeId.Value };
        cardIds.AddRange(directReportIds);

        var cards = await BuildCardsAsync(cardIds, reportingLines);

        return new EmployeeOrgChartDto
        {
            Focus = cards.TryGetValue(focusEmployeeId.Value, out var focusCard) ? focusCard : null,
            ManagerChain = managerChainIds
                .Where(cards.ContainsKey)
                .Select(id => cards[id])
                .ToList(),
            DirectReports = directReportIds
                .Where(cards.ContainsKey)
                .Select(id => cards[id])
                .OrderBy(x => x.FullNameEn)
                .ToList()
        };
    }

    [HttpPost]
    public async Task<List<EmployeeOrgChartNodeDto>> SearchNodes(SearchEmployeeOrgChartNodesInput input)
    {
        var maxResultCount = input.MaxResultCount <= 0
            ? SearchEmployeeOrgChartNodesInput.DefaultResultCount
            : Math.Min(input.MaxResultCount, SearchEmployeeOrgChartNodesInput.MaxAllowedResultCount);

        var filter = input.Filter;

        // Searches every employee, not only those already in a reporting line. Someone looking a colleague
        // up by name should find them and see a single card, rather than be told they do not exist because
        // nobody has filled in their manager yet.
        var matchedIds = await _employeeRepository.GetAll()
            .WhereIf(!filter.IsNullOrWhiteSpace(), x =>
                x.FullNameEn.Contains(filter) ||
                x.FullNameAr.Contains(filter) ||
                x.Code.Contains(filter))
            .OrderBy(x => x.FullNameEn)
            .Take(maxResultCount)
            .Select(x => x.Id)
            .ToListAsync();

        if (matchedIds.Count == 0)
        {
            return new List<EmployeeOrgChartNodeDto>();
        }

        var reportingLines = await LoadReportingLinesAsync();
        var cards = await BuildCardsAsync(matchedIds, reportingLines);

        return matchedIds
            .Where(cards.ContainsKey)
            .Select(id => cards[id])
            .ToList();
    }

    /// <summary>
    /// Each employee's CURRENT manager -- the latest reporting line on record, regardless of whether its
    /// date range has ended.
    ///
    /// Deliberately not filtered to a date window. The chart used to take an "as of" date and show the
    /// lines in force then; that was dropped because the chart is meant to answer "who reports to whom
    /// now", and a date filter silently produces an empty chart whenever the rows have lapsed. Latest
    /// wins: the row with the newest StartDate, breaking ties on the newest EndDate so a correction
    /// entered against the same start date supersedes the row it replaces.
    /// </summary>
    private async Task<ReportingLines> LoadReportingLinesAsync()
    {
        // Projected to an anonymous type and converted in memory rather than straight into ReportingEdge:
        // EF Core's translation of a projection into a user-defined struct is not something to rely on.
        var rows = await _employeeManagerRepository.GetAll()
            .Select(x => new { x.EmployeeId, x.ManagerId, x.StartDate, x.EndDate })
            .ToListAsync();

        var latestPerEmployee = rows
            .GroupBy(x => x.EmployeeId)
            .Select(group => group
                .OrderByDescending(x => x.StartDate)
                .ThenByDescending(x => x.EndDate)
                .First())
            .Select(x => new ReportingEdge { EmployeeId = x.EmployeeId, ManagerId = x.ManagerId })
            .ToList();

        return new ReportingLines(latestPerEmployee);
    }

    /// <summary>
    /// The person to centre on: the one asked for, or -- when the caller sent nobody, as the screen does on
    /// first load -- the root of the largest reporting tree, which is the most useful place to open.
    /// </summary>
    private static int? ResolveFocusEmployeeId(int? requestedEmployeeId, ReportingLines reportingLines)
    {
        if (requestedEmployeeId.HasValue)
        {
            return requestedEmployeeId;
        }

        return reportingLines.FindLargestTreeRoot();
    }

    /// <summary>
    /// Turns a set of employee ids into cards: names and gender from <see cref="Employee"/>, job title from
    /// the org assignment in force on the date, and the two report counts from the in-memory graph.
    ///
    /// <c>HasPhoto</c> is projected as a null check so the photo column never leaves the database -- the
    /// client fetches images separately from <c>GET /api/employee-photo/{id}</c>.
    /// </summary>
    private async Task<Dictionary<int, EmployeeOrgChartNodeDto>> BuildCardsAsync(
        List<int> employeeIds,
        ReportingLines reportingLines)
    {
        var distinctIds = employeeIds.Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return new Dictionary<int, EmployeeOrgChartNodeDto>();
        }

        var employees = await _employeeRepository.GetAll()
            .Where(x => distinctIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.FullNameEn,
                x.FullNameAr,
                x.Gender,
                HasPhoto = x.Photo != null
            })
            .ToListAsync();

        var jobTitles = await LoadJobTitlesAsync(distinctIds);

        return employees.ToDictionary(
            employee => employee.Id,
            employee =>
            {
                jobTitles.TryGetValue(employee.Id, out var jobTitle);

                return new EmployeeOrgChartNodeDto
                {
                    Id = employee.Id,
                    Code = employee.Code,
                    FullNameEn = employee.FullNameEn,
                    FullNameAr = employee.FullNameAr,
                    Gender = employee.Gender,
                    HasPhoto = employee.HasPhoto,
                    JobTitleEn = jobTitle?.NameEn,
                    JobTitleAr = jobTitle?.NameAr,
                    DirectReportCount = reportingLines.CountDirectReports(employee.Id),
                    TotalReportCount = reportingLines.CountAllReports(employee.Id)
                };
            });
    }

    /// <summary>
    /// Each employee's CURRENT job title, taken from their latest org assignment. Not filtered to a date
    /// window, for the same reason as the reporting lines above: the chart shows the present picture, and
    /// a lapsed assignment should still name the title rather than leave the card blank.
    /// </summary>
    private async Task<Dictionary<int, JobTitleName>> LoadJobTitlesAsync(List<int> employeeIds)
    {
        var assignments = await (from assignment in _employeeOrgAssignmentRepository.GetAll()
                                     // EmployeeOrgAssignment.JobTitleId is nullable and JobTitle.Id is not --
                                     // the cast matches the join key types; unassigned rows never match.
                                 join jobTitle in _jobTitleRepository.GetAll()
                                     on assignment.JobTitleId equals (int?)jobTitle.Id
                                 where employeeIds.Contains(assignment.EmployeeId)
                                 select new
                                 {
                                     assignment.EmployeeId,
                                     assignment.StartDate,
                                     jobTitle.NameEn,
                                     jobTitle.NameAr
                                 })
                                .ToListAsync();

        return assignments
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(x => x.StartDate)
                    .Select(x => new JobTitleName { NameEn = x.NameEn, NameAr = x.NameAr })
                    .First());
    }

    private class JobTitleName
    {
        public string NameEn { get; set; }

        public string NameAr { get; set; }
    }

    private class ReportingEdge
    {
        public int EmployeeId { get; set; }

        public int ManagerId { get; set; }
    }

    /// <summary>
    /// The reporting graph for one date, held in memory so the chain walk and the descendant counts are
    /// plain traversals. Every walk is cycle-guarded -- the data does not forbid A managing B while B
    /// manages A, and an unguarded walk would not terminate.
    /// </summary>
    private class ReportingLines
    {
        /// <summary>
        /// Guards against a pathological chain as well as a cycle -- a visited set already stops a true
        /// loop, but a chain thousands deep would mean corrupt data, and stacking that many cards helps
        /// nobody.
        /// </summary>
        private const int MaxChainDepth = 64;

        private readonly Dictionary<int, int> _managerOf = new Dictionary<int, int>();
        private readonly Dictionary<int, List<int>> _reportsOf = new Dictionary<int, List<int>>();

        public ReportingLines(List<ReportingEdge> edges)
        {
            foreach (var edge in edges)
            {
                // Self-management is meaningless and would make the chain walk stall on its own guard.
                if (edge.EmployeeId == edge.ManagerId)
                {
                    continue;
                }

                // First edge wins. A second row for the same employee on the same date is a data fault the
                // chart should survive rather than report.
                if (!_managerOf.ContainsKey(edge.EmployeeId))
                {
                    _managerOf[edge.EmployeeId] = edge.ManagerId;
                }

                if (!_reportsOf.TryGetValue(edge.ManagerId, out var reports))
                {
                    reports = new List<int>();
                    _reportsOf[edge.ManagerId] = reports;
                }

                if (!reports.Contains(edge.EmployeeId))
                {
                    reports.Add(edge.EmployeeId);
                }
            }
        }

        public List<int> GetDirectReports(int employeeId)
        {
            return _reportsOf.TryGetValue(employeeId, out var reports)
                ? new List<int>(reports)
                : new List<int>();
        }

        public int CountDirectReports(int employeeId)
        {
            return _reportsOf.TryGetValue(employeeId, out var reports) ? reports.Count : 0;
        }

        /// <summary>Everyone below this person at any depth, counted once each even if the graph loops.</summary>
        public int CountAllReports(int employeeId)
        {
            var visited = new HashSet<int> { employeeId };
            var pending = new Stack<int>();
            pending.Push(employeeId);

            var total = 0;

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (!_reportsOf.TryGetValue(current, out var reports))
                {
                    continue;
                }

                foreach (var report in reports)
                {
                    if (!visited.Add(report))
                    {
                        continue;
                    }

                    total++;
                    pending.Push(report);
                }
            }

            return total;
        }

        /// <summary>Managers above this person, ROOT FIRST so the screen can stack them top-down.</summary>
        public List<int> BuildManagerChain(int employeeId)
        {
            var chain = new List<int>();
            var visited = new HashSet<int> { employeeId };
            var current = employeeId;

            while (chain.Count < MaxChainDepth && _managerOf.TryGetValue(current, out var managerId))
            {
                if (!visited.Add(managerId))
                {
                    break;
                }

                chain.Add(managerId);
                current = managerId;
            }

            chain.Reverse();
            return chain;
        }

        /// <summary>
        /// The root of the biggest tree: among people who manage someone but report to nobody, the one with
        /// the most people beneath them. That is the top of the main hierarchy on any real dataset, and it
        /// is where the chart should open.
        /// </summary>
        public int? FindLargestTreeRoot()
        {
            int? bestRoot = null;
            var bestTotal = -1;

            foreach (var managerId in _reportsOf.Keys)
            {
                if (_managerOf.ContainsKey(managerId))
                {
                    continue;
                }

                var total = CountAllReports(managerId);
                if (total > bestTotal)
                {
                    bestTotal = total;
                    bestRoot = managerId;
                }
            }

            return bestRoot;
        }
    }
}
