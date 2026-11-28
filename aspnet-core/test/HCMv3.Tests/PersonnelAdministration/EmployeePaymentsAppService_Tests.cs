using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeePaymentsAppService_Tests : AppTestBase
{
    private readonly IEmployeePaymentsAppService _appService;

    public EmployeePaymentsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeePaymentsAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateTestEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
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
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L " + code,
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id,
                Status = status
            }).Entity;
        });
        return employee;
    }

    private WageType CreateTestWageType(string code, BillingType billingType = BillingType.Earning,
        BillingMethod billingMethod = BillingMethod.CalendarDaysPerMonth, bool includedEverywhere = false,
        bool includeInPartialPay = false)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code,
                NameAr = code,
                PayrollOrder = 1,
                BillingType = billingType,
                BillingMethod = billingMethod,
                IncludeInBasicPay = includedEverywhere,
                IncludeInArrearPay = includedEverywhere,
                IncludeInCalculatedPay = includedEverywhere,
                IncludeInPartialPay = includeInPartialPay
            }).Entity;
        });
        return wageType;
    }

    private Currency CreateCurrency(string code = "KWD")
    {
        Currency currency = null;
        UsingDbContext(context =>
        {
            currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code,
                NameAr = code,
                DecimalCount = 3
            }).Entity;
        });
        return currency;
    }

    private async Task<int> CreatePayment(int employeeId, int wageTypeId, int? currencyId = null,
        UnitType unitType = UnitType.Amount, double amount = 250)
    {
        await _appService.CreateOrEdit(new CreateOrEditEmployeePaymentDto
        {
            EmployeeId = employeeId,
            WageTypeId = wageTypeId,
            CurrencyId = currencyId,
            Amount = amount,
            PaymentDate = new DateTime(2025, 4, 15),
            UnitType = unitType
        });

        return UsingDbContext(context => context.Set<EmployeePayment>().Single(p => p.EmployeeId == employeeId).Id);
    }

    [Fact]
    public async Task Should_Create_EmployeePayment_With_Default_Currency()
    {
        // Arrange
        var employee = CreateTestEmployee("EP100");
        var wageType = CreateTestWageType("WT100");
        var kwd = CreateCurrency();

        // Act
        await CreatePayment(employee.Id, wageType.Id);

        // Assert
        var result = await _appService.GetAll(new GetAllEmployeePaymentsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].WageTypeNameEn.ShouldBe("WT100");
        result.Items[0].CurrencyId.ShouldBe(kwd.Id);
    }

    [Fact]
    public async Task Should_Throw_When_Currency_Missing_And_No_Default()
    {
        var employee = CreateTestEmployee("EP101");
        var wageType = CreateTestWageType("WT101");

        await Should.ThrowAsync<UserFriendlyException>(() => CreatePayment(employee.Id, wageType.Id));
    }

    [Fact]
    public async Task Should_Throw_When_Wage_Type_Blank()
    {
        var employee = CreateTestEmployee("EP102");
        var kwd = CreateCurrency();

        await Should.ThrowAsync<UserFriendlyException>(() => CreatePayment(employee.Id, 0, kwd.Id));
    }

    [Fact]
    public async Task Should_Throw_When_Employee_Blank()
    {
        var wageType = CreateTestWageType("WT103");
        var kwd = CreateCurrency();

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(new CreateOrEditEmployeePaymentDto
        {
            WageTypeId = wageType.Id,
            CurrencyId = kwd.Id,
            PaymentDate = new DateTime(2025, 4, 15)
        }));
    }

    [Fact]
    public async Task Should_Throw_When_Payment_Date_Blank()
    {
        var employee = CreateTestEmployee("EP104");
        var wageType = CreateTestWageType("WT104");
        var kwd = CreateCurrency();

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(new CreateOrEditEmployeePaymentDto
        {
            EmployeeId = employee.Id,
            WageTypeId = wageType.Id,
            CurrencyId = kwd.Id
        }));
    }

    [Fact]
    public async Task Should_Update_EmployeePayment()
    {
        var employee = CreateTestEmployee("EP105");
        var wageType = CreateTestWageType("WT105");
        var kwd = CreateCurrency();
        var id = await CreatePayment(employee.Id, wageType.Id, kwd.Id);

        var edit = (await _appService.GetEmployeePaymentForEdit(new EntityDto(id))).EmployeePayment;
        edit.Amount = 99;
        edit.Comments = "changed";
        await _appService.CreateOrEdit(edit);

        var saved = UsingDbContext(context => context.Set<EmployeePayment>().Single(p => p.Id == id));
        saved.Amount.ShouldBe(99);
        saved.Comments.ShouldBe("changed");
    }

    [Fact]
    public async Task Should_Delete_EmployeePayment()
    {
        var employee = CreateTestEmployee("EP106");
        var wageType = CreateTestWageType("WT106");
        var kwd = CreateCurrency();
        var id = await CreatePayment(employee.Id, wageType.Id, kwd.Id);

        await _appService.Delete(new EntityDto(id));

        (await _appService.GetAll(new GetAllEmployeePaymentsInput { EmployeeId = employee.Id })).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_Employee_And_Billing_Type_For_Edit()
    {
        var employee = CreateTestEmployee("EP107");
        var wageType = CreateTestWageType("WT107", BillingType.Deduction);
        var kwd = CreateCurrency();
        var id = await CreatePayment(employee.Id, wageType.Id, kwd.Id);

        var output = await _appService.GetEmployeePaymentForEdit(new EntityDto(id));

        output.EmployeeCode.ShouldBe("EP107");
        output.BillingType.ShouldBe("Deduction");
        output.ExpectedAmount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Filter_List_By_Employee_Code()
    {
        var first = CreateTestEmployee("EP108");
        var second = CreateTestEmployee("XQ109");
        var wageType = CreateTestWageType("WT108");
        var kwd = CreateCurrency();
        await CreatePayment(first.Id, wageType.Id, kwd.Id);
        await CreatePayment(second.Id, wageType.Id, kwd.Id);

        var result = await _appService.GetAll(new GetAllEmployeePaymentsInput { Filter = "XQ109" });

        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeCode.ShouldBe("XQ109");
        result.Items[0].BillingType.ShouldBe("Earning");
    }

    [Fact]
    public async Task Should_Calculate_Expected_Amount_For_Days()
    {
        // Arrange: partial pay 300, calendar days, April 2025 (30 days), 3 days -> 30
        var employee = CreateTestEmployee("EP110");
        var basicWageType = CreateTestWageType("WT110B", includeInPartialPay: true);
        var paymentWageType = CreateTestWageType("WT110P", billingMethod: BillingMethod.CalendarDaysPerMonth);
        var kwd = CreateCurrency();
        UsingDbContext(context =>
        {
            var basicPay = context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                Amount = 300,
                PayInterval = PayInterval.Monthly
            }).Entity;
            context.SaveChanges();
            context.Set<EmployeeBasicPayDetail>().Add(new EmployeeBasicPayDetail
            {
                TenantId = TenantId,
                EmployeeBasicPayId = basicPay.Id,
                WageTypeId = basicWageType.Id,
                CurrencyId = kwd.Id,
                Amount = 300
            });
        });

        // Act
        var expected = await _appService.GetExpectedAmount(new GetExpectedPaymentAmountInput
        {
            EmployeeId = employee.Id,
            WageTypeId = paymentWageType.Id,
            PaymentDate = new DateTime(2025, 4, 15),
            UnitType = UnitType.Day,
            Amount = 3
        });

        // Assert
        expected.ShouldBe(30);
    }

    [Fact]
    public async Task Should_Return_Zero_Expected_Amount_For_Amount_Unit()
    {
        var employee = CreateTestEmployee("EP111");
        var wageType = CreateTestWageType("WT111");

        var expected = await _appService.GetExpectedAmount(new GetExpectedPaymentAmountInput
        {
            EmployeeId = employee.Id,
            WageTypeId = wageType.Id,
            PaymentDate = new DateTime(2025, 4, 15),
            UnitType = UnitType.Amount,
            Amount = 3
        });

        expected.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Exclude_Wage_Types_Included_In_Basic_Arrear_And_Calculated_Pay()
    {
        var offered = CreateTestWageType("WT112");
        var excluded = CreateTestWageType("WT113", includedEverywhere: true);

        var result = await _appService.GetWageTypesForLookup();

        result.Items.ShouldContain(w => w.Id == offered.Id && w.BillingType == "Earning");
        result.Items.ShouldNotContain(w => w.Id == excluded.Id);
    }

    [Fact]
    public async Task Should_Offer_Active_Employees_Only_When_None_Separated()
    {
        var active = CreateTestEmployee("EP114");
        var inactive = CreateTestEmployee("EP115", EmployeeStatus.Inactive);

        var result = await _appService.GetEmployeesForLookup(new GetEmployeePaymentEmployeesInput());

        result.Items.ShouldContain(e => e.Id == active.Id);
        result.Items.ShouldNotContain(e => e.Id == inactive.Id);
    }

    [Fact]
    public async Task Should_Return_Default_Currency_In_Screen_Settings()
    {
        var kwd = CreateCurrency();

        var settings = await _appService.GetScreenSettings();

        settings.DefaultCurrencyId.ShouldBe(kwd.Id);
    }
}
