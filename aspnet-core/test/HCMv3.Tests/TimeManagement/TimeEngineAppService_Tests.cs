using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// Tests for <see cref="TimeEngineAppService"/> -- the on-demand entry point ported from legacy's
/// <c>uscTimeEngine</c> screen.
///
/// <para>The service is resolved from the container (not constructed directly) so ABP's authorization
/// interceptor is actually in the call path -- otherwise the <c>[AbpAuthorize]</c> assertions below would be
/// vacuous. The job manager and the engine are replaced with substitutes before resolution, which is what lets the
/// "enqueues rather than running synchronously" assertion be made positively rather than by absence.</para>
///
/// <para>Every run here is for a payroll area with a control record and configured time settings, because the
/// screen's legacy checks (<see cref="TimeEngineAppService_LegacyChecks_Tests"/>) refuse anything else.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineAppService_Tests : TimeEngineAppServiceTestBase
{
    private const string Area = "Monthly";

    public TimeEngineAppService_Tests()
    {
        SetTimeSettings(futureEvaluationInDays: 3650, earliestEvaluationDate: new DateTime(2020, 1, 1));
        CreatePayrollArea(Area, new DateTime(2024, 6, 30));
    }

    private static RunTimeEngineInput ValidInput()
    {
        return new RunTimeEngineInput
        {
            EmployeeCodes = "E001,E002",
            StartDate = new DateTime(2024, 6, 1),
            EndDate = new DateTime(2024, 6, 30),
            PayrollAreaName = Area
        };
    }

    #region << Permission gating >>

    [Fact]
    public void Should_Declare_The_TimeEngine_Run_Permission_On_RunTimeEngine()
    {
        var method = typeof(TimeEngineAppService).GetMethod(nameof(TimeEngineAppService.RunTimeEngine));

        var authorize = method.GetCustomAttribute<AbpAuthorizeAttribute>();
        authorize.ShouldNotBeNull();
        authorize.Permissions.ShouldContain(AppPermissions.Pages_TimeManagement_TimeEngine_Run);
    }

    [Fact]
    public void The_TimeEngine_Run_Permission_Should_Be_Registered()
    {
        // Guards against the constant existing on AppPermissions but never being wired up in
        // AppAuthorizationProvider -- in which case [AbpAuthorize] would reference a permission that can
        // never be granted to anyone.
        var permissionManager = Resolve<Abp.Authorization.IPermissionManager>();

        permissionManager
            .GetPermissionOrNull(AppPermissions.Pages_TimeManagement_TimeEngine_Run)
            .ShouldNotBeNull();
        permissionManager
            .GetPermissionOrNull(AppPermissions.Pages_TimeManagement_TimeEngine)
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Reject_An_Unauthenticated_Caller()
    {
        AbpSession.UserId = null;

        await Assert.ThrowsAsync<AbpAuthorizationException>(
            () => TimeEngineAppService.RunTimeEngine(ValidInput()));

        await BackgroundJobManager.DidNotReceiveWithAnyArgs()
            .EnqueueAsync<TimeEngineBackgroundJob, TimeEngineJobArgs>(null);
    }

    /// <summary>Watching is the view permission; stopping is the run permission.</summary>
    [Theory]
    [InlineData(nameof(TimeEngineAppService.GetTimeEngineStatus), AppPermissions.Pages_TimeManagement_TimeEngine)]
    [InlineData(nameof(TimeEngineAppService.GetTimeEngineRunEmployees), AppPermissions.Pages_TimeManagement_TimeEngine)]
    [InlineData(nameof(TimeEngineAppService.GetTimeEngineRunDays), AppPermissions.Pages_TimeManagement_TimeEngine)]
    [InlineData(nameof(TimeEngineAppService.GetTimeEngineRunLogToExcel), AppPermissions.Pages_TimeManagement_TimeEngine)]
    [InlineData(nameof(TimeEngineAppService.GetTimeEngineRunCandidates), AppPermissions.Pages_TimeManagement_TimeEngine)]
    [InlineData(nameof(TimeEngineAppService.GetTimeEngineRunCandidateCodes), AppPermissions.Pages_TimeManagement_TimeEngine)]
    [InlineData(nameof(TimeEngineAppService.GetTimeEngineRunHistory), AppPermissions.Pages_TimeManagement_TimeEngine)]
    [InlineData(nameof(TimeEngineAppService.StopTimeEngine), AppPermissions.Pages_TimeManagement_TimeEngine_Run)]
    public void Should_Gate_Each_Endpoint_On_The_Time_Engine_Permissions(string methodName, string permission)
    {
        typeof(TimeEngineAppService).GetMethod(methodName)
            .GetCustomAttribute<AbpAuthorizeAttribute>()
            .Permissions.ShouldContain(permission);
    }

    #endregion

    #region << Enqueue, not synchronous execution >>

    [Fact]
    public async Task Should_Enqueue_A_Background_Job_Instead_Of_Running_The_Engine_Synchronously()
    {
        var output = await TimeEngineAppService.RunTimeEngine(ValidInput());

        output.Queued.ShouldBeTrue();

        await BackgroundJobManager.Received(1).EnqueueAsync<TimeEngineBackgroundJob, TimeEngineJobArgs>(
            Arg.Any<TimeEngineJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());

        // The whole point of the queue: a run spans thousands of employee-days and must never execute inside
        // the HTTP request.
        await TimeEngineService.DidNotReceiveWithAnyArgs().CreateTimeRegisterAsync(null);
    }

    [Fact]
    public async Task Should_Pass_The_Request_Through_To_The_Job_Args()
    {
        var input = ValidInput();

        await TimeEngineAppService.RunTimeEngine(input);

        EnqueuedArgs.ShouldNotBeNull();
        EnqueuedArgs.EmployeeCodes.ShouldBe("E001,E002");
        EnqueuedArgs.StartDate.ShouldBe(input.StartDate.Value.Date);
        EnqueuedArgs.EndDate.ShouldBe(input.EndDate.Date);
        EnqueuedArgs.PayrollAreaName.ShouldBe(Area);
        EnqueuedArgs.TenantId.ShouldBe(AbpSession.TenantId.Value);

        // The job runs as the person who started it.
        EnqueuedArgs.UserId.ShouldBe(AbpSession.UserId);

        // Runs arriving through the API are never payroll-engine-initiated (legacy args[5] = "0").
        EnqueuedArgs.IsRanFromPayrollEngine.ShouldBeFalse();

        // Legacy args[4] was supplied by the caller; here it is the authenticated user.
        EnqueuedArgs.CurrentUserName.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_Truncate_The_Time_Component_Of_The_Run_Window()
    {
        await TimeEngineAppService.RunTimeEngine(new RunTimeEngineInput
        {
            PayrollAreaName = Area,
            StartDate = new DateTime(2024, 6, 1, 13, 45, 12),
            EndDate = new DateTime(2024, 6, 30, 23, 59, 59)
        });

        EnqueuedArgs.StartDate.ShouldBe(new DateTime(2024, 6, 1));
        EnqueuedArgs.EndDate.ShouldBe(new DateTime(2024, 6, 30));
    }

    [Fact]
    public async Task Should_Allow_A_Null_Start_Date()
    {
        // Legacy's DBDtNull sentinel: the engine falls back to the 1st of the current month.
        await TimeEngineAppService.RunTimeEngine(new RunTimeEngineInput
        {
            PayrollAreaName = Area,
            StartDate = null,
            EndDate = new DateTime(2024, 6, 30)
        });

        EnqueuedArgs.StartDate.ShouldBeNull();
    }

    #endregion

    #region << Validation (legacy Main()'s only up-front check) >>

    [Fact]
    public async Task Should_Reject_A_Missing_End_Date()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => TimeEngineAppService.RunTimeEngine(new RunTimeEngineInput { PayrollAreaName = Area, EndDate = default(DateTime) }));

        exception.Message.ShouldNotBeNullOrWhiteSpace();

        await BackgroundJobManager.DidNotReceiveWithAnyArgs()
            .EnqueueAsync<TimeEngineBackgroundJob, TimeEngineJobArgs>(null);
    }

    [Fact]
    public async Task Should_Reject_A_Start_Date_After_The_End_Date()
    {
        await Assert.ThrowsAsync<UserFriendlyException>(
            () => TimeEngineAppService.RunTimeEngine(new RunTimeEngineInput
            {
                PayrollAreaName = Area,
                StartDate = new DateTime(2024, 7, 1),
                EndDate = new DateTime(2024, 6, 30)
            }));

        await BackgroundJobManager.DidNotReceiveWithAnyArgs()
            .EnqueueAsync<TimeEngineBackgroundJob, TimeEngineJobArgs>(null);
    }

    [Fact]
    public async Task Should_Accept_A_Single_Day_Window()
    {
        var day = new DateTime(2024, 6, 30);

        await TimeEngineAppService.RunTimeEngine(
            new RunTimeEngineInput { PayrollAreaName = Area, StartDate = day, EndDate = day });

        EnqueuedArgs.StartDate.ShouldBe(day);
        EnqueuedArgs.EndDate.ShouldBe(day);
    }

    #endregion

    #region << NSwag contract >>

    [Fact]
    public void RunTimeEngine_Should_Be_HttpPost_So_NSwag_Emits_The_Input_Dto()
    {
        // Repo rule: ABP maps Get*/other conventions to verbs by name, and without [HttpPost] NSwag would
        // flatten RunTimeEngineInput into query-string parameters and never generate the DTO class into
        // service-proxies.ts, breaking the Angular/React clients.
        var method = typeof(TimeEngineAppService).GetMethod(nameof(TimeEngineAppService.RunTimeEngine));

        method.GetCustomAttributes<HttpPostAttribute>().Any().ShouldBeTrue();
    }

    /// <summary>The same rule for every new Get* method that takes a custom input DTO.</summary>
    [Theory]
    [InlineData(nameof(TimeEngineAppService.GetTimeEngineRunDays))]
    [InlineData(nameof(TimeEngineAppService.GetTimeEngineRunLogToExcel))]
    [InlineData(nameof(TimeEngineAppService.GetTimeEngineRunCandidates))]
    [InlineData(nameof(TimeEngineAppService.GetTimeEngineRunCandidateCodes))]
    [InlineData(nameof(TimeEngineAppService.GetTimeEngineRunHistory))]
    public void Get_Methods_With_An_Input_Dto_Should_Be_HttpPost(string methodName)
    {
        typeof(TimeEngineAppService).GetMethod(methodName)
            .GetCustomAttributes<HttpPostAttribute>().Any().ShouldBeTrue();
    }

    #endregion
}
