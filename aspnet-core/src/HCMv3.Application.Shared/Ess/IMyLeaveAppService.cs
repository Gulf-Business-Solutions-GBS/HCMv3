using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service leave: an employee's own balances, their own requests, and applying for leave.
///
/// <para>
/// Deliberately NOT the admin <c>IEmployeeAbsencesAppService</c> with a filter bolted on. That service must
/// let HR pick any employee, backdate, and bypass approval; this one must do none of those. Reusing it and
/// passing a scope would mean the same endpoint enforcing two different rule sets -- which is how the
/// legacy ESS project ended up as a parallel implementation in the first place. Both sit on the same entity
/// and the same domain services; only the surface differs.
/// </para>
/// </summary>
public interface IMyLeaveAppService : IApplicationService
{
    /// <summary>Balances for every type the employee holds quota for, including what is bookable.</summary>
    Task<List<MyLeaveBalanceDto>> GetMyBalances();

    Task<PagedResultDto<MyLeaveDto>> GetMyLeaves(GetMyLeavesInput input);

    /// <summary>
    /// Creates the leave and, when the type is workflow-enabled, submits it for approval in one step. The
    /// employee never sees a draft they have to remember to submit.
    /// </summary>
    Task<ApplyForLeaveResultDto> ApplyForLeave(ApplyForLeaveInput input);

    /// <summary>Withdraws a request the employee raised, while it is still theirs to withdraw.</summary>
    Task CancelMyLeave(EntityDto input);

    /// <summary>
    /// Asks for a change to a leave that has already been approved, routing it through the same approval
    /// chain the original went through.
    ///
    /// <para>
    /// This is the path that exists so nobody needs the other one. Without it, changing an approved leave
    /// means editing the row directly -- which takes effect immediately, with no approval and no record that
    /// anyone agreed. The approved leave stays exactly as it is until this request is granted.
    /// </para>
    /// </summary>
    /// <returns>Id of the amendment's <c>WorkflowRequest</c>.</returns>
    Task<int> AmendMyLeave(AmendMyLeaveInput input);

    /// <summary>
    /// Reports the day the employee actually came back, and raises whatever that implies.
    ///
    /// <para>
    /// Coming back ON TIME stamps the return; coming back EARLY additionally pulls the end date in and keeps
    /// the original as <c>InitialEndDate</c>. Both go out as an amendment on the leave, through the same
    /// approval chain the leave itself went through, and neither touches the leave until it is granted.
    /// </para>
    /// <para>
    /// Coming back LATE is REFUSED, deliberately. Returning after the approved end date means more absence
    /// than anyone agreed to, and booking it here would take days out of a quota on nobody's authority. The
    /// employee is told to apply for the extra days as leave, which is the path that asks an approver.
    /// </para>
    /// </summary>
    Task<ReturnFromLeaveResultDto> ReturnFromMyLeave(ReturnFromLeaveInput input);
}
