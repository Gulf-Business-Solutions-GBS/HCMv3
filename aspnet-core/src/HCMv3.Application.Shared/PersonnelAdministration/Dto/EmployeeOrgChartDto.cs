using System.Collections.Generic;

namespace HCMv3.PersonnelAdministration.Dto;

/// <summary>
/// One screenful of the Employee Org Chart: the person in focus, the management chain above them, and
/// their direct reports below.
///
/// Like the Company Structure Overview, this is a drill-down view rather than a whole-tree render -- it
/// returns one level of reports at a time and re-centres when the user clicks someone. That is what keeps
/// it usable on a tenant with thousands of employees.
/// </summary>
public class EmployeeOrgChartDto
{
    /// <summary>The card in the middle. Null when nobody has a reporting line at all.</summary>
    public EmployeeOrgChartNodeDto Focus { get; set; }

    /// <summary>
    /// The focus person's immediate manager, or null when they are the top of their tree.
    ///
    /// Deliberately one person and not the whole chain: the chart shows three rows -- manager, focus,
    /// direct reports -- and moving up is a navigation step that re-centres on the manager, at which point
    /// their own manager is fetched. Returning the full chain would draw a column of ancestors nobody
    /// asked for and grow the response with the depth of the hierarchy.
    /// </summary>
    public EmployeeOrgChartNodeDto Manager { get; set; }

    /// <summary>People reporting directly to <see cref="Focus"/>, ordered by name.</summary>
    public List<EmployeeOrgChartNodeDto> DirectReports { get; set; } = new List<EmployeeOrgChartNodeDto>();
}
