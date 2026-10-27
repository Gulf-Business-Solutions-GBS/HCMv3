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
using HCMv3.Dashboards.Payroll.Dto;
using HCMv3.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.Dashboards.Payroll;

/// <inheritdoc cref="IPayrollDashboardAppService"/>
/// <remarks>
/// <para><b>Legacy source.</b> ESS <c>DashboardController.Payroll</c> (net and basic pay by department, basic-salary
/// ranges, run type, payment type, for one month) and <c>CostToCompany</c> (one employee's salary lines and
/// end-of-service figures). Kept: those figures, the CTC card now opened from any payslip. Changed: gross and
/// deductions follow the engine's net-pay rule instead of five hard-coded codes (legacy read "OT on normal day" as the
/// unpaid-absence deduction); the bands have no gaps; a period rather than one month; one row per payslip, where
/// legacy kept only an employee's first register of the month.</para>
///
/// <para>Queries are synchronous <c>ToList()</c> over <c>GetAll()</c>, as in the other dashboards, so the service can
/// be tested over plain lists.</para>
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_Dashboards_Payroll)]
public class PayrollDashboardAppService : HCMv3AppServiceBase, IPayrollDashboardAppService
{
    private const int MaxPeriodYears = 5;

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeBank> _employeeBankRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IDashboardScopeService _dashboardScopeService;
    private readonly PayrollDashboardExcelExporter _excelExporter;

