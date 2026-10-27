using System;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeTasksAppService_Tests : AppTestBase
{
    private readonly IEmployeeTasksAppService _appService;

    public EmployeeTasksAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeTasksAppService>();
    }

    private Employee CreateTestEmployee(string code)
    {
        Nationality nationality = null;
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

        Employee employee = null;
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

    private TaskType CreateTestTaskType(string code)
    {
        TaskType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<TaskType>().Add(new TaskType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return type;
    }

    [Fact]
    public async Task Should_Create_EmployeeTask()
    {
        //Arrange
        var employee = CreateTestEmployee("ET100");
        var type = CreateTestTaskType("TT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeTaskDto
        {
            EmployeeId = employee.Id,
            TaskTypeId = type.Id,
            TaskDate = new DateTime(2024, 3, 15),
            TaskStatus = 1,
            MonitoringType = 0,
            MonitoringUnit = 0
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeTasksInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].TaskStatus.ShouldBe(1);
    }
}
