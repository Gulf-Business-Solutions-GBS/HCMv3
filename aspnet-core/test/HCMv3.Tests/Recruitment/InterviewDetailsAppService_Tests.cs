using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class InterviewDetailsAppService_Tests : RecruitmentAppServiceTestBase
{
    private InterviewDetailsAppService CreateAppService()
    {
        return CreateService(() => new InterviewDetailsAppService(FakeRepository.For<HCMv3.Recruitment.InterviewDetails>(Context)));
    }

    private int CreateApplicationStatus()
    {
        var applicantCVId = CreateApplicantCV();
        var entity = new ApplicationStatus
        {
            TenantId = AbpSession.TenantId ?? 1,
            ApplicantCVId = applicantCVId,
            AppStatus = AppStatus.Interviewed
        };
        Context.ApplicationStatuses.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_InterviewDetails()
    {
        var applicationStatusId = CreateApplicationStatus();
        var interviewerId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditInterviewDetailsDto
        {
            ApplicationStatusId = applicationStatusId,
            InterviewDate = new DateTime(2026, 3, 1),
            InterviewTime = new DateTime(2026, 3, 1, 10, 0, 0),
            Interviewer1Id = interviewerId,
            Place = "HQ"
        });

        var result = await service.GetAll(new GetAllInterviewDetailsInput { ApplicationStatusId = applicationStatusId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Place.ShouldBe("HQ");
    }

    [Fact]
    public async Task Should_Throw_When_InterviewDate_Is_Default()
    {
        var applicationStatusId = CreateApplicationStatus();
        var interviewerId = CreateEmployee();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(new CreateOrEditInterviewDetailsDto
        {
            ApplicationStatusId = applicationStatusId,
            InterviewDate = default,
            InterviewTime = new DateTime(2026, 3, 1, 10, 0, 0),
            Interviewer1Id = interviewerId
        }));
    }
}
