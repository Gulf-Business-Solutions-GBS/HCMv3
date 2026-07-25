using System.Threading.Tasks;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class WFReadLogsAppService_Tests : WorkflowAppServiceTestBase
{
    private WFReadLogsAppService CreateAppService()
    {
        return CreateService(() => new WFReadLogsAppService(FakeRepository.For<WFReadLog>(Context)));
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
    public async Task Should_Create_And_Retrieve_WFReadLog()
    {
        var workflowRequestId = CreateWorkflowRequest();
        var service = CreateAppService();

        await service.Create(new CreateWFReadLogDto
        {
            WorkflowRequestId = workflowRequestId,
            User = "jdoe"
        });

        var result = await service.GetAll(new GetAllWFReadLogsInput { WorkflowRequestId = workflowRequestId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].User.ShouldBe("jdoe");
    }

    [Fact]
    public async Task Should_Delete_WFReadLog()
    {
        var workflowRequestId = CreateWorkflowRequest();
        var service = CreateAppService();

        await service.Create(new CreateWFReadLogDto { WorkflowRequestId = workflowRequestId, User = "jdoe" });

        var created = (await service.GetAll(new GetAllWFReadLogsInput())).Items[0];

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(created.Id));

        var result = await service.GetAll(new GetAllWFReadLogsInput());
        result.TotalCount.ShouldBe(0);
    }
}
