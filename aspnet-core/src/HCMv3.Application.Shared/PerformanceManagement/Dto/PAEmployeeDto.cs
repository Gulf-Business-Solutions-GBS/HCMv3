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

    public string RequestComments { get; set; }

    public string WorkflowComments { get; set; }

    public string ProposedIncrement { get; set; }

    public string ProposedJobTitle { get; set; }

    public string AreaOfImprovement { get; set; }
}
