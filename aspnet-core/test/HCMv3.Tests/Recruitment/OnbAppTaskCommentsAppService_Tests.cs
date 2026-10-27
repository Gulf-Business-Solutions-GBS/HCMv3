using System.Threading.Tasks;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class OnbAppTaskCommentsAppService_Tests : RecruitmentAppServiceTestBase
{
    private OnbAppTaskCommentsAppService CreateAppService()
    {
        return CreateService(() => new OnbAppTaskCommentsAppService(FakeRepository.For<OnbAppTaskComments>(Context)));
    }

    private int CreateOnbApplicantTask()
    {
        var applicantId = CreateApplicant();
        var entity = new OnbApplicantTasks
        {
            TenantId = AbpSession.TenantId ?? 1,
            ApplicantId = applicantId
        };
        Context.OnbApplicantTasks.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_OnbAppTaskComments()
    {
        var taskId = CreateOnbApplicantTask();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditOnbAppTaskCommentsDto
        {
            OnbApplicantTasksId = taskId,
            Comments = "Looks good",
            IsByAdmin = true
        });

        var result = await service.GetAll(new GetAllOnbAppTaskCommentsInput { OnbApplicantTasksId = taskId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Comments.ShouldBe("Looks good");
        result.Items[0].IsByAdmin.ShouldBeTrue();
    }
}
