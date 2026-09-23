using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class RequestDetailsAppService_Tests : WorkflowAppServiceTestBase
{
    private readonly ISysSettingsAccessor _sysSettingsAccessor = Substitute.For<ISysSettingsAccessor>();

    private RequestDetailsAppService CreateAppService(bool dynamicWorkflowSteps = false)
    {
        _sysSettingsAccessor.GetWorkflowSettingsAsync().Returns(Task.FromResult(new ManageWorkflowSettings
        {
            TenantId = AbpSession.TenantId ?? 1,
            DynamicWorkflowSteps = dynamicWorkflowSteps
        }));

        return CreateService(() => new RequestDetailsAppService(
            FakeRepository.For<RequestDetail>(Context),
            FakeRepository.For<RequestMaster>(Context),
            _sysSettingsAccessor));
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

    private static CreateOrEditRequestDetailDto NewStep(int requestMasterId, int step) => new()
    {
        RequestMasterId = requestMasterId,
        Step = step,
        ApprovalRequire = ApprovalRequire.Single,
        Person = WorkflowPersonScope.Themselves,
        ApproverPerson = WorkflowApproverPersonType.PersonManager
    };

    private int[] StepsOf(int requestMasterId) =>
        Context.RequestDetails.Where(d => d.RequestMasterId == requestMasterId)
            .OrderBy(d => d.Id)
            .Select(d => d.Step)
            .ToArray();

    [Fact]
    public async Task Should_Create_RequestDetail_And_Recalculate_WorkflowSteps()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();

        await service.CreateOrEdit(NewStep(requestMasterId, 1));

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

    /// <summary>Legacy <c>AddStep(count)</c>: a new step is appended whatever number the screen sent.</summary>
    [Fact]
    public async Task Should_Append_A_New_Step_At_The_End_Whatever_Number_Was_Sent()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();

        await service.CreateOrEdit(NewStep(requestMasterId, 7));
        await service.CreateOrEdit(NewStep(requestMasterId, 1));
        await service.CreateOrEdit(NewStep(requestMasterId, 1));

        StepsOf(requestMasterId).ShouldBe(new[] { 1, 2, 3 });
        (await Context.RequestMasters.FindAsync(requestMasterId)).WorkflowSteps.ShouldBe(3);
    }

    /// <summary>Legacy <c>DynamicWorkflowSteps == false</c>: <c>this.Step = this.prevStep</c>.</summary>
    [Fact]
    public async Task With_Dynamic_Steps_Off_Should_Keep_The_Old_Step_Number_On_Edit()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService(dynamicWorkflowSteps: false);
        await service.CreateOrEdit(NewStep(requestMasterId, 1));
        await service.CreateOrEdit(NewStep(requestMasterId, 2));
        await service.CreateOrEdit(NewStep(requestMasterId, 3));
        var third = Context.RequestDetails.Single(d => d.RequestMasterId == requestMasterId && d.Step == 3);

        var edit = NewStep(requestMasterId, 1);
        edit.Id = third.Id;
        await service.CreateOrEdit(edit);

        StepsOf(requestMasterId).ShouldBe(new[] { 1, 2, 3 });
    }

    /// <summary>
    /// Legacy <c>DynamicWorkflowSteps == true</c>: moving step 3 to position 1 pushes the old steps 1 and 2 down
    /// to 2 and 3.
    /// </summary>
    [Fact]
    public async Task With_Dynamic_Steps_On_Should_Move_The_Step_And_Shift_The_Others()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService(dynamicWorkflowSteps: true);
        await service.CreateOrEdit(NewStep(requestMasterId, 1));
        await service.CreateOrEdit(NewStep(requestMasterId, 2));
        await service.CreateOrEdit(NewStep(requestMasterId, 3));
        var third = Context.RequestDetails.Single(d => d.RequestMasterId == requestMasterId && d.Step == 3);

        var edit = NewStep(requestMasterId, 1);
        edit.Id = third.Id;
        await service.CreateOrEdit(edit);

        // Ordered by id: the first created row is now step 2, the second step 3, the moved one step 1.
        StepsOf(requestMasterId).ShouldBe(new[] { 2, 3, 1 });
    }

    [Fact]
    public async Task With_Dynamic_Steps_On_Should_Refuse_A_Step_Number_Outside_The_Chain()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService(dynamicWorkflowSteps: true);
        await service.CreateOrEdit(NewStep(requestMasterId, 1));
        await service.CreateOrEdit(NewStep(requestMasterId, 2));
        var second = Context.RequestDetails.Single(d => d.RequestMasterId == requestMasterId && d.Step == 2);

        var edit = NewStep(requestMasterId, 5);
        edit.Id = second.Id;

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(edit));
    }

    /// <summary>Legacy <c>OnDeleted</c>: the remaining steps close the gap.</summary>
    [Fact]
    public async Task Should_Renumber_The_Remaining_Steps_After_A_Delete()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();
        await service.CreateOrEdit(NewStep(requestMasterId, 1));
        await service.CreateOrEdit(NewStep(requestMasterId, 2));
        await service.CreateOrEdit(NewStep(requestMasterId, 3));
        var second = Context.RequestDetails.Single(d => d.RequestMasterId == requestMasterId && d.Step == 2);

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(second.Id));

        StepsOf(requestMasterId).ShouldBe(new[] { 1, 2 });
        (await Context.RequestMasters.FindAsync(requestMasterId)).WorkflowSteps.ShouldBe(2);
    }
}
