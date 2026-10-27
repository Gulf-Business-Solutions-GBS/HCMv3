using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Organizations;
using Abp.Timing;
using HCMv3.Authorization;
using HCMv3.Dashboards.Dto;
using HCMv3.Dashboards.Workflow.Dto;
using HCMv3.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.Dashboards.Workflow;

/// <inheritdoc cref="IWorkflowDashboardAppService"/>
/// <remarks>
/// <para><b>Legacy source.</b> ESS <c>DashboardController.WorkflowRequestDashboard</c>: counts by status, pies by
/// department and by request type for each status, and an overdue grid. Kept: those figures. Changed: the data follows
/// <see cref="IDashboardScopeService"/> (legacy showed every request to a user with no subordinates); withdrawn and
/// transferred requests are counted, so the parts add up to the total; a chain with no due days is never overdue;
/// the last day of the period is included; time to decide and "waiting with whom" are new.</para>
///
/// <para>Queries are synchronous <c>ToList()</c> over <c>GetAll()</c>, as in the other dashboards, so the service can
/// be tested over plain lists.</para>
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_Dashboards_Workflow)]
public class WorkflowDashboardAppService : HCMv3AppServiceBase, IWorkflowDashboardAppService
{
    private const int MaxPeriodYears = 5;

    /// <summary>Actions that do not move a request on, so they do not reset how long it has waited.</summary>
    private static readonly RequestActionType[] PassiveActions = { RequestActionType.Read, RequestActionType.Remarked };

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly IRepository<RequestLog> _requestLogRepository;
    private readonly IRepository<WorkFlowApprover> _approverRepository;
    private readonly IRepository<WorkflowTransactionType> _transactionTypeRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly IDashboardScopeService _dashboardScopeService;
    private readonly WorkflowDashboardExcelExporter _excelExporter;

