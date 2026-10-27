namespace HCMv3.SuccessionManagement;

/// <summary>
/// Origin of a succession-related score (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SuccessionManagement.ScoreSource</c>), used on
/// <c>EmpPerformanceHistory</c>/<c>EmpPotentialAssessment</c> to flag whether the score came from a live
/// calculated feed or was entered manually.
/// </summary>
public enum ScoreSource
{
    Live = 0,
    Manual = 1
}
