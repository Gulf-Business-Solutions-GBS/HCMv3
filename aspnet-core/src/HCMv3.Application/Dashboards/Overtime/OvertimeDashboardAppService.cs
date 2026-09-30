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
using HCMv3.Dashboards.Overtime.Dto;
using HCMv3.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.Dashboards.Overtime;

/// <inheritdoc cref="IOvertimeDashboardAppService"/>
/// <remarks>
/// <para><b>Legacy source.</b> ESS <c>DashboardController.Overtime</c> / <c>GetOvertimeInfoByType</c>: approved hours by
/// overtime type and by department, the top departments split by type, and a drill-down grid with an overtime
/// amount. Kept: those figures. Changed: the amount is what payroll paid (see <see cref="OvertimeDashboardCalculator"/>)
/// rather than hours times today's basic pay; the series are not hard-coded to type codes 1-4; the department is
/// the assignment in force at the end of the period rather than "current".</para>
///
/// <para>Queries are synchronous <c>ToList()</c> over <c>GetAll()</c>, as in the other dashboards, so the service can
/// be tested over plain lists.</para>
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_Dashboards_Overtime)]
public class OvertimeDashboardAppService : HCMv3AppServiceBase, IOvertimeDashboardAppService
{
    private const int MaxPeriodYears = 5;

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeOvertime> _overtimeRepository;
    private readonly IRepository<OvertimeType> _overtimeTypeRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IDashboardScopeService _dashboardScopeService;
    private readonly OvertimeDashboardExcelExporter _excelExporter;

