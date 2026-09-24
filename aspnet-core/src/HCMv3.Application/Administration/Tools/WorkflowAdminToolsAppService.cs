using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using HCMv3.Administration.Tools.Dto;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.Administration.Tools;

/// <inheritdoc cref="IWorkflowAdminToolsAppService"/>
/// <remarks>
/// <para><b>DELIBERATE DEVIATION — the WinForms MessageBox is gone.</b> Legacy line 109 called
/// <c>System.Windows.Forms.MessageBox.Show(...)</c> from inside an ASP.NET callback to ask "the target
/// manager has no e-mail, proceed anyway?". On a web server that dialog opens on the SERVER (and in
/// practice blocks the request thread forever). This port simply proceeds and reports the missing address
/// in <see cref="AdminToolResultDto.Messages"/>; the confirmation belongs in the Angular screen.</para>
///
/// <para><b>DELIBERATE DEVIATION — notification dispatch not wired.</b> Legacy fired a
/// <c>SystemNotification</c> named "Transfer_Notification" plus an <c>EmailService.SendEmail</c> to the
/// receiving manager (lines 142-156 and 188-210). That path depends on the legacy
/// <c>NotificationConfig</c>/<c>SystemNotification</c> pair, which this port models differently; the
/// transfer itself is the business operation and is ported faithfully, the courtesy notification is
/// reported as an outstanding gap rather than approximated.</para>
///
/// <para><b>DELIBERATE DEVIATION — permission instead of a username check</b> (legacy
/// <c>CheckIfAdminUser()</c>, line 76, compared against literal "Admin"/"SuperAdmin").</para>
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_Administration_Tools)]
public class WorkflowAdminToolsAppService : HCMv3AppServiceBase, IWorkflowAdminToolsAppService
{
    /// <summary>Legacy line 139 wrote this exact comment onto every generated RequestLog row.</summary>
    private const string TransferComment = "This is transfered by our utility ";

    private readonly IRepository<WorkFlowApprover> _workFlowApproverRepository;
    private readonly IRepository<RequestLog> _requestLogRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<RequestMaster> _requestMasterRepository;
    private readonly IRepository<RequestDetail> _requestDetailRepository;

