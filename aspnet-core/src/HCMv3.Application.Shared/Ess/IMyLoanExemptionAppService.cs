using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service loan recovery exemptions: an employee asking to skip one month's instalment on a loan they
/// are already repaying.
///
/// <para>
/// Legacy routed this through a <c>LoanExemptionRequest</c> table that copied itself onto an
/// <c>EmployeeLoanExemption</c> at the final approval step. There is no request table here -- the exemption
/// row IS the request, hidden until approved -- so there is no copy step to fail halfway and no way for the
/// two to disagree about which months were skipped.
/// </para>
/// </summary>
public interface IMyLoanExemptionAppService : IApplicationService
{
    Task<PagedResultDto<MyLoanExemptionDto>> GetMyLoanExemptions(GetMyRequestsInput input);

    /// <summary>
    /// The caller's own loans, so the form can offer a list to pick from rather than asking for an id.
    /// </summary>
    Task<List<MyLoanForExemptionDto>> GetMyLoansForExemption();

    /// <summary>Creates the exemption and, when the type is workflow-enabled, submits it in one step.</summary>
    Task<MyRequestSubmissionDto> RequestLoanExemption(RequestLoanExemptionInput input);

    /// <summary>Withdraws the request while it is still theirs to withdraw.</summary>
    Task CancelMyLoanExemption(EntityDto input);
}
