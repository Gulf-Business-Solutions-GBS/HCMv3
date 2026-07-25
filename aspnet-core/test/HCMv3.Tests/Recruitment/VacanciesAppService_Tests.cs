using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.OrganizationManagement;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class VacanciesAppService_Tests : RecruitmentAppServiceTestBase
{
    private VacanciesAppService CreateAppService()
    {
        return CreateService(() => new VacanciesAppService(FakeRepository.For<Vacancy>(Context)));
    }

    private int CreateJobTitle()
    {
        var entity = new JobTitle
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Engineer",
            NameAr = "مهندس"
        };
        Context.JobTitles.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private CreateOrEditVacancyDto ValidDto(int jobTitleId)
    {
        return new CreateOrEditVacancyDto
        {
            JobTitleId = jobTitleId,
            PublishOn = new DateTime(2026, 1, 1),
            ClosingDate = new DateTime(2026, 2, 1),
            VacancyTitle = "Senior Engineer",
            OrganizationUnitId = 1
        };
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Vacancy()
    {
        var jobTitleId = CreateJobTitle();
        var service = CreateAppService();

        await service.CreateOrEdit(ValidDto(jobTitleId));

        var result = await service.GetAll(new GetAllVacanciesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].VacancyTitle.ShouldBe("Senior Engineer");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
        result.Items[0].CodeWithTitle.ShouldContain("Senior Engineer");
    }

    [Fact]
    public async Task Should_Throw_When_Code_Duplicate()
    {
        var jobTitleId = CreateJobTitle();
        var service = CreateAppService();

        var dto1 = ValidDto(jobTitleId);
        dto1.Code = "V-001";
        await service.CreateOrEdit(dto1);

        var dto2 = ValidDto(jobTitleId);
        dto2.Code = "V-001";
        dto2.VacancyTitle = "Another Title";

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(dto2));
    }
}
