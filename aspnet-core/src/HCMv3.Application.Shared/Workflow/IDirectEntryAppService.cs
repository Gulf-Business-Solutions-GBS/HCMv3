using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;

namespace HCMv3.Workflow;

/// <summary>
/// Answers one question for the admin transaction screens: which transaction types may this user record
/// as final instead of sending for approval.
///
/// <para>
/// Separate from <see cref="IWorkflowTransactionTypesAppService"/> on purpose. That service is gated on
/// the catalog-administration permission, which the HR officers who actually record transactions do not
/// hold -- so a screen asking it "may I bypass this one?" would be refused for everybody who needs the
/// answer. This one is gated on the bypass permission itself, and exposes nothing beyond the codes.
/// </para>
/// </summary>
public interface IDirectEntryAppService : IApplicationService
{
    /// <summary>
    /// The <c>Code</c> of every transaction type the caller may record without approval: active,
    /// workflow-enabled, and flagged <c>AllowDirectEntry</c>.
    ///
    /// <para>
    /// Types with routing switched off are left out. They are already recorded immediately, so offering a
    /// "skip approval" choice there would suggest a decision the user does not have to make.
    /// </para>
    /// </summary>
    Task<List<string>> GetDirectEntryRequestTypes();
}
