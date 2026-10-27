using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class ApplicantCVsAppService_Tests : RecruitmentAppServiceTestBase
{
    private ApplicantCVsAppService CreateAppService()
    {
        return CreateService(() => new ApplicantCVsAppService(FakeRepository.For<ApplicantCV>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ApplicantCV()
    {
        var applicantId = CreateApplicant(email: "cv-owner@example.com");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditApplicantCVDto
        {
            ApplicantId = applicantId,
            CVName = "MyCV",
            UploadDate = new DateTime(2026, 1, 1)
        });

        var result = await service.GetAll(new GetAllApplicantCVsInput { ApplicantId = applicantId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].CVName.ShouldBe("MyCV");
        result.Items[0].EmailCVName.ShouldBe("cv-owner@example.com-MyCV");
    }

    [Fact]
    public async Task Should_Throw_When_CVName_Duplicate()
    {
        var applicantId = CreateApplicant();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditApplicantCVDto
        {
            ApplicantId = applicantId,
            CVName = "SameName",
            UploadDate = new DateTime(2026, 1, 1)
        });

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(new CreateOrEditApplicantCVDto
        {
            ApplicantId = applicantId,
            CVName = "SameName",
            UploadDate = new DateTime(2026, 1, 1)
        }));
    }
}
