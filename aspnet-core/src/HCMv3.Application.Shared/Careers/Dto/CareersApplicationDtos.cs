using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment;

namespace HCMv3.Careers.Dto;

/// <summary>
/// Applies the CALLING candidate to a vacancy. Ported from legacy
/// <c>HCM10.Recruiting/Controllers/VacancyController.QuickApplyForJob</c>
/// (<c>VacancyController.cs:572-880</c>).
///
/// Carries NO applicant id: legacy identified the applicant from <c>Session["UserName"]</c>, here it is
/// <c>Applicant.UserId == AbpSession.UserId</c>. <see cref="ApplicantCvId"/> IS client-supplied and is
/// therefore verified to belong to the caller before it is used.
/// </summary>
public class CareersApplyInput
{
    [Range(1, int.MaxValue)]
    public int VacancyId { get; set; }

    /// <summary>
    /// Which of the caller's own CVs to attach. Optional: when omitted a placeholder CV named
    /// <c>{email}_{ticks}</c> is created, which is exactly what legacy quick-apply did for a first-time
    /// applicant (<c>VacancyController.cs:700</c>).
    /// </summary>
    public int? ApplicantCvId { get; set; }

    [StringLength(4000)]
    public string CoverLetter { get; set; }
}

public class CareersApplyOutput
{
    public int ApplicationId { get; set; }

    public int ApplicantCvId { get; set; }

    public AppStatus AppStatus { get; set; }
}

/// <summary>
/// One row of "where my applications stand". Ported from legacy
/// <c>UserAccountController.ApplicationStatus</c> / <c>ApplicationStatusViewModel</c>
/// (<c>UserAccountController.cs:828-905</c>). Read-only -- a candidate can never write
/// <see cref="AppStatus"/>; that is the recruiter's <c>ApplicationStatusesAppService</c>.
/// </summary>
public class CareersApplicationDto : EntityDto
{
    public int? VacancyId { get; set; }

    public string VacancyTitle { get; set; }

    public int? JobTitleId { get; set; }

    public string JobTitleName { get; set; }

    public long? OrganizationUnitId { get; set; }

    public string DepartmentName { get; set; }

    public int? ApplicantCvId { get; set; }

    public string CVName { get; set; }

    public AppStatus AppStatus { get; set; }

    /// <summary>
    /// When the application was submitted -- the <c>ApplicationStatus</c> row's creation time.
    ///
    /// DEVIATION FROM LEGACY (deliberate, it was a bug): legacy set
    /// <c>ApplicationStatusViewModel.AppliedOn = status.Vacancy.PublishOn</c>
    /// (<c>UserAccountController.cs:896</c>) -- the date the EMPLOYER published the ad, shown to the
    /// candidate as the date THEY applied. Two different candidates applying months apart saw the same
    /// "applied on".
    /// </summary>
    public DateTime AppliedOn { get; set; }

    public DateTime ClosingDate { get; set; }
}
