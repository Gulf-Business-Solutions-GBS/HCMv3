namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Output of <c>EmployeeProjectDetailsAppService.CalculateWorkDaysAsync</c>.
/// </summary>
public class CalculateWorkDaysOutput
{
    public double WorkDays { get; set; }

    /// <summary>Legacy <c>EmployeeProjectDetail.CalendarDays</c>: EndDate - StartDate + 1.</summary>
    public double CalendarDays { get; set; }
}
