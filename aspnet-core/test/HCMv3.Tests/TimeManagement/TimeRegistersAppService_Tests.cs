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
public class TimeRegistersAppService_Tests : TimeManagementAppServiceTestBase
{
    private TimeRegistersAppService CreateAppService()
    {
        return CreateService(() => new TimeRegistersAppService(FakeRepository.For<TimeRegister>(Context)));
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

    [Fact]
    public async Task Should_Create_TimeRegister()
    {
        var employeeId = CreateEmployee("John");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTimeRegisterDto
        {
            EmployeeId = employeeId,
            DayDate = new DateTime(2026, 1, 1),
            ActualWorkingMins = 480,
            Status = TimeRegisterStatus.Opened
        });

        var result = await service.GetAll(new GetAllTimeRegistersInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
        result.Items[0].ActualWorkingMins.ShouldBe(480);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Employee_DayDate()
    {
        var employeeId = CreateEmployee("Jane");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTimeRegisterDto
        {
            EmployeeId = employeeId,
            DayDate = new DateTime(2026, 1, 1)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditTimeRegisterDto
            {
                EmployeeId = employeeId,
                DayDate = new DateTime(2026, 1, 1)
            }));
    }
}
