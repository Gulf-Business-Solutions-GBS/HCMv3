using System.Threading.Tasks;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class WorkFlowApproversAppService_Tests : WorkflowAppServiceTestBase
{
    private WorkFlowApproversAppService CreateAppService()
    {
        return CreateService(() => new WorkFlowApproversAppService(FakeRepository.For<WorkFlowApprover>(Context)));
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
    public async Task Should_Create_And_Retrieve_WorkFlowApprover()
    {
        var workflowRequestId = CreateWorkflowRequest();
        var approverId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkFlowApproverDto
        {
            WorkflowRequestId = workflowRequestId,
            ApproverId = approverId,
            IsApproved = false
        });

        var result = await service.GetAll(new GetAllWorkFlowApproversInput { WorkflowRequestId = workflowRequestId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].ApproverId.ShouldBe(approverId);
    }

    [Fact]
    public async Task Should_Update_IsApproved()
    {
        var workflowRequestId = CreateWorkflowRequest();
        var approverId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkFlowApproverDto
        {
            WorkflowRequestId = workflowRequestId,
            ApproverId = approverId,
            IsApproved = false
        });

        var created = (await service.GetAll(new GetAllWorkFlowApproversInput())).Items[0];

        await service.CreateOrEdit(new CreateOrEditWorkFlowApproverDto
        {
            Id = created.Id,
            WorkflowRequestId = workflowRequestId,
            ApproverId = approverId,
            IsApproved = true
        });

        var updated = (await service.GetAll(new GetAllWorkFlowApproversInput())).Items[0];
        updated.IsApproved.ShouldBeTrue();
    }
}
