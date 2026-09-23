using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.SystemSecurity.Services;

/// <inheritdoc cref="INotificationRecipientResolver"/>
[UnitOfWork]
public class NotificationRecipientResolver : HCMv3DomainServiceBase, INotificationRecipientResolver
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<NotificationGroup> _notificationGroupRepository;
    private readonly IRepository<WorkFlowApprover> _workFlowApproverRepository;
    private readonly IRepository<RequestLog> _requestLogRepository;
    private readonly IWorkflowDelegationResolver _delegationResolver;
    private readonly IWorkflowApproverResolver _approverResolver;

    public NotificationRecipientResolver(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<NotificationGroup> notificationGroupRepository,
        IRepository<WorkFlowApprover> workFlowApproverRepository,
        IRepository<RequestLog> requestLogRepository,
        IWorkflowDelegationResolver delegationResolver,
        IWorkflowApproverResolver approverResolver)
    {
        _employeeRepository = employeeRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _notificationGroupRepository = notificationGroupRepository;
        _workFlowApproverRepository = workFlowApproverRepository;
        _requestLogRepository = requestLogRepository;
        _delegationResolver = delegationResolver;
        _approverResolver = approverResolver;
    }

    public List<Employee> Resolve(NotificationConfig notificationConfig, object obj, string employeeCodeList = null)
    {
        var recipients = new List<Employee>();

        if (notificationConfig == null)
        {
            return recipients;
        }

        var taken = new HashSet<int>();
        var subject = ResolveSubjectEmployee(obj);
        var workflowRequest = obj as WorkflowRequest;

        void Take(int? employeeId)
        {
            if (employeeId == null || !taken.Add(employeeId.Value))
            {
                return;
            }

            var employee = ActiveEmployee(employeeId.Value);
            if (employee == null)
            {
                return;
            }

            recipients.Add(employee);

            // The stand-in is copied in, not swapped: somebody has to be able to act while the recipient is
            // away, but the recipient still wants to know what happened on their own request.
            var delegateToId = _delegationResolver.ResolveDelegateFor(employee.Id);
            if (delegateToId != null && taken.Add(delegateToId.Value))
            {
                var standIn = ActiveEmployee(delegateToId.Value);
                if (standIn != null)
                {
                    recipients.Add(standIn);
                }
            }
        }

        if (notificationConfig.ToEmployee)
        {
            Take(subject?.Id);
        }

        if (notificationConfig.ToRequestCreator)
        {
            Take(ResolveRequestCreatorId(obj));
        }

        if (notificationConfig.ToManager && subject != null)
        {
            Take(CurrentManagerId(subject.Id));
        }

        if (notificationConfig.SendToApprover && workflowRequest != null)
        {
            foreach (var approverId in PendingApproverIds(workflowRequest.Id))
            {
                Take(approverId);
            }
        }

        if (notificationConfig.SendToAllApproved && workflowRequest != null)
        {
            foreach (var approverId in AlreadyApprovedIds(workflowRequest.Id))
            {
                Take(approverId);
            }
        }

        foreach (var groupMemberId in GroupMemberIds(notificationConfig.Id, subject?.Id))
        {
            Take(groupMemberId);
        }

        if (!string.IsNullOrWhiteSpace(employeeCodeList))
        {
            foreach (var namedId in EmployeeIdsForCodes(employeeCodeList))
            {
                Take(namedId);
            }
        }

        if (notificationConfig.SendToAll)
        {
            foreach (var everyoneId in AllActiveEmployeeIds())
            {
                Take(everyoneId);
            }
        }

        return recipients;
    }

    /// <summary>
    /// Whoever the notification is ABOUT. An <see cref="Employee"/> stands for itself; anything else is asked
    /// for an <c>EmployeeId</c>, which is how a workflow request, a leave row or a loan names its subject.
    /// </summary>
    private Employee ResolveSubjectEmployee(object obj)
    {
        if (obj is Employee employee)
        {
            return employee;
        }

        var employeeId = ReadIntProperty(obj, "EmployeeId");

        return employeeId == null ? null : ActiveEmployee(employeeId.Value);
    }

    /// <summary>
    /// Who raised it. Legacy read a <c>SysCreator</c> employee id; this port's audited entities record
    /// <c>CreatorUserId</c> instead, so the employee behind that user is looked up — falling back to
    /// <c>SysCreator</c> for anything that still carries one.
    /// </summary>
    private int? ResolveRequestCreatorId(object obj)
    {
        var sysCreator = ReadIntProperty(obj, "SysCreator");
        if (sysCreator != null)
        {
            return sysCreator;
        }

        var creatorUserId = ReadLongProperty(obj, "CreatorUserId");
        if (creatorUserId == null)
        {
            return null;
        }

        return _employeeRepository.GetAll()
            .Where(e => e.UserId == creatorUserId)
            .Select(e => (int?)e.Id)
            .FirstOrDefault();
    }

    /// <summary>
    /// The manager in post today. Compares the columns directly rather than through <c>DateTime.Date</c>:
    /// these are date-only columns, and the <c>.Date</c> call translates to a SQL function whose result does
    /// not compare as a date on every provider — which silently finds nobody rather than failing.
    /// </summary>
    private int? CurrentManagerId(int employeeId)
    {
        var today = DateTime.Today;

        return _employeeManagerRepository.GetAll()
            .Where(m => m.EmployeeId == employeeId && m.StartDate <= today && m.EndDate >= today)
            .OrderByDescending(m => m.StartDate)
            .Select(m => (int?)m.ManagerId)
            .FirstOrDefault();
    }

    /// <summary>The people the request is sitting with right now.</summary>
    private List<int> PendingApproverIds(int workflowRequestId)
    {
        return _workFlowApproverRepository.GetAll()
            .Where(a => a.WorkflowRequestId == workflowRequestId && !a.IsApproved && a.ApproverId != null)
            .Select(a => a.ApproverId.Value)
            .Distinct()
            .ToList();
    }

    private List<int> AlreadyApprovedIds(int workflowRequestId)
    {
        return _requestLogRepository.GetAll()
            .Where(r => r.WorkflowRequestId == workflowRequestId
                        && r.ActionType == RequestActionType.Approved
                        && r.ActionById != null)
            .Select(r => r.ActionById.Value)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Everyone holding a role the configuration's groups name, narrowed to the subject's own part of the
    /// organisation when the group says so — resolved through the same method that routes the request, so the
    /// people told about a step and the people who can act on it are the same people.
    /// </summary>
    private List<int> GroupMemberIds(int notificationConfigId, int? subjectEmployeeId)
    {
        var groups = _notificationGroupRepository.GetAll()
            .Where(g => g.NotificationConfigId == notificationConfigId && g.WorkflowRoleId != null)
            .Select(g => new { RoleId = g.WorkflowRoleId.Value, g.WorkflowRoleGroup })
            .ToList();

        var memberIds = new List<int>();

        foreach (var group in groups)
        {
            memberIds.AddRange(_approverResolver.ResolveRoleHolders(
                group.RoleId,
                group.WorkflowRoleGroup,
                subjectEmployeeId));
        }

        return memberIds.Distinct().ToList();
    }

    private List<int> EmployeeIdsForCodes(string employeeCodeList)
    {
        var codes = employeeCodeList
            .Split(',')
            .Select(c => c.Trim())
            .Where(c => c.Length > 0)
            .ToList();

        return _employeeRepository.GetAll()
            .Where(e => codes.Contains(e.Code) && e.Status == EmployeeStatus.Active)
            .Select(e => e.Id)
            .ToList();
    }

    private List<int> AllActiveEmployeeIds()
    {
        return _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active)
            .Select(e => e.Id)
            .ToList();
    }

    /// <summary>
    /// A leaver is dropped, but somebody merely suspended is not: legacy told anyone who was not
    /// <c>Inactive</c>, and a suspended manager still has to know what their team is asking for.
    /// </summary>
    private Employee ActiveEmployee(int employeeId)
    {
        return _employeeRepository.GetAll()
            .FirstOrDefault(e => e.Id == employeeId && e.Status != EmployeeStatus.Inactive);
    }

    private static int? ReadIntProperty(object obj, string propertyName)
    {
        var value = obj?.GetType().GetProperty(propertyName)?.GetValue(obj);

        return value != null && int.TryParse(value.ToString(), out var parsed) && parsed != 0 ? parsed : (int?)null;
    }

    private static long? ReadLongProperty(object obj, string propertyName)
    {
        var value = obj?.GetType().GetProperty(propertyName)?.GetValue(obj);

        return value != null && long.TryParse(value.ToString(), out var parsed) && parsed != 0 ? parsed : (long?)null;
    }
}
