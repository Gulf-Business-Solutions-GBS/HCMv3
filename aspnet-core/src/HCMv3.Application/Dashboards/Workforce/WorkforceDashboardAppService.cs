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

        return new DashboardContext
        {
            Scope = scope,
            From = from,
            To = to,
            AsOf = asOf,
            Tree = tree,
            Calculator = calculator,
            ScopedEmployees = scopedEmployees,
            Population = calculator.ApplyAttributeFilters(population, input).ToList()
        };
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
