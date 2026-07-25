using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class RequestMastersAppService_Tests : WorkflowAppServiceTestBase
{
    private RequestMastersAppService CreateAppService()
    {
        return CreateService(() => new RequestMastersAppService(FakeRepository.For<RequestMaster>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_RequestMaster()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRequestMasterDto
        {
            NameEn = "Leave Request Workflow",
            NameAr = "سير عمل طلب الإجازة",
            SecurityLevel = WorkflowSecurityLevel.EmployeeManager,
            DueInDays = 5
        });

        var result = await service.GetAll(new GetAllRequestMastersInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Leave Request Workflow");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRequestMasterDto
        {
            NameEn = "Leave Request Workflow",
            NameAr = "A",
            SecurityLevel = WorkflowSecurityLevel.Everyone
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditRequestMasterDto
            {
                NameEn = "Leave Request Workflow",
                NameAr = "B",
                SecurityLevel = WorkflowSecurityLevel.Everyone
            }));
    }
}
