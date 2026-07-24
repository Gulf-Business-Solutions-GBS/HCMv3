using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.ProjectManagement;
using HCMv3.ProjectManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ProjectManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeProjectsAppService_Tests : ProjectManagementAppServiceTestBase
{
    private EmployeeProjectsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeProjectsAppService(FakeRepository.For<EmployeeProject>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EmployeeProject()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeProjectDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            CostingMethod = ProjectCostingMethod.ActualDays
        });

        var result = await service.GetAll(new GetAllEmployeeProjectsInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].CostingMethod.ShouldBe(ProjectCostingMethod.ActualDays);
    }

    [Fact]
    public async Task Should_Throw_When_StartDate_Not_First_Day_Of_Month()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmployeeProjectDto
            {
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 1, 5),
                EndDate = new DateTime(2026, 1, 31)
            }));
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Not_Last_Day_Of_Month()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmployeeProjectDto
            {
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 1, 30)
            }));
    }

    [Fact]
    public async Task Should_Throw_When_Overlapping_With_Other_EmployeeProject_For_Same_Employee()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeProjectDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31)
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmployeeProjectDto
            {
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 1, 31)
            }));
    }
}