    public WorkflowAdminToolsAppService(
        IRepository<WorkFlowApprover> workFlowApproverRepository,
        IRepository<RequestLog> requestLogRepository,
        IRepository<Employee> employeeRepository,
        IRepository<RequestMaster> requestMasterRepository,
        IRepository<RequestDetail> requestDetailRepository)
    {
        _workFlowApproverRepository = workFlowApproverRepository;
        _requestLogRepository = requestLogRepository;
        _employeeRepository = employeeRepository;
        _requestMasterRepository = requestMasterRepository;
        _requestDetailRepository = requestDetailRepository;
    }

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_Administration_Tools_TransferApprovals)]
    public async Task<AdminToolResultDto> TransferPendingApprovals(TransferApprovalsInput input)
    {
        var result = new AdminToolResultDto();

        if (input.FromApproverEmployeeId == input.ToApproverEmployeeId)
        {
            throw new UserFriendlyException(L("TransferFromAndToManagerMustDiffer"));
        }

        var fromManager = await _employeeRepository.FirstOrDefaultAsync(input.FromApproverEmployeeId);
        if (fromManager == null)
        {
            throw new UserFriendlyException(L("TransferFromManagerNotFound"));
        }

        var toManager = await _employeeRepository.FirstOrDefaultAsync(input.ToApproverEmployeeId);
        if (toManager == null)
        {
            throw new UserFriendlyException(L("TransferToManagerNotFound"));
        }

        // Legacy lines 107-115 warned (via MessageBox) when the receiving manager had no e-mail. Reported
        // instead of blocking -- see class remarks.
        if (string.IsNullOrWhiteSpace(toManager.Email))
        {
            result.Messages.Add(L("TransferToManagerHasNoEmail"));
        }

        // Legacy lines 118-120 / 162-164: only OPEN requests whose approver row is not yet approved. The
        // only difference between legacy's two branches is the optional requesting-employee filter.
        var query = _workFlowApproverRepository.GetAll()
            .Where(r => r.ApproverId == fromManager.Id
                        && !r.IsApproved
                        && r.WorkflowStep == r.WorkflowRequest.WorkflowStep
                        && r.WorkflowRequest.RequestStatus == RequestStatus.Open);

        if (input.RequestingEmployeeId.HasValue)
        {
            query = query.Where(r => r.WorkflowRequest.EmployeeId == input.RequestingEmployeeId.Value);
        }

        var approverRows = query.ToList();

        // Legacy lines 122-124: the log's ActionBy is the currently logged-in user's employee record,
        // falling back to the request's own employee when the operator is not linked to an employee.
        var currentEmployeeId = await GetCurrentUserEmployeeIdOrNullAsync();

        foreach (var approverRow in approverRows)
        {
            var workflowStep = approverRow.WorkflowRequest?.WorkflowStep ?? 0;
            var requestEmployeeId = approverRow.WorkflowRequest?.EmployeeId;

            approverRow.ApproverId = toManager.Id;
            approverRow.Approver = null;
            await _workFlowApproverRepository.UpdateAsync(approverRow);

            await _requestLogRepository.InsertAsync(new RequestLog
            {
                TenantId = AbpSession.GetTenantId(),
                WorkflowRequestId = approverRow.WorkflowRequestId,
                ActionById = currentEmployeeId ?? requestEmployeeId,
                ActionDateTime = Clock.Now,
                ActionType = RequestActionType.Transferred,
                Platform = WorkflowPlatform.Core,
                WorkflowStep = workflowStep,
                // Legacy line 138 hardcodes 0 here (its CalculateActionDays call is commented out).
                ActionDays = 0,
                RequestActionComment = TransferComment
            });

            result.AffectedCount++;
        }

        return result;
    }

    /// <summary>
    /// Legacy <c>uscCopyWorkflowSetup.ASPxCallbackPanel1_Callback</c> (lines 89-187).
    ///
    /// <para><b>Field-by-field parity.</b> Legacy copies exactly these <c>RequestMaster</c> fields onto the
    /// clone (lines 130-133): <c>WorkflowSteps</c>, <c>AutoClose</c>, <c>AutoCloseInDays</c>,
    /// <c>SecurityLevel</c> -- and NOT <c>DefaultApprover</c>, <c>IsCommentRequire</c> or <c>DueInDays</c>.
    /// That selective copy is preserved verbatim; widening it would silently change cloned workflows.</para>
    ///
    /// <para><b>RequestDetail parity</b> (legacy lines 141-160): Step, Person, ApproverPerson,
    /// EnableReadLog, WorkflowRole, WorkflowRoleGroup, WorkflowRoleOrg, ShouldEscalate, EscalateAfterDays,
    /// EscalateWorkflowRole, EscalateWorkflowRoleGroup, EscalateWorkflowRoleOrg. Legacy also copied
    /// <c>NotificationConfiguration</c> (line 159); <b>this port's <c>RequestDetail</c> has no such
    /// property</b> (the legacy <c>NotificationConfig</c> entity was not ported), so that one field is
    /// dropped. <b>Reported gap.</b> Fields that exist here but not on the legacy copy list
    /// (<c>CanSendBack</c>, <c>CanReject</c>, <c>ShowPayment</c>, <c>ApprovalRequire</c>,
    /// <c>SignatoryStep</c>, <c>ReminderDays</c>, <c>IsCommentRequire</c>, <c>RequestDetailParallels</c>)
    /// are deliberately left at their defaults, matching legacy.</para>
    ///
    /// <para>Legacy's four validation messages (blank code / blank name / no source selected / duplicate
    /// code) become <c>UserFriendlyException</c>s. Legacy's message for the duplicate-code case is
    /// reachable only through a dead <c>else</c> branch (line 175 can never run, since it is guarded by
    /// <c>SrcRequestID != 0</c> which was already checked at line 111); the duplicate check itself is at
    /// line 114 and IS ported.</para>
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_Administration_Tools_CopyWorkflowSetup)]
    public async Task<AdminToolResultDto> CopyWorkflowSetup(CopyWorkflowSetupInput input)
    {
        var result = new AdminToolResultDto();

        // Legacy lines 99-107.
        var newCode = (input.NewRequestCode ?? string.Empty).Trim();
        if (newCode.Length == 0)
        {
            throw new UserFriendlyException(L("CopyWorkflowSetupRequestCodeRequired"));
        }

        if (string.IsNullOrWhiteSpace(input.NewRequestNameEn))
        {
            throw new UserFriendlyException(L("CopyWorkflowSetupRequestNameRequired"));
        }

        if (input.SourceRequestMasterId == 0)
        {
            throw new UserFriendlyException(L("CopyWorkflowSetupSourceRequestRequired"));
        }

        // Legacy line 113-114: the target code must not already exist.
        if (_requestMasterRepository.GetAll().Any(r => r.Code == newCode))
        {
            throw new UserFriendlyException(L("CopyWorkflowSetupRequestCodeAlreadyExists"));
        }

        // Legacy line 116-117.
        var sourceMaster = await _requestMasterRepository.FirstOrDefaultAsync(input.SourceRequestMasterId);
        if (sourceMaster == null)
        {
            throw new UserFriendlyException(L("CopyWorkflowSetupSourceRequestMasterNotFound"));
        }

        // Legacy lines 119-120: a source with no step rows is rejected outright -- nothing is created.
        var sourceDetails = _requestDetailRepository.GetAll()
            .Where(r => r.RequestMasterId == sourceMaster.Id)
            .OrderBy(r => r.Step)
            .ToList();

        if (sourceDetails.Count == 0)
        {
            throw new UserFriendlyException(L("CopyWorkflowSetupSourceRequestHasNoDetail"));
        }

        // Legacy lines 123-136.
        var newMaster = new RequestMaster
        {
            TenantId = AbpSession.GetTenantId(),
            Code = newCode,
            NameEn = input.NewRequestNameEn,
            // Not in legacy -- NameAr is [Required] in this port; see CopyWorkflowSetupInput.
            NameAr = string.IsNullOrWhiteSpace(input.NewRequestNameAr)
                ? input.NewRequestNameEn
                : input.NewRequestNameAr,
            WorkflowSteps = sourceMaster.WorkflowSteps,
            AutoClose = sourceMaster.AutoClose,
            AutoCloseInDays = sourceMaster.AutoCloseInDays,
            SecurityLevel = sourceMaster.SecurityLevel
        };

        // InsertAndGetIdAsync, never InsertAsync: every cloned RequestDetail needs this id.
        var newMasterId = await _requestMasterRepository.InsertAndGetIdAsync(newMaster);

        // Legacy lines 139-161.
        foreach (var sourceDetail in sourceDetails)
        {
            await _requestDetailRepository.InsertAsync(new RequestDetail
            {
                TenantId = AbpSession.GetTenantId(),
                RequestMasterId = newMasterId,
                Step = sourceDetail.Step,
                Person = sourceDetail.Person,
                ApproverPerson = sourceDetail.ApproverPerson,
                EnableReadLog = sourceDetail.EnableReadLog,
                WorkflowRoleId = sourceDetail.WorkflowRoleId,
                WorkflowRoleGroup = sourceDetail.WorkflowRoleGroup,
                WorkflowRoleOrgId = sourceDetail.WorkflowRoleOrgId,
                ShouldEscalate = sourceDetail.ShouldEscalate,
                EscalateAfterDays = sourceDetail.EscalateAfterDays,
                EscalateWorkflowRoleId = sourceDetail.EscalateWorkflowRoleId,
                EscalateWorkflowRoleGroup = sourceDetail.EscalateWorkflowRoleGroup,
                EscalateWorkflowRoleOrgId = sourceDetail.EscalateWorkflowRoleOrgId
                // NotificationConfiguration (legacy line 159) has no counterpart here -- reported gap.
            });

            result.AffectedCount++;
        }

        // Legacy line 163.
        result.Messages.Add(L("CopyWorkflowSetupSucceeded"));

        return result;
    }

    /// <summary>
    /// Legacy resolved this as <c>Employee.LoginID == SecuritySystem.CurrentUserName</c> (line 123).
    /// </summary>
    private async Task<int?> GetCurrentUserEmployeeIdOrNullAsync()
    {
        var user = await GetCurrentUserAsync();
        var userName = user.UserName;

        return _employeeRepository.GetAll()
            .Where(r => r.LoginID == userName)
            .Select(r => (int?)r.Id)
            .FirstOrDefault();
    }
}
