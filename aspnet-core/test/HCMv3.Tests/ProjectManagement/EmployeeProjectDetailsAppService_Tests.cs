using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.ProjectManagement;
using HCMv3.ProjectManagement.Dto;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ProjectManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeProjectDetailsAppService_Tests : ProjectManagementAppServiceTestBase
{
    private EmployeeProjectDetailsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeProjectDetailsAppService(
            FakeRepository.For<EmployeeProjectDetail>(Context),
            FakeRepository.For<EmployeeProject>(Context),
            FakeRepository.For<TimeRegister>(Context)));
    }

    private int CreateProjectType()
    {
        var entity = new ProjectType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "PT1",
            NameEn = "Type",
            NameAr = "Type"
        };
        Context.ProjectTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateProject(string code)
    {
        var project = new Project
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            ProjectTypeId = CreateProjectType(),
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };
        Context.Projects.Add(project);
        Context.SaveChanges();
        return project.Id;
    }

    private int CreateEmployeeProject(int employeeId, ProjectCostingMethod costingMethod)
    {
        var entity = new EmployeeProject
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            CostingMethod = costingMethod
        };
        Context.EmployeeProjects.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EmployeeProjectDetail()
    {
        var employeeId = CreateEmployee();
        var employeeProjectId = CreateEmployeeProject(employeeId, ProjectCostingMethod.ActualDays);
        var projectId = CreateProject("P1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeProjectDetailDto
        {
            EmployeeProjectId = employeeProjectId,
            ProjectId = projectId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 15)
        });

        var result = await service.GetAll(new GetAllEmployeeProjectDetailsInput { EmployeeProjectId = employeeProjectId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_DateRange_Outside_Parent_Range()
    {
        var employeeId = CreateEmployee();
        var employeeProjectId = CreateEmployeeProject(employeeId, ProjectCostingMethod.ActualDays);
        var projectId = CreateProject("P1");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmployeeProjectDetailDto
            {
                EmployeeProjectId = employeeProjectId,
                ProjectId = projectId,
                StartDate = new DateTime(2025, 12, 15),
                EndDate = new DateTime(2026, 1, 15)
            }));
    }

    [Fact]
    public async Task Should_Throw_When_Percentage_Total_Exceeds_100()
    {
        var employeeId = CreateEmployee();
        var employeeProjectId = CreateEmployeeProject(employeeId, ProjectCostingMethod.Percentage);
        var project1 = CreateProject("P1");
        var project2 = CreateProject("P2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeProjectDetailDto
        {
            EmployeeProjectId = employeeProjectId,
            ProjectId = project1,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 15),
            Percentage = 70
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmployeeProjectDetailDto
            {
                EmployeeProjectId = employeeProjectId,
                ProjectId = project2,
                StartDate = new DateTime(2026, 1, 16),
                EndDate = new DateTime(2026, 1, 31),
                Percentage = 40
            }));
    }

    [Fact]
    public async Task Should_Throw_When_Deleting_Last_Detail()
    {
        var employeeId = CreateEmployee();
        var employeeProjectId = CreateEmployeeProject(employeeId, ProjectCostingMethod.ActualDays);
        var projectId = CreateProject("P1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeProjectDetailDto
        {
            EmployeeProjectId = employeeProjectId,
            ProjectId = projectId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 15)
        });

        var all = await service.GetAll(new GetAllEmployeeProjectDetailsInput { EmployeeProjectId = employeeProjectId });
        var detailId = all.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.Delete(new Abp.Application.Services.Dto.EntityDto(detailId)));
    }

    [Fact]
    public async Task Should_Calculate_WorkDays_From_TimeRegister()
    {
        var employeeId = CreateEmployee();
        var employeeProjectId = CreateEmployeeProject(employeeId, ProjectCostingMethod.ActualDays);
        var projectId = CreateProject("P1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeProjectDetailDto
        {
            EmployeeProjectId = employeeProjectId,
            ProjectId = projectId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 5)
        });

        var all = await service.GetAll(new GetAllEmployeeProjectDetailsInput { EmployeeProjectId = employeeProjectId });
        var detailId = all.Items[0].Id;

        Context.TimeRegisters.Add(new TimeRegister
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            DayDate = new DateTime(2026, 1, 2),
            ApprovedActualWorkingDay = 1
        });
        Context.TimeRegisters.Add(new TimeRegister
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            DayDate = new DateTime(2026, 1, 3),
            RestDay = 1
        });
        Context.TimeRegisters.Add(new TimeRegister
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            DayDate = new DateTime(2026, 2, 1), // outside range -- must not count
            ApprovedActualWorkingDay = 1
        });
        Context.SaveChanges();

        var result = await service.CalculateWorkDaysAsync(new CalculateWorkDaysInput { EmployeeProjectDetailId = detailId });

        result.WorkDays.ShouldBe(2);
    }
}
