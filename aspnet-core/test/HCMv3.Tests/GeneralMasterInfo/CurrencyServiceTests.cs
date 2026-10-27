using System;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Services;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

// ReSharper disable once InconsistentNaming
public class CurrencyServiceTests : AppTestBase
{
    private readonly ICurrencyService _currencyService;

    public CurrencyServiceTests()
    {
        LoginAsDefaultTenantAdmin();
        _currencyService = Resolve<ICurrencyService>();
    }

    private Employee CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
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
                NationalityId = nationality.Id
            }).Entity;
        });
        return employee;
    }

    private Currency CreateCurrency(string code, int decimalCount)
    {
        Currency currency = null;
        UsingDbContext(context =>
        {
            currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code,
                DecimalCount = decimalCount
            }).Entity;
        });
        return currency;
    }

    [Fact]
    public async Task Should_Resolve_Default_KWD_Currency()
    {
        CreateCurrency("KWD", 3);
        CreateCurrency("USD", 2);

        var defaultCurrency = await _currencyService.GetDefaultCurrencyAsync();

        defaultCurrency.ShouldNotBeNull();
        defaultCurrency.Code.ShouldBe("KWD");
    }

    [Fact]
    public async Task Should_Return_Null_When_No_KWD_Currency_Configured()
    {
        CreateCurrency("USD", 2);

        var defaultCurrency = await _currencyService.GetDefaultCurrencyAsync();

        defaultCurrency.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Round_Value_Using_Employee_Current_Payroll_Area_Currency()
    {
        var employee = CreateEmployee("CUR100");
        var currency = CreateCurrency("KWD", 3);

        long organizationUnitId = 0;
        UsingDbContext(context =>
        {
            var ou = context.Set<Abp.Organizations.OrganizationUnit>().Add(new Abp.Organizations.OrganizationUnit
            {
                TenantId = AbpSession.TenantId,
                Code = "00001",
                DisplayName = "Root"
            }).Entity;
            context.SaveChanges();
            organizationUnitId = ou.Id;
        });

        PayrollPeriod payrollPeriod = null;
        UsingDbContext(context =>
        {
            payrollPeriod = context.Set<PayrollPeriod>().Add(new PayrollPeriod
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "Jan-Dec",
                NameEn = "Jan-Dec",
                NameAr = "Jan-Dec"
            }).Entity;
        });

        PayrollArea payrollArea = null;
        UsingDbContext(context =>
        {
            payrollArea = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "PA1",
                Name = "PA1",
                PayrollPeriodId = payrollPeriod.Id,
                CurrencyId = currency.Id,
                CutOffDay = 25
            }).Entity;
        });

        OrganizationUnitType orgUnitType = null;
        UsingDbContext(context =>
        {
            orgUnitType = context.Set<OrganizationUnitType>().Add(new OrganizationUnitType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = OrganizationUnitType.Level1Code,
                NameEn = "Company",
                NameAr = "Company"
            }).Entity;
        });

        UsingDbContext(context =>
        {
            context.Set<OrgUnitProfile>().Add(new OrgUnitProfile
            {
                TenantId = AbpSession.TenantId ?? 1,
                OrganizationUnitId = organizationUnitId,
                NameAr = "Root",
                OrganizationUnitTypeId = orgUnitType.Id,
                PayrollAreaId = payrollArea.Id
            });
        });

        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employee.Id,
                StartDate = new DateTime(2023, 1, 1),
                EndDate = new DateTime(2099, 12, 31),
                OrganizationUnitId = organizationUnitId
            });
        });

        var rounded = await _currencyService.RoundAsync(employee.Id, 12.34567);

        rounded.ShouldBe(Math.Round(12.34567, 3));
    }

    [Fact]
    public async Task Should_Not_Round_When_Employee_Has_No_Org_Assignment()
    {
        var employee = CreateEmployee("CUR200");

        var rounded = await _currencyService.RoundAsync(employee.Id, 12.34567);

        rounded.ShouldBe(12.34567);
    }

    [Fact]
    public async Task Should_Resolve_Employee_Currency_Via_Latest_BasicPay()
    {
        var employee = CreateEmployee("CUR300");
        var currency = CreateCurrency("KWD", 3);

        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "WT1",
                NameEn = "WT1",
                NameAr = "WT1"
            }).Entity;
        });

        EmployeeBasicPay basicPay = null;
        UsingDbContext(context =>
        {
            basicPay = context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employee.Id,
                StartDate = new DateTime(2023, 1, 1),
                EndDate = new DateTime(2099, 12, 31),
                Amount = 500
            }).Entity;
        });

        UsingDbContext(context =>
        {
            context.Set<EmployeeBasicPayDetail>().Add(new EmployeeBasicPayDetail
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeBasicPayId = basicPay.Id,
                WageTypeId = wageType.Id,
                Amount = 500,
                CurrencyId = currency.Id
            });
        });

        var resolvedCurrency = await _currencyService.GetEmployeeCurrencyAsync(employee.Id);

        resolvedCurrency.ShouldNotBeNull();
        resolvedCurrency.Code.ShouldBe("KWD");
    }

    [Fact]
    public async Task Should_Return_Null_Currency_When_Employee_Has_No_BasicPay()
    {
        var employee = CreateEmployee("CUR400");

        var resolvedCurrency = await _currencyService.GetEmployeeCurrencyAsync(employee.Id);

        resolvedCurrency.ShouldBeNull();
    }
}
