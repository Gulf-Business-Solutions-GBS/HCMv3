using System;
using System.Threading.Tasks;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class CostRegisterDetailsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private CostRegisterDetailsAppService CreateAppService()
    {
        return CreateService(() => new CostRegisterDetailsAppService(FakeRepository.For<CostRegisterDetail>(Context)));
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

    private int CreateCostRegister(int employeeId)
    {
        var entity = new CostRegister
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            Period = new DateTime(2026, 1, 1),
            PayrollRunType = PayrollRunType.Regular
        };
        Context.CostRegisters.Add(entity);
        Context.SaveChanges();
        return entity.Id;
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

    private int CreateCostElement(string code)
    {
        var entity = new CostElement { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.CostElements.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_CostRegisterDetail()
    {
        var employeeId = CreateEmployee();
        var costRegisterId = CreateCostRegister(employeeId);
        var wageTypeId = CreateWageType("CRD_WT1");
        var accountInfoId = CreateAccountInfo("CRD_AI1");
        var costElementId = CreateCostElement("CRD_CE1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCostRegisterDetailDto
        {
            CostRegisterId = costRegisterId,
            WageTypeId = wageTypeId,
            AccountInfoId = accountInfoId,
            CostElement1Id = costElementId,
            Amount = 1500.5
        });

        var result = await service.GetAll(new GetAllCostRegisterDetailsInput { CostRegisterId = costRegisterId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Amount.ShouldBe(1500.5);
        result.Items[0].CostElement1Id.ShouldBe(costElementId);
    }
}
