using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class WorkflowGroupFieldAccessesAppService_Tests : WorkflowAppServiceTestBase
{
    private WorkflowGroupFieldAccessesAppService CreateAppService()
    {
        return CreateService(() => new WorkflowGroupFieldAccessesAppService(
            FakeRepository.For<WorkflowGroupFieldAccess>(Context),
            FakeRepository.For<WorkflowGroupDetail>(Context),
            FakeRepository.For<RequestDetail>(Context)));
    }

    private int CreateWorkflowGroupDetailWithStep(out int requestMasterId)
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
        requestMasterId = requestMaster.Id;

        Context.RequestDetails.Add(new RequestDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            RequestMasterId = requestMaster.Id,
            Step = 1
        });
        Context.SaveChanges();

        var workflowGroup = new WorkflowGroup
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "1",
            NameEn = "Default",
            NameAr = "A"
        };
        Context.WorkflowGroups.Add(workflowGroup);
        Context.SaveChanges();

        var groupDetail = new WorkflowGroupDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            WorkflowGroupId = workflowGroup.Id,
            RequestMasterId = requestMaster.Id,
            RequestTypeName = "LeaveRequest"
        };
        Context.WorkflowGroupDetails.Add(groupDetail);
        Context.SaveChanges();

        return groupDetail.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_WorkflowGroupFieldAccess()
    {
        var groupDetailId = CreateWorkflowGroupDetailWithStep(out _);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkflowGroupFieldAccessDto
        {
            WorkflowGroupDetailId = groupDetailId,
            Step = 1,
            Field = "StartDate",
            IsVisible = true,
            IsEditable = true
        });

        var result = await service.GetAll(new GetAllWorkflowGroupFieldAccessesInput { WorkflowGroupDetailId = groupDetailId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Field.ShouldBe("StartDate");
    }

    [Fact]
    public async Task Should_Throw_When_Step_Does_Not_Exist_On_RequestMaster()
    {
        var groupDetailId = CreateWorkflowGroupDetailWithStep(out _);
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditWorkflowGroupFieldAccessDto
            {
                WorkflowGroupDetailId = groupDetailId,
                Step = 99,
                Field = "StartDate"
            }));
    }
}
