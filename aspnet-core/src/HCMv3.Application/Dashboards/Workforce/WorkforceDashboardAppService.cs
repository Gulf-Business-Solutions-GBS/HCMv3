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
using HCMv3.Dashboards.Workforce.Dto;
using HCMv3.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.Dashboards.Workforce;

/// <inheritdoc cref="IWorkforceDashboardAppService"/>
/// <remarks>
/// <para><b>Legacy source.</b> ESS <c>DashboardController.Workforce</c> / <c>DashboardRepository.GetWorkForceData</c>:
/// age groups, department, gender, nationality, years of service, qualification and job title, each drillable to
/// the employees behind it. Kept: the groups and the charts. Fixed rather than copied: under-21s (legacy's first
/// age group started at 21, so they vanished), age from the birth year only, and a drill-down list that used a
/// different employee set from the chart it came from.</para>
///
/// <para><b>Turnover</b> is new. Legacy's attempts never worked (see <see cref="WorkforceDashboardCalculator"/>).
/// Exits come only from recorded, unrevoked separations (user decision 2026-09-30).</para>
///
/// <para>Queries are synchronous <c>ToList()</c> over <c>GetAll()</c>, as in the Leave Dashboard, so the service
/// can be tested over plain lists.</para>
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_Dashboards_Workforce)]
public class WorkforceDashboardAppService : HCMv3AppServiceBase, IWorkforceDashboardAppService
{
    private const int MaxPeriodYears = 5;

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeSeparation> _separationRepository;
    private readonly IRepository<Nationality> _nationalityRepository;
    private readonly IRepository<Qualification> _qualificationRepository;
    private readonly IRepository<JobTitle> _jobTitleRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IRepository<EmployeeOnHold> _onHoldRepository;
    private readonly IRepository<EmployeeRejoin> _rejoinRepository;
    private readonly IRepository<EmployeeFnFPayment> _settlementRepository;
    private readonly IRepository<EmployeeDocument> _documentRepository;
    private readonly IRepository<DocumentType> _documentTypeRepository;
    private readonly IDashboardScopeService _dashboardScopeService;
    private readonly WorkforceDashboardExcelExporter _excelExporter;

