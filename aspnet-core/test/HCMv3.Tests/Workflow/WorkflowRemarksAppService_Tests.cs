using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class WorkflowRemarksAppService_Tests : WorkflowAppServiceTestBase
{
    private WorkflowRemarksAppService CreateAppService()
    {
        return CreateService(() => new WorkflowRemarksAppService(FakeRepository.For<WorkflowRemark>(Context)));
    }

    private int CreateWorkflowRequest()
    {
        var entity = new HCMv3.Workflow.WorkflowRequest
        {
            TenantId = AbpSession.TenantId ?? 1,
            RequestType = "LeaveRequest",
            RequestNumber = "REQ-0001"
        };
        Context.WorkflowRequests.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_WorkflowRemark()
    {
        var workflowRequestId = CreateWorkflowRequest();
        var ccReceiverId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkflowRemarkDto
        {
            WorkflowRequestId = workflowRequestId,
            CCReceiverId = ccReceiverId
        });

        var result = await service.GetAll(new GetAllWorkflowRemarksInput { WorkflowRequestId = workflowRequestId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].CCReceiverId.ShouldBe(ccReceiverId);
    }

    [Fact]
    public async Task Should_Throw_When_CCReceiver_Duplicated_For_Same_Request()
    {
        var workflowRequestId = CreateWorkflowRequest();
        var ccReceiverId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkflowRemarkDto
        {
            WorkflowRequestId = workflowRequestId,
            CCReceiverId = ccReceiverId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditWorkflowRemarkDto
            {
                WorkflowRequestId = workflowRequestId,
                CCReceiverId = ccReceiverId
            }));
    }
}
