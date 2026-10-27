using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for personnel actions (<see cref="EmployeeAction"/>) -- promotions, transfers,
/// re-gradings, separations.
///
/// <para>
/// THE FIRST TYPE THAT IS NOT SELF-SERVICE. HR raises a personnel action, not the employee, so its catalog
/// row is seeded workflow-capable but ESS-disabled: it appears in an approver's inbox and never on an
/// employee's tile list. The two flags on <c>WorkflowTransactionType</c> exist precisely so a type can be
/// routed for approval without also being something anybody can raise about themselves -- and a promotion
/// you can request for yourself is not an approval chain, it is a suggestion box.
/// </para>
///
/// <para>
/// The stakes are high enough to justify routing. An <c>EmployeeAction</c> is the parent of
/// <c>EmployeeOrgAssignment</c> and <c>EmployeeContract</c> rows, so an unapproved one moves somebody's job
/// title, cost centre and pay in one step. Keeping it invisible until approved means those dependents cannot
/// be built on a decision nobody made.
/// </para>
/// </summary>
public class EmployeeActionWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeAction>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for personnel actions.</summary>
    public const string EmployeeActionRequestType = "EmployeeAction";

    private readonly IRepository<PersonalAction> _personalActionRepository;
    private readonly IRepository<PersonalActionReason> _reasonRepository;
    private readonly IRepository<NotificationConfig> _notificationConfigRepository;
    private readonly IRepository<EmailTemplate> _emailTemplateRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public EmployeeActionWorkflowTransactionHandler(
        IRepository<EmployeeAction> repository,
        IRepository<PersonalAction> personalActionRepository,
        IRepository<PersonalActionReason> reasonRepository,
        IRepository<NotificationConfig> notificationConfigRepository,
        IRepository<EmailTemplate> emailTemplateRepository,
        IRepository<Employee> employeeRepository,
        IEmailService emailService,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _personalActionRepository = personalActionRepository;
        _reasonRepository = reasonRepository;
        _notificationConfigRepository = notificationConfigRepository;
        _emailTemplateRepository = emailTemplateRepository;
        _employeeRepository = employeeRepository;
        _emailService = emailService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => EmployeeActionRequestType;

    /// <summary>
    /// Approves the row as every handler does, then sends the action reason's notification -- legacy
    /// <c>EmployeeActionRequest.OnSaving</c> (RequestStatus Approved): <c>empAction.ActionReason.NotificationConfig</c>
    /// was used as the template, about the employee, with one merge value <c>EmpCodeWithFullNameEn</c>
    /// ("{Code} {FullNameEn}").
    ///
    /// <para>
    /// Kept as legacy did it: email only (legacy sent no in-app notification here), and the configuration's
    /// <c>SendEmail</c> tick-box is not consulted -- legacy called <c>EmailService.SendEmail(config, ...)</c>
    /// directly, which does not read it. Recipients are whoever the configuration addresses, resolved the same way
    /// as every other notification. A configuration without an email template sends nothing (legacy failed the
    /// approval on it instead), and a send that fails is logged, never undoing the approval. An approved
    /// amendment sends nothing: legacy only sent when the action was first created.
    /// </para>
    /// </summary>
    public override async Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        await base.OnApprovedAsync(context);

        if (context.IsAmendment)
        {
            return;
        }

        var action = await GetTargetAsync(context);
        if (action == null)
        {
            return;
        }

        await SendActionReasonNotificationAsync(action);
    }

    private async Task SendActionReasonNotificationAsync(EmployeeAction action)
    {
        try
        {
            var configId = _reasonRepository.GetAll()
                .Where(r => r.Id == action.ActionReasonId)
                .Select(r => r.NotificationConfigId)
                .FirstOrDefault();

            if (configId == null)
            {
                return;
            }

            var config = _notificationConfigRepository.GetAll().FirstOrDefault(c => c.Id == configId.Value);
            if (config?.EmailTemplateId == null)
            {
                return;
            }

            var template = _emailTemplateRepository.GetAll().FirstOrDefault(t => t.Id == config.EmailTemplateId.Value);
            var employee = _employeeRepository.GetAll().FirstOrDefault(e => e.Id == action.EmployeeId);
            if (template == null || employee == null)
            {
                return;
            }

            var mergeValues = new Dictionary<string, object>
            {
                ["EmpCodeWithFullNameEn"] = (employee.Code ?? string.Empty) + " " + (employee.FullNameEn ?? string.Empty)
            };

            await _emailService.SendEmailForNotificationConfigAsync(config, template, employee, mergeValues);
        }
        catch (Exception exception)
        {
            Logger.Error(
                $"Personnel action {action.Id}: could not send the action reason notification: {exception.Message}",
                exception);
        }
    }

    /// <summary>
    /// Refuses a second action of the same kind on the same day for the same employee -- the rule the admin
    /// service already enforces, restated here so it holds for a routed action too.
    ///
    /// <para>
    /// The in-flight rows have to be visible for this to mean anything. Without the opt-out, the admin
    /// service's own duplicate check cannot see a pending action either, so the same promotion could be
    /// raised twice and both would pass.
    /// </para>
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var action = await GetTargetAsync(context);
        if (action == null)
        {
            return;
        }

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var duplicate = Repository.GetAll()
                .Any(a => a.Id != action.Id
                          && a.EmployeeId == action.EmployeeId
                          && a.PersonalActionId == action.PersonalActionId
                          && a.StartDate == action.StartDate
                          && (a.WorkflowStatus == null
                              || a.WorkflowStatus == RequestStatus.Approved
                              || a.WorkflowStatus == RequestStatus.Closed
                              || a.WorkflowStatus == RequestStatus.Open));

            if (duplicate)
            {
                throw new UserFriendlyException(L("DuplicateEmployeeActionExists"));
            }
        }
    }

    /// <summary>
    /// Leads with the action and its reason, because that pair IS the decision -- "Promotion, Annual review"
    /// tells an approver what they are agreeing to in a way an employee name and a date do not. The inbox
    /// shows who it is about separately.
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var action = await GetTargetAsync(context);
        if (action == null)
        {
            return null;
        }

        var personalAction = _personalActionRepository.GetAll()
            .Where(p => p.Id == action.PersonalActionId)
            .Select(p => new { p.NameEn, p.NameAr })
            .FirstOrDefault();

        var reason = _reasonRepository.GetAll()
            .Where(r => r.Id == action.ActionReasonId)
            .Select(r => new { r.NameEn, r.NameAr })
            .FirstOrDefault();

        var effective = action.StartDate.ToString("dd MMM yyyy");

        return new WorkflowSummary(
            $"{personalAction?.NameEn ?? "Action"}, {reason?.NameEn ?? "-"}, from {effective}",
            $"{personalAction?.NameAr ?? "إجراء"}، {reason?.NameAr ?? "-"}، من {effective}");
    }
}
