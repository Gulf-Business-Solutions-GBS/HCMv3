namespace HCMv3.BackgroundJobs;

public static class BackgroundJobsConsts
{
    public const int MaxNameLength = 256;

    /// <summary>Longest error message kept. The full exception, with its stack trace, stays in the application log.</summary>
    public const int MaxErrorLength = 4000;

    /// <summary>
    /// How long a failed queued job's error is kept. ABP deletes a job once it succeeds or is given up, so without
    /// this record the only trace of a failure would be the log; thirty days covers a month-end payroll run and its
    /// investigation.
    /// </summary>
    public const int FailureRetentionDays = 30;
}
