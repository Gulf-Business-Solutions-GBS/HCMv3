namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Implemented by every employee grid-report row DTO that carries the legacy <c>OrgLevel1</c>..
/// <c>OrgLevel7</c> columns, so a single resolver can stamp them after materialization.
/// See <c>EmployeeOrgLevelNameResolver</c> for why they cannot be part of the SQL projection.
/// </summary>
public interface IEmployeeOrgLevelRow
{
    string OrgLevel1 { get; set; }
    string OrgLevel2 { get; set; }
    string OrgLevel3 { get; set; }
    string OrgLevel4 { get; set; }
    string OrgLevel5 { get; set; }
    string OrgLevel6 { get; set; }
    string OrgLevel7 { get; set; }

    /// <summary>
    /// Leaf <c>Abp.Organizations.OrganizationUnit.Id</c> of the org assignment the row was projected from;
    /// the resolver walks up from here. Exposed on the DTO because the stamping happens after the query.
    /// </summary>
    long? OrganizationUnitId { get; set; }
}
