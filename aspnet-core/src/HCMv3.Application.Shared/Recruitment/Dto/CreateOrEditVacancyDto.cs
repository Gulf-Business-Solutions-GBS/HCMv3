using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="Vacancy"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditVacancyDto
{
    public int? Id { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    public DateTime RequestionDate { get; set; }

    [Required]
    public int JobTitleId { get; set; }

    [Required]
    public DateTime PublishOn { get; set; }

    [Required]
    public DateTime ClosingDate { get; set; }

    public int? RecruiterId { get; set; }

    public int? PositionId { get; set; }

    public int? TemplateOnbGroupId { get; set; }

    [Required]
    public long OrganizationUnitId { get; set; }

    public bool IsBudgeted { get; set; }

    public VacancyStatus VacancyStatus { get; set; }

    public int SLADaysToFill { get; set; }

    public string JobDescription { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string VacancyTitle { get; set; }

    public int? WorkLocationId { get; set; }

    public int TotalVacancy { get; set; }

    [StringLength(100)]
    public string MonthlySalaryRange { get; set; }

    public VacancyEmploymentType EmploymentType { get; set; }

    public int YearsOfExperience { get; set; }

    public int? QualificationId { get; set; }

    public int? OnbGroupId { get; set; }
}
