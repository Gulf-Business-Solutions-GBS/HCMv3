using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class RequestDetailsAppService_Tests : WorkflowAppServiceTestBase
{
    private RequestDetailsAppService CreateAppService()
    {
        return CreateService(() => new RequestDetailsAppService(
            FakeRepository.For<RequestDetail>(Context),
            FakeRepository.For<RequestMaster>(Context)));
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
    public async Task Should_Create_RequestDetail_And_Recalculate_WorkflowSteps()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRequestDetailDto
        {
            RequestMasterId = requestMasterId,
            Step = 1,
            ApprovalRequire = ApprovalRequire.Single,
            Person = WorkflowPersonScope.Themselves,
            ApproverPerson = WorkflowApproverPersonType.PersonManager
        });

        var result = await service.GetAll(new GetAllRequestDetailsInput { RequestMasterId = requestMasterId });
        result.TotalCount.ShouldBe(1);

        var requestMaster = await Context.RequestMasters.FindAsync(requestMasterId);
        requestMaster.WorkflowSteps.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_ReminderDays_Not_Less_Than_EscalateAfterDays()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditRequestDetailDto
            {
                RequestMasterId = requestMasterId,
                Step = 1,
                EscalateAfterDays = 3,
                ReminderDays = 5
            }));
    }
}
