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
public class EmployeeBasicPaysAppService_Tests : AppTestBase
{
    private readonly IEmployeeBasicPaysAppService _appService;

    public EmployeeBasicPaysAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeBasicPaysAppService>();
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

    [Fact]
    public async Task Should_Create_BasicPay()
    {
        //Arrange
        var employee = CreateTestEmployee("BP100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2024, 1, 1),
            PayType = PayType.Regular,
            PayInterval = PayInterval.Monthly
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeBasicPaysInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EndDate.Year.ShouldBe(9999);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_StartDate()
    {
        //Arrange
        var employee = CreateTestEmployee("BP200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2024, 1, 1)
        });

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDto
            {
                EmployeeId = employee.Id,
                StartDate = new DateTime(2024, 1, 1)
            }));
    }

    [Fact]
    public async Task Should_Delete_BasicPay()
    {
        //Arrange
        var employee = CreateTestEmployee("BP300");
        await _appService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2024, 1, 1)
        });

        var created = await _appService.GetAll(new GetAllEmployeeBasicPaysInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeBasicPaysInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_Id_Of_Created_BasicPay()
    {
        //Arrange
        var employee = CreateTestEmployee("BP400");

        //Act
        var id = await CreateAsync(employee.Id, new DateTime(2024, 1, 1));

        //Assert
        var edit = await _appService.GetEmployeeBasicPayForEdit(new EntityDto(id));
        edit.EmployeeBasicPay.EmployeeId.ShouldBe(employee.Id);
        edit.EmployeeBasicPay.StartDate.ShouldBe(new DateTime(2024, 1, 1));
    }

    [Fact]
    public async Task Should_End_Previous_BasicPay_The_Day_Before_A_Later_One()
    {
        //Arrange
        var employee = CreateTestEmployee("BP500");
        await CreateAsync(employee.Id, new DateTime(2024, 1, 1));

        //Act
        await CreateAsync(employee.Id, new DateTime(2024, 7, 1));

        //Assert
        var records = await GetPeriodsAsync(employee.Id);
        records[0].EndDate.ShouldBe(new DateTime(2024, 6, 30));
        records[1].EndDate.ShouldBe(new DateTime(9999, 12, 31));
    }

    [Fact]
    public async Task Should_End_Inserted_Earlier_BasicPay_The_Day_Before_The_Next_One()
    {
        //Arrange
        var employee = CreateTestEmployee("BP600");
        await CreateAsync(employee.Id, new DateTime(2024, 7, 1));

        //Act
        await CreateAsync(employee.Id, new DateTime(2024, 1, 1));

        //Assert
        var records = await GetPeriodsAsync(employee.Id);
        records[0].EndDate.ShouldBe(new DateTime(2024, 6, 30));
        records[1].EndDate.ShouldBe(new DateTime(9999, 12, 31));
    }

    [Fact]
    public async Task Should_Recalculate_End_Dates_When_Effective_Date_Is_Edited()
    {
        //Arrange
        var employee = CreateTestEmployee("BP700");
        await CreateAsync(employee.Id, new DateTime(2024, 1, 1));
        var laterId = await CreateAsync(employee.Id, new DateTime(2024, 7, 1));

        //Act
        var edit = (await _appService.GetEmployeeBasicPayForEdit(new EntityDto(laterId))).EmployeeBasicPay;
        edit.StartDate = new DateTime(2024, 4, 1);
        await _appService.CreateOrEdit(edit);

        //Assert
        var records = await GetPeriodsAsync(employee.Id);
        records[0].EndDate.ShouldBe(new DateTime(2024, 3, 31));
        records[1].StartDate.ShouldBe(new DateTime(2024, 4, 1));
        records[1].EndDate.ShouldBe(new DateTime(9999, 12, 31));
    }

    [Fact]
    public async Task Should_Reopen_Previous_BasicPay_When_Latest_Is_Deleted()
    {
        //Arrange
        var employee = CreateTestEmployee("BP800");
        await CreateAsync(employee.Id, new DateTime(2024, 1, 1));
        var laterId = await CreateAsync(employee.Id, new DateTime(2024, 7, 1));

        //Act
        await _appService.Delete(new EntityDto(laterId));

        //Assert
        var records = await GetPeriodsAsync(employee.Id);
        records.Count.ShouldBe(1);
        records[0].EndDate.ShouldBe(new DateTime(9999, 12, 31));
    }

    private Task<int> CreateAsync(int employeeId, DateTime effectiveDate)
    {
        return _appService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDto
        {
            EmployeeId = employeeId,
            StartDate = effectiveDate,
            PayType = PayType.Regular,
            PayInterval = PayInterval.Monthly
        });
    }

    private async Task<System.Collections.Generic.List<EmployeeBasicPayDto>> GetPeriodsAsync(int employeeId)
    {
        var result = await _appService.GetAll(new GetAllEmployeeBasicPaysInput
        {
            EmployeeId = employeeId,
            Sorting = "StartDate",
            MaxResultCount = 100
        });
        return result.Items.ToList();
    }
}
