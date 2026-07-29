using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.BackgroundJobs;
using Abp.Localization;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.ProjectManagement;
using HCMv3.ProjectManagement.Dto;
using NSubstitute;
using Shouldly;
using Xunit;

using Fakes = HCMv3.Tests.ProjectManagement.ProjectCostEngineFakes;

namespace HCMv3.Tests.ProjectManagement;

/// <summary>
/// Unit tests for <see cref="ProjectCostEngineAppService"/> -- the operator surface ported from legacy
/// <c>uscProjectCosting</c>'s buttons and status timer. The app services are constructed directly (no Abp
/// DI/interceptor pipeline), so <c>[AbpAuthorize]</c> is not exercised here -- only the validation,
/// enqueueing and status arithmetic.
/// </summary>
public class ProjectCostEngineAppService_Tests
{
    private readonly List<ProjectLog> _projectLogs = new List<ProjectLog>();
    private readonly List<Employee> _employees = new List<Employee>();
    private readonly IBackgroundJobManager _backgroundJobManager = Substitute.For<IBackgroundJobManager>();

    private ProjectCostEngineAppService CreateService()
    {
        var service = new ProjectCostEngineAppService(
            _backgroundJobManager,
            ProjectCostEngineFakes.RepoOver(_projectLogs),
            ProjectCostEngineFakes.RepoOver(_employees));

        ((ApplicationService)service).AbpSession = Fakes.Session();
        ((ApplicationService)service).LocalizationManager = Fakes.LocalizationManager();
        return service;
    }

    private static RunProjectCostEngineInput ValidInput()
    {
        return new RunProjectCostEngineInput
        {
            PayrollRunType = PayrollRunType.Regular,
            PayInDate = new DateTime(2024, 1, 31),
            PayrollAreaName = "PA"
        };
    }

    [Fact]
    public async Task Should_Queue_A_Run_For_Valid_Input()
    {
        var service = CreateService();

        var result = await service.RunProjectCostEngine(ValidInput());

        result.Queued.ShouldBeTrue();
        await _backgroundJobManager.Received(1)
            .EnqueueAsync<ProjectCostEngineBackgroundJob, ProjectCostEngineJobArgs>(
                Arg.Any<ProjectCostEngineJobArgs>(),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>());
    }

    /// <summary>Legacy line 128: neither payroll area nor employee code entered.</summary>
    [Fact]
    public async Task Should_Reject_When_Neither_PayrollArea_Nor_EmployeeCode_Given()
    {
        var service = CreateService();
        var input = ValidInput();
        input.PayrollAreaName = null;

        await Assert.ThrowsAsync<UserFriendlyException>(() => service.RunProjectCostEngine(input));
    }

    /// <summary>Legacy lines 223-224.</summary>
    [Fact]
    public async Task Should_Reject_OffCycle_Run_Without_A_Payment_Date()
    {
        var service = CreateService();
        var input = ValidInput();
        input.PayrollRunType = PayrollRunType.OffCycle;
        input.OffCyclePaymentDate = null;

        await Assert.ThrowsAsync<UserFriendlyException>(() => service.RunProjectCostEngine(input));
    }

    /// <summary>
    /// Legacy line 152 normalises the reason to "End Of Service" or "" -- anything else must not reach the
    /// engine, because the engine compares it by string equality to decide whether to skip ended contracts.
    /// </summary>
    [Fact]
    public async Task Should_Normalise_OffCycle_Reason()
    {
        var service = CreateService();
        var input = ValidInput();
        input.OffCycleReason = "Advance Leave";

        await service.RunProjectCostEngine(input);

        await _backgroundJobManager.Received(1)
            .EnqueueAsync<ProjectCostEngineBackgroundJob, ProjectCostEngineJobArgs>(
                Arg.Is<ProjectCostEngineJobArgs>(a => a.OffCycleReason == string.Empty),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>());
    }

    /// <summary>Legacy lines 1614-1628: the FIRST log row is flagged, not all of them.</summary>
    [Fact]
    public async Task Stop_Should_Set_StopFlag_On_The_First_Log_Row()
    {
        _projectLogs.Add(new ProjectLog { Id = 1, EmployeeId = 1, RunTimeStamp = DateTime.Now });
        _projectLogs.Add(new ProjectLog { Id = 2, EmployeeId = 1, RunTimeStamp = DateTime.Now });

        var service = CreateService();
        await service.StopProjectCostEngine();

        _projectLogs[0].StopFlag.ShouldBeTrue();
        _projectLogs[1].StopFlag.ShouldBeFalse();
    }

    /// <summary>Legacy <c>TruncateLog</c> (lines 1509-1513).</summary>
    [Fact]
    public async Task Clear_Should_Remove_Every_Log_Row()
    {
        _projectLogs.Add(new ProjectLog { Id = 1, EmployeeId = 1 });
        _projectLogs.Add(new ProjectLog { Id = 2, EmployeeId = 1 });

        var service = CreateService();
        await service.ClearProjectCostEngineLog();

        _projectLogs.ShouldBeEmpty();
    }

    [Fact]
    public async Task Status_Should_Be_All_Zero_When_There_Are_No_Logs()
    {
        var service = CreateService();

        var status = await service.GetProjectCostEngineStatus();

        status.TotalRecords.ShouldBe(0);
        status.SuccessRecords.ShouldBe(0);
        status.FailedRecords.ShouldBe(0);
        status.NoOfEmployee.ShouldBe(0);
        status.StopRequested.ShouldBeFalse();
    }

    /// <summary>Legacy lines 1450-1452 and 1487-1488.</summary>
    [Fact]
    public async Task Status_Should_Summarise_Success_And_Failure_Counts()
    {
        _employees.Add(new Employee { Id = 1, Code = "E1", FullNameEn = "E One" });

        var now = DateTime.Now;
        _projectLogs.Add(new ProjectLog { Id = 1, EmployeeId = 1, Status = 0, NoOfEmployee = 5, RunTimeStamp = now });
        _projectLogs.Add(new ProjectLog { Id = 2, EmployeeId = 1, Status = 0, NoOfEmployee = 5, RunTimeStamp = now });
        _projectLogs.Add(new ProjectLog { Id = 3, EmployeeId = 1, Status = 1, NoOfEmployee = 5, RunTimeStamp = now });

        var service = CreateService();

        var status = await service.GetProjectCostEngineStatus();

        status.TotalRecords.ShouldBe(3);
        status.SuccessRecords.ShouldBe(2);
        status.FailedRecords.ShouldBe(1);
        status.NoOfEmployee.ShouldBe(5);
        status.TotalEmployeesProcessed.ShouldBe(3);
        status.CurrentEmployee.ShouldBe("E1 - E One");
    }

    [Fact]
    public async Task Status_Should_Report_A_Requested_Stop()
    {
        _projectLogs.Add(new ProjectLog { Id = 1, EmployeeId = 1, StopFlag = true, RunTimeStamp = DateTime.Now });

        var service = CreateService();

        var status = await service.GetProjectCostEngineStatus();

        status.StopRequested.ShouldBeTrue();
    }
}
