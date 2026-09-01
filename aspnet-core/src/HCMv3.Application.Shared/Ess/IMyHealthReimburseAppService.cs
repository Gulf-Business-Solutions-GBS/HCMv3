using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service health reimbursement: an employee's own medical claims, what they can still claim, and
/// filing a new one.
///
/// <para>
/// Deliberately not the admin service with a filter bolted on. That one lets HR pick any employee; this one
/// must not. Both sit on the same entity, the same quota service and the same workflow handler, so the
/// balance rules cannot diverge between the two -- only the surface differs.
/// </para>
/// </summary>
public interface IMyHealthReimburseAppService : IApplicationService
{
    Task<PagedResultDto<MyHealthReimburseDto>> GetMyHealthReimburses(GetMyRequestsInput input);

    /// <summary>
    /// What the employee is entitled to, has used, has pending, and can still claim -- answered BEFORE they
    /// file anything, which is the part legacy never offered.
    /// </summary>
    Task<MyHealthReimburseBalanceDto> GetMyHealthReimburseBalance(GetMyHealthReimburseBalanceInput input);

    /// <summary>Creates the claim and, when the type is workflow-enabled, submits it in one step.</summary>
    Task<MyRequestSubmissionDto> RequestHealthReimburse(RequestHealthReimburseInput input);

    /// <summary>Withdraws a claim while it is still theirs to withdraw.</summary>
    Task CancelMyHealthReimburse(EntityDto input);
}
