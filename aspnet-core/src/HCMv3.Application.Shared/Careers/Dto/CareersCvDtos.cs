using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment;

namespace HCMv3.Careers.Dto;

/// <summary>
/// One of the signed-in candidate's own CVs, as listed on the "CV Info" tab. Ported from legacy
/// <c>UserAccountController.UserCvInfo</c> / <c>ApplicantCvViewModel</c>
/// (<c>UserAccountController.cs:214-247</c>).
///
/// NO DOWNLOAD/VIEW URL. Legacy's view model carried <c>DownloadUrl</c>/<c>ViewUrl</c> pointing at
/// <c>DownloadCv</c>/<c>ViewCv</c>, which served the uploaded file out of <c>ApplicantCV.DocAttachment</c>.
/// <c>HCMv3.Recruitment.ApplicantCV</c> HAS NO ATTACHMENT COLUMN -- <c>DocAttachment</c> was dropped when the
/// entity was ported (see the entity's own remarks). A CV here is structured data, not a file. Reported as
/// this wave's largest gap rather than invented.
/// </summary>
public class CareersCvListDto : EntityDto
{
    public string CVName { get; set; }

    public int? JobTitleId { get; set; }

    public string JobTitleName { get; set; }

    public DateTime UploadDate { get; set; }

    /// <summary>
    /// False when at least one application against this CV has moved past
    /// <see cref="AppStatus.Applied" />. Mirrors legacy's delete rule
    /// (<c>UserAccountController.cs:665-667</c>) so a client can hide the button the server would refuse.
    /// </summary>
    public bool CanBeDeleted { get; set; }
}

/// <summary>
/// A candidate's own CV with its child lines. Legacy assembled the same four blocks from one form post in
/// <c>UserAccountController.UploadCv</c> (<c>UserAccountController.cs:428-655</c>).
/// </summary>
public class CareersCvDetailDto : CareersCvListDto
{
    public List<CareersCvQualificationDto> Qualifications { get; set; } = new List<CareersCvQualificationDto>();

    public List<CareersCvWorkExperienceDto> WorkExperiences { get; set; } = new List<CareersCvWorkExperienceDto>();

    public List<CareersCvSkillDto> Skills { get; set; } = new List<CareersCvSkillDto>();

    public List<CareersCvLanguageDto> Languages { get; set; } = new List<CareersCvLanguageDto>();
}

public class CareersCvQualificationDto
{
    public int? QualificationId { get; set; }

    public string QualificationName { get; set; }

    [StringLength(256)]
    public string University { get; set; }

    /// <summary>
    /// Legacy clamped anything above 100 to 0 (<c>UserAccountController.cs:520</c>). Here it is rejected as
    /// invalid input instead of silently zeroed.
    /// </summary>
    [Range(0, 100)]
    public double Percentage { get; set; }

    [Range(0, 9999)]
    public int PassedYear { get; set; }
}

public class CareersCvWorkExperienceDto
{
    [StringLength(256)]
    public string Company { get; set; }

    [StringLength(256)]
    public string Position { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [StringLength(4000)]
    public string Responsibilities { get; set; }
}

public class CareersCvSkillDto
{
    [StringLength(256)]
    public string Skill { get; set; }

    public ProficiencyType Proficiency { get; set; }
}

public class CareersCvLanguageDto
{
    public int? LanguageInfoId { get; set; }

    public string LanguageName { get; set; }

    public ProficiencyType Proficiency { get; set; }
}

/// <summary>
/// Creates a CV for the CALLING candidate. No applicant id: the owner is
/// <c>Applicant.UserId == AbpSession.UserId</c>, resolved server-side.
/// </summary>
public class CreateCareersCvInput
{
    /// <summary>
    /// Legacy refused a second CV with the same name for the same applicant
    /// ("CV Title is duplicated", <c>UserAccountController.cs:650</c>). That rule is ported.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string CVName { get; set; }

    public int? JobTitleId { get; set; }

    public List<CareersCvQualificationDto> Qualifications { get; set; } = new List<CareersCvQualificationDto>();

    public List<CareersCvWorkExperienceDto> WorkExperiences { get; set; } = new List<CareersCvWorkExperienceDto>();

    public List<CareersCvSkillDto> Skills { get; set; } = new List<CareersCvSkillDto>();

    public List<CareersCvLanguageDto> Languages { get; set; } = new List<CareersCvLanguageDto>();
}

/// <summary>
/// Updates one of the calling candidate's own CVs.
///
/// <see cref="Id"/> IS CLIENT-SUPPLIED AND THEREFORE UNTRUSTED. The service re-reads the row with
/// <c>Id == input.Id AND ApplicantId == (caller's applicant) AND TenantId == (caller's tenant)</c> and
/// treats "not mine" and "does not exist" as the same failure, so the endpoint is not an id-enumeration
/// oracle.
///
/// The child lists REPLACE the stored ones wholesale (legacy also only ever bulk-inserted them from one
/// form post). Child rows are consequently never addressed by id from the client at all -- there is no
/// per-child endpoint to attack.
/// </summary>
public class UpdateCareersCvInput : CreateCareersCvInput
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}
