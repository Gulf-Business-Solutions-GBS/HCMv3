using System;
using Abp.Application.Services.Dto;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="Vacancy"/>. Legacy computed <c>CodeWithTitle</c> is reproduced here.
/// </summary>
public class VacancyDto : EntityDto<int>
{
    public string Code { get; set; }

    public string CodeWithTitle { get; set; }

    public DateTime RequestionDate { get; set; }

    public int? JobTitleId { get; set; }

    public string JobTitleNameEn { get; set; }

    public DateTime PublishOn { get; set; }

    public DateTime ClosingDate { get; set; }

    public int? RecruiterId { get; set; }

    public int? TemplateOnbGroupId { get; set; }

    public long? OrganizationUnitId { get; set; }

    public bool IsBudgeted { get; set; }

    public VacancyStatus VacancyStatus { get; set; }

    public int SLADaysToFill { get; set; }

    public string JobDescription { get; set; }

    public string VacancyTitle { get; set; }

    public int? WorkLocationId { get; set; }

    public string WorkLocationNameEn { get; set; }

    public int TotalVacancy { get; set; }

    public string MonthlySalaryRange { get; set; }

    public VacancyEmploymentType EmploymentType { get; set; }

    public int YearsOfExperience { get; set; }

    public int? QualificationId { get; set; }

    public int? OnbGroupId { get; set; }
}
