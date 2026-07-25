using System.Threading.Tasks;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class RequestLogsAppService_Tests : WorkflowAppServiceTestBase
{
    private RequestLogsAppService CreateAppService()
    {
        return CreateService(() => new RequestLogsAppService(FakeRepository.For<RequestLog>(Context)));
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
    public async Task Should_Create_And_Retrieve_RequestLog()
    {
        var workflowRequestId = CreateWorkflowRequest();
        var service = CreateAppService();

        await service.Create(new CreateRequestLogDto
        {
            WorkflowRequestId = workflowRequestId,
            ActionType = RequestActionType.Created,
            Platform = WorkflowPlatform.Core
        });

        var result = await service.GetAll(new GetAllRequestLogsInput { WorkflowRequestId = workflowRequestId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].ActionType.ShouldBe(RequestActionType.Created);
    }

    [Fact]
    public async Task Should_Delete_RequestLog()
    {
        var workflowRequestId = CreateWorkflowRequest();
        var service = CreateAppService();

        await service.Create(new CreateRequestLogDto
        {
            WorkflowRequestId = workflowRequestId,
            ActionType = RequestActionType.Approved
        });

        var created = (await service.GetAll(new GetAllRequestLogsInput())).Items[0];

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(created.Id));

        var result = await service.GetAll(new GetAllRequestLogsInput());
        result.TotalCount.ShouldBe(0);
    }
}
