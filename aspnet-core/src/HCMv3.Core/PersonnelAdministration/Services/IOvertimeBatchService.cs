using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The rules an <see cref="OvertimeBatch"/> must meet before anybody is asked to approve it, and the one place
/// that turns an approved batch into <see cref="EmployeeOvertime"/> rows.
///
/// <para>
/// Lives in Core because three callers need it: the screen creating a batch, the screen editing one that was
/// sent back, and the workflow handler (re-check at submit, post at approval). It is also called directly by
/// the screen when the batch type is not workflow-enabled -- otherwise a batch saved with workflow off would
/// never produce any overtime.
/// </para>
/// </summary>
public interface IOvertimeBatchService
{
    /// <summary>
    /// Throws <see cref="Abp.UI.UserFriendlyException"/> unless: there is at least one line (legacy "Select
    /// atleast one record!"); every line falls inside the batch's dates; no approved hours are negative; no
    /// employee and day appears twice; no day is already claimed by another live batch; and no day already has
    /// an overtime row (legacy <c>EmployeeOvertime.PerformValidations</c>, which made a legacy approval fail
    /// on such a day -- refused here at submit instead, before an approver is asked).
    /// </summary>
    /// <param name="batch">The header, for its dates and its own id (excluded from the "another batch" check).</param>
    /// <param name="lines">The lines, saved or not.</param>
    Task EnsureLinesAreValidAsync(OvertimeBatch batch, IReadOnlyCollection<OvertimeBatchDetail> lines);

    /// <summary>
    /// The (employee, day) pairs already on a line of a batch that still stands -- not routed, in an inbox, or
    /// approved. Legacy's loader hid exactly these days (requests Approved or Open) so the same day could not
    /// be put forward twice.
    /// </summary>
    /// <param name="excludeBatchId">The batch being edited, whose own lines do not count; 0 for none.</param>
    Task<HashSet<(int EmployeeId, System.DateTime Date)>> GetClaimedDaysAsync(
        IReadOnlyCollection<int> employeeIds,
        System.DateTime startDate,
        System.DateTime endDate,
        int excludeBatchId = 0);

    /// <summary>
    /// Creates one <see cref="EmployeeOvertime"/> per line that does not have one yet, exactly as legacy
    /// <c>OvertimeRequest.OnSaving</c> did on approval (lines 162-191): employee, effective date = line date,
    /// overtime and actual hours = round(TotalHours, 2), approved = round(ApprovedHours, 2), overtime type =
    /// the type with code "4". Idempotent: a line already posted is skipped.
    /// </summary>
    Task PostOvertimesAsync(OvertimeBatch batch);
}
