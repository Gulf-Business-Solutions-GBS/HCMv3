using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class PAQuarter1ReviewDto : EntityDto<int>
{
    public int PAFormId { get; set; }

    public int KpiInfoId { get; set; }

    public string KpiInfoName { get; set; }

    public double Weight { get; set; }

    public string Target { get; set; }

    public int? AppraiseeRatingId { get; set; }

    public int? RatingId { get; set; }

    public string AppraiseeComments { get; set; }

    public string AppraiserComments { get; set; }
}
