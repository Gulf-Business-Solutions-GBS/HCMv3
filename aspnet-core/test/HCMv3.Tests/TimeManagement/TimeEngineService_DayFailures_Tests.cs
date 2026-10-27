using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.EngineRuns;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using HCMv3.Tests.EngineRuns;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// AN EMPLOYEE WITH ANY FAILED DAY IS A FAILED EMPLOYEE. Legacy's log was per employee per day -- each failed day a
/// TimeLog row with Status 1 (<c>HCM10.TimeEngine/Program.cs</c> lines 1607-1626), counted by its screen. HCMv3 wrote
/// the same day rows but reported the employee "Successfull", so the failures were invisible on the progress grid.
///
/// <para>The fixture uses legacy's own day-level refusal: a VariableTime policy means "no time register for this day"
/// and is thrown per day (Program.cs, 2020.09.22 block), so every day of the window fails while the employee itself
/// gets through the whole loop.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineService_DayFailures_Tests
{
    private readonly TimeEngineServiceTestHarness _harness = new TimeEngineServiceTestHarness();
    private readonly RecordingEngineRunProgressWriter _progress = new RecordingEngineRunProgressWriter();

    public TimeEngineService_DayFailures_Tests()
    {
        _harness.Service.ProgressWriter = _progress;

        _harness.Add(new Employee
        {
            Code = "V1",
            FullNameEn = "Variable Time",
            Status = EmployeeStatus.Active,
            JoiningDate = new DateTime(2020, 1, 15)
        }, 1);

        _harness.Add(new EmployeeContract
        {
            EmployeeId = 1,
            StartDate = new DateTime(2020, 1, 15),
            EndDate = new DateTime(9999, 12, 31)
        }, 1);

        _harness.Add(new TimePolicy { TimeMgmtStatus = TimeMgmtStatus.VariableTime }, 1);
        _harness.Add(new EmployeeTimePolicy
        {
            EmployeeId = 1,
            TimePolicyId = 1,
            StartDate = new DateTime(2020, 1, 15),
            EndDate = new DateTime(9999, 12, 31)
        }, 1);
    }

    [Fact]
    public async Task Should_Report_An_Employee_With_Failed_Days_As_Failed()
    {
        await RunAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 3));

        var row = _progress.Employees.ShouldHaveSingleItem();
        row.Succeeded.ShouldBeFalse();
    }

    /// <summary>The first failed day leads the message, with how many days failed in all.</summary>
    [Fact]
    public async Task Should_Lead_With_The_First_Failed_Day_And_Count_The_Rest()
    {
        await RunAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 3));

        var message = _progress.Employees.Single().Message;
        message.ShouldStartWith("3 days failed. First 01/01/2026 : ");
        message.ShouldContain("Employee has VariableTime Policy found on ");
    }

    [Fact]
    public async Task Should_Give_A_Single_Failed_Day_Without_A_Count()
    {
        await RunAsync(new DateTime(2026, 1, 5), new DateTime(2026, 1, 5));

        _progress.Employees.Single().Message
            .ShouldStartWith("05/01/2026 : Employee has VariableTime Policy found on ");
    }

    /// <summary>The day rows themselves are still written exactly as legacy wrote them: one per day, Status 1.</summary>
    [Fact]
    public async Task Should_Still_Log_Every_Failed_Day_Against_Its_Date()
    {
        await RunAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 3));

        var days = _harness.Store<TimeLog>().Where(l => l.EmployeeId == 1).OrderBy(l => l.DayDate).ToList();
        days.Select(l => l.DayDate).ShouldBe(new[]
        {
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), new DateTime(2026, 1, 3)
        });
        days.ShouldAllBe(l => l.Status == 1);
    }

    [Fact]
    public async Task Should_Close_The_Run_With_Errors()
    {
        await RunAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 3));

        _progress.CompletedStatus.ShouldBe(EngineRunStatus.CompletedWithErrors);
    }

    private Task<TimeEngineRunResult> RunAsync(DateTime start, DateTime end)
    {
        return _harness.Service.CreateTimeRegisterAsync(new TimeEngineRunInput
        {
            EmployeeCodes = "V1",
            StartDate = start,
            EndDate = end,
            CurrentUserName = "operator"
        });
    }
}
