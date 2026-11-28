using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeRecurringPaymentsAppService_Tests : AppTestBase
{
    private readonly IEmployeeRecurringPaymentsAppService _appService;

    public EmployeeRecurringPaymentsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeRecurringPaymentsAppService>();
    }

    private Employee CreateTestEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });
        return employee;
    }

    private WageType CreateTestWageType(string code)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code,
                PayrollOrder = 1
            }).Entity;
        });
        return wageType;
    }

    [Fact]
    public async Task Should_Create_EmployeeRecurringPayment()
    {
        //Arrange
        var employee = CreateTestEmployee("ERP100");
        var wageType = CreateTestWageType("WT200");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeRecurringPaymentDto
        {
            EmployeeId = employee.Id,
            WageTypeId = wageType.Id,
            Amount = 100,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            PayInterval = PayInterval.Monthly
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeRecurringPaymentsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    private void SetEffectiveDateInRecPayment(bool enabled)
    {
        UsingDbContext(context =>
        {
            var tenantId = AbpSession.TenantId ?? 1;
            var settings = context.Set<HCMv3.SystemSettings.ManagePayrollSettings>().FirstOrDefault(s => s.TenantId == tenantId)
                           ?? context.Set<HCMv3.SystemSettings.ManagePayrollSettings>()
                               .Add(new HCMv3.SystemSettings.ManagePayrollSettings { TenantId = tenantId }).Entity;
            settings.EnableEffectiveDateInRecPayment = enabled;
        });
    }

    /// <summary>
    /// Legacy "Auto EndDate" (Zoho #2783): with the setting on, a new open-ended entry closes the previous
    /// open-ended one the day before it starts instead of being refused as an overlap.
    /// </summary>
    [Fact]
    public async Task With_Effective_Date_On_A_New_Open_Ended_Entry_Should_Close_The_Previous_One()
    {
        var employee = CreateTestEmployee("ERP300");
        var wageType = CreateTestWageType("WT400");
        SetEffectiveDateInRecPayment(true);

        await _appService.CreateOrEdit(new CreateOrEditEmployeeRecurringPaymentDto
        {
            EmployeeId = employee.Id, WageTypeId = wageType.Id, Amount = 100,
            StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(9999, 12, 31), PayInterval = PayInterval.Monthly
        });
        await _appService.CreateOrEdit(new CreateOrEditEmployeeRecurringPaymentDto
        {
            EmployeeId = employee.Id, WageTypeId = wageType.Id, Amount = 150,
            StartDate = new DateTime(2024, 6, 1), EndDate = new DateTime(9999, 12, 31), PayInterval = PayInterval.Monthly
        });

        var rows = (await _appService.GetAll(new GetAllEmployeeRecurringPaymentsInput { EmployeeId = employee.Id })).Items
            .OrderBy(r => r.StartDate).ToList();
        rows.Count.ShouldBe(2);
        rows[0].EndDate.ShouldBe(new DateTime(2024, 5, 31));
        rows[1].EndDate.ShouldBe(new DateTime(9999, 12, 31));
    }

    [Fact]
    public async Task With_Effective_Date_On_A_New_Entry_Starting_Before_The_Open_One_Is_Refused()
    {
        var employee = CreateTestEmployee("ERP310");
        var wageType = CreateTestWageType("WT410");
        SetEffectiveDateInRecPayment(true);

        await _appService.CreateOrEdit(new CreateOrEditEmployeeRecurringPaymentDto
        {
            EmployeeId = employee.Id, WageTypeId = wageType.Id, Amount = 100,
            StartDate = new DateTime(2024, 6, 1), EndDate = new DateTime(9999, 12, 31), PayInterval = PayInterval.Monthly
        });

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => _appService.CreateOrEdit(
            new CreateOrEditEmployeeRecurringPaymentDto
            {
                EmployeeId = employee.Id, WageTypeId = wageType.Id, Amount = 150,
                StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(9999, 12, 31), PayInterval = PayInterval.Monthly
            }));
        exception.Message.ShouldContain("Start Date");
    }

    [Fact]
    public async Task With_Effective_Date_Off_An_Overlapping_Open_Ended_Entry_Is_Still_Refused()
    {
        var employee = CreateTestEmployee("ERP320");
        var wageType = CreateTestWageType("WT420");
        SetEffectiveDateInRecPayment(false);

        await _appService.CreateOrEdit(new CreateOrEditEmployeeRecurringPaymentDto
        {
            EmployeeId = employee.Id, WageTypeId = wageType.Id, Amount = 100,
            StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(9999, 12, 31), PayInterval = PayInterval.Monthly
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() => _appService.CreateOrEdit(
            new CreateOrEditEmployeeRecurringPaymentDto
            {
                EmployeeId = employee.Id, WageTypeId = wageType.Id, Amount = 150,
                StartDate = new DateTime(2024, 6, 1), EndDate = new DateTime(9999, 12, 31), PayInterval = PayInterval.Monthly
            }));
    }

    [Fact]
    public async Task Should_Throw_When_Amount_Not_GreaterThan_Zero()
    {
        //Arrange
        var employee = CreateTestEmployee("ERP200");
        var wageType = CreateTestWageType("WT300");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeRecurringPaymentDto
            {
                EmployeeId = employee.Id,
                WageTypeId = wageType.Id,
                Amount = 0,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(9999, 12, 31)
            }));
    }

    // ------------------------------------------------------------------ legacy overlap message

    [Fact]
    public async Task Should_Name_The_Employee_WageType_And_Overlapping_Dates_When_Refusing_An_Overlap()
    {
        var employee = CreateTestEmployee("ERP400");
        var wageType = CreateTestWageType("WT500");
        SetEffectiveDateInRecPayment(false);
        SeedRecurringPayment(employee.Id, wageType.Id, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => _appService.CreateOrEdit(
            new CreateOrEditEmployeeRecurringPaymentDto
            {
                EmployeeId = employee.Id, WageTypeId = wageType.Id, Amount = 50,
                StartDate = new DateTime(2024, 6, 1), EndDate = new DateTime(2024, 8, 31), PayInterval = PayInterval.Monthly
            }));

        exception.Message.ShouldContain("ERP400");
        exception.Message.ShouldContain("WT500");
        exception.Message.ShouldContain("01/01/2024");
        exception.Message.ShouldContain("31/12/2024");
    }

    // ------------------------------------------------------------------ retro date (legacy Ticket #885)

    [Fact]
    public void EndDateRetroFrom_Should_Be_The_Day_After_The_New_End_Date_When_The_Payment_Is_Stopped()
    {
        EmployeeRecurringPaymentsAppService.EndDateRetroFrom(new DateTime(9999, 12, 31), new DateTime(2026, 5, 31))
            .ShouldBe(new DateTime(2026, 6, 1));
    }

    [Fact]
    public void EndDateRetroFrom_Should_Be_The_Day_After_The_Old_End_Date_When_The_Payment_Is_Extended()
    {
        EmployeeRecurringPaymentsAppService.EndDateRetroFrom(new DateTime(2026, 3, 31), new DateTime(2026, 6, 30))
            .ShouldBe(new DateTime(2026, 4, 1));
    }

    [Fact]
    public async Task Changing_Only_The_End_Date_Should_Recalculate_From_The_Day_After_It()
    {
        var employee = CreateTestEmployee("ERP500");
        AssignPayrollArea(employee.Id);
        var wageType = CreateTestWageType("WT600");
        var id = SeedRecurringPayment(employee.Id, wageType.Id, new DateTime(2026, 1, 1), new DateTime(9999, 12, 31));

        await _appService.CreateOrEdit(new CreateOrEditEmployeeRecurringPaymentDto
        {
            Id = id, EmployeeId = employee.Id, WageTypeId = wageType.Id, Amount = 100,
            StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 5, 31), PayInterval = PayInterval.Monthly
        });

        EarliestMasterDataChange(employee.Id).ShouldBe(new DateTime(2026, 6, 1));
    }

    [Fact]
    public async Task Changing_The_Amount_Should_Recalculate_From_The_Start_Date()
    {
        var employee = CreateTestEmployee("ERP510");
        AssignPayrollArea(employee.Id);
        var wageType = CreateTestWageType("WT610");
        var id = SeedRecurringPayment(employee.Id, wageType.Id, new DateTime(2026, 1, 1), new DateTime(9999, 12, 31));

        await _appService.CreateOrEdit(new CreateOrEditEmployeeRecurringPaymentDto
        {
            Id = id, EmployeeId = employee.Id, WageTypeId = wageType.Id, Amount = 175,
            StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 5, 31), PayInterval = PayInterval.Monthly
        });

        EarliestMasterDataChange(employee.Id).ShouldBe(new DateTime(2026, 1, 1));
    }

    [Fact]
    public async Task A_Row_Closed_By_Auto_End_Date_Should_Not_Recalculate_From_Its_Own_Start_Date()
    {
        var employee = CreateTestEmployee("ERP520");
        AssignPayrollArea(employee.Id);
        var wageType = CreateTestWageType("WT620");
        SetEffectiveDateInRecPayment(true);
        SeedRecurringPayment(employee.Id, wageType.Id, new DateTime(2026, 1, 1), new DateTime(9999, 12, 31));

        await _appService.CreateOrEdit(new CreateOrEditEmployeeRecurringPaymentDto
        {
            EmployeeId = employee.Id, WageTypeId = wageType.Id, Amount = 150,
            StartDate = new DateTime(2026, 6, 1), EndDate = new DateTime(9999, 12, 31), PayInterval = PayInterval.Monthly
        });

        EarliestMasterDataChange(employee.Id).ShouldBe(new DateTime(2026, 6, 1));
    }

    [Fact]
    public async Task GetAll_Should_Find_Rows_By_Wage_Type_Name_Across_Employees()
    {
        var first = CreateTestEmployee("ERP600");
        var second = CreateTestEmployee("ERP601");
        var housing = CreateTestWageType("WTHOUSING7");
        var transport = CreateTestWageType("WTTRANS7");
        SeedRecurringPayment(first.Id, housing.Id, new DateTime(2026, 1, 1), new DateTime(9999, 12, 31));
        SeedRecurringPayment(second.Id, housing.Id, new DateTime(2026, 1, 1), new DateTime(9999, 12, 31));
        SeedRecurringPayment(second.Id, transport.Id, new DateTime(2026, 1, 1), new DateTime(9999, 12, 31));

        var result = await _appService.GetAll(new GetAllEmployeeRecurringPaymentsInput
        {
            Filter = "WTHOUSING7", MaxResultCount = 100
        });

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldAllBe(r => r.WageTypeId == housing.Id);
    }

    [Fact]
    public async Task GetAll_Should_Find_Rows_By_Employee_Code()
    {
        var employee = CreateTestEmployee("ERP610X");
        var other = CreateTestEmployee("ERP611");
        var wageType = CreateTestWageType("WT710");
        SeedRecurringPayment(employee.Id, wageType.Id, new DateTime(2026, 1, 1), new DateTime(9999, 12, 31));
        SeedRecurringPayment(other.Id, wageType.Id, new DateTime(2026, 1, 1), new DateTime(9999, 12, 31));

        var result = await _appService.GetAll(new GetAllEmployeeRecurringPaymentsInput
        {
            Filter = "ERP610X", MaxResultCount = 100
        });

        result.Items.Count.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employee.Id);
    }

    [Fact]
    public async Task GetEmployeesForLookup_Should_Return_Matching_Employees_With_Code_And_Name()
    {
        var employee = CreateTestEmployee("ERPLOOK1");
        CreateTestEmployee("ERP620");

        var result = await _appService.GetEmployeesForLookup(new GetEmployeeRecurringPaymentEmployeesInput
        {
            Filter = "ERPLOOK1"
        });

        result.Items.Count.ShouldBe(1);
        result.Items[0].Id.ShouldBe(employee.Id);
        result.Items[0].Code.ShouldBe("ERPLOOK1");
        result.Items[0].NameEn.ShouldBe("F L");
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>Written straight to the database, so seeding raises no retro date of its own.</summary>
    private int SeedRecurringPayment(int employeeId, int wageTypeId, DateTime start, DateTime end)
    {
        EmployeeRecurringPayment row = null;
        UsingDbContext(context => row = context.Set<EmployeeRecurringPayment>().Add(new EmployeeRecurringPayment
        {
            TenantId = AbpSession.TenantId ?? 1, EmployeeId = employeeId, WageTypeId = wageTypeId, Amount = 100,
            StartDate = start, EndDate = end, PayInterval = PayInterval.Monthly
        }).Entity);
        return row.Id;
    }

    private DateTime EarliestMasterDataChange(int employeeId) =>
        UsingDbContext(context => context.Set<EmployeePayrollStatus>()
            .Where(s => s.EmployeeId == employeeId)
            .Select(s => s.EarliestMasterDataChange)
            .FirstOrDefault());

    /// <summary>The retro trigger resolves the employee's payroll area through the org assignment.</summary>
    private void AssignPayrollArea(int employeeId)
    {
        var tenantId = AbpSession.TenantId ?? 1;
        PayrollArea area = null;
        UsingDbContext(context =>
        {
            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = tenantId, Code = Guid.NewGuid().ToString("N")[..3], NameEn = "KWD", NameAr = "KWD", DecimalCount = 3
            }).Entity;
            var period = context.Set<PayrollPeriod>().Add(new PayrollPeriod
            {
                TenantId = tenantId, Code = Guid.NewGuid().ToString("N")[..4], NameEn = "Monthly", NameAr = "Monthly"
            }).Entity;
            context.SaveChanges();

            area = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = tenantId, Code = Guid.NewGuid().ToString("N")[..4], Name = "Main",
                PayrollPeriodId = period.Id, CurrencyId = currency.Id, CutOffDay = 25
            }).Entity;
        });

        UsingDbContext(context => context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
        {
            TenantId = tenantId, EmployeeId = employeeId, StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(9999, 12, 31), PayrollAreaId = area.Id
        }));
    }
}
