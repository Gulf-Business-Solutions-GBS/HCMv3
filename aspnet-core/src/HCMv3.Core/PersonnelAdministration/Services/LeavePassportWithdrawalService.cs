using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Runtime.Session;
using Abp.Threading;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>Why a leave action did not raise a passport withdrawal.</summary>
public enum LeavePassportWithdrawalSkipReason
{
    /// <summary>Nothing was skipped: a withdrawal should be raised.</summary>
    None = 0,

    /// <summary>The request is missing or is not a leave request.</summary>
    NotALeaveRequest,

    /// <summary>An amendment, a cancellation or a return from leave -- only a new leave raises one.</summary>
    NotANewLeave,

    /// <summary><c>IsPassportWithdrawalRequired</c> is off, or the tenant has no self-service settings yet.</summary>
    SettingOff,

    /// <summary>The action was not taken at <c>PassportWithdrawalSubmissionStep</c>.</summary>
    NotTheConfiguredStep,

    /// <summary>The leave row the request points at no longer exists.</summary>
    LeaveNotFound,

    /// <summary>The employee did not say they are travelling.</summary>
    NotTravelling,

    /// <summary>A withdrawal has already been raised from this leave.</summary>
    AlreadyRaised,

    /// <summary>Passport withdrawal is not workflow-managed, not offered in self-service, or retired.</summary>
    PassportWorkflowOff,

    /// <summary>No document type carries the tenant's withdrawable-document code.</summary>
    NoWithdrawableDocumentType,

    /// <summary>The employee has no document of the withdrawable type on file.</summary>
    NoDocumentOnFile,

    /// <summary>The work is an import or a background job, which never raises approval requests.</summary>
    NonInteractive
}

/// <summary>
/// Whether a leave action raises a passport withdrawal, and with what values. Produced by
/// <see cref="ILeavePassportWithdrawalService.DecideAsync"/> without writing anything.
/// </summary>
public class LeavePassportWithdrawalDecision
{
    public bool ShouldRaise => SkipReason == LeavePassportWithdrawalSkipReason.None;

    public LeavePassportWithdrawalSkipReason SkipReason { get; set; }

    public int TenantId { get; set; }

    public int LeaveId { get; set; }

    /// <summary>The leave's own request number, quoted in the withdrawal's summary and log.</summary>
    public string LeaveRequestNumber { get; set; }

    /// <summary>The employee the leave is for, and so the one the withdrawal is for.</summary>
    public int EmployeeId { get; set; }

    /// <summary>That employee's login, when they have one: the withdrawal is raised as them.</summary>
    public long? EmployeeUserId { get; set; }

    public int EmployeeDocumentId { get; set; }

    public int DocumentTypeId { get; set; }

    /// <summary>The day before the leave starts.</summary>
    public DateTime WithdrawDate { get; set; }

    /// <summary>The day after the leave ends.</summary>
    public DateTime ReturnDate { get; set; }

    /// <summary>The leave type's name.</summary>
    public string WithdrawalReason { get; set; }

    public string LeaveComments { get; set; }

    public static LeavePassportWithdrawalDecision Skip(LeavePassportWithdrawalSkipReason reason)
    {
        return new LeavePassportWithdrawalDecision { SkipReason = reason };
    }
}

/// <summary>
/// Raises a passport withdrawal request from a leave request, when the tenant has asked for it.
///
/// <para>
/// Legacy did this in the browser (<c>LeaveRequest.js</c>, <c>CreatePassWithdrawal</c> /
/// <c>PassportWithdrawalSubmit</c>) and again, separately, in the mobile API
/// (<c>WorkflowServiceApiController.CreatePassportWithdrawalFromLeave</c>): once the leave action had
/// succeeded, a second call posted a passport withdrawal dated from the day before the leave to the day
/// after it, with the leave type as the reason and no guarantor. Here it is one server-side rule, so it
/// holds for every client and no script has to remember it.
/// </para>
///
/// <para>
/// The trigger, as legacy had it: the tenant setting <c>IsPassportWithdrawalRequired</c> is on, the employee
/// is travelling, and the leave is acted on -- submitted by its requester, or approved by an approver --
/// while it sits at <c>PassportWithdrawalSubmissionStep</c>.
/// </para>
///
/// <para>
/// <b>It can never cost the employee their leave.</b> Nothing is written while the leave action is in
/// flight: <see cref="RaiseAfterCommit"/> waits for the leave's unit of work to commit and then works in
/// one of its own, so a withdrawal that cannot be raised -- no approver on the passport chain, say -- rolls
/// back alone and is logged, exactly as legacy reported "leave processed, passport request failed".
/// </para>
/// </summary>
public interface ILeavePassportWithdrawalService : IDomainService
{
    /// <summary>
    /// The cheap half of the decision, safe to ask while the leave action is still in flight: is the setting
    /// on, is this a new leave, and is this the configured step. Reads only.
    /// </summary>
    Task<bool> IsTriggerPointAsync(WorkflowRequest leaveRequest, WorkflowStepAction action, int step);

