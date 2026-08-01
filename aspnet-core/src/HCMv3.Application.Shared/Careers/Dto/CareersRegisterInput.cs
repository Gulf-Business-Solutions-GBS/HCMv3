using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.Careers.Dto;

/// <summary>
/// Anonymous candidate self-registration input for the careers portal.
///
/// DEVIATION FROM LEGACY (deliberate): legacy <c>AccountController.Register</c>
/// (<c>HCM10.Recruiting/Controllers/AccountController.cs:210-316</c>) took a raw <c>FormCollection</c> and
/// created a fully populated <see cref="Recruitment.Applicant"/> in one anonymous POST -- names, birth date,
/// gender, civil ID, mobile and nationality. Civil ID in particular is national-identity PII being accepted
/// from an unauthenticated caller with no verification, and it was also the natural duplicate key.
/// Registration here collects only what an identity needs; the rest of the profile is filled in after login
/// on the authenticated candidate profile surface.
/// </summary>
public class CareersRegisterInput
{
    /// <summary>
    /// Doubles as the ABP user name. Legacy also used the email as the user name
    /// (<c>AccountController.cs:290-292</c>) so this is behaviour-compatible.
    /// </summary>
    [Required]
    [StringLength(256)]
    [EmailAddress]
    public string EmailAddress { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string FirstNameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string LastNameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string FirstNameAr { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string LastNameAr { get; set; }

    /// <summary>
    /// Validated against the tenant's ASP.NET Identity password policy by ABP's
    /// <c>UserManager.CreateAsync</c>. No length/complexity attribute is declared here on purpose so the
    /// tenant-configured policy stays the single source of truth.
    /// </summary>
    [Required]
    [StringLength(128)]
    public string Password { get; set; }

    /// <summary>
    /// Google reCAPTCHA v3 response token. Required whenever the tenant has
    /// <c>App.UserManagement.UseCaptchaOnRegistration</c> enabled (the default).
    /// </summary>
    public string CaptchaResponse { get; set; }
}
