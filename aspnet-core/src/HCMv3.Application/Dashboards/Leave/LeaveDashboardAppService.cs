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
using HCMv3.Dashboards.Leave.Dto;
using HCMv3.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.Dashboards.Leave;

/// <inheritdoc cref="ILeaveDashboardAppService"/>
/// <remarks>
/// <para><b>Legacy sources.</b> HR's <c>Leave_Dashboard</c> DashboardView (<c>Model.DesignedDiffs.xafml</c>)
/// had two EmployeeAbsence lists, "On Leave (as of today)" and "Not Returned from Leave"; both are here. ESS's
/// Leave Planner (<c>DashboardController.GetMonthlyUtilizationTrend</c>, <c>GetLeaveShareByMonthPie</c>,
/// <c>GetDeptComparisonPie</c>) gave the monthly and per-department charts. Its planned-versus-actual figures
/// read the legacy <c>LeavePlanner</c> table, which HCMv3 does not have, so they are not reproduced.</para>
///
/// <para><b>Hourly absences are left out</b> (<c>AbsenceHours &gt; 0</c>): they are short permissions, not
/// days away, and would inflate "on leave today".</para>
///
/// <para>Queries are synchronous <c>ToList()</c> over <c>GetAll()</c>, as in
/// <c>PerformanceAppraisalDashboardAppService</c>, so the service can be tested over plain lists.</para>
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_Dashboards_Leave)]
public class LeaveDashboardAppService : HCMv3AppServiceBase, ILeaveDashboardAppService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeAbsence> _absenceRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IDashboardScopeService _dashboardScopeService;
    private readonly LeaveDashboardExcelExporter _excelExporter;

    public LeaveDashboardAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeAbsence> absenceRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IDashboardScopeService dashboardScopeService,
        LeaveDashboardExcelExporter excelExporter)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _absenceRepository = absenceRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _dashboardScopeService = dashboardScopeService;
        _excelExporter = excelExporter;
    }

    /// <summary>The leave types for the filter. Whose leave is shown is not an option: see <see cref="IDashboardScopeService"/>.</summary>
    public Task<LeaveDashboardOptionsDto> GetOptions()
    {
        var options = new LeaveDashboardOptionsDto
        {
            AbsenceTypes = _absenceTypeRepository.GetAll()
                .Where(t => t.IsActive)
                .OrderBy(t => t.NameEn)
                .Select(t => new LeaveDashboardAbsenceTypeOptionDto { Id = t.Id, NameEn = t.NameEn, NameAr = t.NameAr })
                .ToList()
        };

        return Task.FromResult(options);
    }

    [HttpPost]
    public async Task<LeaveDashboardDto> GetDashboard(LeaveDashboardInput input)
    {
        var context = await BuildContextAsync(input);
        return BuildDashboard(context, input);
    }

    /// <summary>
    /// The whole dashboard (<see cref="LeaveDashboardExportPart.All"/>) or one tile as a workbook. Built from exactly
    /// what <see cref="GetDashboard"/> returns, so the file always matches the screen.
    /// </summary>
    [HttpPost]
    public async Task<FileDto> ExportToExcel(ExportLeaveDashboardInput input)
    {
        var context = await BuildContextAsync(input);
        var dashboard = BuildDashboard(context, input);

        var periodLeaves = input.Part is LeaveDashboardExportPart.All or LeaveDashboardExportPart.LeavesInPeriod
            ? LoadPeriodRows(context, input.Part == LeaveDashboardExportPart.LeavesInPeriod ? input.Day : null)
            : new List<LeaveDashboardLeaveRowDto>();

        var arabic = CultureInfo.CurrentUICulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
        var typeNames = context.AbsenceTypes.ToDictionary(
            t => t.Id,
            t => arabic && !string.IsNullOrWhiteSpace(t.NameAr) ? t.NameAr : t.NameEn ?? t.NameAr);

        var selectedType = input.AbsenceTypeId.HasValue && typeNames.TryGetValue(input.AbsenceTypeId.Value, out var typeName)
            ? typeName
            : L("LeaveDashboard_AllLeaveTypes");

        return _excelExporter.Export(new LeaveDashboardExportData
        {
            Part = input.Part,
            Dashboard = dashboard,
            PeriodLeaves = periodLeaves,
            Day = input.Part == LeaveDashboardExportPart.LeavesInPeriod ? input.Day?.Date : null,
            ScopeLabel = ScopeLabel(dashboard.Scope),
            AbsenceTypeLabel = selectedType,
            TypeNames = typeNames,
            UseArabic = arabic
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

    private LeaveDashboardDto BuildDashboard(DashboardContext context, LeaveDashboardInput input)
    {
        var calculator = context.Calculator;
        var today = context.Today;

        var periodLeaves = LoadLeaves(context, q => q.Where(a => a.StartDate <= context.To && a.EndDate >= context.From));

        var soon = today.AddDays(LeaveDashboardCalculator.SoonDays);
        var currentLeaves = LoadLeaves(context, q => q.Where(a => a.StartDate <= soon && a.EndDate >= today));

        var returnRequiredTypeIds = context.AbsenceTypes.Where(t => t.IsReturnDateRequired).Select(t => t.Id).ToList();
        var overdueLeaves = LoadLeaves(context, q => q.Where(a => a.EndDate < today
                                                                  && !a.IsReturned
                                                                  && a.ReturnDate == default(DateTime)
                                                                  && returnRequiredTypeIds.Contains(a.AbsenceTypeId)));

        List<LeaveDashboardLeaveFact> pendingLeaves;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            pendingLeaves = LoadLeaves(context, q => q.Where(a => a.WorkflowStatus == RequestStatus.Open));
        }

        var dto = new LeaveDashboardDto
        {
            Scope = new DashboardScopeDto
            {
                Reason = context.Scope.Reason,
                OrgUnitNames = context.Scope.OrgUnitNames.ToList(),
                IncludesOwnTeam = context.Scope.IncludesOwnTeam,
                EmployeesInScope = context.ScopedEmployees.Count(e => e.IsActive)
            },
            AsOfDate = today,
            FromDate = context.From,
            ToDate = context.To,
            OnLeaveToday = calculator.OnLeaveToday(currentLeaves),
            NotReturned = calculator.NotReturned(overdueLeaves),
            StartingSoon = calculator.StartingSoon(currentLeaves),
            ReturningSoon = calculator.ReturningSoon(currentLeaves),
            PendingApproval = calculator.PendingApproval(pendingLeaves),
            Granularity = calculator.Granularity,
            Trend = calculator.Trend(periodLeaves),
            ByOrgUnit = calculator.ByOrgUnit(periodLeaves, input.OrganizationUnitId),
            ByType = calculator.ByType(periodLeaves),
            OrgUnitPath = calculator.PathTo(input.OrganizationUnitId),
            OrgUnitOptions = calculator.OrgUnitOptions(context.ScopedEmployees)
        };

        dto.Kpis = new LeaveDashboardKpisDto
        {
            EmployeesInScope = context.FilteredEmployees.Count(e => e.IsActive),
            OnLeaveToday = LeaveDashboardCalculator.DistinctEmployees(dto.OnLeaveToday),
            NotReturned = LeaveDashboardCalculator.DistinctEmployees(dto.NotReturned),
            StartingSoon = LeaveDashboardCalculator.DistinctEmployees(dto.StartingSoon),
            ReturningSoon = LeaveDashboardCalculator.DistinctEmployees(dto.ReturningSoon),
            PendingApproval = dto.PendingApproval.Count,
            LeaveDaysInPeriod = calculator.LeaveDaysInPeriod(periodLeaves)
        };

        return dto;
    }

    /// <summary>The leave behind "leave days in period" and behind a bar of the daily trend.</summary>
    [HttpPost]
    public async Task<PagedResultDto<LeaveDashboardLeaveRowDto>> GetLeaves(GetLeaveDashboardLeavesInput input)
    {
        var context = await BuildContextAsync(input);
        var rows = LoadPeriodRows(context, input.Day);

        return new PagedResultDto<LeaveDashboardLeaveRowDto>(
            rows.Count,
            rows.Skip(input.SkipCount).Take(input.MaxResultCount).ToList());
    }

    /// <summary>Every leave overlapping the period, or covering <paramref name="day"/> when given; newest first.</summary>
    private List<LeaveDashboardLeaveRowDto> LoadPeriodRows(DashboardContext context, DateTime? day)
    {
        var from = day?.Date ?? context.From;
        var to = day?.Date ?? context.To;

        return LoadLeaves(context, q => q.Where(a => a.StartDate <= to && a.EndDate >= from))
            .Select(context.Calculator.ToRow)
            .OrderByDescending(r => r.StartDate)
            .ThenBy(r => r.EmployeeCode)
            .ToList();
    }

    // ---------------------------------------------------------------- context

    private sealed class DashboardContext
    {
        public DashboardScope Scope { get; init; }
        public DateTime Today { get; init; }
        public DateTime From { get; init; }
        public DateTime To { get; init; }
        public int? AbsenceTypeId { get; init; }
        public List<LeaveDashboardEmployee> ScopedEmployees { get; init; }
        public List<LeaveDashboardEmployee> FilteredEmployees { get; init; }

        /// <summary>Null when every employee is in scope and no unit is selected: no need to filter by id.</summary>
        public List<int> EmployeeIdFilter { get; init; }

        public List<AbsenceTypeRow> AbsenceTypes { get; init; }
        public LeaveDashboardCalculator Calculator { get; init; }
    }

    private sealed record AbsenceTypeRow(int Id, string NameEn, string NameAr, bool IsReturnDateRequired);

    private async Task<DashboardContext> BuildContextAsync(LeaveDashboardInput input)
    {
        var today = Clock.Now.Date;
        var (from, to) = ResolvePeriod(input, today);

        // Null = every employee. An empty list (row-level security resolving to nobody) stays empty, so every
        // query below returns nothing rather than everything.
        var scope = await _dashboardScopeService.GetScopeAsync();
        var scopeIds = scope.IsEveryone ? null : scope.EmployeeIds.ToList();

        var employeeQuery = _employeeRepository.GetAll();
        if (scopeIds != null)
        {
            employeeQuery = employeeQuery.Where(e => scopeIds.Contains(e.Id));
        }

        var employeeRows = employeeQuery
            .Select(e => new { e.Id, e.Code, e.FullNameEn, e.FullNameAr, e.Status })
            .ToList();

        var assignmentQuery = _orgAssignmentRepository.GetAll();
        if (scopeIds != null)
        {
            assignmentQuery = assignmentQuery.Where(a => scopeIds.Contains(a.EmployeeId));
        }

        // "Current" is the latest row already started; a new joiner whose first row starts tomorrow still
        // gets that row -- the same rule as the Employee Workbench.
        var unitByEmployee = assignmentQuery
            .Select(a => new { a.EmployeeId, a.OrganizationUnitId, a.StartDate })
            .ToList()
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(a => a.StartDate <= today).ThenByDescending(a => a.StartDate).First().OrganizationUnitId);

        var scopedEmployees = employeeRows
            .Select(e => new LeaveDashboardEmployee(
                e.Id,
                e.Code,
                e.FullNameEn,
                e.FullNameAr,
                e.Status == EmployeeStatus.Active,
                unitByEmployee.TryGetValue(e.Id, out var unitId) ? unitId : null))
            .ToList();

        var orgUnits = _organizationUnitRepository.GetAll()
            .Select(u => new LeaveDashboardOrgUnit(u.Id, u.ParentId, u.DisplayName))
            .ToList();

        var absenceTypes = _absenceTypeRepository.GetAll()
            .Select(t => new AbsenceTypeRow(t.Id, t.NameEn, t.NameAr, t.IsReturnDateRequired))
            .ToList();

        var calculator = new LeaveDashboardCalculator(
            today,
            from,
            to,
            scopedEmployees,
            orgUnits,
            absenceTypes.Select(t => new LeaveDashboardAbsenceType(t.Id, t.NameEn, t.NameAr)));

        var filteredEmployees = scopedEmployees;
        if (input.OrganizationUnitId.HasValue)
        {
            var units = calculator.SelfAndDescendants(input.OrganizationUnitId.Value);
            filteredEmployees = scopedEmployees
                .Where(e => e.OrganizationUnitId.HasValue && units.Contains(e.OrganizationUnitId.Value))
                .ToList();
        }

        return new DashboardContext
        {
            Scope = scope,
            Today = today,
            From = from,
            To = to,
            AbsenceTypeId = input.AbsenceTypeId,
            ScopedEmployees = scopedEmployees,
            FilteredEmployees = filteredEmployees,
            EmployeeIdFilter = scopeIds == null && !input.OrganizationUnitId.HasValue
                ? null
                : filteredEmployees.Select(e => e.Id).ToList(),
            AbsenceTypes = absenceTypes,
            Calculator = calculator
        };
    }

    private List<LeaveDashboardLeaveFact> LoadLeaves(
        DashboardContext context,
        Func<IQueryable<EmployeeAbsence>, IQueryable<EmployeeAbsence>> narrow)
    {
        var query = narrow(_absenceRepository.GetAll().Where(a => a.AbsenceHours == 0));

        if (context.AbsenceTypeId.HasValue)
        {
            var typeId = context.AbsenceTypeId.Value;
            query = query.Where(a => a.AbsenceTypeId == typeId);
        }

        if (context.EmployeeIdFilter != null)
        {
            var ids = context.EmployeeIdFilter;
            query = query.Where(a => ids.Contains(a.EmployeeId));
        }

        var returnRequired = context.AbsenceTypes.Where(t => t.IsReturnDateRequired).Select(t => t.Id).ToHashSet();

        return query
            .Select(a => new
            {
                a.Id,
                a.EmployeeId,
                a.AbsenceTypeId,
                a.StartDate,
                a.EndDate,
                a.TotalLeaveDays,
                a.IsReturned,
                a.ReturnDate,
                a.WorkflowStatus
            })
            .ToList()
            .Select(a => new LeaveDashboardLeaveFact(
                a.Id,
                a.EmployeeId,
                a.AbsenceTypeId,
                a.StartDate,
                a.EndDate,
                a.TotalLeaveDays,
                a.IsReturned,
                a.ReturnDate,
                returnRequired.Contains(a.AbsenceTypeId),
                a.WorkflowStatus))
            .ToList();
    }

    private static (DateTime From, DateTime To) ResolvePeriod(LeaveDashboardInput input, DateTime today)
    {
        var from = input.FromDate?.Date ?? new DateTime(today.Year, 1, 1);
        var to = input.ToDate?.Date ?? new DateTime(today.Year, 12, 31);

        if (to < from)
        {
            (from, to) = (to, from);
        }

        // A mistyped year must not turn into hundreds of monthly buckets over the whole leave table.
        var earliest = to.AddYears(-MaxPeriodYears).AddDays(1);
        return (from < earliest ? earliest : from, to);
    }

    private const int MaxPeriodYears = 5;
}
