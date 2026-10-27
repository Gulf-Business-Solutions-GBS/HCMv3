using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class TempEmployeeBenefitsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private TempEmployeeBenefitsAppService CreateAppService()
    {
        return CreateService(() => new TempEmployeeBenefitsAppService(FakeRepository.For<TempEmployeeBenefit>(Context)));
    }

    private int CreateTempBenefitType(string grade)
    {
        var entity = new TempBenefitType { TenantId = AbpSession.TenantId ?? 1, Grade = grade };
        Context.TempBenefitTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
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
    public async Task Should_Create_TempEmployeeBenefit()
    {
        var employeeId = CreateEmployee();
        var tempBenefitTypeId = CreateTempBenefitType("Grade1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTempEmployeeBenefitDto
        {
            EmployeeId = employeeId,
            TempBenefitTypeId = tempBenefitTypeId
        });

        var result = await service.GetAll(new GetAllTempEmployeeBenefitsInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_Employee_Already_Assigned_To_Any_Grade()
    {
        var employeeId = CreateEmployee();
        var tempBenefitTypeId1 = CreateTempBenefitType("Grade2");
        var tempBenefitTypeId2 = CreateTempBenefitType("Grade3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTempEmployeeBenefitDto
        {
            EmployeeId = employeeId,
            TempBenefitTypeId = tempBenefitTypeId1
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditTempEmployeeBenefitDto
            {
                EmployeeId = employeeId,
                TempBenefitTypeId = tempBenefitTypeId2
            }));
    }
}
