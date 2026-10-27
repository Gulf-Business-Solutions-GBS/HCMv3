namespace HCMv3.HcmNotifications;

public static class NotificationRuleConsts
{
    public const int MaxSourceCodeLength = 64;

    public const int MaxNotificationNameLength = 128;

    /// <summary>
    /// Records read per query while a rule runs. The criterion is evaluated in memory, so the source table is
    /// walked in id order a page at a time rather than loaded whole.
    /// </summary>
    public const int CandidatePageSize = 500;

    /// <summary>How many matching records the preview lists by name. The count is always complete.</summary>
    public const int PreviewSampleSize = 10;
}
