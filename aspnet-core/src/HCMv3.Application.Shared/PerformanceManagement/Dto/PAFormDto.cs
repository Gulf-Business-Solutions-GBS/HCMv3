using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class PAFormDto : EntityDto<int>
{
    public int PAPeriodId { get; set; }

    public string PAPeriodName { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public double OverallScore { get; set; }

    public double ModeratedScore { get; set; }

    public bool PlanSubmitByAppraiser { get; set; }

    public bool PlanSubmitByAppraisee { get; set; }

    public bool Quarter1SubmitByAppraiser { get; set; }

    public bool Quarter1SubmitByAppraisee { get; set; }

    public bool MRSubmitByAppraiser { get; set; }

    public bool MRSubmitByAppraisee { get; set; }

    public bool Quarter3SubmitByAppraiser { get; set; }

    public bool Quarter3SubmitByAppraisee { get; set; }

    public bool ERSubmitByAppraiser { get; set; }

    public bool ERSubmitByAppraisee { get; set; }

    public bool AppraiserSignedOff { get; set; }

    public bool AppraiseeSignedOff { get; set; }

    public string AppraiseeStrength { get; set; }

    public string AreasOfImprovement { get; set; }

    public string ReviewofPreviousGoals { get; set; }

    public string NewlySetGoals { get; set; }

    public string OverallComments { get; set; }

    public string Comments { get; set; }
}
