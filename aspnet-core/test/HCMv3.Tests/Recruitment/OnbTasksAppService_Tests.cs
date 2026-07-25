using System.Threading.Tasks;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class OnbTasksAppService_Tests : RecruitmentAppServiceTestBase
{
    private OnbTasksAppService CreateAppService()
    {
        return CreateService(() => new OnbTasksAppService(FakeRepository.For<OnbTask>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_OnbTask()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditOnbTaskDto
        {
            TaskNameEn = "Submit ID",
            TaskNameAr = "تقديم الهوية",
            DueDays = 3
        });

        var result = await service.GetAll(new GetAllOnbTasksInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].TaskNameEn.ShouldBe("Submit ID");
    }
}