    public OvertimeDashboardAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeOvertime> overtimeRepository,
        IRepository<OvertimeType> overtimeTypeRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IDashboardScopeService dashboardScopeService,
        OvertimeDashboardExcelExporter excelExporter)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _overtimeRepository = overtimeRepository;
        _overtimeTypeRepository = overtimeTypeRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _dashboardScopeService = dashboardScopeService;
        _excelExporter = excelExporter;
    }

    [HttpPost]
    public async Task<OvertimeDashboardDto> GetDashboard(OvertimeDashboardInput input)
    {
        var context = await BuildContextAsync(input);
        return BuildDashboard(context, input);
    }

    /// <summary>The entries behind a figure or a clicked month; newest first.</summary>
    [HttpPost]
    public async Task<PagedResultDto<OvertimeEntryRowDto>> GetEntries(GetOvertimeEntriesInput input)
    {
        var context = await BuildContextAsync(input);
        var rows = EntryRows(context, input.Kind, input.Month);
        return new PagedResultDto<OvertimeEntryRowDto>(rows.Count, rows.Skip(input.SkipCount).Take(input.MaxResultCount).ToList());
    }

    [HttpPost]
    public async Task<FileDto> ExportToExcel(ExportOvertimeDashboardInput input)
    {
        var context = await BuildContextAsync(input);
        var all = input.Part == OvertimeDashboardExportPart.All;
        var dashboard = BuildDashboard(context, input);

        return _excelExporter.Export(new OvertimeDashboardExportData
        {
            Part = input.Part,
            Dashboard = dashboard,
            Entries = all || input.Part == OvertimeDashboardExportPart.Entries
                ? EntryRows(context, all ? OvertimeEntryListKind.Approved : input.Kind, all ? null : input.Month)
                : new List<OvertimeEntryRowDto>(),
            TypeNames = dashboard.Types.ToDictionary(t => t.Id, t => Pick(t.NameEn, t.NameAr)),
            ScopeLabel = ScopeLabel(context.Scope),
            UseArabic = UseArabic
        });
    }

    // ---------------------------------------------------------------- building

    private static bool UseArabic => CultureInfo.CurrentUICulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

    private static string Pick(string en, string ar) =>
        UseArabic ? (string.IsNullOrWhiteSpace(ar) ? en : ar) : (string.IsNullOrWhiteSpace(en) ? ar : en);

    private OvertimeDashboardDto BuildDashboard(DashboardContext context, OvertimeDashboardInput input)
    {
        var calculator = context.Calculator;
        var selected = input.EmployeeId.HasValue ? context.ScopedEmployees.FirstOrDefault(e => e.Id == input.EmployeeId.Value) : null;

        return new OvertimeDashboardDto
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
            EmployeeNameEn = selected?.NameEn,
            EmployeeNameAr = selected?.NameAr,
            Kpis = calculator.Kpis(context.Entries, context.Costs, context.Pending),
            Types = context.Types,
            Trend = calculator.Trend(context.Entries, context.Costs),
            ByType = OvertimeDashboardCalculator.ByType(context.Entries, context.Costs),
            ByOrgUnit = calculator.ByOrgUnit(context.Entries, context.Costs, input.OrganizationUnitId),
            TopEmployees = calculator.TopEmployees(context.Entries, context.Costs),
            OrgUnitPath = context.Tree.PathTo(input.OrganizationUnitId),
            OrgUnitOptions = context.Tree.OptionsFor(context.ScopedEmployees.Select(e => e.OrganizationUnitId))
        };
    }

    private static List<OvertimeEntryRowDto> EntryRows(DashboardContext context, OvertimeEntryListKind kind, DateTime? month)
    {
        IEnumerable<OvertimeEntry> source = kind == OvertimeEntryListKind.Pending ? context.Pending : context.Entries;
        if (month.HasValue)
        {
            var wanted = OvertimeDashboardCalculator.MonthOf(month.Value);
            source = source.Where(e => OvertimeDashboardCalculator.MonthOf(e.EffectiveDate) == wanted);
        }

        return source
            .Select(context.Calculator.ToRow)
            .OrderByDescending(r => r.EffectiveDate)
            .ThenByDescending(r => r.ApprovedHours)
            .ThenBy(r => r.EmployeeCode)
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
        public OvertimeDashboardCalculator Calculator { get; init; }
        public List<OvertimeEmployee> ScopedEmployees { get; init; }
        public int EmployeesInScope { get; init; }
        public List<OvertimeTypeOptionDto> Types { get; init; }
        public List<OvertimeEntry> Entries { get; init; }
        public List<OvertimeEntry> Pending { get; init; }
        public List<OvertimeCost> Costs { get; init; }
    }

    private async Task<DashboardContext> BuildContextAsync(OvertimeDashboardInput input)
    {
        var today = Clock.Now.Date;
        var (from, to) = ResolvePeriod(input, today);

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
            .Select(e => new OvertimeEmployee(e.Id, e.Code, e.FullNameEn, e.FullNameAr, unitByEmployee.TryGetValue(e.Id, out var u) ? u : null))
            .ToList();

        var tree = new DashboardOrgTree(_organizationUnitRepository.GetAll()
            .Select(u => new DashboardOrgUnit(u.Id, u.ParentId, u.DisplayName))
            .ToList());

        IEnumerable<OvertimeEmployee> filtered = scopedEmployees;
        if (input.OrganizationUnitId.HasValue)
        {
            var units = tree.SelfAndDescendants(input.OrganizationUnitId.Value);
            filtered = filtered.Where(e => e.OrganizationUnitId.HasValue && units.Contains(e.OrganizationUnitId.Value));
        }

        if (input.EmployeeId.HasValue)
        {
            filtered = filtered.Where(e => e.Id == input.EmployeeId.Value);
        }

        var ids = filtered.Select(e => e.Id).ToList();
        var byId = scopeIds != null || input.OrganizationUnitId.HasValue || input.EmployeeId.HasValue;

        var typeRows = _overtimeTypeRepository.GetAll()
            .Select(t => new { t.Id, t.Code, t.NameEn, t.NameAr, t.WageTypeId })
            .ToList();
        var types = typeRows
            .OrderBy(t => t.Code)
            .Select(t => new OvertimeTypeOptionDto { Id = t.Id, Code = t.Code, NameEn = t.NameEn, NameAr = t.NameAr })
            .ToList();

        IQueryable<EmployeeOvertime> Narrow(IQueryable<EmployeeOvertime> q)
        {
            if (byId)
            {
                q = q.Where(o => ids.Contains(o.EmployeeId));
            }

            if (input.OvertimeTypeId.HasValue)
            {
                var typeId = input.OvertimeTypeId.Value;
                q = q.Where(o => o.OvertimeTypeId == typeId);
            }

            return q;
        }

        static List<OvertimeEntry> Project(IQueryable<EmployeeOvertime> q) =>
            q.Select(o => new { o.EmployeeId, o.EffectiveDate, o.OvertimeTypeId, o.OvertimeHours, o.ActualOvertimeHours, o.ApprovedOvertimeHours, o.WorkflowStatus })
                .ToList()
                .Select(o => new OvertimeEntry(o.EmployeeId, o.EffectiveDate, o.OvertimeTypeId, o.OvertimeHours,
                    o.ActualOvertimeHours, o.ApprovedOvertimeHours, o.WorkflowStatus))
                .ToList();

        // Approved entries only: the default ApprovedOnly filter is on.
        var entries = Project(Narrow(_overtimeRepository.GetAll().Where(o => o.EffectiveDate >= from && o.EffectiveDate <= to)));

        // Pending requests, whatever their month: the filter that hides them from payroll is lifted for this read.
        List<OvertimeEntry> pending;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            pending = Project(Narrow(_overtimeRepository.GetAll().Where(o => o.WorkflowStatus == RequestStatus.Open)));
        }

        var costs = LoadCosts(from, to, byId ? ids : null, typeRows.Where(t => t.WageTypeId.HasValue)
            .Where(t => !input.OvertimeTypeId.HasValue || t.Id == input.OvertimeTypeId.Value)
            .GroupBy(t => t.WageTypeId.Value)
            .ToDictionary(g => g.Key, g => g.First().Id));

        return new DashboardContext
        {
            Scope = scope,
            From = from,
            To = to,
            Tree = tree,
            Calculator = new OvertimeDashboardCalculator(from, to, tree, scopedEmployees),
            ScopedEmployees = scopedEmployees,
            EmployeesInScope = employeeRows.Count(e => e.Status == EmployeeStatus.Active),
            Types = types,
            Entries = entries,
            Pending = pending,
            Costs = costs
        };
    }

    /// <summary>
    /// Payroll amounts under the overtime wage types, from each month's regular result -- the register calculated in
    /// the month it is for (<c>InPeriod == ForPeriod</c>), as legacy's payroll dashboard read it -- so a retro
    /// recalculation is not counted twice.
    /// </summary>
    private List<OvertimeCost> LoadCosts(DateTime from, DateTime to, List<int> employeeIds, Dictionary<int, int> typeByWageType)
    {
        if (typeByWageType.Count == 0)
        {
            return new List<OvertimeCost>();
        }

        var firstMonth = OvertimeDashboardCalculator.MonthOf(from);
        var lastDay = OvertimeDashboardCalculator.MonthOf(to).AddMonths(1).AddDays(-1);

        var registerQuery = _salaryRegisterRepository.GetAll().Where(r => r.ForPeriod >= firstMonth && r.ForPeriod <= lastDay);
        if (employeeIds != null)
        {
            registerQuery = registerQuery.Where(r => employeeIds.Contains(r.EmployeeId));
        }

        var registers = registerQuery
            .Select(r => new { r.Id, r.EmployeeId, r.ForPeriod, r.InPeriod })
            .ToList()
            .Where(r => r.InPeriod.Date == r.ForPeriod.Date)
            .ToDictionary(r => r.Id);
        if (registers.Count == 0)
        {
            return new List<OvertimeCost>();
        }

        var registerIds = registers.Keys.ToList();
        var wageTypeIds = typeByWageType.Keys.ToList();

        return _salaryRegisterDetailRepository.GetAll()
            .Where(d => registerIds.Contains(d.SalaryRegisterId) && wageTypeIds.Contains(d.WageTypeId))
            .Select(d => new { d.SalaryRegisterId, d.WageTypeId, d.Amount })
            .ToList()
            .Select(d =>
            {
                var register = registers[d.SalaryRegisterId];
                return new OvertimeCost(register.EmployeeId, register.ForPeriod, typeByWageType[d.WageTypeId], d.Amount);
            })
            .ToList();
    }

    private static (DateTime From, DateTime To) ResolvePeriod(OvertimeDashboardInput input, DateTime today)
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
