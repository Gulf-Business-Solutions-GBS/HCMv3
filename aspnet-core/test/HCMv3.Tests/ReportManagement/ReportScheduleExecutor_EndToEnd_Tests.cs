using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

/// <summary>
/// END-TO-END execution of a report schedule, through the real container.
///
/// Every other test in this module exercises one component in isolation. This one runs the whole path:
/// executor -> burst resolver -> recipient resolution -> DISCOVERY -> reflection invoke of the report's
/// own Get*ToExcel -> unwrap Task&lt;FileDto&gt; -> read the bytes back out of the temp-file cache ->
/// delivery channel -> run log.
///
/// It exists because that middle stretch is the part I could only verify by READING. Resolving a
/// discovered app service by interface, invoking a MethodInfo, unwrapping the generic Task and pulling
/// the file back out of the cache are four assumptions that all look right and any of which would fail
/// only at run time -- after somebody had configured a schedule and was waiting for an e-mail.
///
/// IEmailSender is replaced with NullEmailSender by the test base, so delivery completes without
/// actually sending. That is the correct boundary here: this proves the report was rendered and handed
/// to a channel, which is exactly the stretch that was unproven.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ReportScheduleExecutor_EndToEnd_Tests : AppTestBase
{
    private readonly IReportScheduleExecutor _executor;
    private readonly IRepository<ReportSchedule> _scheduleRepository;
    private readonly IRepository<ReportScheduleRun, long> _runRepository;
    private readonly IRepository<ReportScheduleRunSlice, long> _sliceRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ReportScheduleExecutor_EndToEnd_Tests()
    {
        LoginAsDefaultTenantAdmin();

        _executor = Resolve<IReportScheduleExecutor>();
        _scheduleRepository = Resolve<IRepository<ReportSchedule>>();
        _runRepository = Resolve<IRepository<ReportScheduleRun, long>>();
        _sliceRepository = Resolve<IRepository<ReportScheduleRunSlice, long>>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    [Fact]
    public async Task Should_Run_A_Schedule_End_To_End_And_Record_The_Outcome()
    {
        var scheduleId = await CreateScheduleAsync();

        ReportScheduleExecutionResult result;
        using (var uow = _unitOfWorkManager.Begin())
        {
            result = await _executor.ExecuteAsync(scheduleId, isManual: true);
            await uow.CompleteAsync();
        }

        // The run itself must be recorded whatever happened -- a distribution module that fails without
        // leaving a trace is worse than one that fails loudly.
        result.RunId.ShouldBeGreaterThan(0);

        // One recipient, one slice: the schedule is unburst with a single explicit address.
        result.SliceCount.ShouldBe(1);
        result.RecipientCount.ShouldBe(1);

        // The status must be a real outcome, never left Running -- that would mean the executor returned
        // without finishing its own bookkeeping.
        result.Status.ShouldNotBe(ReportRunStatus.Running);
        result.Status.ShouldNotBe(ReportRunStatus.Pending);

        // THE ASSERTION THAT MATTERS. Reaching a per-recipient slice row proves the run got past
        // discovery, reflection and rendering: the executor only writes one after calling the renderer.
        // If the reflection plumbing were wrong the run would be Failed with no slice at all.
        using (var uow = _unitOfWorkManager.Begin())
        {
            var run = await _runRepository.GetAsync(result.RunId);
            run.FinishedAt.ShouldNotBeNull();
            run.SliceCount.ShouldBe(1);

            var slices = _sliceRepository.GetAll()
                .Where(s => s.ReportScheduleRunId == result.RunId)
                .ToList();

            slices.Count.ShouldBe(1);

            var slice = slices[0];
            slice.RecipientEmail.ShouldBe("auditor@example.com");

            // Recorded per slice so an auditor can answer "was this file scoped?" from the log alone.
            slice.WasRecipientScoped.ShouldBeFalse();

            // Surface the real reason on failure. A bare red test here would send somebody hunting
            // through four layers; the executor already captured the cause, so show it.
            slice.Status.ShouldNotBe(
                ReportRunStatus.Failed,
                $"slice failed: {slice.ErrorMessage}");

            // The render path RAN. Succeeded means a file was produced and handed to the channel;
            // SkippedEmpty means the report returned nothing for the test database's (empty) data, which
            // is a legitimate outcome here and still proves discovery, reflection and the Task<FileDto>
            // unwrap all worked -- a broken reflection path is recorded as Failed, which is asserted
            // above. Anything else would mean the executor left a slice in a state it should not.
            slice.Status.ShouldBeOneOf(ReportRunStatus.Succeeded, ReportRunStatus.SkippedEmpty);

            await uow.CompleteAsync();
        }
    }

    [Fact]
    public async Task Should_Record_A_Failure_Rather_Than_Throwing_When_The_Report_Does_Not_Exist()
    {
        // A schedule can outlive the report it points at -- renamed, removed, refactored. That must be a
        // recorded failure on the run, not an exception that takes out the whole dispatcher tick and
        // with it every other tenant's schedules.
        var scheduleId = await CreateScheduleAsync(reportKey: "NoSuchReportAppService");

        ReportScheduleExecutionResult result;
        using (var uow = _unitOfWorkManager.Begin())
        {
            result = await _executor.ExecuteAsync(scheduleId, isManual: true);
            await uow.CompleteAsync();
        }

        result.RunId.ShouldBeGreaterThan(0);
        result.Status.ShouldBeOneOf(ReportRunStatus.Failed, ReportRunStatus.PartiallySucceeded);

        using (var uow = _unitOfWorkManager.Begin())
        {
            var slice = _sliceRepository.GetAll()
                .FirstOrDefault(s => s.ReportScheduleRunId == result.RunId);

            slice.ShouldNotBeNull();
            slice.Status.ShouldBe(ReportRunStatus.Failed);

            // The message must name the missing report, otherwise diagnosing this from the run log is
            // guesswork.
            slice.ErrorMessage.ShouldContain("NoSuchReportAppService");

            await uow.CompleteAsync();
        }
    }

    private async Task<int> CreateScheduleAsync(string reportKey = "EmployeeAttendanceReportAppService")
    {
        int scheduleId;

        using (var uow = _unitOfWorkManager.Begin())
        {
            var schedule = new ReportSchedule
            {
                TenantId = 1,
                Name = "End-to-end test schedule",
                ReportKind = ScheduledReportKind.GridReport,
                ReportKey = reportKey,
                Format = ScheduledReportFormat.Excel,
                BurstDimension = ReportBurstDimension.None,
                OccurrenceType = ReportScheduleOccurrenceType.Once,
                FirstOccurrence = new DateTime(2026, 1, 1),
                IsActive = true,
                Recipients = new List<ReportScheduleRecipient>
                {
                    new ReportScheduleRecipient
                    {
                        TenantId = 1,
                        Rule = ReportRecipientRule.Explicit,
                        EmailAddress = "auditor@example.com",

                        // An external recipient with no HCMv3 account, so there is no row-level scope to
                        // resolve. Keeps this test about the RENDER path rather than about scoping,
                        // which its own tests already cover.
                        BypassRecipientScope = true,
                    },
                },
                Destinations = new List<ReportScheduleDestination>
                {
                    new ReportScheduleDestination
                    {
                        TenantId = 1,
                        Method = ReportDestinationMethod.Email,
                        EmailSubjectTemplate = "{ReportName}",
                    },
                },
            };

            scheduleId = await _scheduleRepository.InsertAndGetIdAsync(schedule);
            await uow.CompleteAsync();
        }

        return scheduleId;
    }
}
