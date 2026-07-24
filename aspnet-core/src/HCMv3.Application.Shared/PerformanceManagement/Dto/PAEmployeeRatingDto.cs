using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class PAEmployeeRatingDto : EntityDto<int>
{
    public int PAEmployeeId { get; set; }

    public int PAEmployeeReviewerId { get; set; }

    public int ReviewerEmployeeId { get; set; }

    public string ReviewerEmployeeFullNameEn { get; set; }

    public int PAEmployeeDetailId { get; set; }

    public int KpiInfoId { get; set; }

    public string KpiInfoName { get; set; }

    public double Weight { get; set; }

    public string Target { get; set; }

    public double Marks { get; set; }

    public int? RatingId { get; set; }

    public string Comments { get; set; }
}
