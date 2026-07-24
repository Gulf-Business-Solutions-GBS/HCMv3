using Abp.Application.Services.Dto;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// Read/list DTO combining ASP.NET Zero's own <c>OrganizationUnit</c> fields with the HR-specific
/// <see cref="OrgUnitProfile"/> extension row. Note the <c>long</c> id, matching Zero's own
/// <c>OrganizationUnit.Id</c> type (unlike this app's usual <c>int</c> entity ids).
/// </summary>
public class OrgUnitDto : EntityDto<long>
{
    /// <summary>Zero's own auto-maintained hierarchical tree-position code, e.g. "00001.00002".</summary>
    public string TreeCode { get; set; }

    /// <summary>Zero's own <c>OrganizationUnit.DisplayName</c> -- serves as the English name.</summary>
    public string NameEn { get; set; }

    public string NameAr { get; set; }

    /// <summary>User-assigned short business reference code (distinct from <see cref="TreeCode"/>).</summary>
    public string Code { get; set; }

    public long? ParentId { get; set; }

    public string ParentNameEn { get; set; }

    public int OrganizationUnitTypeId { get; set; }

    public string OrganizationUnitTypeName { get; set; }

    public string Address { get; set; }

    public string OrgGeoLocation { get; set; }

    public int? PayrollAreaId { get; set; }

    public string PayrollAreaName { get; set; }
}
