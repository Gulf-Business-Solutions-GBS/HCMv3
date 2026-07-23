using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeSubstituteDutiesAppService_Tests : TimeManagementAppServiceTestBase
{
    private EmployeeSubstituteDutiesAppService CreateAppService()
    {
        return CreateService(() => new EmployeeSubstituteDutiesAppService(FakeRepository.For<EmployeeSubstituteDuty>(Context)));
    }

    private int CreateNationality()
    {
        var entity = new HCMv3.GeneralMasterInfo.Nationality
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Local",
            NameAr = "محلي"
        };
        Context.Nationalities.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateEmployee(string firstName)
    {
        var entity = new Employee
        {
            TenantId = AbpSession.TenantId ?? 1,
            FirstNameEn = firstName,
            LastNameEn = "Doe",
            FirstNameAr = firstName,
            LastNameAr = "Doe",
            FullNameEn = firstName,
            FullNameAr = firstName,
            NationalityId = CreateNationality()
        };
        Context.Employees.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateDuty(string code)
    {
        var entity = new Duty
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.Duties.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_EmployeeSubstituteDuty()
    {
        var employeeId = CreateEmployee("John");
        var dutyId = CreateDuty("D1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubstituteDutyDto
        {
            EmployeeId = employeeId,
            DutyId = dutyId,
            SubstituteDate = new DateTime(2026, 1, 1)
        });

        var result = await service.GetAll(new GetAllEmployeeSubstituteDutiesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
        result.Items[0].DutyId.ShouldBe(dutyId);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_SubstituteDate_For_Same_Employee()
    {
        var employeeId = CreateEmployee("Jane");
        var dutyId = CreateDuty("D2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubstituteDutyDto
        {
            EmployeeId = employeeId,
            DutyId = dutyId,
            SubstituteDate = new DateTime(2026, 1, 1)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeSubstituteDutyDto
            {
                EmployeeId = employeeId,
                DutyId = dutyId,
                SubstituteDate = new DateTime(2026, 1, 1)
            }));
    }
}
