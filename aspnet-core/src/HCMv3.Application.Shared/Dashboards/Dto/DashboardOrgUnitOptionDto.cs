namespace HCMv3.Dashboards.Dto;

/// <summary>An org unit as a dashboard's unit filter and breadcrumb show it. Shared by every dashboard.</summary>
public class DashboardOrgUnitOptionDto
{
    public long Id { get; set; }

    public string Name { get; set; }

    public int Depth { get; set; }
}
