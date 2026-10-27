namespace HCMv3.Recruitment;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.Recruitment.VacancyStatus</c>. Legacy numeric
/// values (0, 1, 3, 4 -- 2/"NotFilled" was commented out in the legacy source) are preserved as-is.
/// </summary>
public enum VacancyStatus
{
    Open = 0,
    Filled = 1,
    OnHold = 3,
    Dropped = 4
}
