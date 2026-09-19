using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Workflow.Services;

/// <inheritdoc cref="IWorkflowDelegationResolver"/>
///
/// <remarks>
/// <para>
/// <b>A delegation READ goes through the ordinary filtered repository, and that is the point.</b>
/// <see cref="EmployeeDelegation"/> is an <see cref="IWorkflowEnabledEntity"/>, so the <c>ApprovedOnly</c>
/// filter is already deciding, on every query below, whether a delegation is real. A row still sitting in an
/// approver's inbox grants nothing; a rejected one grants nothing; an <c>Approved</c> one grants everything
/// described here. Nothing in this class disables that filter, which is why "asking for cover" and "having
/// cover" stay two different states.
/// </para>
/// <para>
/// A null <c>WorkflowStatus</c> also passes, and that is correct rather than a loophole: it means the row was
/// never workflow-managed -- HR typed it in directly, or it predates delegation routing being switched on --
/// and every such row behaved as real cover before this feature existed.
/// </para>
///
/// <para>
/// NOTE on navigation properties: <see cref="EmployeeDelegation.DelegateTo"/> is resolved via a second flat-FK
/// lookup against <see cref="Employee"/> rather than the navigation property itself. This Core-layer domain
/// service has no EF Core <c>Include()</c> available (same reason as <c>EmailService</c>), and without eager
/// loading or an already-tracked instance the navigation would come back null.
/// </para>
/// </remarks>
public class WorkflowDelegationResolver : HCMv3DomainServiceBase, IWorkflowDelegationResolver
{
    private readonly IRepository<EmployeeDelegation> _employeeDelegationRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public WorkflowDelegationResolver(
        IRepository<EmployeeDelegation> employeeDelegationRepository,
        IRepository<Employee> employeeRepository)
    {
        _employeeDelegationRepository = employeeDelegationRepository;
        _employeeRepository = employeeRepository;
    }

    public int? ResolveDelegateFor(int employeeId)
    {
        if (employeeId <= 0)
        {
            return null;
        }

        var today = DateTime.Now.Date;

        // ORDERING IS DELIBERATE. Nothing in the schema stops two delegations covering the same day for the
        // same person -- the app services and the workflow handler both refuse an overlap, but rows created
        // before those rules existed, or imported, can still overlap. The previous reader of this table took
        // FirstOrDefault with no ordering at all, so which colleague held somebody's authority depended on
        // the query plan. Most recently created wins: the last thing a person said about their own cover is
        // the thing they meant.
        var delegateToId = _employeeDelegationRepository.GetAll()
            .Where(d => d.EmployeeId == employeeId
                        // A delegation to yourself is a no-op, not a loop. Refused on creation now, but old
                        // rows exist; treating it as cover would take an employee's own queue away from them
                        // and hand it straight back, which reads as an outage.
                        && d.DelegateToId != d.EmployeeId
                        && d.StartDate.Date <= today
                        && d.EndDate.Date >= today)
            .OrderByDescending(d => d.CreationTime)
            .ThenByDescending(d => d.Id)
            .Select(d => (int?)d.DelegateToId)
            .FirstOrDefault();

        if (delegateToId == null)
        {
            return null;
        }

        // SAFETY VALVE -- and only this one. It does not soften the replacement rule below; it stops the
        // rule producing a request that literally nobody can approve. Substituting to a leaver (or to an
        // employee row that has since gone) would remove the delegator AND put an unreachable person in
        // their place, which is strictly worse than the stalling this feature exists to fix.
        var delegateEmployee = _employeeRepository.GetAll()
            .Where(e => e.Id == delegateToId.Value)
            .Select(e => new { e.Id, e.Status })
            .FirstOrDefault();

        if (delegateEmployee == null || delegateEmployee.Status == EmployeeStatus.Inactive)
        {
            return null;
        }

        return delegateToId;
    }

    public bool HasActiveDelegationTo(int employeeId)
    {
        if (employeeId <= 0)
        {
            return false;
        }

        var today = DateTime.Now.Date;

        return _employeeDelegationRepository.GetAll()
            .Any(d => d.DelegateToId == employeeId
                      && d.DelegateToId != d.EmployeeId
                      && d.StartDate.Date <= today
                      && d.EndDate.Date >= today);
    }

    public List<int> ApplySubstitution(IEnumerable<int> candidateEmployeeIds)
    {
        var result = new List<int>();

        if (candidateEmployeeIds == null)
        {
            return result;
        }

        foreach (var candidateId in candidateEmployeeIds)
        {
            // DELIBERATE DIVERGENCE FROM LEGACY -- do not "fix" this back.
            //
            // Legacy (WorkFlowServices.GetDelegatorEmployeeByOid, line ~4511) ADDED the delegate as an extra
            // valid approver and left the delegator able to act as well. This REPLACES them: while cover is
            // running, the person who arranged the cover is out of the candidate list and their stand-in is
            // in it.
            //
            // The trade was made knowingly. Replacement means a delegation with wrong dates, or an approver
            // who comes back early, locks that approver out of their own queue until somebody edits the
            // delegation row. The alternative -- both people able to act -- means "I have arranged cover"
            // carries no guarantee that the cover is what actually happens, and two people each believing
            // the other has it is precisely how a request sits untouched for a fortnight.
            var effectiveId = ResolveDelegateFor(candidateId) ?? candidateId;

            if (!result.Contains(effectiveId))
            {
                result.Add(effectiveId);
            }
        }

        return result;
    }

    public IReadOnlyDictionary<int, int> BuildSubstitutionMap(IEnumerable<int> candidateEmployeeIds)
    {
        var map = new Dictionary<int, int>();

        if (candidateEmployeeIds == null)
        {
            return map;
        }

        foreach (var candidateId in candidateEmployeeIds)
        {
            var delegateId = ResolveDelegateFor(candidateId);

            // First delegator wins when two of them nominated the same stand-in for the same step. The map
            // exists to name somebody in the audit line, and naming the first of the two is honest in a way
            // that naming neither is not.
            if (delegateId.HasValue && !map.ContainsKey(delegateId.Value))
            {
                map[delegateId.Value] = candidateId;
            }
        }

        return map;
    }
}
