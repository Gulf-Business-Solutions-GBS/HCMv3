using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Per-step workflow notifications: who a <see cref="NotificationConfig"/> addresses, and which configuration
/// each action reads.
///
/// <para>
/// The tests stop at the recipient list rather than at the inbox. Whether a message was delivered is the
/// notification framework's business and the SMTP server's; whether the RIGHT PEOPLE were on the list is this
/// port's, and it is the part that goes wrong — a rule that quietly addresses nobody, or the requester, looks
/// exactly like a working one until somebody complains they were not told.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class WorkflowNotifications_Tests : EssLeaveWorkflowTestBase
{
    private readonly INotificationRecipientResolver _recipients;
    private readonly IWorkflowNotificationDispatcher _dispatcher;

    public WorkflowNotifications_Tests()
    {
        _recipients = Resolve<INotificationRecipientResolver>();
        _dispatcher = Resolve<IWorkflowNotificationDispatcher>();
    }

    // ------------------------------------------------------------------------------- who gets told

    [Fact]
    public void ToEmployee_Should_Reach_Whoever_The_Request_Is_About()
    {
        var employeeId = GivenAnActiveEmployee("NTF100");
        var request = GivenARequestFor(employeeId);
        var config = GivenAConfig(c => c.ToEmployee = true);

        Resolve(config, request).ShouldBe(new[] { employeeId });
    }

    [Fact]
    public void ToManager_Should_Reach_The_Manager_In_Post_Today()
    {
        var employeeId = GivenAnActiveEmployee("NTF110");
        var oldManagerId = GivenAnActiveEmployee("NTF111");
        var managerId = GivenAnActiveEmployee("NTF112");

        GivenAManager(employeeId, oldManagerId, from: DateTime.Today.AddYears(-2), to: DateTime.Today.AddDays(-1));
        GivenAManager(employeeId, managerId, from: DateTime.Today, to: DateTime.Today.AddYears(1));

        var request = GivenARequestFor(employeeId);
        var config = GivenAConfig(c => c.ToManager = true);

        Resolve(config, request).ShouldBe(new[] { managerId });
    }

    [Fact]
    public void SendToApprover_Should_Reach_Whoever_Is_Holding_It_Right_Now()
    {
        var employeeId = GivenAnActiveEmployee("NTF120");
        var waitingId = GivenAnActiveEmployee("NTF121");
        var doneId = GivenAnActiveEmployee("NTF122");

        var request = GivenARequestFor(employeeId);
        GivenAnApprover(request.Id, waitingId, approved: false);
        GivenAnApprover(request.Id, doneId, approved: true);

        var config = GivenAConfig(c => c.SendToApprover = true);

        Resolve(config, request).ShouldBe(new[] { waitingId });
    }

    [Fact]
    public void SendToAllApproved_Should_Reach_Everyone_Who_Already_Signed()
    {
        var employeeId = GivenAnActiveEmployee("NTF130");
        var firstApproverId = GivenAnActiveEmployee("NTF131");

        var request = GivenARequestFor(employeeId);
        GivenAnApprovalLog(request.Id, firstApproverId);

        var config = GivenAConfig(c => c.SendToAllApproved = true);

        Resolve(config, request).ShouldBe(new[] { firstApproverId });
    }

    [Fact]
    public void A_Notification_Group_Should_Reach_Everyone_Holding_Its_Role()
    {
        // The fan-out the first pass of this port skipped for want of the entities it needed.
        var employeeId = GivenAnActiveEmployee("NTF140");
        var holderId = GivenAnActiveEmployee("NTF141");
        var roleId = GivenARoleHeldBy(holderId);

        var config = GivenAConfig(c => { });
        GivenAGroup(config.Id, roleId);

        Resolve(config, GivenARequestFor(employeeId)).ShouldBe(new[] { holderId });
    }

    [Fact]
    public void A_Leaver_Should_Not_Be_Told_Anything()
    {
        var employeeId = GivenAnActiveEmployee("NTF150");
        MarkInactive(employeeId);

        var config = GivenAConfig(c => c.ToEmployee = true);

        Resolve(config, GivenARequestFor(employeeId)).ShouldBeEmpty();
    }

    [Fact]
    public void Somebody_Addressed_Twice_Should_Be_Told_Once()
    {
        // Their own request, and they are also the approver sitting on it.
        var employeeId = GivenAnActiveEmployee("NTF160");
        var request = GivenARequestFor(employeeId);
        GivenAnApprover(request.Id, employeeId, approved: false);

        var config = GivenAConfig(c =>
        {
            c.ToEmployee = true;
            c.SendToApprover = true;
        });

        Resolve(config, request).ShouldBe(new[] { employeeId });
    }

    [Fact]
    public void A_Config_That_Asks_For_Nobody_Should_Reach_Nobody()
    {
        var employeeId = GivenAnActiveEmployee("NTF170");

        Resolve(GivenAConfig(c => { }), GivenARequestFor(employeeId)).ShouldBeEmpty();
    }

    // ------------------------------------------------------------------------------- which config applies

    [Fact]
    public async Task A_Step_With_No_Configuration_Should_Send_Nothing()
    {
        var employeeId = Activate(CreateCurrentUserEmployee("NTF200"));
        EnableLeaveWorkflowWithApprover("NTF200APP");

        var request = GivenARequestFor(employeeId);

        (await _dispatcher.DispatchAsync(request, step: 1, WorkflowNotificationAction.Approve))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Each_Action_Should_Read_Its_Own_Configuration()
    {
        var employeeId = Activate(CreateCurrentUserEmployee("NTF210"));
        EnableLeaveWorkflowWithApprover("NTF210APP");

        var config = GivenAConfig(c => c.ToEmployee = true);

        // Only the REJECT slot is filled in, so only a rejection has anything to say.
        SetStepConfig(step: 1, d => d.RejectNotificationConfigId = config.Id);

        var request = GivenARequestFor(employeeId);

        (await _dispatcher.DispatchAsync(request, 1, WorkflowNotificationAction.Reject)).ShouldBeTrue();
        (await _dispatcher.DispatchAsync(request, 1, WorkflowNotificationAction.Approve)).ShouldBeFalse();
        (await _dispatcher.DispatchAsync(request, 1, WorkflowNotificationAction.Submit)).ShouldBeFalse();
        (await _dispatcher.DispatchAsync(request, 1, WorkflowNotificationAction.SendBack)).ShouldBeFalse();
    }

    [Fact]
    public async Task A_Configured_Step_Should_Report_Itself_Handled_Even_With_Nobody_To_Tell()
    {
        // Otherwise the reminder worker would override a deliberate routing with its own default.
        var employeeId = Activate(CreateCurrentUserEmployee("NTF220"));
        EnableLeaveWorkflowWithApprover("NTF220APP");

        var config = GivenAConfig(c => c.SendToApprover = true);
        SetStepConfig(step: 1, d => d.ReminderNotificationConfigId = config.Id);

        var request = GivenARequestFor(employeeId);

        (await _dispatcher.DispatchAsync(request, 1, WorkflowNotificationAction.Reminder)).ShouldBeTrue();
    }

    // ------------------------------------------------------------------------------- arrange helpers

    private int[] Resolve(NotificationConfig config, WorkflowRequest request) =>
        _recipients.Resolve(config, request).Select(e => e.Id).OrderBy(id => id).ToArray();

    /// <summary>
    /// An employee who has not left. The shared helper leaves <see cref="Employee.Status"/> at its default,
    /// which is <see cref="EmployeeStatus.Inactive"/> — harmless for the leave tests, but every notification
    /// rule deliberately skips leavers, so a test built on the default would assert that nobody is ever told
    /// anything and pass.
    /// </summary>
    private int GivenAnActiveEmployee(string code) => Activate(CreateEmployee(code));

    private int Activate(int employeeId)
    {
        UsingDbContext(context =>
            context.Set<Employee>().Single(e => e.Id == employeeId).Status = EmployeeStatus.Active);

        return employeeId;
    }

    private WorkflowRequest GivenARequestFor(int employeeId)
    {
        WorkflowRequest request = null;

        UsingDbContext(context =>
        {
            request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                RequestType = "EmployeeAbsence",
                RequestNumber = Guid.NewGuid().ToString("N").Substring(0, 8),
                SummaryEn = "Annual leave",
                WorkflowStep = 1,
                RequestStatus = RequestStatus.Open
            }).Entity;
        });

        return request;
    }

    private NotificationConfig GivenAConfig(Action<NotificationConfig> configure)
    {
        NotificationConfig config = null;

        UsingDbContext(context =>
        {
            // Both channels off: these tests assert WHO a configuration addresses and WHICH configuration an
            // action reads. Turning delivery on would drag the notification framework and an SMTP server into
            // every one of them, and neither decides the answer.
            config = new NotificationConfig
            {
                TenantId = TenantId,
                Name = Guid.NewGuid().ToString("N").Substring(0, 8)
            };

            configure(config);
            context.Set<NotificationConfig>().Add(config);
        });

        return config;
    }

    private void GivenAGroup(int configId, int roleId) =>
        UsingDbContext(context => context.Set<NotificationGroup>().Add(new NotificationGroup
        {
            TenantId = TenantId,
            NotificationConfigId = configId,
            WorkflowRoleId = roleId,
            WorkflowRoleGroup = WorkflowRoleGroup.None
        }));

    private int GivenARoleHeldBy(int employeeId)
    {
        var roleId = 0;

        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Notified",
                NameAr = "Notified"
            }).Entity;

            context.SaveChanges();
            roleId = role.Id;

            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                WorkflowRoleId = role.Id,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(1)
            });
        });

        return roleId;
    }

    private void GivenAManager(int employeeId, int managerId, DateTime from, DateTime to) =>
        UsingDbContext(context => context.Set<EmployeeManager>().Add(new EmployeeManager
        {
            TenantId = TenantId,
            EmployeeId = employeeId,
            ManagerId = managerId,
            StartDate = from,
            EndDate = to
        }));

    private void GivenAnApprover(int requestId, int approverId, bool approved) =>
        UsingDbContext(context => context.Set<WorkFlowApprover>().Add(new WorkFlowApprover
        {
            TenantId = TenantId,
            WorkflowRequestId = requestId,
            ApproverId = approverId,
            IsApproved = approved
        }));

    private void GivenAnApprovalLog(int requestId, int approverId) =>
        UsingDbContext(context => context.Set<RequestLog>().Add(new RequestLog
        {
            TenantId = TenantId,
            WorkflowRequestId = requestId,
            ActionType = RequestActionType.Approved,
            ActionById = approverId,
            ActionDateTime = DateTime.Now
        }));

    private void SetStepConfig(int step, Action<RequestDetail> configure) =>
        UsingDbContext(context => configure(context.Set<RequestDetail>().Single(d => d.Step == step)));

    private void MarkInactive(int employeeId) =>
        UsingDbContext(context =>
            context.Set<Employee>().Single(e => e.Id == employeeId).Status = EmployeeStatus.Inactive);
}
