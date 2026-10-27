using HCMv3.SystemSettings;

namespace HCMv3.PerformanceManagement.Dto;

/// <summary>
/// The appraisal settings the PA form screens read (legacy <c>SysService.GetAppraisalSettings</c>):
/// which review stages the cycle has, whether peer review is on and how many peers it takes, and whether
/// marks rather than ratings are scored.
/// </summary>
public class AppraisalFormSettingsDto
{
    /// <summary>
    /// Legacy <c>PAFormController</c>: Quarterly shows every stage; HalfYearly hides the Quarter 1 and
    /// Quarter 3 stages; Annually hides Quarter 1, Mid-Year and Quarter 3.
    /// </summary>
    public AppraisalCycle AppraisalCycle { get; set; }

    public bool EnablePAPeerReview { get; set; }

    public int SetPeerMinLimit { get; set; }

    public int SetPeerMaxLimit { get; set; }

    public bool EnableMarksInPAReview { get; set; }

    public MarksOn MarksOn { get; set; }
}
