using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>Legacy EmployeeFundDetail.UpdateDueBalance: every detail save/delete re-totals the parent fund.</summary>
// ReSharper disable once InconsistentNaming
public class EmployeeFundDetailsAppService_Retotal_Tests : AppTestBase
{
    private readonly IEmployeeFundDetailsAppService _appService;
    private readonly IEmployeeFundsAppService _fundsAppService;

    public EmployeeFundDetailsAppService_Retotal_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeFundDetailsAppService>();
        _fundsAppService = Resolve<IEmployeeFundsAppService>();
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

    private async Task<int> CreateFundAsync(string employeeCode)
    {
        var employee = CreateTestEmployee(employeeCode);
        return await _fundsAppService.CreateOrEdit(new CreateOrEditEmployeeFundDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31)
        });
    }

    private Task AddRowAsync(int fundId, FundPayType type, double amount) =>
        _appService.CreateOrEdit(new CreateOrEditEmployeeFundDetailDto
        {
            EmployeeFundId = fundId,
            TransactionDate = new DateTime(2024, 1, 31),
            Amount = amount,
            FundPayType = type
        });

    private EmployeeFund LoadFund(int id)
    {
        EmployeeFund fund = null;
        UsingDbContext(context => fund = context.Set<EmployeeFund>().Single(f => f.Id == id));
        return fund;
    }

    private int RowId(int fundId, FundPayType type)
    {
        var id = 0;
        UsingDbContext(context => id = context.Set<EmployeeFundDetail>()
            .First(d => d.EmployeeFundId == fundId && d.FundPayType == type).Id);
        return id;
    }

    [Fact]
    public async Task Should_Retotal_Fund_When_Rows_Are_Created()
    {
        // Arrange
        var fundId = await CreateFundAsync("FR100");

        // Act
        await AddRowAsync(fundId, FundPayType.Opening, 500);
        await AddRowAsync(fundId, FundPayType.Deduction, 50);
        await AddRowAsync(fundId, FundPayType.ExternalDeduction, 20);
        await AddRowAsync(fundId, FundPayType.Payment, 100);

        // Assert
        var fund = LoadFund(fundId);
        fund.Amount.ShouldBe(570);
        fund.PaidAmount.ShouldBe(100);
        fund.BalanceAmount.ShouldBe(470);
    }

    [Fact]
    public async Task Should_Retotal_Fund_When_Row_Is_Edited()
    {
        // Arrange
        var fundId = await CreateFundAsync("FR200");
        await AddRowAsync(fundId, FundPayType.Deduction, 50);
        var rowId = RowId(fundId, FundPayType.Deduction);

        // Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeFundDetailDto
        {
            Id = rowId,
            EmployeeFundId = fundId,
            TransactionDate = new DateTime(2024, 1, 31),
            Amount = 50,
            FundPayType = FundPayType.Payment
        });

        // Assert
        var fund = LoadFund(fundId);
        fund.Amount.ShouldBe(0);
        fund.PaidAmount.ShouldBe(50);
        fund.BalanceAmount.ShouldBe(-50);
    }

    [Fact]
    public async Task Should_Retotal_Fund_When_Row_Is_Deleted()
    {
        // Arrange
        var fundId = await CreateFundAsync("FR300");
        await AddRowAsync(fundId, FundPayType.Deduction, 40);
        await AddRowAsync(fundId, FundPayType.Payment, 15);
        var paymentId = RowId(fundId, FundPayType.Payment);

        // Act
        await _appService.Delete(new EntityDto(paymentId));

        // Assert
        var fund = LoadFund(fundId);
        fund.Amount.ShouldBe(40);
        fund.PaidAmount.ShouldBe(0);
        fund.BalanceAmount.ShouldBe(40);
    }

    [Fact]
    public async Task Should_Retotal_Both_Funds_When_Row_Moves_To_Another_Fund()
    {
        // Arrange
        var fromFundId = await CreateFundAsync("FR400");
        var toFundId = await CreateFundAsync("FR401");
        await AddRowAsync(fromFundId, FundPayType.Deduction, 70);
        var rowId = RowId(fromFundId, FundPayType.Deduction);

        // Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeFundDetailDto
        {
            Id = rowId,
            EmployeeFundId = toFundId,
            TransactionDate = new DateTime(2024, 1, 31),
            Amount = 70,
            FundPayType = FundPayType.Deduction
        });

        // Assert
        LoadFund(fromFundId).Amount.ShouldBe(0);
        LoadFund(toFundId).Amount.ShouldBe(70);
    }
}
