using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.Organizations;
using ClosedXML.Excel;
using HCMv3.Dashboards;
using HCMv3.Dashboards.Payroll;
using HCMv3.Dashboards.Payroll.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using NSubstitute;
using Shouldly;
using Xunit;

using Fakes = HCMv3.Tests.ProjectManagement.ProjectCostEngineFakes;

namespace HCMv3.Tests.Dashboards;

/// <summary>
/// The Payroll Dashboard: payslips of the employees <see cref="IDashboardScopeService"/> allows, narrowed by unit and
/// employee. Rules: <see cref="PayrollDashboardCalculator_Tests"/>. Period: Q1 2026.
/// </summary>
public class PayrollDashboardAppService_Tests
{
    private const int Basic = 1;
    private const int NetPay = 2;
    private const int EmployerPifss = 3;

    private static readonly DateTime Jan = new(2026, 1, 31);
    private static readonly DateTime Feb = new(2026, 2, 28);

    private readonly List<Employee> _employees = new();
    private readonly List<EmployeeOrgAssignment> _assignments = new();
    private readonly List<EmployeeBank> _banks = new();
    private readonly List<SalaryRegister> _registers = new();
    private readonly List<SalaryRegisterDetail> _details = new();
    private readonly List<WageType> _wageTypes = new()
    {
        new WageType { Id = Basic, TenantId = 1, Code = "1000", NameEn = "Basic", BillingType = BillingType.Earning },
        new WageType { Id = NetPay, TenantId = 1, Code = "9000", NameEn = "Net Pay", BillingType = BillingType.Earning },
        new WageType { Id = EmployerPifss, TenantId = 1, Code = "3", NameEn = "PIFSS employer", BillingType = BillingType.Deduction, DontImpactNetPay = true }
    };
    private readonly List<OrganizationUnit> _units = new()
    {
        new OrganizationUnit(1, "Company") { Id = 1 },
        new OrganizationUnit(1, "Finance", 1) { Id = 2 }
    };

    private readonly IDashboardScopeService _scopeService = Substitute.For<IDashboardScopeService>();
    private readonly ITempFileCacheManager _tempFiles = Substitute.For<ITempFileCacheManager>();
    private readonly Dictionary<string, byte[]> _savedFiles = new();

    public PayrollDashboardAppService_Tests()
    {
        AddEmployee(1, unitId: 1);
        AddEmployee(2, unitId: 2);
        ScopeIs(DashboardScope.Everyone(DashboardScopeReason.Admin));

        _tempFiles.When(t => t.SetFile(Arg.Any<string>(), Arg.Any<byte[]>()))
            .Do(call => _savedFiles[call.ArgAt<string>(0)] = call.ArgAt<byte[]>(1));
    }

    private void ScopeIs(DashboardScope scope) => _scopeService.GetScopeAsync().Returns(Task.FromResult(scope));

    private void AddEmployee(int id, long unitId)
    {
        _employees.Add(new Employee { Id = id, TenantId = 1, Code = "E" + id, FullNameEn = "Employee " + id, Status = EmployeeStatus.Active });
        _assignments.Add(new EmployeeOrgAssignment
        {
            Id = id, TenantId = 1, EmployeeId = id, OrganizationUnitId = unitId,
            StartDate = new DateTime(2020, 1, 1), EndDate = DateTime.MaxValue.Date
        });
    }

    /// <summary>A payslip of basic pay with net pay equal to it and employer PIFSS on top.</summary>
    private void AddPayslip(int employeeId, DateTime month, double basic, double employerPifss = 0,
        DateTime? inPeriod = null, PayrollRunType runType = PayrollRunType.Regular)
    {
        var register = new SalaryRegister
        {
            Id = _registers.Count + 1, TenantId = 1, EmployeeId = employeeId, ForPeriod = month, InPeriod = inPeriod ?? month, PayrollRunType = runType
        };
        _registers.Add(register);
        void Line(int wageType, double amount) => _details.Add(new SalaryRegisterDetail
        {
            Id = _details.Count + 1, TenantId = 1, SalaryRegisterId = register.Id, WageTypeId = wageType, Amount = amount
        });

        Line(Basic, basic);
        Line(NetPay, basic);
        if (employerPifss != 0)
        {
            Line(EmployerPifss, employerPifss);
        }
    }

