namespace HCMv3.SurveyManagement;

/// <summary>
/// Rating scale (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SurveyManagement.RatingScale</c>).
///
/// JUDGMENT CALL: in the legacy source this enum was declared inside <c>EmployeeSurveyDetail.cs</c> but was
/// never actually wired to any property on <c>EmployeeSurveyDetail</c> (its <c>Answer</c> is a plain
/// <c>string</c>) or any other class in the module -- it was dead/unused there too. It is still ported here
/// (as a standalone enum, not a property) purely for parity with the compiled legacy type inventory; no
/// entity in this port references it, matching legacy behavior exactly.
/// </summary>
public enum RatingScale
{
    VeryGood = 0,
    Good = 1,
    Neutral = 2,
    Poor = 3,
    VeryPoor = 4
}
