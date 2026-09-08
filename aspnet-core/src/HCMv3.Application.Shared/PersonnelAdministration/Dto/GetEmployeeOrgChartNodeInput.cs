namespace HCMv3.PersonnelAdministration.Dto;

public class GetEmployeeOrgChartNodeInput
{
    /// <summary>
    /// The person to centre the chart on. Null means "start at the top": the service picks the root of the
    /// largest reporting tree, which is what the screen sends before the user has chosen anyone.
    /// </summary>
    public int? EmployeeId { get; set; }
}