    private PayrollDashboardAppService CreateService()
    {
        var unitRepository = Substitute.For<IRepository<OrganizationUnit, long>>();
        unitRepository.GetAll().Returns(_ => _units.AsQueryable());

        var service = new PayrollDashboardAppService(
            Fakes.RepoOver(_employees),
            Fakes.RepoOver(_assignments),
            Fakes.RepoOver(_banks),
            Fakes.RepoOver(_registers),
            Fakes.RepoOver(_details),
            Fakes.RepoOver(_wageTypes),
            unitRepository,
            _scopeService,
            new PayrollDashboardExcelExporter(_tempFiles) { LocalizationManager = Fakes.LocalizationManager() });

        ((ApplicationService)service).AbpSession = Fakes.Session();
        ((ApplicationService)service).LocalizationManager = Fakes.LocalizationManager();
        ((ApplicationService)service).UnitOfWorkManager = Fakes.UnitOfWorkManager();
        return service;
    }

    private static PayrollDashboardInput Q1(long? unitId = null, int? employeeId = null) => new()
    {
        FromDate = new DateTime(2026, 1, 1), ToDate = new DateTime(2026, 3, 31), OrganizationUnitId = unitId, EmployeeId = employeeId
    };

    [Fact]
    public async Task GetDashboard_Should_Add_Up_Pay_And_Employer_Cost()
    {
        AddPayslip(1, Jan, 1000, employerPifss: 110);
        AddPayslip(2, Jan, 500);
        AddPayslip(1, Feb, 1000, employerPifss: 110);

        var result = await CreateService().GetDashboard(Q1());

        result.Kpis.Gross.ShouldBe(2500);
        result.Kpis.Net.ShouldBe(2500);
        result.Kpis.EmployerCost.ShouldBe(220);
        result.Kpis.CostToCompany.ShouldBe(2720);
        result.Kpis.EmployeesPaid.ShouldBe(1);
        result.LatestMonth.ShouldBe(new DateTime(2026, 2, 1));
        result.Trend.Count.ShouldBe(3);
    }

    /// <summary>A retro recalculation carries the month again; counting it too would double the pay.</summary>
    [Fact]
    public async Task GetDashboard_Should_Count_Only_Results_Calculated_In_Their_Own_Month()
    {
        AddPayslip(1, Jan, 1000);
        AddPayslip(1, Jan, 1200, inPeriod: Feb);

        (await CreateService().GetDashboard(Q1())).Kpis.Gross.ShouldBe(1000);
    }

    [Fact]
    public async Task GetDashboard_Should_Narrow_By_Unit_And_Employee()
    {
        AddPayslip(1, Jan, 1000);
        AddPayslip(2, Jan, 500);

        (await CreateService().GetDashboard(Q1(unitId: 2))).Kpis.Gross.ShouldBe(500);

        var one = await CreateService().GetDashboard(Q1(employeeId: 1));
        one.Kpis.Gross.ShouldBe(1000);
        one.EmployeeNameEn.ShouldBe("Employee 1");
    }

    [Fact]
    public async Task GetDashboard_Should_Count_Only_The_Employees_In_Scope()
    {
        ScopeIs(DashboardScope.Restricted(new[] { 2 }, new List<string>(), includesOwnTeam: true));
        AddPayslip(1, Jan, 1000);
        AddPayslip(2, Jan, 500);

        var result = await CreateService().GetDashboard(Q1());

        result.Kpis.Gross.ShouldBe(500);
        result.Scope.EmployeesInScope.ShouldBe(1);
    }

    [Fact]
    public async Task GetDashboard_Should_Show_Nothing_When_The_Scope_Is_Nobody()
    {
        ScopeIs(DashboardScope.Restricted(new List<int>(), new List<string>(), includesOwnTeam: false));
        AddPayslip(1, Jan, 1000);

        var result = await CreateService().GetDashboard(Q1());

        result.Kpis.Gross.ShouldBe(0);
        result.LatestMonth.ShouldBeNull();
    }

    [Fact]
    public async Task GetDashboard_Should_Read_The_Payment_Method_In_Force_At_The_Latest_Month_End()
    {
        AddPayslip(1, Feb, 1000);
        AddPayslip(2, Feb, 500);
        _banks.Add(new EmployeeBank { Id = 1, TenantId = 1, EmployeeId = 1, PaymentType = PaymentType.Cash, StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2026, 1, 31) });
        _banks.Add(new EmployeeBank { Id = 2, TenantId = 1, EmployeeId = 1, PaymentType = PaymentType.Bank, StartDate = new DateTime(2026, 2, 1), EndDate = DateTime.MaxValue.Date });

