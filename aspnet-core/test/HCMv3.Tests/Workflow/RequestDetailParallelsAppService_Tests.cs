using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class RequestDetailParallelsAppService_Tests : WorkflowAppServiceTestBase
{
    private RequestDetailParallelsAppService CreateAppService()
    {
        return CreateService(() => new RequestDetailParallelsAppService(FakeRepository.For<RequestDetailParallel>(Context)));
    }

    private int CreateRequestDetail()
    {
        var requestMaster = new RequestMaster
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "1",
            NameEn = "Leave Request Workflow",
            NameAr = "A"
        };
        Context.RequestMasters.Add(requestMaster);
        Context.SaveChanges();

        var requestDetail = new RequestDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            RequestMasterId = requestMaster.Id,
            Step = 2
        };
        Context.RequestDetails.Add(requestDetail);
        Context.SaveChanges();

        return requestDetail.Id;
    }

    private int CreateWorkflowRole()
    {
        var entity = new WorkflowRole
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "1",
            NameEn = "HR Manager",
            NameAr = "A",
            WorkflowRoleGroup = WorkflowRoleGroup.Department
        };
        Context.WorkflowRoles.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_RequestDetailParallel()
    {
        var requestDetailId = CreateRequestDetail();
        var workflowRoleId = CreateWorkflowRole();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRequestDetailParallelDto
        {
            RequestDetailId = requestDetailId,
            WorkflowRoleId = workflowRoleId
        });

        var result = await service.GetAll(new GetAllRequestDetailParallelsInput { RequestDetailId = requestDetailId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].WorkflowRoleId.ShouldBe(workflowRoleId);
    }

    [Fact]
    public async Task Should_Throw_When_WorkflowRole_Not_Specified()
    {
        var requestDetailId = CreateRequestDetail();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditRequestDetailParallelDto
            {
                RequestDetailId = requestDetailId,
                WorkflowRoleId = null
            }));
    }
}
