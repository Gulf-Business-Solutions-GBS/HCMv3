using System.Threading.Tasks;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class OnbApplicantTasksAppService_Tests : RecruitmentAppServiceTestBase
{
    private OnbApplicantTasksAppService CreateAppService()
    {
        return CreateService(() => new OnbApplicantTasksAppService(FakeRepository.For<OnbApplicantTasks>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_OnbApplicantTasks()
    {
        var applicantId = CreateApplicant();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditOnbApplicantTasksDto
        {
            ApplicantId = applicantId,
            DueDays = 5
        });

        var result = await service.GetAll(new GetAllOnbApplicantTasksInput { ApplicantId = applicantId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].DueDays.ShouldBe(5);
    }
}
