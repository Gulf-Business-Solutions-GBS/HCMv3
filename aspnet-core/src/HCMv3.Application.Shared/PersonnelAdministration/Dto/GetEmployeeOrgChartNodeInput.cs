using System;

namespace HCMv3.PersonnelAdministration.Dto;

public class GetEmployeeOrgChartNodeInput
{
    /// <summary>
    /// The person to centre the chart on. Null means "start at the top": the service picks the root of the
    /// largest reporting tree, which is what the screen sends before the user has chosen anyone.
    /// </summary>
    public int? EmployeeId { get; set; }

    /// <summary>
    /// The date the reporting lines and job titles are read at. Null means today. Time-of-day is ignored --
    /// manager assignments are whole-day ranges.
    /// </summary>
    public DateTime? AsOfDate { get; set; }
}
