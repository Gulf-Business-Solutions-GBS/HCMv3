using System;
using System.Linq;
using Abp.BackgroundJobs;
using Abp.Events.Bus;
using Abp.Events.Bus.Exceptions;
using HCMv3.BackgroundJobs;
using HCMv3.Test.Base;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.BackgroundJobs;

/// <summary>
/// Keeping the reason a queued job failed. These raise the same event ABP's job manager raises, through the real event
/// bus, so they also prove the handler is registered -- a handler the bus never calls would pass a direct call.
/// </summary>
// ReSharper disable once InconsistentNaming
public class BackgroundJobFailureRecorder_Tests : AppTestBase
{
    private const string PayrollJobType = "HCMv3.PayrollManagement.PayrollEngineBackgroundJob, HCMv3.Application, Version=1.0.0.0";

    private readonly IEventBus _eventBus;

    public BackgroundJobFailureRecorder_Tests()
    {
        _eventBus = Resolve<IEventBus>();
    }

    [Fact]
    public void Should_Record_A_Failed_Job_Through_The_Event_Bus()
    {
        RaiseJobFailure(jobId: 501, tenantId: 1, tryCount: 2, reason: "payroll period is closed");

        var failure = Failures().Single(f => f.JobId == 501);
        failure.JobName.ShouldBe("PayrollEngineBackgroundJob");
        failure.TryCount.ShouldBe(2);
        failure.TenantId.ShouldBe(1);
        failure.Error.ShouldContain("payroll period is closed");
    }

    [Fact]
    public void Should_Ignore_An_Exception_That_Is_Not_A_Job_Failure()
    {
        var before = Failures().Count;

        _eventBus.Trigger(this, new AbpHandledExceptionData(new InvalidOperationException("a screen error")));

        Failures().Count.ShouldBe(before);
    }

    [Fact]
    public void Should_Give_A_Job_With_Unreadable_Arguments_To_The_Host()
    {
        // The safe side: a host row is visible to the host only.
        RaiseJobFailure(jobId: 502, tenantId: null, tryCount: 1, reason: "x", rawArgs: "not json");

        Failures().Single(f => f.JobId == 502).TenantId.ShouldBeNull();
    }

    [Fact]
    public void Should_Delete_Failures_Older_Than_The_Retention_Period()
    {
        UsingDbContext(null, c => c.BackgroundJobFailures.Add(new BackgroundJobFailure
        {
            TenantId = 1,
            JobId = 900,
            JobName = "OldJob",
            TryCount = 1,
            FailedTime = DateTime.Now.AddDays(-(BackgroundJobsConsts.FailureRetentionDays + 5)),
            Error = "long ago"
        }));

        RaiseJobFailure(jobId: 503, tenantId: 1, tryCount: 1, reason: "today");

        var failures = Failures();
        failures.ShouldNotContain(f => f.JobId == 900);
        failures.ShouldContain(f => f.JobId == 503);
    }

    [Theory]
    [InlineData("{\"TenantId\":7,\"EngineRunId\":3}", 7)]
    [InlineData("{\"tenantId\":7}", 7)]
    [InlineData("{\"EngineRunId\":3}", null)]
    [InlineData("{\"TenantId\":null}", null)]
    [InlineData("", null)]
    [InlineData("[1,2]", null)]
    public void Should_Read_The_Company_From_The_Job_Arguments(string args, int? expected)
    {
        BackgroundJobFailureRecorder.TenantIdOf(args).ShouldBe(expected);
    }

    [Theory]
    [InlineData(PayrollJobType, "PayrollEngineBackgroundJob")]
    [InlineData("TimeEngineBackgroundJob", "TimeEngineBackgroundJob")]
    [InlineData("HCMv3.Importing.ImportToExcelJobBase`1[[HCMv3.X, HCMv3]], HCMv3.Application", "ImportToExcelJobBase")]
    [InlineData("", "(unknown)")]
    public void Should_Name_A_Job_By_Its_Class(string jobType, string expected)
    {
        BackgroundJobFailureRecorder.JobNameOf(jobType).ShouldBe(expected);
    }

    private void RaiseJobFailure(long jobId, int? tenantId, short tryCount, string reason, string rawArgs = null)
    {
        var args = rawArgs ?? (tenantId.HasValue ? "{\"TenantId\":" + tenantId + ",\"EngineRunId\":1}" : "{}");

        var job = new BackgroundJobInfo
        {
            Id = jobId,
            JobType = PayrollJobType,
            JobArgs = args,
            TryCount = tryCount
        };

        _eventBus.Trigger(this, new AbpHandledExceptionData(
            new BackgroundJobException("A background job execution is failed.", new InvalidOperationException(reason))
            {
                BackgroundJob = job
            }));
    }

    private System.Collections.Generic.List<BackgroundJobFailure> Failures()
    {
        return UsingDbContext(null, c => c.BackgroundJobFailures.IgnoreQueryFilters().ToList());
    }
}