    /// <summary>The whole decision, with the values a withdrawal would carry. Writes nothing.</summary>
    Task<LeavePassportWithdrawalDecision> DecideAsync(int leaveRequestId, WorkflowStepAction action, int step);

    /// <summary>
    /// Decides and, when a withdrawal is due, creates it and submits it into the passport type's own
    /// approval chain. Returns the new movement's id, or null when nothing was due -- which is logged at
    /// information level and is never an error. A genuine failure to raise one does throw; the caller that
    /// must not fail uses <see cref="RaiseAfterCommit"/>.
    /// </summary>
    Task<int?> RaiseIfRequiredAsync(int leaveRequestId, WorkflowStepAction action, int step);

    /// <summary>
    /// Runs <see cref="RaiseIfRequiredAsync"/> once the current unit of work has committed, in a unit of
    /// work of its own, and swallows and logs any failure.
    /// </summary>
    void RaiseAfterCommit(int leaveRequestId, WorkflowStepAction action, int step);
}

/// <inheritdoc cref="ILeavePassportWithdrawalService"/>
public class LeavePassportWithdrawalService : HCMv3DomainServiceBase, ILeavePassportWithdrawalService
{
    /// <summary>The tag in front of a passport withdrawal's request number, as self-service uses.</summary>
    public const string RequestNumberPrefix = "PPW";

    /// <summary>The note written on a withdrawal raised from a leave. {0} is the leave's request number.</summary>
    public const string RaisedFromLeaveNoteKey = "PassportWithdrawalRaisedAutomaticallyFromLeaveRequest";

    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly IRepository<EmployeeAbsence> _leaveRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<EmployeeDocument> _documentRepository;
    private readonly IRepository<EmployeeDocumentMvmt> _movementRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IRepository<ManageSelfServiceSettings> _selfServiceSettingsRepository;
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly IWithdrawableDocumentTypeResolver _withdrawableDocumentTypeResolver;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IAbpSession _abpSession;
    private readonly IIocResolver _iocResolver;

    public LeavePassportWithdrawalService(
        IRepository<WorkflowRequest> requestRepository,
        IRepository<EmployeeAbsence> leaveRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<EmployeeDocument> documentRepository,
        IRepository<EmployeeDocumentMvmt> movementRepository,
        IRepository<Employee> employeeRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IRepository<ManageSelfServiceSettings> selfServiceSettingsRepository,
        IRequestMasterResolver requestMasterResolver,
        IWithdrawableDocumentTypeResolver withdrawableDocumentTypeResolver,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession abpSession,
        IIocResolver iocResolver)
    {
        _requestRepository = requestRepository;
        _leaveRepository = leaveRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _documentRepository = documentRepository;
        _movementRepository = movementRepository;
        _employeeRepository = employeeRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _selfServiceSettingsRepository = selfServiceSettingsRepository;
        _requestMasterResolver = requestMasterResolver;
        _withdrawableDocumentTypeResolver = withdrawableDocumentTypeResolver;
        _unitOfWorkManager = unitOfWorkManager;
        _abpSession = abpSession;
        _iocResolver = iocResolver;
    }

