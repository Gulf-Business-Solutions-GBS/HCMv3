using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service passport (and other document) withdrawal: an employee asking for one of their own documents
/// out of the company's custody, and the record of when it is due back.
///
/// <para>
/// Legacy split this in two -- a <c>PassportWithdrawalRequest</c> row that went through approval, and an
/// <c>EmployeeDocumentMvmt</c> row copied out of it at the last approval step. There is one row here, so
/// there is no copy step to fail halfway and no way for the two to disagree about who is holding the
/// passport.
/// </para>
/// </summary>
public interface IMyPassportWithdrawalAppService : IApplicationService
{
    Task<PagedResultDto<MyPassportWithdrawalDto>> GetMyPassportWithdrawals(GetMyRequestsInput input);

    /// <summary>Creates the record and, when the type is workflow-enabled, submits it in one step.</summary>
    Task<MyRequestSubmissionDto> RequestPassportWithdrawal(RequestPassportWithdrawalInput input);

    /// <summary>Withdraws the request while it is still theirs to withdraw.</summary>
    Task CancelMyPassportWithdrawal(EntityDto input);
}
