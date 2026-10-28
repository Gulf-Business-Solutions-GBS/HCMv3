using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.SystemSettings.Services;
using HCMv3.GeneralMasterInfo;
using HCMv3.Workflow.Dto;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Workflow;

/// <summary>
/// Application service for <see cref="RequestDetail"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.RequestDetail</c>).
///
/// STEP NUMBERING (legacy <c>PerformValidation</c>/<c>AddStep</c>/<c>OnDeleted</c>):
/// <list type="bullet">
/// <item>A new step always goes to the end of the chain, whatever step number the screen sent
/// (legacy <c>AddStep(count)</c>).</item>
/// <item>Changing an existing step's number is governed by <c>ManageWorkflowSettings.DynamicWorkflowSteps</c>:
/// off, and the number is put back to what it was (legacy <c>this.Step = this.prevStep</c>); on, and the
/// step moves to the requested position with the steps in between shifting to make room, after a 1..count
/// range check.</item>
/// <item>After every create, move or delete the chain is renumbered 1..n and
/// <see cref="RequestMaster.WorkflowSteps"/> is set to n.</item>
/// </list>
/// Plus the legacy Reminder/Escalate day-ordering check.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Workflow_RequestDetails)]
public class RequestDetailsAppService : HCMv3AppServiceBase, IRequestDetailsAppService
{
    private readonly IRepository<RequestDetail> _repository;
    private readonly IRepository<RequestMaster> _requestMasterRepository;
    private readonly IRepository<NotificationConfig> _notificationConfigRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IWorkflowTransactionHandlerResolver _handlerResolver;
    private readonly IRepository<WorkflowTransactionType> _transactionTypeRepository;

    public RequestDetailsAppService(
        IRepository<RequestDetail> repository,
        IRepository<RequestMaster> requestMasterRepository,
        IRepository<NotificationConfig> notificationConfigRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IWorkflowTransactionHandlerResolver handlerResolver,
        IRepository<WorkflowTransactionType> transactionTypeRepository)
    {
        _repository = repository;
        _requestMasterRepository = requestMasterRepository;
        _notificationConfigRepository = notificationConfigRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _handlerResolver = handlerResolver;
        _transactionTypeRepository = transactionTypeRepository;
    }

    /// <summary>
    /// Every employee field on every transaction type, for the step dialog's Request Field dropdown --
    /// "Leave Request – Primary Delegation", "Document Withdrawal – Guarantor". Discovered from the types'
    /// entities, so a new employee field on a transaction appears here without a code change.
    /// </summary>
    public async Task<List<RequestPersonFieldOptionDto>> GetRequestPersonFieldOptions()
    {
        return (await BuildRequestPersonFieldOptionsAsync())
            .OrderBy(o => o.AppliesTo)
            .ThenBy(o => o.FieldName)
            .ToList();
    }

    private async Task<List<RequestPersonFieldOptionDto>> BuildRequestPersonFieldOptionsAsync()
    {
        var handlers = _handlerResolver.GetAll();

        var typeNames = await _transactionTypeRepository.GetAll()
            .Select(t => new { t.Code, t.NameEn, t.NameAr })
            .ToListAsync();

        var isArabic = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

        string NameOf(string code)
        {
            var type = typeNames.FirstOrDefault(t => string.Equals(t.Code, code, System.StringComparison.OrdinalIgnoreCase));
            if (type == null)
            {
                return code;
            }

            return isArabic && !type.NameAr.IsNullOrWhiteSpace() ? type.NameAr : type.NameEn;
        }

        return handlers
            .SelectMany(h => h.RequestPersonFields.Select(property => new RequestPersonFieldOptionDto
            {
                Code = WorkflowRequestPersonFields.Compose(h.RequestType, property),
                DisplayName = NameOf(h.RequestType) + " – " + FieldLabel(property),
                FieldName = FieldLabel(property),
                AppliesTo = NameOf(h.RequestType)
            }))
            .ToList();
    }

    /// <summary>
    /// The field's own label, reusing the key its form already shows ("GuarantorId" -> "Guarantor",
    /// "PrimaryDelegateId" -> "PrimaryDelegate"), or the property name split into words when there is none.
    /// </summary>
    private string FieldLabel(string propertyName)
    {
        var key = propertyName.EndsWith("Id") ? propertyName.Substring(0, propertyName.Length - 2) : propertyName;

        return LocalizationSource.GetStringOrNull(key)
               ?? System.Text.RegularExpressions.Regex.Replace(key, "(?<=[a-z])(?=[A-Z])", " ");
    }

