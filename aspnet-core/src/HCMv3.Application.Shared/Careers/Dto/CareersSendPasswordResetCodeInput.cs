using System.ComponentModel.DataAnnotations;

namespace HCMv3.Careers.Dto;

/// <summary>
/// "Forgot password" input for the careers portal.
///
/// The endpoint that consumes this returns <c>void</c> and never signals whether the address matched an
/// account -- see <c>CareersAccountAppService.SendPasswordResetCode</c> for the enumeration-resistance
/// notes.
/// </summary>
public class CareersSendPasswordResetCodeInput
{
    [Required]
    [StringLength(256)]
    [EmailAddress]
    public string EmailAddress { get; set; }

    public string CaptchaResponse { get; set; }
}
