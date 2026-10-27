using System.Threading.Tasks;
using Abp.UI;
using HCMv3.JobProfileManagement;
using HCMv3.JobProfileManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.JobProfileManagement;

// ReSharper disable once InconsistentNaming
public class JobDescriptionInterviewQuestionsAppService_Tests : JobProfileManagementAppServiceTestBase
{
    private JobDescriptionInterviewQuestionsAppService CreateAppService()
    {
        return CreateService(() => new JobDescriptionInterviewQuestionsAppService(FakeRepository.For<JobDescriptionInterviewQuestion>(Context)));
    }

    private int CreateInterviewQuestion(string nameEn = "Why us?")
    {
        var entity = new InterviewQuestion
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = nameEn,
            NameAr = nameEn
        };
        Context.InterviewQuestions.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_JobDescriptionInterviewQuestion()
    {
        var service = CreateAppService();
        var jobDescriptionId = CreateJobDescription();
        var questionId = CreateInterviewQuestion();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionInterviewQuestionDto
        {
            JobDescriptionId = jobDescriptionId,
            InterviewQuestionId = questionId
        });

        var result = await service.GetAll(new GetAllJobDescriptionInterviewQuestionsInput { JobDescriptionId = jobDescriptionId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].InterviewQuestionNameEn.ShouldBe("Why us?");
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_InterviewQuestion_For_Same_JobDescription()
    {
        var service = CreateAppService();
        var jobDescriptionId = CreateJobDescription();
        var questionId = CreateInterviewQuestion();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionInterviewQuestionDto
        {
            JobDescriptionId = jobDescriptionId,
            InterviewQuestionId = questionId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditJobDescriptionInterviewQuestionDto
            {
                JobDescriptionId = jobDescriptionId,
                InterviewQuestionId = questionId
            }));
    }
}
