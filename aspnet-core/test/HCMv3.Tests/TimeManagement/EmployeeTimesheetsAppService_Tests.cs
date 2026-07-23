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
public class EmployeeTimesheetsAppService_Tests : TimeManagementAppServiceTestBase
{
    private EmployeeTimesheetsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeTimesheetsAppService(FakeRepository.For<EmployeeTimesheet>(Context)));
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
    public async Task Should_Create_EmployeeTimesheet()
    {
        var employeeId = CreateEmployee("John");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeTimesheetDto
        {
            EmployeeId = employeeId,
            TransDate = new DateTime(2026, 1, 1),
            ActualStartTime = new TimeSpan(8, 0, 0),
            BreakStartTime = new TimeSpan(12, 0, 0),
            BreakEndTime = new TimeSpan(13, 0, 0),
            ActualEndTime = new TimeSpan(17, 0, 0),
            TotalHours = 8,
            Approved = 0,
            Comments = "Test entry"
        });

        var result = await service.GetAll(new GetAllEmployeeTimesheetsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
        result.Items[0].TotalHours.ShouldBe(8);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_TransDate_For_Same_Employee()
    {
        var employeeId = CreateEmployee("Jane");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeTimesheetDto
        {
            EmployeeId = employeeId,
            TransDate = new DateTime(2026, 1, 1),
            ActualStartTime = new TimeSpan(8, 0, 0),
            ActualEndTime = new TimeSpan(17, 0, 0)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeTimesheetDto
            {
                EmployeeId = employeeId,
                TransDate = new DateTime(2026, 1, 1),
                ActualStartTime = new TimeSpan(9, 0, 0),
                ActualEndTime = new TimeSpan(18, 0, 0)
            }));
    }
}
