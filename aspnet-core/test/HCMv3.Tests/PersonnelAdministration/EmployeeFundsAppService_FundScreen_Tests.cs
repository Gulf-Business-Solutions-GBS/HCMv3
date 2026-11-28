using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>The Fund screen (legacy LHRMS "Fund"): list filter, employee lookup, read-only totals, cascade delete.</summary>
// ReSharper disable once InconsistentNaming
public class EmployeeFundsAppService_FundScreen_Tests : AppTestBase
{
    private readonly IEmployeeFundsAppService _appService;
    private readonly IEmployeeFundDetailsAppService _detailsAppService;
    private readonly IEmployeeFundExemptionsAppService _exemptionsAppService;

    public EmployeeFundsAppService_FundScreen_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeFundsAppService>();
        _detailsAppService = Resolve<IEmployeeFundDetailsAppService>();
        _exemptionsAppService = Resolve<IEmployeeFundExemptionsAppService>();
    }

    private Employee CreateTestEmployee(string code, string fullNameEn = "F L")
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
                FullNameEn = fullNameEn,
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });
        return employee;
    }

    private Task<int> CreateFundAsync(int employeeId) =>
        _appService.CreateOrEdit(new CreateOrEditEmployeeFundDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31)
        });

    private Task AddRowAsync(int fundId, FundPayType type, double amount) =>
        _detailsAppService.CreateOrEdit(new CreateOrEditEmployeeFundDetailDto
        {
            EmployeeFundId = fundId,
            TransactionDate = new DateTime(2024, 1, 31),
            Amount = amount,
            FundPayType = type
        });

    [Fact]
    public async Task Should_Return_New_Fund_Id_From_CreateOrEdit()
    {
        // Arrange
        var employee = CreateTestEmployee("FS100");

        // Act
        var id = await CreateFundAsync(employee.Id);

        // Assert
        id.ShouldBeGreaterThan(0);
        UsingDbContext(context =>
            context.Set<EmployeeFund>().Any(f => f.Id == id && f.EmployeeId == employee.Id).ShouldBeTrue());
    }

    [Fact]
    public async Task Should_Return_Same_Id_When_Editing()
    {
        // Arrange
        var employee = CreateTestEmployee("FS110");
        var id = await CreateFundAsync(employee.Id);

        // Act
        var editedId = await _appService.CreateOrEdit(new CreateOrEditEmployeeFundDto
        {
            Id = id,
            EmployeeId = employee.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            Comments = "Edited"
        });

        // Assert
        editedId.ShouldBe(id);
    }

    [Fact]
    public async Task Should_Throw_When_Employee_Missing()
    {
        // Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(() => CreateFundAsync(0));
    }

    [Fact]
    public async Task Should_Filter_List_By_Employee_Code()
    {
        // Arrange
        var match = CreateTestEmployee("FSFILTER1");
        var other = CreateTestEmployee("FS120");
        await CreateFundAsync(match.Id);
        await CreateFundAsync(other.Id);

        // Act
        var result = await _appService.GetAll(new GetAllEmployeeFundsInput { Filter = "FSFILTER1" });

        // Assert
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeCode.ShouldBe("FSFILTER1");
    }

    [Fact]
    public async Task Should_Return_Employee_And_Totals_For_Edit()
    {
        // Arrange
        var employee = CreateTestEmployee("FS130", "Fund Person");
        var id = await CreateFundAsync(employee.Id);
        await AddRowAsync(id, FundPayType.Deduction, 80);
        await AddRowAsync(id, FundPayType.Payment, 30);

        // Act
        var output = await _appService.GetEmployeeFundForEdit(new EntityDto(id));

        // Assert
        output.EmployeeCode.ShouldBe("FS130");
        output.EmployeeFullNameEn.ShouldBe("Fund Person");
        output.Amount.ShouldBe(80);
        output.PaidAmount.ShouldBe(30);
        output.BalanceAmount.ShouldBe(50);
    }

    [Fact]
    public async Task Should_Keep_Totals_When_Fund_Is_Saved()
    {
        // Arrange
        var employee = CreateTestEmployee("FS140");
        var id = await CreateFundAsync(employee.Id);
        await AddRowAsync(id, FundPayType.Opening, 60);

        // Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeFundDto
        {
            Id = id,
            EmployeeId = employee.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            Comments = "x"
        });

        // Assert
        var output = await _appService.GetEmployeeFundForEdit(new EntityDto(id));
        output.Amount.ShouldBe(60);
        output.BalanceAmount.ShouldBe(60);
    }

    [Fact]
    public async Task Should_Delete_Details_And_Exemptions_With_Fund()
    {
        // Arrange
        var employee = CreateTestEmployee("FS150");
        var id = await CreateFundAsync(employee.Id);
        await AddRowAsync(id, FundPayType.Deduction, 10);
        await _exemptionsAppService.CreateOrEdit(new CreateOrEditEmployeeFundExemptionDto
        {
            EmployeeFundId = id,
            StartDate = new DateTime(2024, 3, 1),
            EndDate = new DateTime(2024, 3, 31)
        });

        // Act
        await _appService.Delete(new EntityDto(id));

        // Assert
        UsingDbContext(context =>
        {
            context.Set<EmployeeFundDetail>().Count(d => d.EmployeeFundId == id && !d.IsDeleted).ShouldBe(0);
            context.Set<EmployeeFundExemption>().Count(e => e.EmployeeFundId == id && !e.IsDeleted).ShouldBe(0);
        });
    }

    [Fact]
    public async Task Should_Find_Employee_In_Lookup_By_Code()
    {
        // Arrange
        CreateTestEmployee("FSLOOKUP1", "Lookup Person");

        // Act
        var result = await _appService.GetEmployeesForLookup(new GetEmployeeFundEmployeesInput { Filter = "FSLOOKUP1" });

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Lookup Person");
    }
}
