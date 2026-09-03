using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Ess.Dto;

/// <summary>
/// Self-service mobile device registration DTOs -- the port of legacy <c>MobileAppDeviceRequest</c>'s form,
/// which the mobile app posted to <c>WorkflowServiceApiController</c>.
///
/// <para>
/// Kept in their own file rather than appended to <c>MyRequestDtos.cs</c>: that file is edited by every
/// self-service wave at once and has already lost classes to a bad merge.
/// </para>
/// </summary>
public class MyMobileDeviceDto : MyRequestDtoBase
{
    /// <summary>
    /// The handset identifier as the app reported it. Shown in full because it is the employee's own device
    /// and they may need to read it back to HR when a punch is refused.
    /// </summary>
    public string DeviceId { get; set; }

    public string DeviceType { get; set; }

    /// <summary>From when punches from this handset may be accepted -- never before approval, whatever this
    /// says.</summary>
    public DateTime EffectiveDate { get; set; }

    public bool IsActive { get; set; }

    public string Comments { get; set; }

    /// <summary>
    /// Whether the registration is usable right now: granted, active and in effect today. Computed rather
    /// than stored, so the screen can say "approved but not until March" instead of showing a green tick for
    /// a device that will refuse the next punch.
    /// </summary>
    public bool IsUsableToday { get; set; }
}

/// <summary>
/// "This is my phone -- let the app accept my attendance punches from it."
///
/// <para>
/// There is no employee field, on purpose: this row authorises punching AS somebody, so a form that named
/// its own employee would be a form that could authorise punching as anybody. The employee comes from the
/// session, which is also how the mobile app actually calls it.
/// </para>
/// </summary>
public class RegisterMyMobileDeviceInput
{
    /// <summary>
    /// The handset identifier. Required in earnest -- legacy compared it against <c>""</c>, which is false
    /// for null, so a registration for no device at all was accepted. <c>MinimumLength</c> makes the empty
    /// string fail here too, before the server-side rule has to.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string DeviceId { get; set; }

    /// <summary>What the handset is ("iPhone 14"), so HR can recognise it. Same legacy bug, same fix.</summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string DeviceType { get; set; }

    /// <summary>
    /// From when the device should be usable. Legacy defaulted this to today in <c>AfterConstruction</c>;
    /// the same default is applied server-side when the caller leaves it unset, because an unset date would
    /// otherwise read as year 1 and make the registration look eternally in effect.
    /// </summary>
    public DateTime? EffectiveDate { get; set; }

    [StringLength(1000)]
    public string Comments { get; set; }
}
