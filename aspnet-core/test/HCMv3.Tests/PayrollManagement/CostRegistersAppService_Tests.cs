using System;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class CostRegistersAppService_Tests : PayrollManagementAppServiceTestBase
{
    private CostRegistersAppService CreateAppService()
    {
        return CreateService(() => new CostRegistersAppService(FakeRepository.For<CostRegister>(Context)));
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
    public async Task Should_Create_And_Retrieve_CostRegister()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCostRegisterDto
        {
            EmployeeId = employeeId,
            Period = new DateTime(2026, 1, 1),
            PayrollRunType = PayrollRunType.Regular
        });

        var result = await service.GetAll(new GetAllCostRegistersInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].PayrollRunType.ShouldBe(PayrollRunType.Regular);
    }
}
