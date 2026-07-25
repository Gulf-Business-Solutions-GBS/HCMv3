using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.JobProfileManagement;
using HCMv3.JobProfileManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.JobProfileManagement;

// ReSharper disable once InconsistentNaming
public class JobDescriptionsAppService_Tests : JobProfileManagementAppServiceTestBase
{
    private JobDescriptionsAppService CreateAppService()
    {
        return CreateService(() => new JobDescriptionsAppService(FakeRepository.For<JobDescription>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_JobDescription()
    {
        var service = CreateAppService();
        var jobTitleId = CreateJobTitle();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionDto
        {
            JobTitleId = jobTitleId,
            ShortDescriptionEn = "Short EN",
            ShortDescriptionAr = "قصير",
            LongDescriptionEn = "Long EN",
            LongDescriptionAr = "طويل",
            Version = "1.0",
            VersionChangedOn = DateTime.Now
        });

        var result = await service.GetAll(new GetAllJobDescriptionsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].ShortDescriptionEn.ShouldBe("Short EN");
        result.Items[0].JobTitleNameEn.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_ShortDescriptionEn_Duplicate()
    {
        var service = CreateAppService();
        var jobTitleId = CreateJobTitle();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionDto
        {
            JobTitleId = jobTitleId,
            ShortDescriptionEn = "Dup",
            ShortDescriptionAr = "A1",
            LongDescriptionEn = "L1",
            LongDescriptionAr = "LA1"
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditJobDescriptionDto
            {
                JobTitleId = jobTitleId,
                ShortDescriptionEn = "Dup",
                ShortDescriptionAr = "A2",
                LongDescriptionEn = "L2",
                LongDescriptionAr = "LA2"
            }));
    }

    [Fact]
    public async Task Should_Update_JobDescription()
    {
        var service = CreateAppService();
        var jobDescriptionId = CreateJobDescription();

        var forEdit = await service.GetJobDescriptionForEdit(new EntityDto(jobDescriptionId));
        forEdit.JobDescription.ShortDescriptionEn = "Updated";

        await service.CreateOrEdit(forEdit.JobDescription);

        var reloaded = await service.GetJobDescriptionForEdit(new EntityDto(jobDescriptionId));
        reloaded.JobDescription.ShortDescriptionEn.ShouldBe("Updated");
    }

    [Fact]
    public async Task Should_Delete_JobDescription()
    {
        var service = CreateAppService();
        var jobDescriptionId = CreateJobDescription();

        await service.Delete(new EntityDto(jobDescriptionId));

        var result = await service.GetAll(new GetAllJobDescriptionsInput());
        result.TotalCount.ShouldBe(0);
    }
}