    [HttpPost]
    public async Task<PagedResultDto<RequestDetailDto>> GetAll(GetAllRequestDetailsInput input)
    {
        var query = _repository.GetAll()
            .Include(d => d.WorkflowRole)
            .WhereIf(input.RequestMasterId.HasValue, d => d.RequestMasterId == input.RequestMasterId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Step" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<RequestDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetRequestDetailForEditOutput> GetRequestDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetRequestDetailForEditOutput { RequestDetail = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditRequestDetailDto input)
    {
        if (input.Id.HasValue)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        var requestMasterId = entity.RequestMasterId;

        await _repository.DeleteAsync(input.Id);

        await RecalculateWorkflowStepsAsync(requestMasterId);
    }

    /// <summary>
    /// Puts the chain's steps in the order given, then renumbers 1..n.
    ///
    /// <para>
    /// Takes the whole order rather than a move instruction, so a drag needs one call and there is no
    /// position arithmetic to get wrong on either side. The list must be exactly this chain's steps: a
    /// missing or foreign id means the grid was working from a stale read, and quietly reordering what it
    /// sent would renumber the chain around a step somebody else had just deleted.
    /// </para>
    ///
    /// <para>
    /// Requests already in flight keep the approver rows they were given, so nothing loses its approvers.
    /// What changes for them is the configuration their current step number now points at. The same is
    /// already true of editing a step number by hand, which is why this does not refuse -- but it is worth
    /// knowing before reordering a chain that is carrying live requests.
    /// </para>
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestDetails_Edit)]
    public async Task ReorderSteps(ReorderRequestDetailsInput input)
    {
        var details = await _repository.GetAll()
            .Where(d => d.RequestMasterId == input.RequestMasterId)
            .ToListAsync();

        var ordered = input.OrderedIds ?? new List<int>();

        // Set equality, not just count: the same id twice would pass a count check and leave one step
        // unnumbered.
        if (ordered.Count != details.Count || ordered.Distinct().Count() != ordered.Count ||
            ordered.Any(id => details.All(d => d.Id != id)))
        {
            throw new UserFriendlyException(L("ApprovalStepsChangedReloadAndTryAgain"));
        }

        for (var i = 0; i < ordered.Count; i++)
        {
            var detail = details.First(d => d.Id == ordered[i]);
            detail.Step = i + 1;
        }

        await RecalculateWorkflowStepsAsync(input.RequestMasterId);
    }

