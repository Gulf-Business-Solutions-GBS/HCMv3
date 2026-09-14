using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.MultiTenancy;
using HCMv3.Authorization.Users;
using HCMv3.BackgroundJobs;
using HCMv3.BackgroundJobs.Dto;
using HCMv3.Test.Base;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.BackgroundJobs;

/// <summary>
/// The Background Jobs screen, and above all who sees what: the host sees every company, a company sees only its own
/// jobs and failures, and nobody but the host sees a system-wide task's error.
/// </summary>
// ReSharper disable once InconsistentNaming
public class BackgroundJobsAppService_Tests : AppTestBase
{
    private const int DefaultTenantId = 1;
    private const int OtherTenantId = 99;

    private readonly IBackgroundJobsAppService _service;

    public BackgroundJobsAppService_Tests()
    {
        _service = Resolve<IBackgroundJobsAppService>();
    }

    // ---------------------------------------------------------------- queued jobs

    [Fact]
    public async Task Should_Show_A_Company_Only_Its_Own_Queued_Jobs()
    {
        QueueJob(DefaultTenantId);
        QueueJob(OtherTenantId);

        LoginAsDefaultTenantAdmin();
        var overview = await _service.GetOverview();

        overview.IsHost.ShouldBeFalse();
        overview.QueuedJobs.ShouldNotBeEmpty();
        overview.QueuedJobs.ShouldAllBe(j => j.TenantId == DefaultTenantId);
    }

    [Fact]
    public async Task Should_Show_The_Host_Every_Companys_Queued_Jobs()
    {
        QueueJob(DefaultTenantId);
        QueueJob(OtherTenantId);

        LoginAsHostAdmin();
        var overview = await _service.GetOverview();

        overview.IsHost.ShouldBeTrue();
        overview.QueuedJobs.ShouldContain(j => j.TenantId == DefaultTenantId);
        overview.QueuedJobs.ShouldContain(j => j.TenantId == OtherTenantId);

        // Company names are the host's to see.
        overview.QueuedJobs.Single(j => j.TenantId == DefaultTenantId).TenantName.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(0, false, QueuedJobState.Waiting)]
    [InlineData(2, false, QueuedJobState.Retrying)]
    [InlineData(5, true, QueuedJobState.Abandoned)]
    public async Task Should_Say_Where_A_Queued_Job_Stands(short tryCount, bool abandoned, QueuedJobState expected)
    {
        var id = QueueJob(DefaultTenantId, tryCount, abandoned);

        LoginAsDefaultTenantAdmin();
        var overview = await _service.GetOverview();

        overview.QueuedJobs.Single(j => j.Id == id).State.ShouldBe(expected);
    }

    [Fact]
    public async Task Should_Name_A_Job_In_Plain_Words()
    {
        var id = QueueJob(DefaultTenantId);

        LoginAsDefaultTenantAdmin();
        var job = (await _service.GetOverview()).QueuedJobs.Single(j => j.Id == id);

        job.JobName.ShouldBe("PayrollEngineBackgroundJob");
        job.DisplayName.ShouldBe("Payroll engine run");
    }

    [Fact]
    public async Task Should_Show_A_Retrying_Job_Why_It_Failed()
    {
        var id = QueueJob(DefaultTenantId, tryCount: 1);
        AddFailure(DefaultTenantId, id, "period is locked");

        LoginAsDefaultTenantAdmin();
        var job = (await _service.GetOverview()).QueuedJobs.Single(j => j.Id == id);

        job.LastError.ShouldContain("period is locked");
    }

    // ---------------------------------------------------------------- failures

    [Fact]
    public async Task Should_Show_A_Company_Only_Its_Own_Failures()
    {
        AddFailure(DefaultTenantId, 7001, "ours");
        AddFailure(OtherTenantId, 7002, "theirs");

        LoginAsDefaultTenantAdmin();
        var failures = (await _service.GetOverview()).RecentFailures;

        failures.ShouldContain(f => f.JobId == 7001);
        failures.ShouldNotContain(f => f.JobId == 7002);
    }