    public WorkforceDashboardAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeSeparation> separationRepository,
        IRepository<Nationality> nationalityRepository,
        IRepository<Qualification> qualificationRepository,
        IRepository<JobTitle> jobTitleRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IRepository<EmployeeOnHold> onHoldRepository,
        IRepository<EmployeeRejoin> rejoinRepository,
        IRepository<EmployeeFnFPayment> settlementRepository,
        IRepository<EmployeeDocument> documentRepository,
        IRepository<DocumentType> documentTypeRepository,
        IDashboardScopeService dashboardScopeService,
        WorkforceDashboardExcelExporter excelExporter)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _separationRepository = separationRepository;
        _nationalityRepository = nationalityRepository;
        _qualificationRepository = qualificationRepository;
        _jobTitleRepository = jobTitleRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _onHoldRepository = onHoldRepository;
        _rejoinRepository = rejoinRepository;
        _settlementRepository = settlementRepository;
        _documentRepository = documentRepository;
        _documentTypeRepository = documentTypeRepository;
        _dashboardScopeService = dashboardScopeService;
        _excelExporter = excelExporter;
    }

    [HttpPost]
    public async Task<WorkforceDashboardDto> GetDashboard(WorkforceDashboardInput input)
    {
        var context = await BuildContextAsync(input);
        return BuildDashboard(context, input);
    }

    /// <summary>The employees behind the Headcount, Joiners and Leavers figures, with every filter applied.</summary>
    [HttpPost]
    public async Task<PagedResultDto<WorkforceEmployeeRowDto>> GetEmployees(GetWorkforceEmployeesInput input)
    {
        var context = await BuildContextAsync(input);
        var rows = ListRows(context, input.Kind);

        return new PagedResultDto<WorkforceEmployeeRowDto>(
            rows.Count,
            rows.Skip(input.SkipCount).Take(input.MaxResultCount).ToList());
    }

    /// <summary>The employees behind one of the movement counts, latest first.</summary>
    [HttpPost]
    public async Task<PagedResultDto<WorkforceMovementRowDto>> GetMovements(GetWorkforceMovementsInput input)
    {
        var context = await BuildContextAsync(input);
        var rows = MovementRows(context, input.Kind);
        return new PagedResultDto<WorkforceMovementRowDto>(rows.Count, rows.Skip(input.SkipCount).Take(input.MaxResultCount).ToList());
    }

    /// <summary>Documents expired or about to expire, soonest first, narrowed to a type and a status when given.</summary>
    [HttpPost]
    public async Task<PagedResultDto<WorkforceDocumentRowDto>> GetDocuments(GetWorkforceDocumentsInput input)
    {
        var context = await BuildContextAsync(input);
        var rows = DocumentRows(context, input.DocumentTypeId, input.Status);
        return new PagedResultDto<WorkforceDocumentRowDto>(rows.Count, rows.Skip(input.SkipCount).Take(input.MaxResultCount).ToList());
    }

    private static List<WorkforceMovementRowDto> MovementRows(DashboardContext context, WorkforceMovementKind? kind) =>
        context.Movements
            .Where(r => kind == null || r.Kind == kind)
            .OrderBy(r => r.Kind)
            .ThenByDescending(r => r.Date)
            .ThenBy(r => r.EmployeeCode)
            .ToList();

    private static List<WorkforceDocumentRowDto> DocumentRows(DashboardContext context, int? documentTypeId, WorkforceDocumentStatus? status) =>
        context.Documents
            .Where(r => (documentTypeId == null || r.DocumentTypeId == documentTypeId) && (status == null || r.Status == status))
            .ToList();

    /// <summary>The whole dashboard or one tile as a workbook, built from exactly what the screen shows.</summary>
    [HttpPost]
    public async Task<FileDto> ExportToExcel(ExportWorkforceDashboardInput input)
    {
        var context = await BuildContextAsync(input);
        var dashboard = BuildDashboard(context, input);
        var all = input.Part == WorkforceDashboardExportPart.All;

        List<WorkforceEmployeeRowDto> Rows(WorkforceDashboardExportPart part, WorkforceEmployeeListKind kind) =>
            all || input.Part == part ? ListRows(context, kind) : new List<WorkforceEmployeeRowDto>();

        return _excelExporter.Export(new WorkforceDashboardExportData
        {
            Part = input.Part,
            Dashboard = dashboard,
            HeadcountRows = Rows(WorkforceDashboardExportPart.Headcount, WorkforceEmployeeListKind.Headcount),
            JoinerRows = Rows(WorkforceDashboardExportPart.Joiners, WorkforceEmployeeListKind.Joiners),
            LeaverRows = Rows(WorkforceDashboardExportPart.Leavers, WorkforceEmployeeListKind.Leavers),
            MovementRows = all || input.Part == WorkforceDashboardExportPart.Movements ? MovementRows(context, null) : new List<WorkforceMovementRowDto>(),
            DocumentRows = all || input.Part == WorkforceDashboardExportPart.Documents ? DocumentRows(context, null, null) : new List<WorkforceDocumentRowDto>(),
            ScopeLabel = ScopeLabel(dashboard.Scope),
            UseArabic = CultureInfo.CurrentUICulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase)
        });
    }

    /// <summary>The same wording the screen shows above the tiles, for the export's Summary sheet.</summary>
    private string ScopeLabel(DashboardScopeDto scope)
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

    private WorkforceDashboardDto BuildDashboard(DashboardContext context, WorkforceDashboardInput input)
    {
        var calculator = context.Calculator;
        var population = context.Population;
        var headcount = population.Where(e => WorkforceDashboardCalculator.IsEmployedOn(e, context.AsOf)).ToList();

        return new WorkforceDashboardDto
        {
            Scope = new DashboardScopeDto
            {
                Reason = context.Scope.Reason,
                OrgUnitNames = context.Scope.OrgUnitNames.ToList(),
                IncludesOwnTeam = context.Scope.IncludesOwnTeam,
                EmployeesInScope = context.ScopedEmployees.Count(e => WorkforceDashboardCalculator.IsEmployedOn(e, context.AsOf))
            },
            AsOfDate = context.AsOf,
            FromDate = context.From,
            ToDate = context.To,
            Kpis = calculator.Kpis(population),
            Trend = calculator.Trend(population),
            AgeBands = calculator.AgeBands(headcount),
            ServiceBands = calculator.ServiceBands(headcount),
            Genders = WorkforceDashboardCalculator.Genders(headcount),
            Nationalities = calculator.Nationalities(headcount),
            Qualifications = calculator.Qualifications(headcount),
            JobTitles = calculator.JobTitles(headcount),
            ByOrgUnit = calculator.ByOrgUnit(population, input.OrganizationUnitId),
            Movements = WorkforceActivityCalculator.Counts(context.Movements),
            Documents = context.ActivityCalculator.DocumentBuckets(context.Documents),
            OrgUnitPath = context.Tree.PathTo(input.OrganizationUnitId),
            OrgUnitOptions = context.Tree.OptionsFor(context.ScopedEmployees.Select(e => e.OrganizationUnitId))
        };
    }

    private static List<WorkforceEmployeeRowDto> ListRows(DashboardContext context, WorkforceEmployeeListKind kind) =>
        context.Calculator.Select(context.Population, kind)
            .Select(context.Calculator.ToRow)
            .OrderBy(r => r.EmployeeCode)
            .ToList();

    // ---------------------------------------------------------------- context

    private sealed class DashboardContext
    {
        public DashboardScope Scope { get; init; }
        public DateTime From { get; init; }
        public DateTime To { get; init; }
        public DateTime AsOf { get; init; }
        public DashboardOrgTree Tree { get; init; }
        public WorkforceDashboardCalculator Calculator { get; init; }

        /// <summary>Everybody the user may see, before any filter.</summary>
        public List<WorkforceEmployee> ScopedEmployees { get; init; }

        /// <summary>After the unit filter and the clicked-bar filters: what every tile counts.</summary>
        public List<WorkforceEmployee> Population { get; init; }

        public WorkforceActivityCalculator ActivityCalculator { get; init; }

        /// <summary>Movements of the population: on hold, rejoined, separated, settled, awaiting settlement.</summary>
        public List<WorkforceMovementRowDto> Movements { get; init; }

        /// <summary>Documents of the population expired or about to expire.</summary>
        public List<WorkforceDocumentRowDto> Documents { get; init; }
    }

    private async Task<DashboardContext> BuildContextAsync(WorkforceDashboardInput input)
    {
        var today = Clock.Now.Date;
        var (from, to) = ResolvePeriod(input, today);
        var asOf = to > today ? today : to;

        // Null = every employee. An empty list (the scope resolving to nobody) stays empty, so every count below
        // is zero rather than the whole company.
        var scope = await _dashboardScopeService.GetScopeAsync();
        var scopeIds = scope.IsEveryone ? null : scope.EmployeeIds.ToList();

        var employeeQuery = _employeeRepository.GetAll();
        var assignmentQuery = _orgAssignmentRepository.GetAll();
        var separationQuery = _separationRepository.GetAll().Where(s => s.RevokeDate == null);
        if (scopeIds != null)
        {
            employeeQuery = employeeQuery.Where(e => scopeIds.Contains(e.Id));
            assignmentQuery = assignmentQuery.Where(a => scopeIds.Contains(a.EmployeeId));
            separationQuery = separationQuery.Where(s => scopeIds.Contains(s.EmployeeId));
        }

        // The assignment in force on the as-of date (latest already started); for someone whose first row starts
        // later, their earliest row. Job title and unit both come from it.
        var assignmentByEmployee = assignmentQuery
            .Select(a => new { a.EmployeeId, a.OrganizationUnitId, a.JobTitleId, a.StartDate })
            .ToList()
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(a => a.StartDate <= asOf).ThenByDescending(a => a.StartDate).First());

        // Latest unrevoked separation per employee.
        var separationByEmployee = separationQuery
            .Select(s => new { s.EmployeeId, s.LastDayInService, s.SeparationType })
            .ToList()
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.LastDayInService).First());

        var scopedEmployees = employeeQuery
            .Select(e => new
            {
                e.Id, e.Code, e.FullNameEn, e.FullNameAr, e.Gender, e.BirthDate, e.NationalityId, e.QualificationId,
                e.JoiningDate, e.Status
            })
            .ToList()
            .Select(e =>
            {
                assignmentByEmployee.TryGetValue(e.Id, out var assignment);
                separationByEmployee.TryGetValue(e.Id, out var separation);
                return new WorkforceEmployee(
                    e.Id, e.Code, e.FullNameEn, e.FullNameAr, e.Gender, e.BirthDate, e.NationalityId, e.QualificationId,
                    assignment?.JobTitleId, assignment?.OrganizationUnitId, e.JoiningDate,
                    e.Status == EmployeeStatus.Active, separation?.LastDayInService, separation?.SeparationType);
            })
            .ToList();

        var tree = new DashboardOrgTree(_organizationUnitRepository.GetAll()
            .Select(u => new DashboardOrgUnit(u.Id, u.ParentId, u.DisplayName))
            .ToList());

        var calculator = new WorkforceDashboardCalculator(
            from,
            asOf,
            tree,
            _nationalityRepository.GetAll().Select(n => new WorkforceLookup(n.Id, n.NameEn, n.NameAr)).ToList(),
            _qualificationRepository.GetAll().Select(q => new WorkforceLookup(q.Id, q.NameEn, q.NameAr)).ToList(),
            _jobTitleRepository.GetAll().Select(j => new WorkforceLookup(j.Id, j.NameEn, j.NameAr)).ToList());

        IEnumerable<WorkforceEmployee> population = scopedEmployees;
        if (input.OrganizationUnitId.HasValue)
        {
            var units = tree.SelfAndDescendants(input.OrganizationUnitId.Value);
            population = population.Where(e => e.OrganizationUnitId.HasValue && units.Contains(e.OrganizationUnitId.Value));
        }

        var filtered = calculator.ApplyAttributeFilters(population, input).ToList();
        var activity = BuildActivity(filtered, from, asOf, tree);

        return new DashboardContext
        {
            Scope = scope,
            From = from,
            To = to,
            AsOf = asOf,
            Tree = tree,
            Calculator = calculator,
            ScopedEmployees = scopedEmployees,
            Population = filtered,
            ActivityCalculator = activity.Calculator,
            Movements = activity.Movements,
            Documents = activity.Documents
        };
    }

    /// <summary>
    /// Holds, rejoins, separations, final settlements and documents of the filtered population, read once per
    /// request. Only the population's own rows are loaded.
    /// </summary>
    private (WorkforceActivityCalculator Calculator, List<WorkforceMovementRowDto> Movements, List<WorkforceDocumentRowDto> Documents) BuildActivity(
        List<WorkforceEmployee> population, DateTime from, DateTime asOf, DashboardOrgTree tree)
    {
        var ids = population.Select(e => e.Id).ToList();
        var documentTypes = _documentTypeRepository.GetAll()
            .Select(t => new { t.Id, t.NameEn, t.NameAr, t.NotificationPeriod })
            .ToList()
            .Select(t => new WorkforceDocumentType(t.Id, t.NameEn, t.NameAr, t.NotificationPeriod))
            .ToList();
        var activity = new WorkforceActivityCalculator(from, asOf, tree, population, documentTypes);

        if (ids.Count == 0)
        {
            return (activity, new List<WorkforceMovementRowDto>(), new List<WorkforceDocumentRowDto>());
        }

        var holds = _onHoldRepository.GetAll()
            .Where(h => h.OnHold && ids.Contains(h.EmployeeId))
            .Select(h => new { h.EmployeeId, h.StartDate, h.EndDate })
            .ToList()
            .Select(h => new WorkforceHold(h.EmployeeId, h.StartDate, h.EndDate))
            .ToList();
        var rejoins = _rejoinRepository.GetAll()
            .Where(r => ids.Contains(r.EmployeeId))
            .Select(r => new { r.EmployeeId, r.RejoinDate })
            .ToList()
            .Select(r => new WorkforceDatedEvent(r.EmployeeId, r.RejoinDate))
            .ToList();
        var separations = _separationRepository.GetAll()
            .Where(s => s.RevokeDate == null && ids.Contains(s.EmployeeId))
            .Select(s => new { s.EmployeeId, s.SubmissionDate, s.LastDayInService })
            .ToList()
            .Select(s => new WorkforceSeparation(s.EmployeeId, s.SubmissionDate, s.LastDayInService))
            .ToList();
        var settlements = _settlementRepository.GetAll()
            .Where(p => ids.Contains(p.EmployeeId))
            .Select(p => new { p.EmployeeId, p.CompensationDate })
            .ToList()
            .Select(p => new WorkforceDatedEvent(p.EmployeeId, p.CompensationDate))
            .ToList();

        // Approved documents only: the default ApprovedOnly filter is on. Every document is read, not only those
        // near expiry: the latest of each type decides, so a renewal must be seen to hide the old expired one.
        var floor = new DateTime(1900, 1, 1);
        var documents = _documentRepository.GetAll()
            .Where(d => ids.Contains(d.EmployeeId) && d.ExpiryDate > floor)
            .Select(d => new { d.EmployeeId, d.DocumentTypeId, d.DocumentNum, d.ExpiryDate })
            .ToList()
            .Select(d => new WorkforceDocument(d.EmployeeId, d.DocumentTypeId, d.DocumentNum, d.ExpiryDate))
            .ToList();

        return (activity, activity.Movements(holds, rejoins, separations, settlements), activity.Documents(documents));
    }

    private static (DateTime From, DateTime To) ResolvePeriod(WorkforceDashboardInput input, DateTime today)
    {
        var from = input.FromDate?.Date ?? new DateTime(today.Year, 1, 1);
        var to = input.ToDate?.Date ?? today;

        if (to < from)
        {
            (from, to) = (to, from);
        }

        // A mistyped year must not turn into hundreds of monthly trend points over the whole company.
        var earliest = to.AddYears(-MaxPeriodYears).AddDays(1);
        return (from < earliest ? earliest : from, to);
    }
}
