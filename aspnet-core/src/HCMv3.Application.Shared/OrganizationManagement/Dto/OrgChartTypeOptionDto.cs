using Abp.Application.Services.Dto;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// An <c>OrganizationUnitType</c> reduced to what the org-chart toolbar's type filter needs. The full
/// lookup lives behind <c>IOrganizationUnitTypesAppService</c>, but that carries its own permission --
/// a user allowed to view the chart is not necessarily allowed to administer the type master, so the
/// chart serves its own trimmed copy rather than forcing callers to hold both permissions.
/// </summary>
public class OrgChartTypeOptionDto : EntityDto
{
    /// <summary>One of <c>OrganizationUnitType.AllowedCodes</c> ("Level0".."Level9").</summary>
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}
