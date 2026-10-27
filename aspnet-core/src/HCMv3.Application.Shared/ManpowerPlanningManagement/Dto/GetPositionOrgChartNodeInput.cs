namespace HCMv3.ManpowerPlanningManagement.Dto;

public class GetPositionOrgChartNodeInput
{
    /// <summary>
    /// The position to centre the chart on. Null means "start at the top": the service picks the root of
    /// the largest position tree, which is what the screen sends before the user has chosen anything.
    /// </summary>
    public int? PositionId { get; set; }
}
