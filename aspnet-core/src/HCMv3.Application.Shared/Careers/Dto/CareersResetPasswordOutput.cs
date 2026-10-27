namespace HCMv3.Careers.Dto;

/// <summary>
/// Result of a completed careers-portal password reset.
/// </summary>
public class CareersResetPasswordOutput
{
    public bool CanLogin { get; set; }

    /// <summary>
    /// Returned so the portal can pre-fill the sign-in form. Safe to expose here: the caller has already
    /// proven possession of a reset code that was mailed to that account's own address.
    /// </summary>
    public string UserName { get; set; }
}
