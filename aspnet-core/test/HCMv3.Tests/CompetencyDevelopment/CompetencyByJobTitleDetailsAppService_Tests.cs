using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.CompetencyDevelopment;
using HCMv3.CompetencyDevelopment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.CompetencyDevelopment;

// ReSharper disable once InconsistentNaming
public class CompetencyByJobTitleDetailsAppService_Tests : CompetencyDevelopmentAppServiceTestBase
{
    private CompetencyByJobTitleDetailsAppService CreateAppService()
    {
        return CreateService(() => new CompetencyByJobTitleDetailsAppService(FakeRepository.For<CompetencyByJobTitleDetail>(Context)));
    }

    private int CreateParent()
    {
        var jobTitleId = CreateJobTitle("Engineer");
        var entity = new CompetencyByJobTitle
        {
            TenantId = AbpSession.TenantId ?? 1,
            JobTitleId = jobTitleId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };
        Context.CompetencyByJobTitles.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateCompetency(string nameEn)
    {
        var type = new CompetencyType { TenantId = AbpSession.TenantId ?? 1, NameEn = "T", NameAr = "T" };
        Context.CompetencyTypes.Add(type);
        Context.SaveChanges();

        var entity = new Competency { TenantId = AbpSession.TenantId ?? 1, NameEn = nameEn, NameAr = nameEn, CompetencyTypeId = type.Id };
        Context.Competencies.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Detail()
    {
        var parentId = CreateParent();
        var competencyId = CreateCompetency("Teamwork");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCompetencyByJobTitleDetailDto
        {
            CompetencyByJobTitleId = parentId,
            CompetencyId = competencyId,
            Weight = 50,
            Target = "Deliver on time"
        });

        var result = await service.GetAll(new GetAllCompetencyByJobTitleDetailsInput { CompetencyByJobTitleId = parentId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].CompetencyName.ShouldBe("Teamwork");
    }

    [Fact]
    public async Task Should_Throw_When_Total_Weight_Exceeds_100()
    {
        var parentId = CreateParent();
        var competency1 = CreateCompetency("Teamwork");
        var competency2 = CreateCompetency("Communication");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCompetencyByJobTitleDetailDto
        {
            CompetencyByJobTitleId = parentId,
            CompetencyId = competency1,
            Weight = 70,
            Target = "T1"
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditCompetencyByJobTitleDetailDto
            {
                CompetencyByJobTitleId = parentId,
                CompetencyId = competency2,
                Weight = 40,
                Target = "T2"
            }));
    }
}
