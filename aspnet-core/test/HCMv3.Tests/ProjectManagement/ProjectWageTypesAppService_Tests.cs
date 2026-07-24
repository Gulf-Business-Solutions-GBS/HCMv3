using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.ProjectManagement;
using HCMv3.ProjectManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ProjectManagement;

// ReSharper disable once InconsistentNaming
public class ProjectWageTypesAppService_Tests : ProjectManagementAppServiceTestBase
{
    private ProjectWageTypesAppService CreateAppService()
    {
        return CreateService(() => new ProjectWageTypesAppService(FakeRepository.For<ProjectWageType>(Context)));
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

    private int CreateWageType(string code)
    {
        var wageType = new WageType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.WageTypes.Add(wageType);
        Context.SaveChanges();
        return wageType.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ProjectWageType()
    {
        var projectId = CreateProject();
        var wageTypeId = CreateWageType("WT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditProjectWageTypeDto
        {
            ProjectId = projectId,
            WageTypeId = wageTypeId
        });

        var result = await service.GetAll(new GetAllProjectWageTypesInput { ProjectId = projectId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_WageType_Already_Assigned_To_Same_Project()
    {
        var projectId = CreateProject();
        var wageTypeId = CreateWageType("WT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditProjectWageTypeDto { ProjectId = projectId, WageTypeId = wageTypeId });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditProjectWageTypeDto { ProjectId = projectId, WageTypeId = wageTypeId }));
    }
}
