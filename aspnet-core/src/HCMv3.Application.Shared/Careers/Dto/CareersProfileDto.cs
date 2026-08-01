using System;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.Careers.Dto;

/// <summary>
/// The signed-in candidate's OWN profile. Ported from what legacy
/// <c>HCM10.Recruiting/Controllers/UserAccountController.UserProfile</c> (GET, <c>:32-107</c>) put on the
/// ViewBag.
///
/// This DTO is only ever produced for the applicant row resolved from
/// <c>Applicant.UserId == AbpSession.UserId</c>; there is no endpoint anywhere on the careers surface that
/// renders it for an applicant id supplied by the client.
///
/// Deliberately EXCLUDED from this DTO although present on <c>HCMv3.Recruitment.Applicant</c>: internal
/// recruitment/onboarding state (<c>Code</c>, <c>JoiningDate</c>, <c>ProbationEndDate</c>, <c>Comments</c>,
/// <c>IsOnbdStarted</c>, <c>IsOnbdInit</c>, <c>IsAgency</c>, <c>OnbCoordinatorId</c>, <c>OnbBuddyId</c>),
/// <c>TenantId</c>, <c>UserId</c> and every <c>FullAuditedEntity</c> audit column.
/// </summary>
public class CareersProfileDto : EntityDto
{
    public string FirstNameEn { get; set; }

    public string SecondNameEn { get; set; }

    public string ThirdNameEn { get; set; }

    public string LastNameEn { get; set; }

    public string FirstNameAr { get; set; }

    public string SecondNameAr { get; set; }

    public string ThirdNameAr { get; set; }

    public string LastNameAr { get; set; }

    /// <summary>
    /// Legacy computed <c>Applicant.FullNameEn</c> -- not persisted on the HCMv3 entity, composed here.
    /// </summary>
    public string FullNameEn { get; set; }

    /// <summary>
    /// READ-ONLY on this surface. It is the address the candidate's ABP user account was created with, and
    /// changing it here would silently desynchronise <c>Applicant.Email</c> from <c>AbpUsers.EmailAddress</c>
    /// (which is also the login name). Legacy <c>UserAccountController.UserProfile</c> POST let the form
    /// rewrite BOTH <c>Applicant.Email</c> and <c>Applicant.UserName</c> -- i.e. it changed the login
    /// credential from an unvalidated profile form (<c>UserAccountController.cs:157-158</c>). Not ported;
    /// see <c>UpdateCareersProfileInput</c>.
    /// </summary>
    public string Email { get; set; }

    public int? NationalityId { get; set; }

    public string NationalityName { get; set; }

    public int? JobTitleId { get; set; }

    public string JobTitleName { get; set; }

    public Gender Gender { get; set; }

    public DateTime BirthDate { get; set; }

    public long CivilID { get; set; }

    public long Mobile { get; set; }

    public long Telephone { get; set; }

    public string CurrentAddress { get; set; }

    public string PermanentAddress { get; set; }

    /// <summary>
    /// Legacy <c>UserAccountController.GetProfileCompletionPoints</c> (<c>:1024-1215</c>) rendered as a
    /// percentage the same way legacy did (<c>:74</c>, <c>:118</c>).
    /// </summary>
    public int ProfileCompletionPercent { get; set; }

    public bool HasPhoto { get; set; }
}