    /// <summary>
    /// Whether an action at <paramref name="step"/> is the one the setting names.
    ///
    /// <para>
    /// Legacy compared the setting with the step the request sat at when the form was opened, i.e. the step
    /// being LEFT (<c>LeaveRequest.js</c> 2810-2816, <c>RequestForm.cshtml</c> 40). An approval therefore
    /// matches when the approver approves at exactly that step.
    /// </para>
    ///
    /// <para>
    /// A submission matched when the setting was 0 -- a new form has no step yet -- which is also the
    /// setting's default. HCMv3 additionally accepts the chain's creator step (normally 1) for a submission,
    /// because that is the number an administrator reading the chain would type for "when the employee
    /// submits".
    /// </para>
    /// </summary>
    public static bool IsConfiguredStep(int configuredStep, WorkflowStepAction action, int step)
    {
        if (action == WorkflowStepAction.Submitted)
        {
            return configuredStep <= 0 || (step > 0 && configuredStep == step);
        }

        return configuredStep > 0 && configuredStep == step;
    }

    /// <summary>Only a new leave: not an amendment, a cancellation or a return from leave.</summary>
    public static bool IsNewLeaveRequest(WorkflowRequest request)
    {
        return request != null
               && request.RequestType == LeaveWorkflowTransactionHandler.LeaveRequestType
               && !request.IsAmendment
               && request.RequestAction == WorkflowRequestAction.New;
    }

    [UnitOfWork]
    public virtual async Task<bool> IsTriggerPointAsync(WorkflowRequest leaveRequest, WorkflowStepAction action, int step)
    {
        if (!IsNewLeaveRequest(leaveRequest) || NonInteractiveScope.IsActive)
        {
            return false;
        }

        // Read straight off the table, not through the settings accessor: the accessor CREATES the tenant's
        // row when there is none, and this runs inside the leave action's own unit of work, where nothing
        // may be written (see IWorkflowTransactionHandler.OnStepActionAsync).
        var settings = await _selfServiceSettingsRepository.GetAll().FirstOrDefaultAsync();

        if (settings == null)
        {
            // No row yet, so nothing can be ruled out here: the accessor builds the first one from the
            // tenant's configuration. Decided after the commit, where creating it is safe.
            return true;
        }

        return settings.IsPassportWithdrawalRequired
               && IsConfiguredStep(settings.PassportWithdrawalSubmissionStep, action, step);
    }

    [UnitOfWork]
    public virtual async Task<LeavePassportWithdrawalDecision> DecideAsync(int leaveRequestId, WorkflowStepAction action, int step)
    {
        // Imports and background jobs never raise approval requests; see NonInteractiveScope.
        if (NonInteractiveScope.IsActive)
        {
            return LeavePassportWithdrawalDecision.Skip(LeavePassportWithdrawalSkipReason.NonInteractive);
        }

        var request = await _requestRepository.FirstOrDefaultAsync(r => r.Id == leaveRequestId);

        if (request == null || request.RequestType != LeaveWorkflowTransactionHandler.LeaveRequestType)
        {
            return LeavePassportWithdrawalDecision.Skip(LeavePassportWithdrawalSkipReason.NotALeaveRequest);
        }

        if (!IsNewLeaveRequest(request))
        {
            return LeavePassportWithdrawalDecision.Skip(LeavePassportWithdrawalSkipReason.NotANewLeave);
        }

        var settings = await _sysSettingsAccessor.GetSelfServiceSettingsAsync();

        if (settings == null || !settings.IsPassportWithdrawalRequired)
        {
            return LeavePassportWithdrawalDecision.Skip(LeavePassportWithdrawalSkipReason.SettingOff);
        }

        if (!IsConfiguredStep(settings.PassportWithdrawalSubmissionStep, action, step))
        {
            return LeavePassportWithdrawalDecision.Skip(LeavePassportWithdrawalSkipReason.NotTheConfiguredStep);
        }

        EmployeeAbsence leave = null;

        // The leave is in flight at every step but the last, which is exactly what ApprovedOnly hides.
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            if (request.TargetEntityId.HasValue)
            {
                var leaveId = request.TargetEntityId.Value;
                leave = await _leaveRepository.FirstOrDefaultAsync(a => a.Id == leaveId);
            }
        }

        if (leave == null)
        {
            return LeavePassportWithdrawalDecision.Skip(LeavePassportWithdrawalSkipReason.LeaveNotFound);
        }

        // Legacy's own condition (LeaveRequest.js 2814): only a leave the employee is travelling on.
        if (!leave.IsTravelling)
        {
            return LeavePassportWithdrawalDecision.Skip(LeavePassportWithdrawalSkipReason.NotTravelling);
        }

