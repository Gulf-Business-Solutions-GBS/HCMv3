using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Authorization.Users;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers assigned tasks (<see cref="EmployeeAssignedTask"/>, legacy <c>TaskRequest</c>) end to end.
///
/// <para>
/// Two things here are worth more than the usual routing assertions. First, the type carries NO approval side
/// effect -- legacy had none -- so the only thing approval does is make the row visible, which means the
/// filter behaviour IS the feature and is asserted in both directions (approved shows, rejected does not).
/// Second, the creator/assignee rule: legacy expressed it as an XAF appearance rule that greyed four boxes
/// out on one screen and stopped nothing, so the tests below pin it down as a real server-side refusal.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class AssignedTaskWorkflow_Tests : AppTestBase
{
    private readonly IEmployeeAssignedTasksAppService _tasks;

    public AssignedTaskWorkflow_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _tasks = Resolve<IEmployeeAssignedTasksAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = code,
                LastNameEn = "T",
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code + " T",
                FullNameAr = "ت ت",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    /// <summary>Turns assigned tasks on for approval with a real approver holding the step's role.</summary>
    private int EnableWorkflow(string approverCode)
    {
        var approverId = CreateEmployee(approverCode);

        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Approver",
                NameAr = "Approver"
            }).Entity;

            var master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = "TSK-STD",
                NameEn = "Standard",
                NameAr = "Standard",
                WorkflowSteps = 1
            }).Entity;

            context.SaveChanges();

            context.Set<RequestDetail>().Add(new RequestDetail
            {
                TenantId = TenantId,
                RequestMasterId = master.Id,
                Step = 1,
                WorkflowRoleId = role.Id,
                ApproverPerson = WorkflowApproverPersonType.Person,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = true,
                CanSendBack = true
            });

            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = approverId,
                WorkflowRoleId = role.Id,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2)
            });

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeAssignedTask");
            type.IsWorkflowEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    /// <summary>
    /// A second real user, granted the assigned-task permissions directly. Needed because the whole point of
    /// the creator rule is that it distinguishes two people, and an unknown user id would simply be refused
    /// by authorization long before the rule was reached.
    /// </summary>
    private long CreateUserFor(int employeeId, string userName)
    {
        User user = null;

        UsingDbContext(context =>
        {
            user = context.Users.Add(new User
            {
                TenantId = TenantId,
                UserName = userName,
                Name = userName,
                Surname = "T",
                EmailAddress = userName + "@test.local",
                IsEmailConfirmed = true,
                Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw==",
                Permissions = new List<UserPermissionSetting>
                {
                    new UserPermissionSetting
                    {
                        TenantId = TenantId,
                        Name = AppPermissions.Pages_PersonnelAdministration_EmployeeAssignedTasks,
                        IsGranted = true
                    },
                    new UserPermissionSetting
                    {
                        TenantId = TenantId,
                        Name = AppPermissions.Pages_PersonnelAdministration_EmployeeAssignedTasks_Edit,
                        IsGranted = true
                    }
                }
            }).Entity;

            user.SetNormalizedNames();
        });

        // Linked to the employee so the row reads as the assignee's own account, which is what it would be
        // in practice.
        UsingDbContext(context =>
        {
            var employee = context.Set<Employee>().First(e => e.Id == employeeId);
            employee.UserId = user.Id;
        });

        return user.Id;
    }

    private static CreateOrEditEmployeeAssignedTaskDto TaskFor(int employeeId) =>
        new CreateOrEditEmployeeAssignedTaskDto
        {
            EmployeeId = employeeId,
            Subject = "Prepare Q3 headcount pack",
            Description = "Consolidate the department returns and reconcile against budget.",
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(10),
            TaskStatus = AssignedTaskStatus.Opened
        };

    private EmployeeAssignedTask TaskRow(int employeeId) =>
        UsingDbContext(c => c.Set<EmployeeAssignedTask>().IgnoreQueryFilters().First(t => t.EmployeeId == employeeId));

    private int RequestId() =>
        UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.RequestType == "EmployeeAssignedTask").Id);

    private bool IsVisibleToTheRestOfTheSystem(int employeeId)
    {
        var repository = Resolve<IRepository<EmployeeAssignedTask>>();
        var unitOfWorkManager = Resolve<IUnitOfWorkManager>();

        using (unitOfWorkManager.Begin())
        {
            // ApprovedOnly is ON here, exactly as any other screen or report would see it.
            return repository.GetAll().Any(t => t.EmployeeId == employeeId);
        }
    }

    [Fact]
    public async Task Should_Record_Immediately_While_The_Type_Is_Not_Routed()
    {
        // The catalog seeds every type dormant, so this is the behaviour on a fresh install.
        var employeeId = CreateEmployee("TSK100");

        await _tasks.CreateOrEdit(TaskFor(employeeId));

        TaskRow(employeeId).WorkflowStatus.ShouldBeNull();

        UsingDbContext(c => c.Set<WorkflowRequest>().Count(r => r.RequestType == "EmployeeAssignedTask")).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Route_For_Approval_Once_The_Type_Is_Enabled()
    {
        var employeeId = CreateEmployee("TSK110");
        EnableWorkflow("TSK110APP");

        await _tasks.CreateOrEdit(TaskFor(employeeId));

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.RequestType == "EmployeeAssignedTask"));

        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.RequestNumber.ShouldStartWith("TSK-" + DateTime.Today.Year);

        // The request is about the ASSIGNEE, not whoever handed the work out.
        request.EmployeeId.ShouldBe(employeeId);

        // The subject leads the label: with no type or amount to summarise it is the only thing telling one
        // task from another in an inbox.
        request.SummaryEn.ShouldStartWith("Prepare Q3 headcount pack");
    }

    [Fact]
    public async Task A_Submitted_Task_Should_Be_Hidden_Until_It_Is_Approved()
    {
        var employeeId = CreateEmployee("TSK120");
        var approverId = EnableWorkflow("TSK120APP");

        await _tasks.CreateOrEdit(TaskFor(employeeId));

        IsVisibleToTheRestOfTheSystem(employeeId)
            .ShouldBeFalse("A task nobody has approved yet was visible to the rest of the system.");

        await Resolve<IWorkflowApprovalService>().ApproveAsync(RequestId(), approverId, "Approved");

        var row = TaskRow(employeeId);
        row.WorkflowStatus.ShouldBe(RequestStatus.Approved);

        IsVisibleToTheRestOfTheSystem(employeeId)
            .ShouldBeTrue("An approved task was still hidden by the ApprovedOnly filter.");
    }

    [Fact]
    public async Task Approving_Should_Make_The_Task_Real_And_Nothing_More()
    {
        // Legacy had NO approval side effect for this type -- nothing in OnSaving, WorkFlowServices or
        // RequestProcessApproval runs when the status becomes Approved. Asserting the absence stops a future
        // handler inventing one, and stops anyone "restoring" a shadow TaskRequest row.
        var employeeId = CreateEmployee("TSK130");
        var approverId = EnableWorkflow("TSK130APP");

        await _tasks.CreateOrEdit(TaskFor(employeeId));

        var requestId = RequestId();

        await Resolve<IWorkflowApprovalService>().ApproveAsync(requestId, approverId, "Approved");

        var row = TaskRow(employeeId);
        row.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        row.WorkflowRequestId.ShouldBe(requestId);

        // Still exactly one row: approval creates nothing.
        UsingDbContext(c => c.Set<EmployeeAssignedTask>().IgnoreQueryFilters()
            .Count(t => t.EmployeeId == employeeId)).ShouldBe(1);

        // And the status the assigner set is untouched -- approval is not progress on the work.
        row.TaskStatus.ShouldBe(AssignedTaskStatus.Opened);
    }

    [Fact]
    public async Task A_Rejected_Task_Should_Stay_Invisible()
    {
        var employeeId = CreateEmployee("TSK140");
        var approverId = EnableWorkflow("TSK140APP");

        await _tasks.CreateOrEdit(TaskFor(employeeId));

        await Resolve<IWorkflowApprovalService>().RejectAsync(RequestId(), approverId, "Not this quarter");

        TaskRow(employeeId).WorkflowStatus.ShouldBe(RequestStatus.Rejected);

        IsVisibleToTheRestOfTheSystem(employeeId)
            .ShouldBeFalse("A rejected task was visible to the rest of the system.");
    }

    [Fact]
    public async Task Should_Refuse_An_End_Date_Before_The_Start_Date()
    {
        // Legacy checked this, but threw a raw Exception carrying a hardcoded English string from inside a
        // catch that rethrew with `throw ex`. The rule survives; the plumbing does not.
        var employeeId = CreateEmployee("TSK150");

        var input = TaskFor(employeeId);
        input.StartDate = DateTime.Today.AddDays(10);
        input.EndDate = DateTime.Today.AddDays(1);

        await Should.ThrowAsync<UserFriendlyException>(() => _tasks.CreateOrEdit(input));

        UsingDbContext(c => c.Set<EmployeeAssignedTask>().IgnoreQueryFilters()
            .Count(t => t.EmployeeId == employeeId)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Refuse_A_Task_With_No_Subject()
    {
        var employeeId = CreateEmployee("TSK160");

        var input = TaskFor(employeeId);
        input.Subject = "   ";

        // Whitespace-only fails the DTO's [Required] before the service's own guard is reached, so this
        // asserts the outcome rather than the exception type -- both layers refuse it, which is the point.
        await Assert.ThrowsAnyAsync<Exception>(() => _tasks.CreateOrEdit(input));

        UsingDbContext(c => c.Set<EmployeeAssignedTask>().IgnoreQueryFilters()
            .Count(t => t.EmployeeId == employeeId)).ShouldBe(0);
    }

    [Fact]
    public async Task The_Assignee_Should_Not_Be_Able_To_Rewrite_The_Job_Once_It_Is_In_The_Chain()
    {
        var employeeId = CreateEmployee("TSK170");
        EnableWorkflow("TSK170APP");

        await _tasks.CreateOrEdit(TaskFor(employeeId));

        var taskId = TaskRow(employeeId).Id;
        var assigneeUserId = CreateUserFor(employeeId, "tsk170assignee");

        AbpSession.UserId = assigneeUserId;

        var edit = TaskFor(employeeId);
        edit.Id = taskId;
        edit.Subject = "Something else entirely";

        var exception = await Should.ThrowAsync<UserFriendlyException>(() =>
            Resolve<IEmployeeAssignedTasksAppService>().CreateOrEdit(edit));

        exception.Message.ShouldContain("assigned");

        TaskRow(employeeId).Subject.ShouldBe("Prepare Q3 headcount pack");
    }

    [Fact]
    public async Task The_Assignee_Should_Still_Be_Able_To_Move_The_Status()
    {
        // The one field the assignee owns -- they are the only person who knows whether the work has started.
        var employeeId = CreateEmployee("TSK180");
        EnableWorkflow("TSK180APP");

        await _tasks.CreateOrEdit(TaskFor(employeeId));

        var taskId = TaskRow(employeeId).Id;
        var assigneeUserId = CreateUserFor(employeeId, "tsk180assignee");

        AbpSession.UserId = assigneeUserId;

        var edit = TaskFor(employeeId);
        edit.Id = taskId;
        edit.TaskStatus = AssignedTaskStatus.Working;

        await Resolve<IEmployeeAssignedTasksAppService>().CreateOrEdit(edit);

        TaskRow(employeeId).TaskStatus.ShouldBe(AssignedTaskStatus.Working);
    }

    [Fact]
    public async Task The_Creator_Should_Still_Be_Able_To_Correct_The_Job()
    {
        // The other half of the rule. Legacy also capped the CREATOR at WorkflowStep <= 1, so past the
        // second step nobody could fix a typo; that cap is deliberately not ported.
        var employeeId = CreateEmployee("TSK190");
        EnableWorkflow("TSK190APP");

        await _tasks.CreateOrEdit(TaskFor(employeeId));

        var edit = TaskFor(employeeId);
        edit.Id = TaskRow(employeeId).Id;
        edit.Subject = "Prepare Q3 headcount pack (revised)";

        await _tasks.CreateOrEdit(edit);

        TaskRow(employeeId).Subject.ShouldBe("Prepare Q3 headcount pack (revised)");
    }

    [Fact]
    public async Task Anyone_Should_Be_Able_To_Edit_A_Task_That_Is_Not_In_A_Chain()
    {
        // Workflow off for the type: there is no chain, so there is nothing for the rule to protect. Legacy
        // said the same thing with `WorkflowStep <= 0 -> IsCreator = 1`. Getting this wrong would lock a
        // workflow-free installation out of its own records.
        var employeeId = CreateEmployee("TSK200");

        await _tasks.CreateOrEdit(TaskFor(employeeId));

        var taskId = TaskRow(employeeId).Id;
        var assigneeUserId = CreateUserFor(employeeId, "tsk200assignee");

        AbpSession.UserId = assigneeUserId;

        var edit = TaskFor(employeeId);
        edit.Id = taskId;
        edit.Subject = "Renamed by the assignee";

        await Resolve<IEmployeeAssignedTasksAppService>().CreateOrEdit(edit);

        TaskRow(employeeId).Subject.ShouldBe("Renamed by the assignee");
    }

    [Fact]
    public async Task GetAll_Should_Show_Tasks_That_Are_Still_Awaiting_Approval()
    {
        // The management grid deliberately disables ApprovedOnly: knowing that an assignment has been
        // proposed is the point of the list, and hiding it invites the same work being handed out twice.
        var employeeId = CreateEmployee("TSK210");
        EnableWorkflow("TSK210APP");

        await _tasks.CreateOrEdit(TaskFor(employeeId));

        var result = await _tasks.GetAll(new GetAllEmployeeAssignedTasksInput { EmployeeId = employeeId });

        result.TotalCount.ShouldBe(1);
        result.Items.Single().WorkflowStatus.ShouldBe(RequestStatus.Open);
    }

    [Fact]
    public void Assigned_Tasks_Should_Not_Be_Offered_As_Self_Service()
    {
        // A task is HANDED to somebody: legacy keeps creator and subject as different people and locks the
        // subject/description/dates against anyone but the creator, so it was never something an employee
        // raised about themselves. A route here with no My* service behind it would be a dead tile.
        var type = UsingDbContext(c => c.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeAssignedTask"));

        type.EssRoute.ShouldBeNull();
        type.EssPermissionName.ShouldBeNull();
        type.IsEssEnabled.ShouldBeFalse();
    }

    [Fact]
    public void The_Assigned_Task_Table_Should_Be_Separate_From_The_HR_Reminder_Table()
    {
        // A guard against the one mistake the naming invites. EmployeeTask is the HR reminder record
        // (TaskTypeId/ReminderDate/MonitoringType); EmployeeAssignedTask is assigned work that routes for
        // approval. If someone ever "unifies" them this fails rather than silently merging two concepts.
        var assignedTasks = UsingDbContext(c => c.Model.FindEntityType(typeof(EmployeeAssignedTask)).GetTableName());
        var reminders = UsingDbContext(c => c.Model.FindEntityType(typeof(EmployeeTask)).GetTableName());

        assignedTasks.ShouldNotBe(reminders);
    }
}
