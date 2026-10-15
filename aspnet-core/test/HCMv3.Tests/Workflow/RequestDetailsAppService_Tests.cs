using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.GeneralMasterInfo;
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

            // The tenant's default notification configurations. Empty here: these tests are about step
            // numbering, and a step saved with no defaults to find simply keeps its nulls.
            FakeRepository.For<NotificationConfig>(Context),
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

    // ------------------------------------------------------------------ reorder and copy

    /// <summary>Adds a step and hands back its id, which CreateOrEdit does not return.</summary>
    private async Task<int> AddStepAsync(RequestDetailsAppService service, int requestMasterId)
    {
        var before = Context.RequestDetails.Where(d => d.RequestMasterId == requestMasterId).Select(d => d.Id).ToList();

        await service.CreateOrEdit(NewStep(requestMasterId, 1));

        return Context.RequestDetails
            .Where(d => d.RequestMasterId == requestMasterId && !before.Contains(d.Id))
            .Select(d => d.Id)
            .Single();
    }

    private List<int> IdsInStepOrder(int requestMasterId) =>
        Context.RequestDetails
            .Where(d => d.RequestMasterId == requestMasterId)
            .OrderBy(d => d.Step)
            .Select(d => d.Id)
            .ToList();

    [Fact]
    public async Task Should_Reorder_Steps_And_Renumber_From_One()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();
        var first = await AddStepAsync(service, requestMasterId);
        var second = await AddStepAsync(service, requestMasterId);
        var third = await AddStepAsync(service, requestMasterId);

        await service.ReorderSteps(new ReorderRequestDetailsInput
        {
            RequestMasterId = requestMasterId,
            OrderedIds = new List<int> { third, first, second }
        });

        IdsInStepOrder(requestMasterId).ShouldBe(new[] { third, first, second });

        // StepsOf reads in id order, so this says the same thing from the other side: the step created
        // first is now second, the one created last is now first.
        StepsOf(requestMasterId).ShouldBe(new[] { 2, 3, 1 });
    }

    /// <summary>
    /// WorkflowSteps is what the engine reads as the total, so it must come out of a reorder unchanged --
    /// an undercount ends a chain early and an overcount leaves a request waiting for a step that is not
    /// there.
    /// </summary>
    [Fact]
    public async Task Reordering_Should_Leave_The_Step_Count_Alone()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();
        var first = await AddStepAsync(service, requestMasterId);
        var second = await AddStepAsync(service, requestMasterId);

        await service.ReorderSteps(new ReorderRequestDetailsInput
        {
            RequestMasterId = requestMasterId,
            OrderedIds = new List<int> { second, first }
        });

        (await Context.RequestMasters.FindAsync(requestMasterId)).WorkflowSteps.ShouldBe(2);
    }

    /// <summary>A grid working from a stale read must be refused rather than renumbering around a gap.</summary>
    [Fact]
    public async Task Should_Refuse_A_Reorder_That_Does_Not_List_Every_Step()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();
        var first = await AddStepAsync(service, requestMasterId);
        await AddStepAsync(service, requestMasterId);

        await Should.ThrowAsync<UserFriendlyException>(() => service.ReorderSteps(new ReorderRequestDetailsInput
        {
            RequestMasterId = requestMasterId,
            OrderedIds = new List<int> { first }
        }));
    }

    [Fact]
    public async Task Should_Refuse_A_Reorder_Naming_A_Step_From_Another_Chain()
    {
        var requestMasterId = CreateRequestMaster();
        var otherMasterId = CreateRequestMaster();
        var service = CreateAppService();
        var mine = await AddStepAsync(service, requestMasterId);
        var theirs = await AddStepAsync(service, otherMasterId);

        await Should.ThrowAsync<UserFriendlyException>(() => service.ReorderSteps(new ReorderRequestDetailsInput
        {
            RequestMasterId = requestMasterId,
            OrderedIds = new List<int> { mine, theirs }
        }));
    }

    /// <summary>The same id twice passes a count check, so it is rejected on its own account.</summary>
    [Fact]
    public async Task Should_Refuse_A_Reorder_Listing_The_Same_Step_Twice()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();
        var first = await AddStepAsync(service, requestMasterId);
        await AddStepAsync(service, requestMasterId);

        await Should.ThrowAsync<UserFriendlyException>(() => service.ReorderSteps(new ReorderRequestDetailsInput
        {
            RequestMasterId = requestMasterId,
            OrderedIds = new List<int> { first, first }
        }));
    }

    [Fact]
    public async Task Should_Copy_A_Step_Onto_The_End_Of_Its_Chain()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();
        var sourceId = await AddStepAsync(service, requestMasterId);

        var source = Context.RequestDetails.Single(d => d.Id == sourceId);
        source.CanReject = true;
        source.CanSendBack = true;
        source.IsCommentRequire = true;
        source.EscalateAfterDays = 7;
        source.ReminderDays = 3;
        source.ApprovalRequire = ApprovalRequire.All;
        Context.SaveChanges();

        var created = await service.CopyStep(new EntityDto(sourceId));

        var copy = Context.RequestDetails.Single(d => d.Id == created.Id);
        copy.Step.ShouldBe(2);
        copy.RequestMasterId.ShouldBe(requestMasterId);
        copy.CanReject.ShouldBeTrue();
        copy.CanSendBack.ShouldBeTrue();
        copy.IsCommentRequire.ShouldBeTrue();
        copy.EscalateAfterDays.ShouldBe(7);
        copy.ReminderDays.ShouldBe(3);
        copy.ApprovalRequire.ShouldBe(ApprovalRequire.All);
    }

    /// <summary>A copy is a new step, so the chain's total has to grow with it.</summary>
    [Fact]
    public async Task Copying_A_Step_Should_Raise_The_Step_Count()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();
        var sourceId = await AddStepAsync(service, requestMasterId);

        await service.CopyStep(new EntityDto(sourceId));

        (await Context.RequestMasters.FindAsync(requestMasterId)).WorkflowSteps.ShouldBe(2);
    }

    // ------------------------------------------------------------------ one creator step per chain

    /// <summary>
    /// A step with no approver is the requester's own, and approvals begin at the first step that names
    /// somebody. A second one is a step nobody is ever asked to act on, sitting mid-chain and stalling
    /// every request that reaches it.
    /// </summary>
    [Fact]
    public async Task Should_Refuse_A_Second_Creator_Step()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();

        var first = NewStep(requestMasterId, 1);
        first.ApproverPerson = WorkflowApproverPersonType.None;
        await service.CreateOrEdit(first);

        var second = NewStep(requestMasterId, 2);
        second.ApproverPerson = WorkflowApproverPersonType.None;

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(second));
    }

    [Fact]
    public async Task Should_Allow_One_Creator_Step_Alongside_Real_Approvers()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();

        var creator = NewStep(requestMasterId, 1);
        creator.ApproverPerson = WorkflowApproverPersonType.None;
        await service.CreateOrEdit(creator);

        var approver = NewStep(requestMasterId, 2);
        approver.ApproverPerson = WorkflowApproverPersonType.PersonManager;
        await service.CreateOrEdit(approver);

        StepsOf(requestMasterId).Length.ShouldBe(2);
    }

    /// <summary>Editing the only creator step must not trip over itself.</summary>
    [Fact]
    public async Task Should_Allow_Editing_The_Existing_Creator_Step()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();

        var creator = NewStep(requestMasterId, 1);
        creator.ApproverPerson = WorkflowApproverPersonType.None;
        await service.CreateOrEdit(creator);

        var saved = Context.RequestDetails.Single(d => d.RequestMasterId == requestMasterId);
        var edit = NewStep(requestMasterId, 1);
        edit.Id = saved.Id;
        edit.ApproverPerson = WorkflowApproverPersonType.None;
        edit.CanReject = true;

        await service.CreateOrEdit(edit);

        Context.RequestDetails.Single(d => d.Id == saved.Id).CanReject.ShouldBeTrue();
    }

    /// <summary>Turning an approver step into a second creator step is the same mistake by another route.</summary>
    [Fact]
    public async Task Should_Refuse_Turning_An_Approver_Step_Into_A_Second_Creator_Step()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();

        var creator = NewStep(requestMasterId, 1);
        creator.ApproverPerson = WorkflowApproverPersonType.None;
        await service.CreateOrEdit(creator);

        var approver = NewStep(requestMasterId, 2);
        approver.ApproverPerson = WorkflowApproverPersonType.PersonManager;
        await service.CreateOrEdit(approver);

        var saved = Context.RequestDetails.Single(d => d.RequestMasterId == requestMasterId && d.Step == 2);
        var edit = NewStep(requestMasterId, 2);
        edit.Id = saved.Id;
        edit.ApproverPerson = WorkflowApproverPersonType.None;

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(edit));
    }

    /// <summary>Copy carries the approver setting across, so it is the likeliest way to get a second one.</summary>
    [Fact]
    public async Task Should_Refuse_Copying_The_Creator_Step()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();

        var creator = NewStep(requestMasterId, 1);
        creator.ApproverPerson = WorkflowApproverPersonType.None;
        await service.CreateOrEdit(creator);

        var saved = Context.RequestDetails.Single(d => d.RequestMasterId == requestMasterId);

        await Should.ThrowAsync<UserFriendlyException>(() => service.CopyStep(new EntityDto(saved.Id)));
    }

    // ------------------------------------------------------------------ a workflow role is enough

    private int CreateWorkflowRole()
    {
        var role = new WorkflowRole { TenantId = AbpSession.TenantId ?? 1, Code = "HR", NameEn = "HR", NameAr = "HR" };
        Context.WorkflowRoles.Add(role);
        Context.SaveChanges();
        return role.Id;
    }

    /// <summary>
    /// Legacy's own rule: an approval step with only a workflow role goes to the role's holders. It must not
    /// be mistaken for a second creator step just because no approver type was picked.
    /// </summary>
    [Fact]
    public async Task Should_Save_A_Role_Only_Step_As_A_Role_Holder_Step()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();

        var creator = NewStep(requestMasterId, 1);
        creator.ApproverPerson = WorkflowApproverPersonType.None;
        await service.CreateOrEdit(creator);

        var roleOnly = NewStep(requestMasterId, 2);
        roleOnly.ApproverPerson = WorkflowApproverPersonType.None;
        roleOnly.WorkflowRoleId = CreateWorkflowRole();
        await service.CreateOrEdit(roleOnly);

        Context.RequestDetails.Single(d => d.RequestMasterId == requestMasterId && d.Step == 2)
            .ApproverPerson.ShouldBe(WorkflowApproverPersonType.Person);
    }

    [Fact]
    public async Task Should_Keep_The_Manager_Level_When_A_Step_Names_Both_A_Manager_And_A_Role()
    {
        var requestMasterId = CreateRequestMaster();
        var service = CreateAppService();

        var both = NewStep(requestMasterId, 1);
        both.ApproverPerson = WorkflowApproverPersonType.PersonManager;
        both.WorkflowRoleId = CreateWorkflowRole();
        await service.CreateOrEdit(both);

        var saved = Context.RequestDetails.Single(d => d.RequestMasterId == requestMasterId);
        saved.ApproverPerson.ShouldBe(WorkflowApproverPersonType.PersonManager);
        saved.WorkflowRoleId.ShouldNotBeNull();
    }
}
