using System.Collections.Generic;

namespace HCMv3.Careers.Dto;

/// <summary>
/// Facet lists for the public careers vacancy list. Legacy built two facets in
/// <c>VacancyController.Index</c>: job titles (<c>VacancyController.cs:63-78</c>) and companies
/// (<c>VacancyController.cs:80-95</c>).
///
/// The company facet is an explicit GAP: legacy derived "company" by walking an org unit's parents until it
/// hit one whose <c>OrganizationUnitType.NameEn</c> equalled <c>OrgService.OrgUT_Company</c>
/// (<c>VacancyController.cs:163-198</c>). HCMv3 has no such marker -- <c>HCMv3.OrganizationManagement
/// .OrganizationUnitType</c> only allows the fixed codes Level0..Level9 and nothing designates "company".
/// <see cref="WorkLocations"/> is provided in its place; restoring the company facet needs a product
/// decision about which org level represents a company.
/// </summary>
public class PublicVacancyFiltersDto
{
    public List<PublicVacancyFilterOptionDto> JobTitles { get; set; } = new List<PublicVacancyFilterOptionDto>();

    public List<PublicVacancyFilterOptionDto> WorkLocations { get; set; } = new List<PublicVacancyFilterOptionDto>();
}
