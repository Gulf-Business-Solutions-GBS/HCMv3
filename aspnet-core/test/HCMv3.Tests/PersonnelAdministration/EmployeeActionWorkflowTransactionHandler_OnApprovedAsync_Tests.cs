using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Localization;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Legacy <c>EmployeeActionRequest</c> (RequestStatus Approved): the action reason's notification configuration
/// is emailed about the employee. Built over in-memory rows, as <c>AppraisalWorkflow_Tests.HandlerOver</c> does.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeActionWorkflowTransactionHandler_OnApprovedAsync_Tests : AppTestBase
{
    private const int TenantId = 1;

    private readonly IEmailService _emailService = Substitute.For<IEmailService>();

    private readonly Employee _employee = new Employee { Id = 11, TenantId = TenantId, Code = "E1", FullNameEn = "John Doe" };

    private readonly EmailTemplate _template = new EmailTemplate { Id = 21, TenantId = TenantId, NameEn = "T", NameAr = "T", Subject = "S" };

    private EmployeeActionWorkflowTransactionHandler HandlerOver(
        EmployeeAction action,
        PersonalActionReason reason,
        NotificationConfig config)
    {
        var unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        unitOfWorkManager.Current.Returns(Substitute.For<IActiveUnitOfWork>());

        var handler = new EmployeeActionWorkflowTransactionHandler(
            ListRepository(action),
            ListRepository<PersonalAction>(),
            ListRepository(reason),
            config == null ? ListRepository<NotificationConfig>() : ListRepository(config),
            ListRepository(_template),
            ListRepository(_employee),
            _emailService,
            unitOfWorkManager);

        handler.LocalizationManager = LocalIocManager.Resolve<ILocalizationManager>();
        return handler;
    }

    private static IRepository<TEntity> ListRepository<TEntity>(params TEntity[] rows)
        where TEntity : class, IEntity<int>
    {
        var repository = Substitute.For<IRepository<TEntity>>();
        repository.GetAll().Returns(_ => rows.AsQueryable());
        return repository;
    }

    private static EmployeeAction Action(int reasonId)
    {
        return new EmployeeAction
        {
            Id = 31,
            TenantId = TenantId,
            EmployeeId = 11,
            PersonalActionId = 41,
            ActionReasonId = reasonId,
            StartDate = new DateTime(2026, 5, 1),
            WorkflowStatus = RequestStatus.Open
        };
    }

    private static PersonalActionReason Reason(int? configId)
    {
        return new PersonalActionReason { Id = 51, TenantId = TenantId, NameEn = "Annual review", NameAr = "مراجعة سنوية", NotificationConfigId = configId };
    }

    private NotificationConfig Config(bool sendEmail = false, int? templateId = 21)
    {
        return new NotificationConfig { Id = 61, TenantId = TenantId, Name = "Promotion", SendEmail = sendEmail, ToEmployee = true, EmailTemplateId = templateId };
    }

    private static WorkflowTransactionContext ContextFor(EmployeeAction action, bool isAmendment = false)
    {
        return new WorkflowTransactionContext(
            new WorkflowRequest
            {
                Id = 71,
                TenantId = TenantId,
                EmployeeId = action.EmployeeId,
                RequestType = EmployeeActionWorkflowTransactionHandler.EmployeeActionRequestType,
                TargetEntityId = action.Id,
                RequestNumber = "EA-TEST",
                RequestStatus = RequestStatus.Approved,
                IsAmendment = isAmendment
            },
            actingEmployeeId: 99);
    }

    [Fact]
    public async Task Should_Email_The_Action_Reason_Configuration_About_The_Employee_On_Approval()
    {
        var action = Action(51);
        var handler = HandlerOver(action, Reason(61), Config());

        await handler.OnApprovedAsync(ContextFor(action));

        await _emailService.Received(1).SendEmailForNotificationConfigAsync(
            Arg.Is<NotificationConfig>(c => c.Id == 61),
            Arg.Is<EmailTemplate>(t => t.Id == 21),
            Arg.Is<object>(o => o == (object)_employee),
            Arg.Is<IDictionary<string, object>>(m => (string)m["EmpCodeWithFullNameEn"] == "E1 John Doe"));
        action.WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task Should_Email_Even_When_The_Configuration_Has_SendEmail_Off()
    {
        // Pinned legacy behaviour: EmailService.SendEmail(config, ...) never read the SendEmail tick-box.
        var action = Action(51);
        var handler = HandlerOver(action, Reason(61), Config(sendEmail: false));

        await handler.OnApprovedAsync(ContextFor(action));

        await _emailService.Received(1).SendEmailForNotificationConfigAsync(
            Arg.Any<NotificationConfig>(), Arg.Any<EmailTemplate>(), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task Should_Send_Nothing_When_The_Reason_Has_No_Configuration()
    {
        var action = Action(51);
        var handler = HandlerOver(action, Reason(null), Config());

        await handler.OnApprovedAsync(ContextFor(action));

        _emailService.ReceivedCalls().ShouldBeEmpty();
        action.WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task Should_Send_Nothing_When_The_Configuration_Has_No_Template()
    {
        var action = Action(51);
        var handler = HandlerOver(action, Reason(61), Config(templateId: null));

        await handler.OnApprovedAsync(ContextFor(action));

        _emailService.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>
    /// An employee action cannot be amended through workflow at all: the base handler refuses the amendment
    /// before anything is applied, so no notification goes out either.
    /// </summary>
    [Fact]
    public async Task Should_Refuse_An_Amendment_And_Send_Nothing()
    {
        var action = Action(51);
        var handler = HandlerOver(action, Reason(61), Config());

        await Should.ThrowAsync<Abp.UI.UserFriendlyException>(
            () => handler.OnApprovedAsync(ContextFor(action, isAmendment: true)));

        _emailService.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Send_Nothing_On_Rejection()
    {
        var action = Action(51);
        var handler = HandlerOver(action, Reason(61), Config());

        await handler.OnRejectedAsync(ContextFor(action));

        _emailService.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Still_Approve_When_The_Email_Fails()
    {
        var action = Action(51);
        var handler = HandlerOver(action, Reason(61), Config());
        _emailService.SendEmailForNotificationConfigAsync(
                Arg.Any<NotificationConfig>(), Arg.Any<EmailTemplate>(), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>())
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        await Should.NotThrowAsync(() => handler.OnApprovedAsync(ContextFor(action)));

        action.WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }
}
