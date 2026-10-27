using Abp.Application.Services.Dto;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// One drawn card on an org-chart screen: an <c>Abp.Organizations.OrganizationUnit</c> node combined with
/// its <c>OrgUnitProfile</c> extension row and the three figures the card displays. Note the <c>long</c>
/// id, matching Zero's own <c>OrganizationUnit.Id</c> type (unlike this app's usual <c>int</c> entity ids).
///
/// The three counts are all resolved "as of" the date the caller asked for -- see
/// <c>CompanyStructureChartAppService.GetNode</c> for how they are computed and what the
/// <see cref="TotalEmployeeCount"/> figure does with an employee holding two concurrent assignments.
/// </summary>
public class OrgChartNodeDto : EntityDto<long>
{
    /// <summary>Zero's own auto-maintained hierarchical tree-position code, e.g. "00001.00002".</summary>
    public string TreeCode { get; set; }

    /// <summary>User-assigned short business reference code from <c>OrgUnitProfile</c> (may be null).</summary>
    public string Code { get; set; }

    /// <summary>Zero's own <c>OrganizationUnit.DisplayName</c> -- serves as the English name.</summary>
    public string NameEn { get; set; }

    /// <summary>From <c>OrgUnitProfile.NameAr</c> (null when the unit has no profile row).</summary>
    public string NameAr { get; set; }

    public long? ParentId { get; set; }

    /// <summary>Null when the unit has no <c>OrgUnitProfile</c> row (profiles are left-joined).</summary>
    public int? OrganizationUnitTypeId { get; set; }

    /// <summary>
    /// One of <c>OrganizationUnitType.AllowedCodes</c> ("Level0".."Level9"). The card's left colour bar is
    /// keyed off this, so every node classified at the same level looks the same on every chart.
    /// </summary>
    public string OrganizationUnitTypeCode { get; set; }

    /// <summary>Shown as the card's small heading, e.g. "Legal Entity" / "Department".</summary>
    public string OrganizationUnitTypeNameEn { get; set; }

    public string OrganizationUnitTypeNameAr { get; set; }

    /// <summary>Employees whose org assignment names this exact unit and spans the as-of date.</summary>
    public int DirectEmployeeCount { get; set; }

    /// <summary>As <see cref="DirectEmployeeCount"/>, but over this unit plus every descendant unit.</summary>
    public int TotalEmployeeCount { get; set; }

    /// <summary>Immediate child units -- the figure in the card's coloured strip.</summary>
    public int ChildUnitCount { get; set; }
}
