namespace HCMv3.PerformanceManagement;

/// <summary>
/// Appraisal review-stage activity type (ported from legacy XAF <c>PAActivityType</c> enum,
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.PATimeline</c>). The legacy misspelling
/// <c>AnnnualReview</c> is corrected to <see cref="AnnualReview"/> here.
/// </summary>
public enum ActivityType
{
    Plan = 0,
    MidYearReview = 1,
    AnnualReview = 2,
    Quarter1Review = 3,
    Quarter3Review = 4,
}
