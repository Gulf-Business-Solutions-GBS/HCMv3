using System;
using System.Threading.Tasks;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// What a registered mobile device means: whether a given handset may be registered at all, and whether a
/// given handset is allowed to punch for a given employee on a given day.
///
/// <para>
/// <b>This is the part legacy never built.</b> Legacy wrote <c>DeviceId</c> onto a request row and then never
/// read it back anywhere -- the only other occurrence of the name in the whole codebase is a duplicated
/// validation string in <c>WorkFlowServices.cs</c>. A registration nobody can query is a form that does
/// nothing, which is why this interface exists alongside the entity rather than being left for the screen
/// that consumes it.
/// </para>
///
/// <para>
/// Deliberately NOT called from <see cref="TimeEvent"/> or any existing punch path yet. Turning an
/// unanswered question into an enforced one is a behaviour change: every employee already punching from an
/// unregistered handset would be locked out on the day it shipped. The capability exists and is tested; the
/// mobile punch endpoint that asks it is a separate decision.
/// </para>
///
/// <para>
/// Both members live here rather than inside the workflow handler for the same reason the loan rules do:
/// HR's own screen, the self-service form and the approval handler must all reach the identical verdict, and
/// three copies of the rule would drift apart on the first change.
/// </para>
/// </summary>
public interface IMobileDeviceRegistrationService
{
    /// <summary>
    /// Whether <paramref name="deviceId"/> is registered, approved, active and in effect for
    /// <paramref name="employeeId"/> as at <paramref name="asOf"/> -- the question a mobile punch endpoint
    /// has to answer before accepting an attendance punch from a handset.
    ///
    /// <para>
    /// True requires all four: a row for that employee and device id, a workflow status that counts as
    /// granted (null for an HR-entered registration, Approved, or Closed), <c>IsActive</c>, and an
    /// <c>EffectiveDate</c> on or before <paramref name="asOf"/>. A pending registration, a rejected one, a
    /// deactivated one, a future-dated one, and another employee's device all read as false.
    /// </para>
    ///
    /// <para>
    /// The status is checked explicitly rather than relying on the <c>ApprovedOnly</c> filter being on. The
    /// filter can legitimately be disabled higher up the call stack -- self-service lists do exactly that --
    /// and an authorisation check whose answer depends on the caller's ambient filter state is one that will
    /// eventually say yes to a pending device.
    /// </para>
    /// </summary>
    /// <param name="employeeId">The employee the punch would be recorded against.</param>
    /// <param name="deviceId">The handset identifier the mobile app reported.</param>
    /// <param name="asOf">The moment of the punch. Only the date part is considered.</param>
    Task<bool> IsDeviceRegisteredAsync(int employeeId, string deviceId, DateTime asOf);

    /// <summary>
    /// Throws <see cref="Abp.UI.UserFriendlyException"/> unless the registration may be created: the device
    /// id and device type must be genuinely filled in, the employee must not already hold a live
    /// registration for the same device, and the device must not be live for somebody else.
    ///
    /// <para>
    /// Legacy's version of this compared against <c>""</c>, so a NULL device id passed validation and a
    /// registration for no device at all was recorded. Emptiness is judged after trimming here, which also
    /// catches the whitespace-only value a mobile form will happily post.
    /// </para>
    ///
    /// <para>
    /// The duplicate checks look PAST the <c>ApprovedOnly</c> filter, so a registration still sitting in an
    /// approver's inbox blocks a second one. Without that an employee who did not see their first submission
    /// land would file it again, each invisible to the other.
    /// </para>
    /// </summary>
    /// <param name="employeeId">The employee registering the handset.</param>
    /// <param name="deviceId">The handset identifier.</param>
    /// <param name="deviceType">Free-text description of the handset.</param>
    /// <param name="excludeDeviceRegistrationId">
    /// The row being checked, so a re-run against an already-saved registration does not make it compete
    /// with itself.
    /// </param>
    Task EnsureRegistrationIsAllowedAsync(
        int employeeId,
        string deviceId,
        string deviceType,
        int? excludeDeviceRegistrationId = null);
}
