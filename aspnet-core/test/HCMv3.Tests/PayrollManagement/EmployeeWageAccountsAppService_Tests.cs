using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeWageAccountsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private EmployeeWageAccountsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeWageAccountsAppService(FakeRepository.For<EmployeeWageAccount>(Context)));
    }

    private int CreateWageType(string code)
    {
        var entity = new WageType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code, PayrollOrder = 1 };
        Context.WageTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateAccountInfo(string code)
    {
        var accountType = new AccountType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.AccountTypes.Add(accountType);
        Context.SaveChanges();

        var accountInfo = new AccountInfo
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            AccountTypeId = accountType.Id
        };
        Context.AccountInfos.Add(accountInfo);
        Context.SaveChanges();
        return accountInfo.Id;
    }

    private int CreateEmployee()
    {
        var nationality = new HCMv3.GeneralMasterInfo.Nationality
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "N",
            NameAr = "N"
        };
        Context.Nationalities.Add(nationality);
        Context.SaveChanges();

        var entity = new Employee
        {
            TenantId = AbpSession.TenantId ?? 1,
            FirstNameEn = "F",
            LastNameEn = "L",
            FirstNameAr = "ف",
            LastNameAr = "ل",
            NationalityId = nationality.Id,
            JoiningDate = new DateTime(2020, 1, 1)
        };
        Context.Employees.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_EmployeeWageAccount_With_Infinite_EndDate()
    {
        var employeeId = CreateEmployee();
        var wageTypeId = CreateWageType("EWA_WT1");
        var accountInfoId = CreateAccountInfo("EWA_AI1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeWageAccountDto
        {
            EmployeeId = employeeId,
            WageTypeId = wageTypeId,
            StartDate = new DateTime(2025, 1, 1),
            AccountInfoId = accountInfoId,
            CostPercentage = 100
        });

        var result = await service.GetAll(new GetAllEmployeeWageAccountsInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EndDate.ShouldBe(InfiniteEndDate);
    }

    [Fact]
    public async Task Should_Auto_Chain_EndDate_When_New_Record_Added()
    {
        var employeeId = CreateEmployee();
        var wageTypeId = CreateWageType("EWA_WT2");
        var accountInfoId1 = CreateAccountInfo("EWA_AI2");
        var accountInfoId2 = CreateAccountInfo("EWA_AI3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeWageAccountDto
        {
            EmployeeId = employeeId,
            WageTypeId = wageTypeId,
            StartDate = new DateTime(2025, 1, 1),
            AccountInfoId = accountInfoId1,
            CostPercentage = 100
        });

        await service.CreateOrEdit(new CreateOrEditEmployeeWageAccountDto
        {
            EmployeeId = employeeId,
            WageTypeId = wageTypeId,
            StartDate = new DateTime(2025, 6, 1),
            AccountInfoId = accountInfoId2,
            CostPercentage = 100
        });

        var records = Context.EmployeeWageAccounts
            .Where(a => a.EmployeeId == employeeId && a.WageTypeId == wageTypeId)
            .OrderBy(a => a.StartDate)
            .ToList();

        records.Count.ShouldBe(2);
        records[0].EndDate.ShouldBe(new DateTime(2025, 5, 31));
        records[1].EndDate.ShouldBe(InfiniteEndDate);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Combination()
    {
        var employeeId = CreateEmployee();
        var wageTypeId = CreateWageType("EWA_WT3");
        var accountInfoId = CreateAccountInfo("EWA_AI4");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeWageAccountDto
        {
            EmployeeId = employeeId,
            WageTypeId = wageTypeId,
            StartDate = new DateTime(2025, 1, 1),
            AccountInfoId = accountInfoId,
            CostPercentage = 50
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeWageAccountDto
            {
                EmployeeId = employeeId,
                WageTypeId = wageTypeId,
                StartDate = new DateTime(2025, 1, 1),
                AccountInfoId = accountInfoId,
                CostPercentage = 50
            }));
    }

    [Fact]
    public async Task Should_Allow_Two_Rows_Summing_To_Exactly_100()
    {
        var employeeId = CreateEmployee();
        var wageTypeId = CreateWageType("EWA_WT4");
        var accountInfoId1 = CreateAccountInfo("EWA_AI5");
        var accountInfoId2 = CreateAccountInfo("EWA_AI6");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeWageAccountDto
        {
            EmployeeId = employeeId,
            WageTypeId = wageTypeId,
            StartDate = new DateTime(2025, 1, 1),
            AccountInfoId = accountInfoId1,
            CostPercentage = 60
        });

        await service.CreateOrEdit(new CreateOrEditEmployeeWageAccountDto
        {
            EmployeeId = employeeId,
            WageTypeId = wageTypeId,
            StartDate = new DateTime(2025, 1, 1),
            AccountInfoId = accountInfoId2,
            CostPercentage = 40
        });

        var result = await service.GetAll(new GetAllEmployeeWageAccountsInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Throw_When_Sum_Of_CostPercentage_Exceeds_100()
    {
        var employeeId = CreateEmployee();
        var wageTypeId = CreateWageType("EWA_WT5");
        var accountInfoId1 = CreateAccountInfo("EWA_AI7");
        var accountInfoId2 = CreateAccountInfo("EWA_AI8");
        var accountInfoId3 = CreateAccountInfo("EWA_AI9");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeWageAccountDto
        {
            EmployeeId = employeeId,
            WageTypeId = wageTypeId,
            StartDate = new DateTime(2025, 1, 1),
            AccountInfoId = accountInfoId1,
            CostPercentage = 60
        });

        await service.CreateOrEdit(new CreateOrEditEmployeeWageAccountDto
        {
            EmployeeId = employeeId,
            WageTypeId = wageTypeId,
            StartDate = new DateTime(2025, 1, 1),
            AccountInfoId = accountInfoId2,
            CostPercentage = 40
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeWageAccountDto
            {
                EmployeeId = employeeId,
                WageTypeId = wageTypeId,
                StartDate = new DateTime(2025, 1, 1),
                AccountInfoId = accountInfoId3,
                CostPercentage = 1
            }));
    }
}
