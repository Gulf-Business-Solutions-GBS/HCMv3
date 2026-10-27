using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.Careers.Dto;

/// <summary>
/// The fields a signed-in candidate may change on their OWN profile. Ported from legacy
/// <c>HCM10.Recruiting/Controllers/UserAccountController.UserProfile</c> POST
/// (<c>UserAccountController.cs:128-180</c>).
///
/// THERE IS NO APPLICANT ID ON THIS INPUT, ON PURPOSE. The row updated is resolved server-side from
/// <c>Applicant.UserId == AbpSession.UserId</c>. An id here would be the classic IDOR: <c>[AbpAuthorize]</c>
/// proves the caller is *a* candidate, never that they are *that* candidate.
///
/// DEVIATIONS FROM LEGACY (deliberate):
/// * No <c>Email</c> / <c>UserName</c>. Legacy rewrote both from this form
///   (<c>UserAccountController.cs:157-158</c>), and <c>Applicant.UserName</c> WAS the login credential --
///   so the profile form could take over another account's login name. Candidate identity is now the ABP
///   user; email/login changes belong to the account surface with the usual confirmation flow.
/// * No <c>Photo</c>. Legacy's <c>UpdateImage</c> (<c>:1218-1250</c>) accepted an arbitrary base64 data URL
///   with no type/size check and stored it on <c>Applicant.Photo</c>. Reported as a gap in this wave rather
///   than ported as-is.
/// </summary>
public class UpdateCareersProfileInput
{
    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string FirstNameEn { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string SecondNameEn { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string ThirdNameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string LastNameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string FirstNameAr { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string SecondNameAr { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string ThirdNameAr { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string LastNameAr { get; set; }

    public int? NationalityId { get; set; }

    public Gender Gender { get; set; }

    public DateTime BirthDate { get; set; }

    [Range(0, long.MaxValue)]
    public long CivilID { get; set; }

    [Range(0, long.MaxValue)]
    public long Mobile { get; set; }

    [Range(0, long.MaxValue)]
    public long Telephone { get; set; }

    [StringLength(1024)]
    public string CurrentAddress { get; set; }

    [StringLength(1024)]
    public string PermanentAddress { get; set; }
}
