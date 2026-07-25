using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class WorkflowGroupDetailsAppService_Tests : WorkflowAppServiceTestBase
{
    private WorkflowGroupDetailsAppService CreateAppService()
    {
        return CreateService(() => new WorkflowGroupDetailsAppService(
            FakeRepository.For<WorkflowGroupDetail>(Context),
            FakeRepository.For<RequestMaster>(Context)));
    }

    private int CreateWorkflowGroup()
    {
        var entity = new WorkflowGroup
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "1",
            NameEn = "Default",
            NameAr = "A"
        };
        Context.WorkflowGroups.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateRequestMaster()
    {
        var entity = new RequestMaster
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "1",
            NameEn = "Leave Request Workflow",
            NameAr = "A"
        };
        Context.RequestMasters.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_WorkflowGroupDetail()
    {
        var workflowGroupId = CreateWorkflowGroup();
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkflowGroupDetailDto
        {
            WorkflowGroupId = workflowGroupId,
            RequestMasterId = requestMasterId,
            RequestTypeName = "LeaveRequest"
        });

        var result = await service.GetAll(new GetAllWorkflowGroupDetailsInput { WorkflowGroupId = workflowGroupId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].RequestTypeName.ShouldBe("LeaveRequest");
    }

    [Fact]
    public async Task Should_Throw_When_RequestType_Already_Assigned()
    {
        var workflowGroupId = CreateWorkflowGroup();
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkflowGroupDetailDto
        {
            WorkflowGroupId = workflowGroupId,
            RequestMasterId = requestMasterId,
            RequestTypeName = "LeaveRequest"
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditWorkflowGroupDetailDto
            {
                WorkflowGroupId = workflowGroupId,
                RequestMasterId = requestMasterId,
                RequestTypeName = "LeaveRequest"
            }));
    }
}
