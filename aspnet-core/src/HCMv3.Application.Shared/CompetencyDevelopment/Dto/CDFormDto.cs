using Abp.Application.Services.Dto;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Read/list DTO for <see cref="CDForm"/>.</summary>
public class CDFormDto : EntityDto<int>
{
    public int CDPeriodId { get; set; }

    public string CDPeriodName { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; }

    public double OverallScore { get; set; }

    public double ModeratedScore { get; set; }

    public bool EmployeeSignedOff { get; set; }

    public bool ManagerSignedOff { get; set; }
}
