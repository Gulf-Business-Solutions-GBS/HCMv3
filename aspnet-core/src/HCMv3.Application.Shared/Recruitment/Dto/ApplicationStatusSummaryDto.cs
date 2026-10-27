using System;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read-only header of an application's own page: the application, the vacancy it is for, and a summary of
/// the applicant behind the CV. Read under the Application Statuses permission alone, so a recruiter does not
/// also need the Applicants screen to see who applied.
/// </summary>
public class ApplicationStatusSummaryDto : EntityDto<int>
{
    public AppStatus AppStatus { get; set; }

    public string CoverLetter { get; set; }

    public int? VacancyId { get; set; }

    public string VacancyCode { get; set; }

    public string VacancyTitle { get; set; }

    public int? ApplicantCVId { get; set; }

    public string CVName { get; set; }

    public DateTime? CVUploadDate { get; set; }

    public int? ApplicantId { get; set; }

    public string ApplicantCode { get; set; }

    public string ApplicantFullNameEn { get; set; }

    public string ApplicantFullNameAr { get; set; }

    public Gender? Gender { get; set; }

    public DateTime? BirthDate { get; set; }

    public string NationalityNameEn { get; set; }

    public string NationalityNameAr { get; set; }

    public string JobTitleNameEn { get; set; }

    public string JobTitleNameAr { get; set; }

    public string Email { get; set; }

    public long? Mobile { get; set; }
}
