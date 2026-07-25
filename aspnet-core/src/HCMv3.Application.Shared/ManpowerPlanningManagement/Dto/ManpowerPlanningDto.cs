using Abp.Application.Services.Dto;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="ManpowerPlanning"/>.
/// </summary>
public class ManpowerPlanningDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public long OrganizationUnitId { get; set; }

    public string OrganizationUnitDisplayName { get; set; }

    public int PeriodId { get; set; }

    public string PeriodName { get; set; }

    public double GrandTotalAmount { get; set; }

    public string RequestComments { get; set; }

    public ManpowerPlanningStatus Status { get; set; }
}