        var methods = (await CreateService().GetDashboard(Q1())).PaymentMethods;

        methods.Single(m => m.PaymentType == PaymentType.Bank).Employees.ShouldBe(1);
        methods.Single(m => m.PaymentType == null).Employees.ShouldBe(1);
        methods.ShouldNotContain(m => m.PaymentType == PaymentType.Cash);
    }

    [Fact]
    public async Task GetEmployees_Should_List_A_Month_A_Band_And_A_Run_Type()
    {
        AddPayslip(1, Jan, 1000);
        AddPayslip(1, Feb, 1000);
        AddPayslip(2, Feb, 450);
        AddPayslip(2, Feb, 200, runType: PayrollRunType.OffCycle);
        var service = CreateService();

        GetPayrollEmployeesInput List(PayrollDashboardListKind kind) => new()
        {
            FromDate = new DateTime(2026, 1, 1), ToDate = new DateTime(2026, 3, 31), Kind = kind, MaxResultCount = 10
        };

        var latest = await service.GetEmployees(List(PayrollDashboardListKind.Month));
        latest.TotalCount.ShouldBe(3);   // February, both run types

        var january = List(PayrollDashboardListKind.Month);
        january.Month = new DateTime(2026, 1, 15);
        (await service.GetEmployees(january)).Items.Single().EmployeeCode.ShouldBe("E1");

        var band = List(PayrollDashboardListKind.BasicBand);
        band.Band = 1; // 300 - 500
        (await service.GetEmployees(band)).Items.Single().EmployeeCode.ShouldBe("E2");

        var offCycle = List(PayrollDashboardListKind.RunType);
        offCycle.RunType = PayrollRunType.OffCycle;
        (await service.GetEmployees(offCycle)).Items.Single().Net.ShouldBe(200);
    }

    [Fact]
    public async Task GetCostCard_Should_Use_The_Latest_Regular_Payslip_Up_To_The_Period_End()
    {
        AddPayslip(1, Jan, 1000, employerPifss: 110);
        AddPayslip(1, Feb, 1100, employerPifss: 120);
        AddPayslip(1, Feb, 300, runType: PayrollRunType.OffCycle);

        var card = await CreateService().GetCostCard(new GetPayrollCostCardInput { EmployeeId = 1, ToDate = new DateTime(2026, 3, 31) });

        card.Month.ShouldBe(new DateTime(2026, 2, 1));
        card.Gross.ShouldBe(1100);
        card.CostToCompany.ShouldBe(1220);

        var january = await CreateService().GetCostCard(new GetPayrollCostCardInput { EmployeeId = 1, Month = new DateTime(2026, 1, 1) });
        january.Gross.ShouldBe(1000);
    }

    [Fact]
    public async Task GetCostCard_Should_Not_Show_An_Employee_Outside_The_Scope()
    {
        ScopeIs(DashboardScope.Restricted(new[] { 2 }, new List<string>(), includesOwnTeam: true));
        AddPayslip(1, Jan, 1000);

        var card = await CreateService().GetCostCard(new GetPayrollCostCardInput { EmployeeId = 1, ToDate = new DateTime(2026, 3, 31) });

        card.Month.ShouldBeNull();
        card.EmployeeCode.ShouldBeNull();
        card.Gross.ShouldBe(0);
    }

    [Fact]
    public async Task ExportToExcel_Should_Put_The_Whole_Dashboard_On_A_Summary_Sheet_Plus_One_Sheet_Per_Tile()
    {
        AddPayslip(1, Jan, 1000);

        var file = await CreateService().ExportToExcel(new ExportPayrollDashboardInput
        {
            FromDate = new DateTime(2026, 1, 1), ToDate = new DateTime(2026, 3, 31), Part = PayrollDashboardExportPart.All
        });

        using var book = new XLWorkbook(new MemoryStream(_savedFiles[file.FileToken]));
        book.Worksheets.Count.ShouldBe(11);
        book.Worksheet(1).Name.ShouldBe("Summary");
        book.Worksheet(1).Cell(3, 2).GetString().ShouldBe("Dashboard_ScopeAllAdmin");
    }
}
