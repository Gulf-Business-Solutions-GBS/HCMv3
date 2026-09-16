using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeBasicPayDetailsAppService_Tests : AppTestBase
{
    private readonly IEmployeeBasicPayDetailsAppService _appService;
    private readonly IEmployeeBasicPaysAppService _basicPaysAppService;

    public EmployeeBasicPayDetailsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeBasicPayDetailsAppService>();
        _basicPaysAppService = Resolve<IEmployeeBasicPaysAppService>();
    }

    private Employee CreateTestEmployee(string code)
    {
        Nationality nationality = null;
        Employee employee = null;
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
                NameAr = code
            }).Entity;
        });
        return wageType;
    }

    private Currency CreateTestCurrency(string code)
    {
        Currency currency = null;
        UsingDbContext(context =>
        {
            currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return currency;
    }

    private async Task<int> CreateTestBasicPayAsync(int employeeId, DateTime startDate)
    {
        await _basicPaysAppService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDto
        {
            EmployeeId = employeeId,
            StartDate = startDate
        });

        var result = await _basicPaysAppService.GetAll(new GetAllEmployeeBasicPaysInput { EmployeeId = employeeId });
        return result.Items[0].Id;
    }

    [Fact]
    public async Task Should_Create_BasicPayDetail()
    {
        //Arrange
        var employee = CreateTestEmployee("BPD100");
        var wageType = CreateTestWageType("WT100");
        var currency = CreateTestCurrency("CUR100");
        var basicPayId = await CreateTestBasicPayAsync(employee.Id, new DateTime(2024, 1, 1));

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDetailDto
        {
            EmployeeBasicPayId = basicPayId,
            WageTypeId = wageType.Id,
            CurrencyId = currency.Id,
            Amount = 1000
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeBasicPayDetailsInput { EmployeeBasicPayId = basicPayId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Amount.ShouldBe(1000);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_WageType()
    {
        //Arrange
        var employee = CreateTestEmployee("BPD200");
        var wageType = CreateTestWageType("WT200");
        var currency = CreateTestCurrency("CUR200");
        var basicPayId = await CreateTestBasicPayAsync(employee.Id, new DateTime(2024, 1, 1));

        await _appService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDetailDto
        {
            EmployeeBasicPayId = basicPayId,
            WageTypeId = wageType.Id,
            CurrencyId = currency.Id,
            Amount = 1000
        });

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDetailDto
            {
                EmployeeBasicPayId = basicPayId,
                WageTypeId = wageType.Id,
                CurrencyId = currency.Id,
                Amount = 500
            }));
    }

    [Fact]
    public async Task Should_Keep_The_Basic_Pay_Amount_At_The_Total_Of_Its_Lines()
    {
        //Arrange
        var employee = CreateTestEmployee("BPD300");
        var basic = CreateTestWageType("WT300");
        var housing = CreateTestWageType("WT301");
        var currency = CreateTestCurrency("CUR300");
        var basicPayId = await CreateTestBasicPayAsync(employee.Id, new DateTime(2024, 1, 1));

        //Act & Assert: two lines add up
        await _appService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDetailDto
        {
            EmployeeBasicPayId = basicPayId, WageTypeId = basic.Id, CurrencyId = currency.Id, Amount = 400
        });
        await _appService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDetailDto
        {
            EmployeeBasicPayId = basicPayId, WageTypeId = housing.Id, CurrencyId = currency.Id, Amount = 150.25
        });
        (await HeaderAmountAsync(employee.Id)).ShouldBe(550.25);

        //Act & Assert: an edited line changes the total
        var lines = await _appService.GetAll(new GetAllEmployeeBasicPayDetailsInput { EmployeeBasicPayId = basicPayId });
        var housingLine = lines.Items.Single(l => l.WageTypeId == housing.Id);
        await _appService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDetailDto
        {
            Id = housingLine.Id, EmployeeBasicPayId = basicPayId, WageTypeId = housing.Id, CurrencyId = currency.Id, Amount = 100
        });
        (await HeaderAmountAsync(employee.Id)).ShouldBe(500);

        //Act & Assert: a deleted line leaves the total
        await _appService.Delete(new EntityDto(housingLine.Id));
        (await HeaderAmountAsync(employee.Id)).ShouldBe(400);
    }

    [Fact]
    public async Task Should_Filter_Lines_By_Wage_Type()
    {
        //Arrange
        var employee = CreateTestEmployee("BPD400");
        var housing = CreateTestWageType("HOUSING400");
        var transport = CreateTestWageType("TRANSPORT400");
        var currency = CreateTestCurrency("CUR400");
        var basicPayId = await CreateTestBasicPayAsync(employee.Id, new DateTime(2024, 1, 1));
        foreach (var wageType in new[] { housing, transport })
        {
            await _appService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDetailDto
            {
                EmployeeBasicPayId = basicPayId, WageTypeId = wageType.Id, CurrencyId = currency.Id, Amount = 10
            });
        }

        //Act
        var result = await _appService.GetAll(new GetAllEmployeeBasicPayDetailsInput { EmployeeBasicPayId = basicPayId, Filter = "TRANS" });

        //Assert
        result.TotalCount.ShouldBe(1);
        result.Items[0].WageTypeId.ShouldBe(transport.Id);
    }

    private async Task<double> HeaderAmountAsync(int employeeId)
    {
        var result = await _basicPaysAppService.GetAll(new GetAllEmployeeBasicPaysInput { EmployeeId = employeeId });
        return result.Items.Single().Amount;
    }
}
