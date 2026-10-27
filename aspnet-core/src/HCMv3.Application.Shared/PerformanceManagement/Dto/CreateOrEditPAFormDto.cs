using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditPAFormDto
{
    public int? Id { get; set; }

    [Required]
    public int PAPeriodId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

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

    [StringLength(4000)]
    public string AppraiseeStrength { get; set; }

    [StringLength(4000)]
    public string AreasOfImprovement { get; set; }

    [StringLength(4000)]
    public string ReviewofPreviousGoals { get; set; }

    [StringLength(4000)]
    public string NewlySetGoals { get; set; }

    [StringLength(4000)]
    public string OverallComments { get; set; }

    [StringLength(4000)]
    public string Comments { get; set; }
}
