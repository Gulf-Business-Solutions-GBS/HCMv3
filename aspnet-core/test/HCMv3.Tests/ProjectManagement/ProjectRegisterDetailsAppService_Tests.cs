using System;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.ProjectManagement;
using HCMv3.ProjectManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ProjectManagement;

// ReSharper disable once InconsistentNaming
public class ProjectRegisterDetailsAppService_Tests : ProjectManagementAppServiceTestBase
{
    private ProjectRegisterDetailsAppService CreateAppService()
    {
        return CreateService(() => new ProjectRegisterDetailsAppService(FakeRepository.For<ProjectRegisterDetail>(Context)));
    }

    private int CreateProjectRegister(int employeeId)
    {
        var entity = new ProjectRegister
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            Period = new DateTime(2026, 1, 1)
        };
        Context.ProjectRegisters.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateProject()
    {
        var projectType = new ProjectType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "PT1",
            NameEn = "Type",
            NameAr = "Type"
        };
        Context.ProjectTypes.Add(projectType);
        Context.SaveChanges();

        var project = new Project
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "P1",
            NameEn = "Project",
            NameAr = "Project",
            ProjectTypeId = projectType.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };
        Context.Projects.Add(project);
        Context.SaveChanges();
        return project.Id;
    }

    private int CreateWageType()
    {
        var wageType = new WageType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "WT1",
            NameEn = "WT1",
            NameAr = "WT1"
        };
        Context.WageTypes.Add(wageType);
        Context.SaveChanges();
        return wageType.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ProjectRegisterDetail()
    {
        var employeeId = CreateEmployee();
        var projectRegisterId = CreateProjectRegister(employeeId);
        var projectId = CreateProject();
        var wageTypeId = CreateWageType();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditProjectRegisterDetailDto
        {
            ProjectRegisterId = projectRegisterId,
            ProjectId = projectId,
            WageTypeId = wageTypeId,
            Number = 5,
            Amount = 500
        });

        var result = await service.GetAll(new GetAllProjectRegisterDetailsInput { ProjectRegisterId = projectRegisterId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Amount.ShouldBe(500);
    }
}