    [Fact]
    public async Task Should_Show_The_Host_Every_Companys_Failures()
    {
        AddFailure(DefaultTenantId, 7101, "ours");
        AddFailure(OtherTenantId, 7102, "theirs");

        LoginAsHostAdmin();
        var failures = (await _service.GetOverview()).RecentFailures;

        failures.ShouldContain(f => f.JobId == 7101);
        failures.ShouldContain(f => f.JobId == 7102);
    }

    // ---------------------------------------------------------------- scheduled tasks

    [Fact]
    public async Task Should_List_Every_Task_Including_Ones_That_Have_Not_Run()
    {
        LoginAsDefaultTenantAdmin();
        var tasks = (await _service.GetOverview()).ScheduledTasks;

        var escalation = tasks.Single(t => t.WorkerName == "WorkflowEscalationWorker");
        escalation.DisplayName.ShouldBe("Workflow escalation and reminders");
        tasks.Count.ShouldBeGreaterThanOrEqualTo(9);
    }

    [Fact]
    public async Task Should_Show_A_Task_Error_To_The_Host_But_Not_To_A_Company()
    {
        Resolve<IBackgroundWorkerRecorder>()
            .RecordRun("TimeEngineAutorunBackgroundWorker", 86_400_000, DateTime.Now, new Exception("company 99 has no calendar"));

        LoginAsHostAdmin();
        var hostView = (await _service.GetOverview()).ScheduledTasks.Single(t => t.WorkerName == "TimeEngineAutorunBackgroundWorker");

        LoginAsDefaultTenantAdmin();
        var companyView = (await _service.GetOverview()).ScheduledTasks.Single(t => t.WorkerName == "TimeEngineAutorunBackgroundWorker");

        hostView.LastError.ShouldContain("company 99 has no calendar");
        companyView.LastError.ShouldBeNull();

        // What a company does see: that it ran, when, and that it failed.
        companyView.HasRun.ShouldBeTrue();
        companyView.LastSucceeded.ShouldBe(false);
        companyView.IntervalMinutes.ShouldBe(1440);
    }

    // ---------------------------------------------------------------- access

    [Fact]
    public async Task Should_Refuse_A_User_Without_The_Permission()
    {
        var userName = "bgjobs" + Guid.NewGuid().ToString("N").Substring(0, 8);

        UsingDbContext(context => context.Users.Add(new User
        {
            TenantId = AbpSession.TenantId,
            UserName = userName,
            Name = "No",
            Surname = "Permissions",
            EmailAddress = userName + "@gbskw.com",
            IsEmailConfirmed = true,
            Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw=="
        }));

        LoginAsTenant(AbpTenantBase.DefaultTenantName, userName);

        await Should.ThrowAsync<AbpAuthorizationException>(() => _service.GetOverview());
    }

    // ---------------------------------------------------------------- helpers

    private long QueueJob(int tenantId, short tryCount = 0, bool abandoned = false)
    {
        return UsingDbContext(null, context =>
        {
            var job = context.BackgroundJobs.Add(new BackgroundJobInfo
            {
                JobType = "HCMv3.PayrollManagement.PayrollEngineBackgroundJob, HCMv3.Application",
                JobArgs = "{\"TenantId\":" + tenantId + ",\"EngineRunId\":1}",
                TryCount = tryCount,
                IsAbandoned = abandoned,
                NextTryTime = DateTime.Now.AddMinutes(5),
                LastTryTime = tryCount > 0 ? DateTime.Now.AddMinutes(-1) : null,
                Priority = BackgroundJobPriority.Normal
            }).Entity;

            context.SaveChanges();
            return job.Id;
        });
    }

    private void AddFailure(int tenantId, long jobId, string error)
    {
        UsingDbContext(tenantId, context => context.BackgroundJobFailures.Add(new BackgroundJobFailure
        {
            TenantId = tenantId,
            JobId = jobId,
            JobName = "PayrollEngineBackgroundJob",
            TryCount = 1,
            FailedTime = DateTime.Now,
            Error = error
        }));
    }
}
