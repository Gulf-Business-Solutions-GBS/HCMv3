using System;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment;

namespace HCMv3.Careers.Dto;

/// <summary>
/// A vacancy as shown on the PUBLIC careers portal list. Purpose-built for anonymous consumption -- this is
/// deliberately NOT <c>HCMv3.Recruitment.Dto.VacancyDto</c> (the admin DTO), which carries internal
/// requisition data.
///
/// Deliberately EXCLUDED from every public careers DTO (present on <c>HCMv3.Recruitment.Vacancy</c>):
/// <c>TenantId</c>, <c>RequestionDate</c>, <c>RecruiterId</c> (requisition owner), <c>IsBudgeted</c>
/// (approval/budget state), <c>VacancyStatus</c> (internal workflow state -- used only as a server-side
/// filter), <c>SLADaysToFill</c>, <c>TotalVacancy</c> (headcount), <c>MonthlySalaryRange</c> (salary band),
/// <c>OnbGroupId</c> / <c>TemplateOnbGroupId</c> (internal onboarding templates), and all
/// <c>FullAuditedEntity</c> audit columns (CreatorUserId, LastModifierUserId, DeleterUserId, timestamps,
/// IsDeleted).
/// </summary>
public class PublicVacancyListDto : EntityDto
{
    /// <summary>Public reference code shown to candidates (legacy <c>VacancyViewModel.Code</c>).</summary>
    public string Code { get; set; }

    public string VacancyTitle { get; set; }

    public int? JobTitleId { get; set; }

    public string JobTitleName { get; set; }

    public int? WorkLocationId { get; set; }

    /// <summary>Legacy <c>VacancyViewModel.Location</c>; "Unspecified" when the vacancy has no location.</summary>
    public string WorkLocationName { get; set; }

    public long? OrganizationUnitId { get; set; }

    /// <summary>Legacy <c>VacancyViewModel.Department</c> -- the vacancy's org unit display name.</summary>
    public string DepartmentName { get; set; }

    public VacancyEmploymentType EmploymentType { get; set; }

    public DateTime PublishOn { get; set; }

    public DateTime ClosingDate { get; set; }
}
