using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The rules a payment batch has to satisfy before anybody is asked to approve it, and the query that says
/// which wage types may be paid through one.
///
/// <para>
/// Both live here rather than inside the app service or the workflow handler because three entry points need
/// them: HR creating a batch, HR editing one that has not been submitted, and the handler re-checking at
/// submit time (which is the only check that still runs if a batch is ever created by an import). Three
/// copies would drift.
/// </para>
/// </summary>
public interface IEmployeePaymentBatchService
{
    /// <summary>
    /// Throws <see cref="Abp.UI.UserFriendlyException"/> unless the batch's lines are payable: there must be
    /// at least one, no employee may appear twice, and no amount may be zero.
    ///
    /// <para>
    /// <b>Legacy defined both of the interesting rules and then never ran them.</b>
    /// <c>PaymentRequestDetail.PerformValidation()</c> contains the duplicate-employee check and the
    /// amount-must-not-be-zero check, and no code path anywhere in HCM10 calls it -- <c>OnSaving</c> on that
    /// class does nothing but chain to <c>base</c>. So in production a batch could pay the same person twice
    /// in one document, or carry a zero line that produced an <c>EmployeePayment</c> worth nothing. Both are
    /// correct rules; they are enforced for real here.
    /// </para>
    ///
    /// <para>
    /// The empty-batch refusal is an addition. Legacy would happily route a batch with no lines, so an
    /// approver could be asked to approve nothing and the approval would create nothing -- the same defect
    /// <c>TimeLogApproval</c> refuses.
    /// </para>
    /// </summary>
    /// <param name="lines">
    /// The proposed lines. Entities rather than ids so this works identically before they are inserted (from
    /// the app service, on a graph EF has not saved yet) and after (from the handler, read back by batch id).
    /// </param>
    Task EnsureLinesArePayableAsync(IReadOnlyCollection<EmployeePaymentBatchDetail> lines);

    /// <summary>
    /// The wage types a payment batch may be paid under, as ids (ported from legacy
    /// <c>PaymentRequest.PaymentWageType</c>).
    ///
    /// <para>
    /// Legacy expressed this as a <c>DataSourceProperty</c> on the request object, which meant the rule only
    /// existed while a form was open: nothing checked it on save, and nothing else could ask the question.
    /// It is a query here so the app service can both offer the list AND refuse a batch that names a wage
    /// type outside it.
    /// </para>
    /// </summary>
    Task<List<int>> GetSelectableWageTypeIdsAsync();
}
