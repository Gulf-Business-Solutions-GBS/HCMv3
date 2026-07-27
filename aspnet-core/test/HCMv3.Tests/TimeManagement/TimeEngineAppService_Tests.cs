using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.UI;
using Castle.MicroKernel.Registration;
using HCMv3.Authorization;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using HCMv3.TimeManagement.Services;
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
/// vacuous. <see cref="IBackgroundJobManager"/> and <see cref="ITimeEngineService"/> are replaced with
/// substitutes before resolution, which is what lets the "enqueues rather than running synchronously"
/// assertion be made positively rather than by absence.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineAppService_Tests : AppTestBase
{
    private readonly ITimeEngineAppService _timeEngineAppService;

    private readonly IBackgroundJobManager _backgroundJobManager = Substitute.For<IBackgroundJobManager>();
    private readonly ITimeEngineService _timeEngineService = Substitute.For<ITimeEngineService>();

    private TimeEngineJobArgs _enqueuedArgs;

    public TimeEngineAppService_Tests()
    {
        _backgroundJobManager
            .EnqueueAsync<TimeEngineBackgroundJob, TimeEngineJobArgs>(
                Arg.Do<TimeEngineJobArgs>(args => _enqueuedArgs = args),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>())
            .Returns(Task.FromResult("job-1"));

        LocalIocManager.IocContainer.Register(
            Component.For<IBackgroundJobManager>().Instance(_backgroundJobManager).IsDefault());
        LocalIocManager.IocContainer.Register(
            Component.For<ITimeEngineService>().Instance(_timeEngineService).IsDefault());

        _timeEngineAppService = Resolve<ITimeEngineAppService>();
    }

    private static RunTimeEngineInput ValidInput()
    {
        return new RunTimeEngineInput
        {
            EmployeeCodes = "E001,E002",
            StartDate = new DateTime(2024, 6, 1),
            EndDate = new DateTime(2024, 6, 30),
            PayrollAreaName = "Monthly"
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
            () => _timeEngineAppService.RunTimeEngine(ValidInput()));

        await _backgroundJobManager.DidNotReceiveWithAnyArgs()
            .EnqueueAsync<TimeEngineBackgroundJob, TimeEngineJobArgs>(null);
    }

    #endregion

    #region << Enqueue, not synchronous execution >>

    [Fact]
    public async Task Should_Enqueue_A_Background_Job_Instead_Of_Running_The_Engine_Synchronously()
    {
        var output = await _timeEngineAppService.RunTimeEngine(ValidInput());

        output.Queued.ShouldBeTrue();

        await _backgroundJobManager.Received(1).EnqueueAsync<TimeEngineBackgroundJob, TimeEngineJobArgs>(
            Arg.Any<TimeEngineJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());

        // The whole point of the queue: a run spans thousands of employee-days and must never execute inside
        // the HTTP request.
        await _timeEngineService.DidNotReceiveWithAnyArgs().CreateTimeRegisterAsync(null);
    }

    [Fact]
    public async Task Should_Pass_The_Request_Through_To_The_Job_Args()
    {
        var input = ValidInput();

        await _timeEngineAppService.RunTimeEngine(input);

        _enqueuedArgs.ShouldNotBeNull();
        _enqueuedArgs.EmployeeCodes.ShouldBe("E001,E002");
        _enqueuedArgs.StartDate.ShouldBe(input.StartDate.Value.Date);
        _enqueuedArgs.EndDate.ShouldBe(input.EndDate.Date);
        _enqueuedArgs.PayrollAreaName.ShouldBe("Monthly");
        _enqueuedArgs.TenantId.ShouldBe(AbpSession.TenantId.Value);

        // Runs arriving through the API are never payroll-engine-initiated (legacy args[5] = "0").
        _enqueuedArgs.IsRanFromPayrollEngine.ShouldBeFalse();

        // Legacy args[4] was supplied by the caller; here it is the authenticated user.
        _enqueuedArgs.CurrentUserName.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_Truncate_The_Time_Component_Of_The_Run_Window()
    {
        await _timeEngineAppService.RunTimeEngine(new RunTimeEngineInput
        {
            StartDate = new DateTime(2024, 6, 1, 13, 45, 12),
            EndDate = new DateTime(2024, 6, 30, 23, 59, 59)
        });

        _enqueuedArgs.StartDate.ShouldBe(new DateTime(2024, 6, 1));
        _enqueuedArgs.EndDate.ShouldBe(new DateTime(2024, 6, 30));
    }

    [Fact]
    public async Task Should_Allow_A_Null_Start_Date()
    {
        // Legacy's DBDtNull sentinel: the engine falls back to the 1st of the current month.
        await _timeEngineAppService.RunTimeEngine(new RunTimeEngineInput
        {
            StartDate = null,
            EndDate = new DateTime(2024, 6, 30)
        });

        _enqueuedArgs.StartDate.ShouldBeNull();
    }

    #endregion

    #region << Validation (legacy Main()'s only up-front check) >>

    [Fact]
    public async Task Should_Reject_A_Missing_End_Date()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _timeEngineAppService.RunTimeEngine(new RunTimeEngineInput { EndDate = default(DateTime) }));

        exception.Message.ShouldNotBeNullOrWhiteSpace();

        await _backgroundJobManager.DidNotReceiveWithAnyArgs()
            .EnqueueAsync<TimeEngineBackgroundJob, TimeEngineJobArgs>(null);
    }

    [Fact]
    public async Task Should_Reject_A_Start_Date_After_The_End_Date()
    {
        await Assert.ThrowsAsync<UserFriendlyException>(
            () => _timeEngineAppService.RunTimeEngine(new RunTimeEngineInput
            {
                StartDate = new DateTime(2024, 7, 1),
                EndDate = new DateTime(2024, 6, 30)
            }));

        await _backgroundJobManager.DidNotReceiveWithAnyArgs()
            .EnqueueAsync<TimeEngineBackgroundJob, TimeEngineJobArgs>(null);
    }

    [Fact]
    public async Task Should_Accept_A_Single_Day_Window()
    {
        var day = new DateTime(2024, 6, 30);

        await _timeEngineAppService.RunTimeEngine(
            new RunTimeEngineInput { StartDate = day, EndDate = day });

        _enqueuedArgs.StartDate.ShouldBe(day);
        _enqueuedArgs.EndDate.ShouldBe(day);
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

    #endregion
}