    public PayrollDashboardAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeBank> employeeBankRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IDashboardScopeService dashboardScopeService,
        PayrollDashboardExcelExporter excelExporter)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _employeeBankRepository = employeeBankRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _wageTypeRepository = wageTypeRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _dashboardScopeService = dashboardScopeService;
        _excelExporter = excelExporter;
    }

    [HttpPost]
    public async Task<PayrollDashboardDto> GetDashboard(PayrollDashboardInput input)
    {
        var context = await BuildContextAsync(input);
        return BuildDashboard(context, input);
    }

    /// <summary>The payslips behind a figure: a month, a basic band, a payment method or a run type.</summary>
    [HttpPost]
    public async Task<PagedResultDto<PayrollEmployeeRowDto>> GetEmployees(GetPayrollEmployeesInput input)
    {
        var context = await BuildContextAsync(input);
        var rows = EmployeeRows(context, input.Kind, input.Month, input.Band, input.PaymentType, input.RunType);
        return new PagedResultDto<PayrollEmployeeRowDto>(rows.Count, rows.Skip(input.SkipCount).Take(input.MaxResultCount).ToList());
    }

    /// <summary>
    /// One employee's cost card. Only an employee the dashboard would show: outside the user's scope the card is
    /// empty, never someone else's salary.
    /// </summary>
    [HttpPost]
    public async Task<PayrollCostCardDto> GetCostCard(GetPayrollCostCardInput input)
    {
        var to = input.ToDate?.Date ?? Clock.Now.Date;
        var month = input.Month.HasValue ? PayrollDashboardCalculator.MonthOf(input.Month.Value) : (DateTime?)null;
        var from = month ?? PayrollDashboardCalculator.MonthOf(to).AddYears(-MaxPeriodYears);
        var context = await BuildContextAsync(new PayrollDashboardInput
        {
            FromDate = from,
            ToDate = month?.AddMonths(1).AddDays(-1) ?? to,
            EmployeeId = input.EmployeeId
        });

        if (context.ScopedEmployees.All(e => e.Id != input.EmployeeId))
        {
            return new PayrollCostCardDto { EmployeeId = input.EmployeeId, Balances = new PayrollProvisionsDto() };
        }

        var payslip = context.Payslips
            .Where(p => p.EmployeeId == input.EmployeeId && p.RunType == PayrollRunType.Regular)
            .OrderByDescending(p => p.Month)
            .FirstOrDefault();

        return context.Calculator.CostCard(input.EmployeeId, payslip);
    }

    [HttpPost]
    public async Task<FileDto> ExportToExcel(ExportPayrollDashboardInput input)
    {
        var context = await BuildContextAsync(input);
        var all = input.Part == PayrollDashboardExportPart.All;
        var dashboard = BuildDashboard(context, input);

        return _excelExporter.Export(new PayrollDashboardExportData
        {
            Part = input.Part,
            Dashboard = dashboard,
            Employees = all || input.Part == PayrollDashboardExportPart.Employees
                ? all
                    ? EmployeeRows(context, PayrollDashboardListKind.Month, null, null, null, null)
                    : EmployeeRows(context, input.Kind, input.Month, input.Band, input.PaymentType, input.RunType)
                : new List<PayrollEmployeeRowDto>(),
            ScopeLabel = ScopeLabel(context.Scope),
            UseArabic = UseArabic
        });
    }

    // ---------------------------------------------------------------- building

    private static bool UseArabic => CultureInfo.CurrentUICulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

    private PayrollDashboardDto BuildDashboard(DashboardContext context, PayrollDashboardInput input)
    {
        var calculator = context.Calculator;
        var payslips = context.Payslips;
        var selected = input.EmployeeId.HasValue ? context.ScopedEmployees.FirstOrDefault(e => e.Id == input.EmployeeId.Value) : null;

        return new PayrollDashboardDto
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
            LatestMonth = context.LatestMonth,
            EmployeeNameEn = selected?.NameEn,
            EmployeeNameAr = selected?.NameAr,
            Kpis = calculator.Kpis(payslips),
            Trend = calculator.Trend(payslips),
            CostMix = calculator.CostMix(payslips),
            ByOrgUnit = calculator.ByOrgUnit(payslips, input.OrganizationUnitId),
            BasicBands = calculator.BasicBands(payslips),
            Earnings = calculator.Earnings(payslips),
            Deductions = calculator.Deductions(payslips),
            PaymentMethods = calculator.PaymentMethods(payslips, context.PaymentTypes),
            RunTypes = calculator.RunTypes(payslips),
            Provisions = calculator.Provisions(payslips),
            OrgUnitPath = context.Tree.PathTo(input.OrganizationUnitId),
            OrgUnitOptions = context.Tree.OptionsFor(context.ScopedEmployees.Select(e => e.OrganizationUnitId))
        };
    }

    private static List<PayrollEmployeeRowDto> EmployeeRows(
        DashboardContext context,
        PayrollDashboardListKind kind,
        DateTime? month,
        int? band,
        PaymentType? paymentType,
        PayrollRunType? runType)
    {
        var payslips = context.Payslips;
        var latest = context.LatestMonth;
        IEnumerable<PayrollPayslip> source;

        switch (kind)
        {
            case PayrollDashboardListKind.BasicBand:
                var basics = context.Calculator.BasicByEmployee(payslips);
                source = PayrollDashboardCalculator.RegularIn(payslips, latest)
                    .Where(p => PayrollDashboardCalculator.BandOf(basics.GetValueOrDefault(p.EmployeeId)) == (band ?? 0));
                break;
            case PayrollDashboardListKind.PaymentMethod:
                source = PayrollDashboardCalculator.RegularIn(payslips, latest)
                    .Where(p => (context.PaymentTypes.TryGetValue(p.EmployeeId, out var t) ? t : (PaymentType?)null) == paymentType);
                break;
            case PayrollDashboardListKind.RunType:
                source = payslips.Where(p => p.RunType == (runType ?? PayrollRunType.Regular));
                break;
            default:
                var wanted = month.HasValue ? PayrollDashboardCalculator.MonthOf(month.Value) : latest;
                source = wanted == null ? Enumerable.Empty<PayrollPayslip>() : payslips.Where(p => PayrollDashboardCalculator.MonthOf(p.Month) == wanted);
                break;
        }

        return source
            .Select(p => context.Calculator.ToRow(p, context.PaymentTypes))
            .OrderByDescending(r => r.Month)
            .ThenBy(r => r.EmployeeCode)
            .ThenBy(r => r.RunType)
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
        public PayrollDashboardCalculator Calculator { get; init; }
        public List<PayrollEmployee> ScopedEmployees { get; init; }
        public int EmployeesInScope { get; init; }
        public List<PayrollPayslip> Payslips { get; init; }
        public DateTime? LatestMonth { get; init; }
        public Dictionary<int, PaymentType> PaymentTypes { get; init; }
    }

    private async Task<DashboardContext> BuildContextAsync(PayrollDashboardInput input)
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

        var employeeRows = employeeQuery.Select(e => new { e.Id, e.Code, e.FullNameEn, e.FullNameAr, e.Status, e.JoiningDate }).ToList();
        var scopedEmployees = employeeRows
            .Select(e => new PayrollEmployee(e.Id, e.Code, e.FullNameEn, e.FullNameAr,
                unitByEmployee.TryGetValue(e.Id, out var u) ? u : null, e.JoiningDate))
            .ToList();

        var tree = new DashboardOrgTree(_organizationUnitRepository.GetAll()
            .Select(u => new DashboardOrgUnit(u.Id, u.ParentId, u.DisplayName))
            .ToList());

        IEnumerable<PayrollEmployee> filtered = scopedEmployees;
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

        var wageTypes = _wageTypeRepository.GetAll()
            .Select(w => new { w.Id, w.Code, w.NameEn, w.NameAr, w.BillingType, w.DontImpactNetPay })
            .ToList()
            .Select(w => new PayrollWageInfo(w.Id, w.Code, w.NameEn, w.NameAr, w.BillingType, w.DontImpactNetPay))
            .ToList();

        var payslips = LoadPayslips(from, to, byId ? ids : null);
        var latest = PayrollDashboardCalculator.LatestMonth(payslips);

        return new DashboardContext
        {
            Scope = scope,
            From = from,
            To = to,
            Tree = tree,
            Calculator = new PayrollDashboardCalculator(from, to, tree, scopedEmployees, wageTypes),
            ScopedEmployees = scopedEmployees,
            EmployeesInScope = employeeRows.Count(e => e.Status == EmployeeStatus.Active),
            Payslips = payslips,
            LatestMonth = latest,
            PaymentTypes = LoadPaymentTypes(latest, payslips.Select(p => p.EmployeeId).Distinct().ToList())
        };
    }

    /// <summary>
    /// Each payroll result calculated in the month it is for (<c>InPeriod == ForPeriod</c>), with its lines. A retro
    /// recalculation of an earlier month is left out: it carries that month again, and the difference is already paid
    /// as arrears in the later month's own result.
    /// </summary>
    private List<PayrollPayslip> LoadPayslips(DateTime from, DateTime to, List<int> employeeIds)
    {
        if (employeeIds is { Count: 0 })
        {
            return new List<PayrollPayslip>();
        }

        var firstMonth = PayrollDashboardCalculator.MonthOf(from);
        var lastDay = PayrollDashboardCalculator.MonthOf(to).AddMonths(1).AddDays(-1);

        var registerQuery = _salaryRegisterRepository.GetAll().Where(r => r.ForPeriod >= firstMonth && r.ForPeriod <= lastDay);
        if (employeeIds != null)
        {
            registerQuery = registerQuery.Where(r => employeeIds.Contains(r.EmployeeId));
        }

        var registers = registerQuery
            .Select(r => new { r.Id, r.EmployeeId, r.ForPeriod, r.InPeriod, r.PayrollRunType })
            .ToList()
            .Where(r => r.InPeriod.Date == r.ForPeriod.Date)
            .ToList();
        if (registers.Count == 0)
        {
            return new List<PayrollPayslip>();
        }

        var registerIds = registers.Select(r => r.Id).ToList();
        var lines = _salaryRegisterDetailRepository.GetAll()
            .Where(d => registerIds.Contains(d.SalaryRegisterId))
            .Select(d => new { d.SalaryRegisterId, d.WageTypeId, d.Amount, d.Number })
            .ToList()
            .ToLookup(d => d.SalaryRegisterId, d => new PayrollLine(d.WageTypeId, d.Amount, d.Number));

        return registers
            .Select(r => new PayrollPayslip(r.EmployeeId, r.ForPeriod, r.PayrollRunType, lines[r.Id].ToList()))
            .ToList();
    }

    /// <summary>How each employee was paid at the end of the latest paid month, from their bank details in force then.</summary>
    private Dictionary<int, PaymentType> LoadPaymentTypes(DateTime? latestMonth, List<int> employeeIds)
    {
        if (latestMonth == null || employeeIds.Count == 0)
        {
            return new Dictionary<int, PaymentType>();
        }

        var monthEnd = latestMonth.Value.AddMonths(1).AddDays(-1);
        return _employeeBankRepository.GetAll()
            .Where(b => employeeIds.Contains(b.EmployeeId) && b.StartDate <= monthEnd && b.EndDate >= monthEnd)
            .Select(b => new { b.EmployeeId, b.PaymentType, b.StartDate })
            .ToList()
            .GroupBy(b => b.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.StartDate).First().PaymentType);
    }

    private static (DateTime From, DateTime To) ResolvePeriod(PayrollDashboardInput input, DateTime today)
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
