using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class PAPeerReviewDto : EntityDto<int>
{
    public int PAFormId { get; set; }

    public int PeerId { get; set; }

    public string PeerFullNameEn { get; set; }

    public int KpiInfoId { get; set; }

    public string KpiInfoName { get; set; }

    public int? RatingId { get; set; }

    public string Comments { get; set; }

    /// <summary>Computed from the peer's <see cref="PAPeerNominee.Submitted"/> flag for this form's Period/Employee (legacy NonPersistent parity).</summary>
    public bool Submitted { get; set; }
}
