using Abp.Application.Services.Dto;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Read/list DTO for <see cref="CDMidYearReview"/>.</summary>
public class CDMidYearReviewDto : EntityDto<int>
{
    public int CDFormId { get; set; }

    public int CompetencyId { get; set; }

    public string CompetencyName { get; set; }

    public double Weight { get; set; }

    public string Target { get; set; }

    public string EmployeeComments { get; set; }

    public string ManagerComments { get; set; }
}
