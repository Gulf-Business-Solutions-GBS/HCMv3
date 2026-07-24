using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.ProjectManagement;
using HCMv3.ProjectManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ProjectManagement;

// ReSharper disable once InconsistentNaming
public class ProjectsAppService_Tests : ProjectManagementAppServiceTestBase
{
    private ProjectsAppService CreateAppService()
    {
        return CreateService(() => new ProjectsAppService(FakeRepository.For<Project>(Context)));
    }

    private int CreateProjectType()
    {
        var entity = new ProjectType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "PT1",
            NameEn = "Construction",
            NameAr = "بناء"
        };
        Context.ProjectTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Project()
    {
        var projectTypeId = CreateProjectType();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditProjectDto
        {
            NameEn = "Tower A",
            NameAr = "برج أ",
            ProjectTypeId = projectTypeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            Comments = "First project"
        });

        var result = await service.GetAll(new GetAllProjectsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Tower A");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var projectTypeId = CreateProjectType();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditProjectDto
        {
            NameEn = "Tower A",
            NameAr = "A",
            ProjectTypeId = projectTypeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditProjectDto
            {
                NameEn = "Tower A",
                NameAr = "B",
                ProjectTypeId = projectTypeId,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31)
            }));
    }
}
