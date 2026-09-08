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
    /// Managers above <see cref="Focus"/>, ROOT FIRST, so the screen can stack them top-down without
    /// reversing. Empty when the focus is the top of their tree.
    /// </summary>
    public List<EmployeeOrgChartNodeDto> ManagerChain { get; set; } = new List<EmployeeOrgChartNodeDto>();

    /// <summary>People reporting directly to <see cref="Focus"/>, ordered by name.</summary>
    public List<EmployeeOrgChartNodeDto> DirectReports { get; set; } = new List<EmployeeOrgChartNodeDto>();
}