    /// <summary>
    /// Duplicates a step onto the end of its chain.
    ///
    /// <para>
    /// Steps in one chain differ in a field or two and agree on the other twenty -- the same escalation
    /// days, the same notification settings, the same send-back and reject rules. Copying one and changing
    /// what differs is the difference between a chain taking a minute to build and taking ten.
    /// </para>
    ///
    /// <para>
    /// Appended rather than inserted after the original, because the new step is going to be edited
    /// immediately anyway and can be dragged where it belongs. Parallel approvers are not copied: they name
    /// specific people for a specific step, and carrying them over silently would give the new step
    /// approvers nobody chose for it.
    /// </para>
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestDetails_Create)]
    public async Task<EntityDto> CopyStep(EntityDto input)
    {
        var source = await _repository.GetAsync(input.Id);

        // Copying the creator step is the likeliest way a chain would end up with two of them, since the
        // copy carries the approver setting across untouched.
        if (source.ApproverPerson == WorkflowApproverPersonType.None)
        {
            throw new UserFriendlyException(L("AChainCanHaveOnlyOneCreatorStep"));
        }

        var copy = new RequestDetail
        {
            TenantId = AbpSession.TenantId ?? 0,
            RequestMasterId = source.RequestMasterId,
            WorkflowRoleId = source.WorkflowRoleId,
            ShowPayment = source.ShowPayment,
            CanSendBack = source.CanSendBack,
            CanReject = source.CanReject,
            ShouldEscalate = source.ShouldEscalate,
            EscalateAfterDays = source.EscalateAfterDays,
            EscalateNextLevel = source.EscalateNextLevel,
            EscalateWorkflowRoleId = source.EscalateWorkflowRoleId,
            EscalateWorkflowRoleGroup = source.EscalateWorkflowRoleGroup,
            EscalateWorkflowRoleOrgId = source.EscalateWorkflowRoleOrgId,
            EnableReadLog = source.EnableReadLog,
            Person = source.Person,
            ApproverPerson = source.ApproverPerson,
            RequestPersonField = source.RequestPersonField,
            WorkflowRoleGroup = source.WorkflowRoleGroup,
            WorkflowRoleOrgId = source.WorkflowRoleOrgId,
            ApprovalRequire = source.ApprovalRequire,
            IsCommentRequire = source.IsCommentRequire,
            SignatoryStep = source.SignatoryStep,
            ReminderDays = source.ReminderDays,
            NotificationConfigId = source.NotificationConfigId,
            SubmitNotificationConfigId = source.SubmitNotificationConfigId,
            RejectNotificationConfigId = source.RejectNotificationConfigId,
            SendBackNotificationConfigId = source.SendBackNotificationConfigId,
            RemarksNotificationConfigId = source.RemarksNotificationConfigId,
            AssignNotificationConfigId = source.AssignNotificationConfigId,
            ReminderNotificationConfigId = source.ReminderNotificationConfigId,
            Step = await _repository.GetAll().CountAsync(d => d.RequestMasterId == source.RequestMasterId) + 1
        };

        await _repository.InsertAsync(copy);

        await RecalculateWorkflowStepsAsync(source.RequestMasterId);

        return new EntityDto(copy.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditRequestDetailDto input)
    {
        var entity = MapToEntity(input, new RequestDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateReminderVsEscalate(entity);
        await ValidateSingleCreatorStepAsync(entity);

        // Legacy AddStep(count): a new step is appended, whatever number the screen sent.
        entity.Step = await _repository.GetAll().CountAsync(d => d.RequestMasterId == entity.RequestMasterId) + 1;

        await _repository.InsertAsync(entity);

        await RecalculateWorkflowStepsAsync(entity.RequestMasterId);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditRequestDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        var previousStep = entity.Step;
        entity = MapToEntity(input, entity);

        ValidateReminderVsEscalate(entity);
        await ValidateSingleCreatorStepAsync(entity);

        if (entity.Step != previousStep)
        {
            var settings = await _sysSettingsAccessor.GetWorkflowSettingsAsync();
            if (settings == null || !settings.DynamicWorkflowSteps)
            {
                // Legacy: this.Step = this.prevStep -- the number is not editable.
                entity.Step = previousStep;
            }
            else
            {
                await MoveStepAsync(entity, previousStep);
            }
        }

        await _repository.UpdateAsync(entity);

        await RecalculateWorkflowStepsAsync(entity.RequestMasterId);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c>, <c>DynamicWorkflowSteps == true</c> branch: the step takes the
    /// requested position and every step between the old and the new position shifts one place to make room.
    /// </summary>
    private async Task MoveStepAsync(RequestDetail entity, int previousStep)
    {
        var count = await _repository.GetAll().CountAsync(d => d.RequestMasterId == entity.RequestMasterId);
        if (entity.Step < 1 || entity.Step > count)
        {
            throw new UserFriendlyException(L("RequestDetailStepOutOfRange", count));
        }

        var siblings = await _repository.GetAll()
            .Where(d => d.RequestMasterId == entity.RequestMasterId && d.Id != entity.Id)
            .ToListAsync();

        foreach (var sibling in siblings)
        {
            if (previousStep > entity.Step && sibling.Step >= entity.Step && sibling.Step < previousStep)
            {
                sibling.Step += 1;
            }
            else if (previousStep < entity.Step && sibling.Step > previousStep && sibling.Step <= entity.Step)
            {
                sibling.Step -= 1;
            }
        }
    }

    private void ValidateReminderVsEscalate(RequestDetail entity)
    {
        if (entity.EscalateAfterDays != 0 && entity.ReminderDays >= entity.EscalateAfterDays)
        {
            throw new UserFriendlyException(L("ReminderDaysMustBeLessThanEscalateAfterDays"));
        }
    }

    /// <summary>
    /// A chain may hold at most one creator step.
    ///
    /// <para>
    /// A step naming nobody is the requester's own: approvals begin at the first step that DOES name
    /// somebody. A second one is therefore not a second anything -- it is a step no one is ever asked to
    /// act on, sitting in the middle of a chain, silently stalling every request that reaches it.
    /// </para>
    ///
    /// <para>
    /// Enforced on the server rather than only in the form, so it holds for the copy action and for any
    /// other caller as well as for the dialog.
    /// </para>
    /// </summary>
    private async Task ValidateSingleCreatorStepAsync(RequestDetail entity)
    {
        if (entity.ApproverPerson != WorkflowApproverPersonType.None)
        {
            return;
        }

        var alreadyHasOne = await _repository.GetAll()
            .AnyAsync(d => d.RequestMasterId == entity.RequestMasterId
                           && d.Id != entity.Id
                           && d.ApproverPerson == WorkflowApproverPersonType.None);

        if (alreadyHasOne)
        {
            throw new UserFriendlyException(L("AChainCanHaveOnlyOneCreatorStep"));
        }
    }

    private async Task RecalculateWorkflowStepsAsync(int requestMasterId)
    {
        // The pending insert/delete MUST be flushed first. CountAsync issues a SQL COUNT, which does not see
        // rows EF Core is still holding in the change tracker, so without this the count is always one
        // behind: zero after the first step is added, one after the second, and so on.
        //
        // That is not cosmetic. WorkflowApprovalService reads WorkflowSteps as the total number of steps, so
        // an undercount ends the approval chain early -- on a two-step chain the request would be marked
        // fully approved the moment the FIRST approver acted, and the second approver would never be asked.
        await CurrentUnitOfWork.SaveChangesAsync();

        // Legacy AddStep / OnDeleted: the chain is always numbered 1..n with no gaps.
        var details = await _repository.GetAll()
            .Where(d => d.RequestMasterId == requestMasterId)
            .OrderBy(d => d.Step)
            .ThenBy(d => d.Id)
            .ToListAsync();

        for (var i = 0; i < details.Count; i++)
        {
            if (details[i].Step != i + 1)
            {
                details[i].Step = i + 1;
            }
        }

        var requestMaster = await _requestMasterRepository.GetAsync(requestMasterId);
        requestMaster.WorkflowSteps = details.Count;
        await _requestMasterRepository.UpdateAsync(requestMaster);
    }

    // Not static: it reads the tenant's default notification configurations, which needs a repository.
    private RequestDetail MapToEntity(CreateOrEditRequestDetailDto input, RequestDetail entity)
    {
        entity.RequestMasterId = input.RequestMasterId;
        entity.Step = input.Step;
        entity.WorkflowRoleId = input.WorkflowRoleId;
        entity.ShowPayment = input.ShowPayment;
        entity.CanSendBack = input.CanSendBack;
        entity.CanReject = input.CanReject;
        entity.ShouldEscalate = input.ShouldEscalate;
        entity.EscalateAfterDays = input.EscalateAfterDays;
        entity.EscalateNextLevel = input.EscalateNextLevel;
        entity.EscalateWorkflowRoleId = input.EscalateWorkflowRoleId;
        entity.EscalateWorkflowRoleGroup = input.EscalateWorkflowRoleGroup;
        entity.EscalateWorkflowRoleOrgId = input.EscalateWorkflowRoleOrgId;
        // Unset means on. See the DTO: false and "not specified" are indistinguishable on a plain bool,
        // and defaulting to off left every chain with an empty read log.
        entity.EnableReadLog = input.EnableReadLog ?? true;
        entity.Person = input.Person;
        entity.ApproverPerson = NormalizeApproverPerson(input.ApproverPerson, input.WorkflowRoleId);
        ApplyRequestPersonField(entity, input.RequestPersonField);
        entity.WorkflowRoleGroup = input.WorkflowRoleGroup;
        entity.WorkflowRoleOrgId = input.WorkflowRoleOrgId;
        entity.ApprovalRequire = input.ApprovalRequire;
        entity.IsCommentRequire = input.IsCommentRequire;
        entity.SignatoryStep = input.SignatoryStep;
        entity.ReminderDays = input.ReminderDays;
        entity.NotificationConfigId = input.NotificationConfigId;
        entity.SubmitNotificationConfigId = input.SubmitNotificationConfigId;
        entity.RejectNotificationConfigId = input.RejectNotificationConfigId;
        entity.SendBackNotificationConfigId = input.SendBackNotificationConfigId;
        entity.RemarksNotificationConfigId = input.RemarksNotificationConfigId;
        entity.AssignNotificationConfigId = input.AssignNotificationConfigId;
        entity.ReminderNotificationConfigId = input.ReminderNotificationConfigId;

        ApplyDefaultNotificationConfigs(entity);

        return entity;
    }

    /// <summary>
    /// A step that names a workflow role and no approver type goes to the role's holders.
    ///
    /// <para>
    /// This is legacy's own rule: there, <c>ApproverPerson = None</c> on an approval step meant "whoever holds
    /// the step's workflow role" (<c>WorkFlowServices.GetApprover</c>). HCMv3 stores that as
    /// <see cref="WorkflowApproverPersonType.Person"/> and keeps <c>None</c> for the creator step, so an
    /// administrator who picks only a role -- the natural way to say "send it to HR" -- would otherwise have
    /// made a second creator step and been refused. Choosing the role is enough; the approver type is only
    /// needed to ALSO send the step to a manager.
    /// </para>
    /// </summary>
    private static WorkflowApproverPersonType NormalizeApproverPerson(
        WorkflowApproverPersonType approverPerson, int? workflowRoleId)
    {
        return approverPerson == WorkflowApproverPersonType.None && workflowRoleId.HasValue
            ? WorkflowApproverPersonType.Person
            : approverPerson;
    }

    /// <summary>
    /// A "Person on Request" step must say which field names its approver; every other step carries none.
    ///
    /// <para>
    /// The workflow role is cleared on such a step rather than kept: the engine routes it to the named person
    /// alone, and a role left showing on the screen would read as a second approver that is never asked.
    /// </para>
    /// </summary>
    private void ApplyRequestPersonField(RequestDetail entity, string requestPersonField)
    {
        if (entity.ApproverPerson != WorkflowApproverPersonType.PersonOnRequest)
        {
            entity.RequestPersonField = null;
            return;
        }

        if (!IsKnownRequestPersonField(requestPersonField))
        {
            throw new UserFriendlyException(L("RequestPersonFieldIsRequired"));
        }

        entity.RequestPersonField = requestPersonField;
        entity.WorkflowRoleId = null;
    }

    /// <summary>A field some transaction type actually has: its type has a handler, and the handler offers it.</summary>
    private bool IsKnownRequestPersonField(string code)
    {
        return WorkflowRequestPersonFields.TryParse(code, out var requestType, out var propertyName)
               && _handlerResolver.Resolve(requestType)?.RequestPersonFields.Contains(propertyName) == true;
    }

    /// <summary>
    /// Points a step at the tenant's default notification configurations wherever it names none.
    ///
    /// <para>
    /// The dispatcher sends nothing when a step has no configuration for the action, so a step saved with
    /// these left blank -- which is every step, since the screen does not require them -- notifies nobody.
    /// A chain that routes correctly and tells no one reads as broken notifications rather than as missing
    /// setup, and there is no obvious place to go and look.
    /// </para>
    ///
    /// <para>
    /// Only fills what the caller left null, so an administrator who has chosen a configuration keeps it,
    /// and one who wants a step silent can point it at a configuration that addresses nobody. It does not
    /// overwrite on edit for the same reason.
    /// </para>
    /// </summary>
    private void ApplyDefaultNotificationConfigs(RequestDetail entity)
    {
        var defaults = _notificationConfigRepository.GetAll()
            .Where(c => c.Name == WorkflowNotificationDefaults.ToApproverName
                        || c.Name == WorkflowNotificationDefaults.ToRequesterName)
            .Select(c => new { c.Id, c.Name })
            .ToList();

        var toApprover = defaults
            .FirstOrDefault(c => c.Name == WorkflowNotificationDefaults.ToApproverName)?.Id;

        var toRequester = defaults
            .FirstOrDefault(c => c.Name == WorkflowNotificationDefaults.ToRequesterName)?.Id;

        // Arriving somewhere: whoever it is now with needs to know.
        entity.SubmitNotificationConfigId ??= toApprover;
        entity.AssignNotificationConfigId ??= toApprover;
        entity.ReminderNotificationConfigId ??= toApprover;

        // Being decided: the person who raised it needs to know.
        entity.NotificationConfigId ??= toRequester;
        entity.RejectNotificationConfigId ??= toRequester;
        entity.SendBackNotificationConfigId ??= toRequester;
        entity.RemarksNotificationConfigId ??= toRequester;
    }

    private static RequestDetailDto ToDto(RequestDetail entity)
    {
        return new RequestDetailDto
        {
            Id = entity.Id,
            RequestMasterId = entity.RequestMasterId,
            Step = entity.Step,
            WorkflowRoleId = entity.WorkflowRoleId,
            WorkflowRoleNameEn = entity.WorkflowRole?.NameEn,
            ShowPayment = entity.ShowPayment,
            CanSendBack = entity.CanSendBack,
            CanReject = entity.CanReject,
            ShouldEscalate = entity.ShouldEscalate,
            EscalateAfterDays = entity.EscalateAfterDays,
            EscalateNextLevel = entity.EscalateNextLevel,
            EscalateWorkflowRoleId = entity.EscalateWorkflowRoleId,
            EscalateWorkflowRoleGroup = entity.EscalateWorkflowRoleGroup,
            EscalateWorkflowRoleOrgId = entity.EscalateWorkflowRoleOrgId,
            EnableReadLog = entity.EnableReadLog,
            Person = entity.Person,
            ApproverPerson = entity.ApproverPerson,
            RequestPersonField = entity.RequestPersonField,
            WorkflowRoleGroup = entity.WorkflowRoleGroup,
            WorkflowRoleOrgId = entity.WorkflowRoleOrgId,
            ApprovalRequire = entity.ApprovalRequire,
            IsCommentRequire = entity.IsCommentRequire,
            SignatoryStep = entity.SignatoryStep,
            ReminderDays = entity.ReminderDays,
            NotificationConfigId = entity.NotificationConfigId,
            SubmitNotificationConfigId = entity.SubmitNotificationConfigId,
            RejectNotificationConfigId = entity.RejectNotificationConfigId,
            SendBackNotificationConfigId = entity.SendBackNotificationConfigId,
            RemarksNotificationConfigId = entity.RemarksNotificationConfigId,
            AssignNotificationConfigId = entity.AssignNotificationConfigId,
            ReminderNotificationConfigId = entity.ReminderNotificationConfigId
        };
    }

    private static CreateOrEditRequestDetailDto ToCreateOrEditDto(RequestDetail entity)
    {
        return new CreateOrEditRequestDetailDto
        {
            Id = entity.Id,
            RequestMasterId = entity.RequestMasterId,
            Step = entity.Step,
            WorkflowRoleId = entity.WorkflowRoleId,
            ShowPayment = entity.ShowPayment,
            CanSendBack = entity.CanSendBack,
            CanReject = entity.CanReject,
            ShouldEscalate = entity.ShouldEscalate,
            EscalateAfterDays = entity.EscalateAfterDays,
            EscalateNextLevel = entity.EscalateNextLevel,
            EscalateWorkflowRoleId = entity.EscalateWorkflowRoleId,
            EscalateWorkflowRoleGroup = entity.EscalateWorkflowRoleGroup,
            EscalateWorkflowRoleOrgId = entity.EscalateWorkflowRoleOrgId,
            EnableReadLog = entity.EnableReadLog,
            Person = entity.Person,
            ApproverPerson = entity.ApproverPerson,
            RequestPersonField = entity.RequestPersonField,
            WorkflowRoleGroup = entity.WorkflowRoleGroup,
            WorkflowRoleOrgId = entity.WorkflowRoleOrgId,
            ApprovalRequire = entity.ApprovalRequire,
            IsCommentRequire = entity.IsCommentRequire,
            SignatoryStep = entity.SignatoryStep,
            ReminderDays = entity.ReminderDays,
            NotificationConfigId = entity.NotificationConfigId,
            SubmitNotificationConfigId = entity.SubmitNotificationConfigId,
            RejectNotificationConfigId = entity.RejectNotificationConfigId,
            SendBackNotificationConfigId = entity.SendBackNotificationConfigId,
            RemarksNotificationConfigId = entity.RemarksNotificationConfigId,
            AssignNotificationConfigId = entity.AssignNotificationConfigId,
            ReminderNotificationConfigId = entity.ReminderNotificationConfigId
        };
    }
}