        if (await HasBeenRaisedForAsync(leave.Id))
        {
            return LeavePassportWithdrawalDecision.Skip(LeavePassportWithdrawalSkipReason.AlreadyRaised);
        }

        // "A normal request in every respect" needs a chain to go into and a list to appear in. With either
        // switched off the row would be written as immediately effective, which nobody approved.
        var passportType = await _requestMasterResolver.GetTransactionTypeAsync(
            PassportWithdrawalWorkflowTransactionHandler.PassportWithdrawalRequestType);

        if (passportType == null || !passportType.IsActive || !passportType.IsWorkflowEnabled || !passportType.IsEssEnabled)
        {
            return LeavePassportWithdrawalDecision.Skip(LeavePassportWithdrawalSkipReason.PassportWorkflowOff);
        }

        // Legacy: the document type with code "2" (WorkflowServiceApiController 582-590), a tenant config
        // value here. Self-service treats "no such type" as "any document"; there is no form here to choose
        // one on, so there is nothing to withdraw.
        var documentTypeId = await _withdrawableDocumentTypeResolver.ResolveDocumentTypeIdAsync();

        if (!documentTypeId.HasValue)
        {
            return LeavePassportWithdrawalDecision.Skip(LeavePassportWithdrawalSkipReason.NoWithdrawableDocumentType);
        }

        int? documentId;

        // Only a document in effect, as self-service requires. Enabled explicitly: a caller may have the
        // filter off to see the pending leave.
        using (_unitOfWorkManager.Current.EnableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            // Legacy took "the active one". There is no active flag here, so the one valid longest stands
            // in for it: a renewed passport outranks the expired one it replaced.
            documentId = await _documentRepository.GetAll()
                .Where(d => d.EmployeeId == leave.EmployeeId && d.DocumentTypeId == documentTypeId.Value)
                .OrderByDescending(d => d.ExpiryDate)
                .ThenByDescending(d => d.Id)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();
        }

        if (!documentId.HasValue)
        {
            return LeavePassportWithdrawalDecision.Skip(LeavePassportWithdrawalSkipReason.NoDocumentOnFile);
        }

        var absenceType = await _absenceTypeRepository.GetAll()
            .Where(t => t.Id == leave.AbsenceTypeId)
            .Select(t => new { t.NameEn, t.NameAr })
            .FirstOrDefaultAsync();

        var employeeUserId = await _employeeRepository.GetAll()
            .Where(e => e.Id == leave.EmployeeId)
            .Select(e => e.UserId)
            .FirstOrDefaultAsync();

        var reason = absenceType == null
            ? null
            : absenceType.NameEn.IsNullOrWhiteSpace() ? absenceType.NameAr : absenceType.NameEn;

