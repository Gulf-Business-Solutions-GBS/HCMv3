using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.JobProfileManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.JobProfileManagement;

// ReSharper disable once InconsistentNaming
public class InterviewQuestionsAppService_Tests : JobProfileManagementAppServiceTestBase
{
    private InterviewQuestionsAppService CreateAppService()
    {
        return CreateService(() => new InterviewQuestionsAppService(FakeRepository.For<InterviewQuestion>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_InterviewQuestion()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Tell me about yourself",
            NameAr = "حدثني عن نفسك"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Tell me about yourself");
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Why us?", NameAr = "Why us?" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Why us?", NameAr = "Other" }));
    }
}
