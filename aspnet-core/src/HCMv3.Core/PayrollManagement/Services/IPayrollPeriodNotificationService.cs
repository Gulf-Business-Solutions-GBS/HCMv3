using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Tells people that a payroll period has moved: locked for payroll, exited, or released for correction.
///
/// <para>
/// Legacy <c>PayrollControlRecordController</c> (<c>ReleaseForPayroll_Execute</c>, <c>ExitPayroll_Execute</c>,
/// <c>ReleaseForCorrection_Execute</c>) sent the payroll area's <c>PAReleaseNotification</c>,
/// <c>PAExitNotification</c> or <c>PACorrectionNotification</c> configuration after each action. This is that
/// send, reproduced as legacy did it -- including its quirks, which are pinned by tests rather than corrected
/// (see <see cref="PayrollPeriodNotificationService"/>).
/// </para>
///
/// <para>Never fails the state change: a notification that cannot be sent is logged and stepped over.</para>
/// </summary>
public interface IPayrollPeriodNotificationService : IDomainService
{
    /// <summary>
    /// Sends the configuration the record's payroll area names for <paramref name="state"/>, if any.
    /// <see cref="PayrollPeriodState.Open"/> has no legacy action and sends nothing.
    /// </summary>
    Task NotifyStateChangedAsync(PayrollControlRecord record, PayrollPeriodState state);
}