        return new LeavePassportWithdrawalDecision
        {
            SkipReason = LeavePassportWithdrawalSkipReason.None,
            TenantId = request.TenantId,
            LeaveId = leave.Id,
            LeaveRequestNumber = request.RequestNumber.IsNullOrWhiteSpace() ? "#" + leave.Id : request.RequestNumber,
            EmployeeId = leave.EmployeeId,
            EmployeeUserId = employeeUserId,
            EmployeeDocumentId = documentId.Value,
            DocumentTypeId = documentTypeId.Value,

            // Legacy: withdrawal the day before the leave starts, return the day after it ends
            // (LeaveRequest.js 2888-2893, WorkflowServiceApiController 531-535).
            WithdrawDate = leave.StartDate.Date.AddDays(-1),
            ReturnDate = leave.EndDate.Date.AddDays(1),

            // Legacy: the reason is the leave type.
            WithdrawalReason = reason?.Trim().Truncate(EmployeeDocumentMvmt.MaxWithdrawalReasonLength),
            LeaveComments = leave.Comments
        };
    }

    [UnitOfWork]
    public virtual async Task<int?> RaiseIfRequiredAsync(int leaveRequestId, WorkflowStepAction action, int step)
    {
        var decision = await DecideAsync(leaveRequestId, action, step);

        if (!decision.ShouldRaise)
        {
            Logger.Info(
                $"No passport withdrawal was raised from leave request {leaveRequestId} at step {step} ({action}): {decision.SkipReason}.");

            return null;
        }

        var note = L(RaisedFromLeaveNoteKey, decision.LeaveRequestNumber);

        var movement = new EmployeeDocumentMvmt
        {
            TenantId = decision.TenantId,
            EmployeeId = decision.EmployeeId,
            EmployeeDocumentId = decision.EmployeeDocumentId,
            WithdrawDate = decision.WithdrawDate,
            ReturnDate = decision.ReturnDate,

            // Not back yet; see EmployeeDocumentMvmt.ActualReturnDate.
            ActualReturnDate = default,

            // Legacy sent no guarantor.
            GuarantorId = null,

            WithdrawalReason = decision.WithdrawalReason,

            // Says where it came from, then what the employee wrote on the leave -- legacy copied the
            // leave's comment across. The engine carries this onto the request log's first entry.
            Comments = (decision.LeaveComments.IsNullOrWhiteSpace() ? note : note + " -- " + decision.LeaveComments.Trim())
                .Truncate(EmployeeDocumentMvmt.MaxCommentsLength),

            SourceEmployeeAbsenceId = decision.LeaveId
        };

        // Raised AS the employee, whoever clicked. Legacy submitted the passport request under the leave
        // employee's login even when an approver's approval was what triggered it
        // (PassportWithdrawalRequestController 328-329, WorkflowServiceApiController 548), and the chain's
        // creator rule ("employees may raise this for themselves") is evaluated from the session. An
        // employee with no login leaves the session without a user, which that rule treats as unrestricted
        // and the submitter resolves to the employee the row is about.
        using (_abpSession.Use(decision.TenantId, decision.EmployeeUserId))
        {
            // Resolved here rather than injected: the submitter depends on the approval service, which
            // depends on every handler, one of which depends on this.
            using (var submitter = _iocResolver.ResolveAsDisposable<IAdminTransactionSubmitter>())
            {
                var movementId = await submitter.Object.CreateAndSubmitAsync(
                    _movementRepository,
                    movement,
                    PassportWithdrawalWorkflowTransactionHandler.PassportWithdrawalRequestType,
                    RequestNumberPrefix,
                    // Routed by document type, as a self-service withdrawal is.
                    decision.DocumentTypeId);

                Logger.Info(
                    $"Passport withdrawal {movementId} was raised automatically from leave request {decision.LeaveRequestNumber} at step {step} ({action}).");

                return movementId;
            }
        }
    }

    public void RaiseAfterCommit(int leaveRequestId, WorkflowStepAction action, int step)
    {
        var current = _unitOfWorkManager.Current;

        if (current == null)
        {
            // Nothing in flight to wait for.
            RaiseDetached(leaveRequestId, action, step);
            return;
        }

        // After the commit, so the leave action is already safe whatever happens next, and so the new unit
        // of work reads the leave as it now stands instead of waiting on rows this one still holds.
        current.Completed += (sender, args) => RaiseDetached(leaveRequestId, action, step);
    }

    /// <summary>
    /// The raise in a unit of work of its own, with every failure caught. There is nowhere to await from a
    /// unit-of-work event, so this bridges with <see cref="AsyncHelper.RunSync(Func{Task})"/>, as ABP does.
    /// </summary>
    private void RaiseDetached(int leaveRequestId, WorkflowStepAction action, int step)
    {
        try
        {
            AsyncHelper.RunSync(async () =>
            {
                // RequiresNew: the unit of work that has just completed is still the ambient one here, and
                // joining it would write into a transaction that is already over.
                using (var unitOfWork = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
                {
                    await RaiseIfRequiredAsync(leaveRequestId, action, step);

                    await unitOfWork.CompleteAsync();
                }
            });
        }
        catch (Exception exception)
        {
            Logger.Warn(
                $"The leave request {leaveRequestId} was processed, but the passport withdrawal that should follow it at step {step} ({action}) could not be raised.",
                exception);
        }
    }

    /// <summary>
    /// Whether a withdrawal has EVER been raised from this leave. Soft-deleted rows count -- a request the
    /// employee withdrew, or a movement an extension replaced -- and so do pending ones, so a send-back and
    /// a second approval at the same step raise nothing new.
    /// </summary>
    private async Task<bool> HasBeenRaisedForAsync(int leaveId)
    {
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly, AbpDataFilters.SoftDelete))
        {
            return await _movementRepository.GetAll().AnyAsync(m => m.SourceEmployeeAbsenceId == leaveId);
        }
    }
}
