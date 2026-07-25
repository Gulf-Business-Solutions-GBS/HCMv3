using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class WorkflowGroupsAppService_Tests : WorkflowAppServiceTestBase
{
    private WorkflowGroupsAppService CreateAppService()
    {
        return CreateService(() => new WorkflowGroupsAppService(FakeRepository.For<WorkflowGroup>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_WorkflowGroup()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkflowGroupDto
        {
            NameEn = "Default Group",
            NameAr = "المجموعة الافتراضية",
            IsDefault = true
        });

        var result = await service.GetAll(new GetAllWorkflowGroupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Throw_When_Second_Default_Group_Created()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkflowGroupDto
        {
            NameEn = "Default Group",
            NameAr = "A",
            IsDefault = true
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditWorkflowGroupDto
            {
                NameEn = "Another Group",
                NameAr = "B",
                IsDefault = true
            }));
    }
}