    public WorkflowDashboardAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<WorkflowRequest> requestRepository,
        IRepository<RequestLog> requestLogRepository,
        IRepository<WorkFlowApprover> approverRepository,
        IRepository<WorkflowTransactionType> transactionTypeRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IRequestMasterResolver requestMasterResolver,
        IDashboardScopeService dashboardScopeService,
        WorkflowDashboardExcelExporter excelExporter)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _requestRepository = requestRepository;
        _requestLogRepository = requestLogRepository;
        _approverRepository = approverRepository;
        _transactionTypeRepository = transactionTypeRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _requestMasterResolver = requestMasterResolver;
        _dashboardScopeService = dashboardScopeService;
        _excelExporter = excelExporter;
    }

    [HttpPost]
    public async Task<WorkflowDashboardDto> GetDashboard(WorkflowDashboardInput input)
    {
        var context = await BuildContextAsync(input);
        return BuildDashboard(context, input);
    }

    /// <summary>The requests behind a figure, newest first.</summary>
    [HttpPost]
    public async Task<PagedResultDto<WorkflowRequestRowDto>> GetRequests(GetWorkflowRequestsInput input)
    {
        var context = await BuildContextAsync(input);
        var rows = RequestRows(context, input.Kind, input.Status, input.Month, input.ApproverId, input.Band);
        return new PagedResultDto<WorkflowRequestRowDto>(rows.Count, rows.Skip(input.SkipCount).Take(input.MaxResultCount).ToList());
    }

    [HttpPost]
    public async Task<FileDto> ExportToExcel(ExportWorkflowDashboardInput input)
    {
        var context = await BuildContextAsync(input);
        var all = input.Part == WorkflowDashboardExportPart.All;
        var dashboard = BuildDashboard(context, input);

        return _excelExporter.Export(new WorkflowDashboardExportData
        {
            Part = input.Part,
            Dashboard = dashboard,
            Requests = all || input.Part == WorkflowDashboardExportPart.Requests
                ? all
                    ? RequestRows(context, WorkflowRequestListKind.All, null, null, null, null)
                    : RequestRows(context, input.Kind, input.Status, input.Month, input.ApproverId, input.Band)
                : new List<WorkflowRequestRowDto>(),
            TypeNames = dashboard.Types.ToDictionary(t => t.Code, t => Pick(t.NameEn, t.NameAr)),
            ScopeLabel = ScopeLabel(context.Scope),
            UseArabic = UseArabic
        });
    }

    // ---------------------------------------------------------------- building

    private static bool UseArabic => CultureInfo.CurrentUICulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

    private static string Pick(string en, string ar) =>
        UseArabic ? (string.IsNullOrWhiteSpace(ar) ? en : ar) : (string.IsNullOrWhiteSpace(en) ? ar : en);

    private WorkflowDashboardDto BuildDashboard(DashboardContext context, WorkflowDashboardInput input)
    {
        var calculator = context.Calculator;
        var items = context.Items;

        return new WorkflowDashboardDto
        {
            Scope = new DashboardScopeDto
            {
                Reason = context.Scope.Reason,
                OrgUnitNames = context.Scope.OrgUnitNames.ToList(),
                IncludesOwnTeam = context.Scope.IncludesOwnTeam,
                EmployeesInScope = context.EmployeesInScope
            },
            FromDate = context.From,
            ToDate = context.To,
            Kpis = calculator.Kpis(items),
            Types = context.Types,
            Trend = calculator.Trend(items),
            ByType = calculator.ByType(items),
            ByOrgUnit = calculator.ByOrgUnit(items, input.OrganizationUnitId),
            Waiting = calculator.Waiting(items, context.Waiting),
            WaitingBands = calculator.WaitingBands(items),
            OrgUnitPath = context.Tree.PathTo(input.OrganizationUnitId),
            OrgUnitOptions = context.Tree.OptionsFor(context.ScopedEmployees.Select(e => e.OrganizationUnitId))
        };
    }

    private static List<WorkflowRequestRowDto> RequestRows(
        DashboardContext context,
        WorkflowRequestListKind kind,
        RequestStatus? status,
        DateTime? month,
        int? approverId,
        int? band)
    {
        var calculator = context.Calculator;
        IEnumerable<WorkflowItem> source = context.Items;

        switch (kind)
        {
            case WorkflowRequestListKind.Status:
                source = source.Where(i => i.Status == (status ?? RequestStatus.Open));
                break;
            case WorkflowRequestListKind.Month:
                var wanted = WorkflowDashboardCalculator.MonthOf(month ?? context.To);
                source = source.Where(i => WorkflowDashboardCalculator.MonthOf(i.CreationTime) == wanted);
                break;
            case WorkflowRequestListKind.Overdue:
                source = source.Where(calculator.IsOverdue);
                break;
            case WorkflowRequestListKind.Approver:
                var ids = context.Waiting.Where(w => w.ApproverId == approverId).Select(w => w.RequestId).ToHashSet();
                source = source.Where(i => i.Status == RequestStatus.Open && ids.Contains(i.Id));
                break;
            case WorkflowRequestListKind.WaitingBand:
                source = source.Where(i => i.Status == RequestStatus.Open
                                           && WorkflowDashboardCalculator.BandOf(calculator.DaysWaiting(i)) == (band ?? 0));
                break;
        }

        var waitingByRequest = context.Waiting.ToLookup(w => w.RequestId, w => w.ApproverId);
        return source
            .OrderByDescending(i => i.CreationTime)
            .ThenBy(i => i.RequestNumber)
            .Select(i => calculator.ToRow(i, waitingByRequest[i.Id]))
            .ToList();
    }

    private string ScopeLabel(DashboardScope scope)
    {
        switch (scope.Reason)
        {
            case DashboardScopeReason.Admin:
                return L("Dashboard_ScopeAllAdmin");
            case DashboardScopeReason.Nobody:
                return L("Dashboard_ScopeNobody");
        }

        var parts = new List<string>(scope.OrgUnitNames);
        if (scope.IncludesOwnTeam)
        {
            parts.Add(L("Dashboard_ScopeYourTeam"));
        }

        return string.Join(" + ", parts);
    }

    // ---------------------------------------------------------------- context

    private sealed class DashboardContext
    {
        public DashboardScope Scope { get; init; }
        public DateTime From { get; init; }
        public DateTime To { get; init; }
        public DashboardOrgTree Tree { get; init; }
        public WorkflowDashboardCalculator Calculator { get; init; }
        public List<WorkflowPerson> ScopedEmployees { get; init; }
        public int EmployeesInScope { get; init; }
        public List<WorkflowRequestTypeOptionDto> Types { get; init; }
        public List<WorkflowItem> Items { get; init; }
        public List<WorkflowWaiting> Waiting { get; init; }
    }

    private async Task<DashboardContext> BuildContextAsync(WorkflowDashboardInput input)
    {
        var now = Clock.Now;
        var (from, to) = ResolvePeriod(input, now.Date);

        var scope = await _dashboardScopeService.GetScopeAsync();
        var scopeIds = scope.IsEveryone ? null : scope.EmployeeIds.ToList();

        var employeeQuery = _employeeRepository.GetAll();
        var assignmentQuery = _orgAssignmentRepository.GetAll();
        if (scopeIds != null)
        {
            employeeQuery = employeeQuery.Where(e => scopeIds.Contains(e.Id));
            assignmentQuery = assignmentQuery.Where(a => scopeIds.Contains(a.EmployeeId));
        }

        var unitByEmployee = assignmentQuery
            .Select(a => new { a.EmployeeId, a.OrganizationUnitId, a.StartDate })
            .ToList()
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.StartDate <= to).ThenByDescending(a => a.StartDate).First().OrganizationUnitId);

        var employeeRows = employeeQuery.Select(e => new { e.Id, e.Code, e.FullNameEn, e.FullNameAr, e.Status }).ToList();
        var scopedEmployees = employeeRows
            .Select(e => new WorkflowPerson(e.Id, e.Code, e.FullNameEn, e.FullNameAr, unitByEmployee.TryGetValue(e.Id, out var u) ? u : null))
            .ToList();

        var tree = new DashboardOrgTree(_organizationUnitRepository.GetAll()
            .Select(u => new DashboardOrgUnit(u.Id, u.ParentId, u.DisplayName))
            .ToList());

        IEnumerable<WorkflowPerson> filtered = scopedEmployees;
        if (input.OrganizationUnitId.HasValue)
        {
            var units = tree.SelfAndDescendants(input.OrganizationUnitId.Value);
            filtered = filtered.Where(e => e.OrganizationUnitId.HasValue && units.Contains(e.OrganizationUnitId.Value));
        }

        var ids = filtered.Select(e => e.Id).ToList();
        var byId = scopeIds != null || input.OrganizationUnitId.HasValue;

        // Raised from the first day to the end of the last day; drafts were never submitted.
        var end = to.AddDays(1);
        var requestQuery = _requestRepository.GetAll()
            .Where(r => r.CreationTime >= from && r.CreationTime < end && r.RequestStatus != RequestStatus.Draft);
        if (byId)
        {
            requestQuery = requestQuery.Where(r => r.EmployeeId.HasValue && ids.Contains(r.EmployeeId.Value));
        }

        if (!string.IsNullOrWhiteSpace(input.RequestType))
        {
            var type = input.RequestType;
            requestQuery = requestQuery.Where(r => r.RequestType == type);
        }

        var requests = requestQuery
            .Select(r => new
            {
                r.Id, r.TenantId, r.RequestNumber, r.RequestType, r.RequestSubTypeId, r.EmployeeId, r.CreationTime, r.RequestStatus, r.WorkflowStep
            })
            .ToList();

        var requestIds = requests.Select(r => r.Id).ToList();
        var logs = requestIds.Count == 0
            ? new List<(int RequestId, RequestActionType Type, DateTime At)>()
            : _requestLogRepository.GetAll()
                .Where(l => requestIds.Contains(l.WorkflowRequestId))
                .Select(l => new { l.WorkflowRequestId, l.ActionType, l.ActionDateTime })
                .ToList()
                .Select(l => (RequestId: l.WorkflowRequestId, Type: l.ActionType, At: l.ActionDateTime))
                .ToList();
        var logsByRequest = logs.ToLookup(l => l.RequestId);

        var openIds = requests.Where(r => r.RequestStatus == RequestStatus.Open).Select(r => r.Id).ToList();
        var stepById = requests.ToDictionary(r => r.Id, r => r.WorkflowStep);
        var waiting = openIds.Count == 0
            ? new List<WorkflowWaiting>()
            : _approverRepository.GetAll()
                .Where(a => openIds.Contains(a.WorkflowRequestId) && !a.IsApproved && a.ApproverId.HasValue)
                .Select(a => new { a.WorkflowRequestId, a.ApproverId, a.WorkflowStep })
                .ToList()
                .Where(a => a.WorkflowStep == stepById[a.WorkflowRequestId])
                .Select(a => new WorkflowWaiting(a.WorkflowRequestId, a.ApproverId.Value))
                .Distinct()
                .ToList();

        // Approvers are named even when they are outside the scope: "waiting with" is about who holds the request.
        var knownIds = scopedEmployees.Select(e => e.Id).ToHashSet();
        var approverIds = waiting.Select(w => w.ApproverId).Where(id => !knownIds.Contains(id)).Distinct().ToList();
        var approvers = approverIds.Count == 0
            ? new List<WorkflowPerson>()
            : _employeeRepository.GetAll()
                .Where(e => approverIds.Contains(e.Id))
                .Select(e => new { e.Id, e.Code, e.FullNameEn, e.FullNameAr })
                .ToList()
                .Select(e => new WorkflowPerson(e.Id, e.Code, e.FullNameEn, e.FullNameAr, null))
                .ToList();

        var dueByRequest = new Dictionary<int, int?>();
        var dueByChain = new Dictionary<(string, int?, int?), int?>();
        foreach (var r in requests.Where(r => r.RequestStatus == RequestStatus.Open))
        {
            var key = (r.RequestType, r.EmployeeId, r.RequestSubTypeId);
            if (!dueByChain.TryGetValue(key, out var due))
            {
                due = await DueInDaysAsync(r.RequestType, r.EmployeeId, r.RequestSubTypeId);
                dueByChain[key] = due;
            }

            dueByRequest[r.Id] = due;
        }

        var items = requests
            .Select(r =>
            {
                var requestLogs = logsByRequest[r.Id].ToList();
                var decided = requestLogs
                    .Where(l => l.Type == (r.RequestStatus == RequestStatus.Rejected ? RequestActionType.Rejected : RequestActionType.Approved))
                    .Select(l => (DateTime?)l.At)
                    .Max();
                var lastAction = requestLogs
                    .Where(l => !PassiveActions.Contains(l.Type))
                    .Select(l => (DateTime?)l.At)
                    .Max();
                return new WorkflowItem(r.Id, r.RequestNumber, r.RequestType, r.EmployeeId, r.CreationTime, r.RequestStatus, r.WorkflowStep,
                    r.RequestStatus is RequestStatus.Approved or RequestStatus.Rejected ? decided : null,
                    lastAction is DateTime at && at > r.CreationTime ? at : r.CreationTime,
                    dueByRequest.GetValueOrDefault(r.Id));
            })
            .ToList();

        var codes = items.Select(i => i.RequestType).Where(c => c != null).Distinct().ToList();
        var typeRows = _transactionTypeRepository.GetAll()
            .Where(t => codes.Contains(t.Code))
            .Select(t => new { t.Code, t.NameEn, t.NameAr })
            .ToList()
            .GroupBy(t => t.Code)
            .ToDictionary(g => g.Key, g => g.First());
        var types = codes
            .Select(c => typeRows.TryGetValue(c, out var t)
                ? new WorkflowRequestTypeOptionDto { Code = c, NameEn = t.NameEn, NameAr = t.NameAr }
                : new WorkflowRequestTypeOptionDto { Code = c, NameEn = c, NameAr = c })
            .OrderBy(t => Pick(t.NameEn, t.NameAr))
            .ToList();

        return new DashboardContext
        {
            Scope = scope,
            From = from,
            To = to,
            Tree = tree,
            Calculator = new WorkflowDashboardCalculator(from, to, now, tree, scopedEmployees.Concat(approvers)),
            ScopedEmployees = scopedEmployees,
            EmployeesInScope = employeeRows.Count(e => e.Status == EmployeeStatus.Active),
            Types = types,
            Items = items,
            Waiting = waiting
        };
    }

    /// <summary>The "due in days" of the approval chain a request follows; empty when no chain can be found.</summary>
    private async Task<int?> DueInDaysAsync(string requestType, int? employeeId, int? requestSubTypeId)
    {
        try
        {
            var master = await _requestMasterResolver.ResolveAsync(requestType, employeeId, requestSubTypeId);
            return master?.DueInDays;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A request whose type has since lost its chain still shows; it just cannot be overdue.
            Logger.Warn($"Workflow dashboard: no approval chain for {requestType}: {ex.Message}");
            return null;
        }
    }

    private static (DateTime From, DateTime To) ResolvePeriod(WorkflowDashboardInput input, DateTime today)
    {
        var from = input.FromDate?.Date ?? new DateTime(today.Year, 1, 1);
        var to = input.ToDate?.Date ?? today;

        if (to < from)
        {
            (from, to) = (to, from);
        }

        var earliest = to.AddYears(-MaxPeriodYears).AddDays(1);
        return (from < earliest ? earliest : from, to);
    }
}
