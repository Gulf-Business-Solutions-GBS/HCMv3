using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class PAEmployeeReviewerDto : EntityDto<int>
{
    public int PAEmployeeId { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public int? AverageRatingId { get; set; }

    public double AverageScore { get; set; }

    public double TotalMarks { get; set; }

    public bool IsPeer { get; set; }

    public bool ReviewSubmitted { get; set; }

    public int? PeerAddedById { get; set; }

    public string CustomTextField1 { get; set; }

    public string CustomTextField2 { get; set; }

    public string CustomTextField3 { get; set; }
}
