using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class PAEmployeeDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public int PACycleId { get; set; }

    public string PACycleName { get; set; }

    public int FormTypeId { get; set; }

    public string FormTypeName { get; set; }

    public int? OverallAverageRatingId { get; set; }

    public double OverallAverageScore { get; set; }

    public int? PeerAverageRatingId { get; set; }

    public double PeerAverageScore { get; set; }

    public int? EmployeeAverageRatingId { get; set; }

    public double EmployeeAverageScore { get; set; }

    /// <summary>
    /// LEGACY, read-only. Returned so text captured before HCMv3's workflow engine stays visible; it is
    /// never written back. See <see cref="CreateOrEditPAEmployeeDto"/>.
    /// </summary>
    public string RequestComments { get; set; }

    /// <summary>
    /// LEGACY, read-only. The old concatenated approval conversation, kept visible for migrated appraisals.
    /// New comments live in the <c>WorkflowComment</c> thread on the governing request.
    /// </summary>
    public string WorkflowComments { get; set; }

    public string ProposedIncrement { get; set; }

    public string ProposedJobTitle { get; set; }

    public string AreaOfImprovement { get; set; }
}
