using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.ProjectManagement;
using HCMv3.ProjectManagement.Services;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ProjectManagement;

/// <summary>
/// Unit tests for <see cref="ProjectCostEngineService"/> -- the port of legacy
/// <c>HCM10.Web/UserControls/ProjectManagement/uscProjectCosting.ascx.cs</c>.
///
/// <para>The money-bearing assertions (working-day pro-ration, percentage split) use figures chosen so the
/// expected value is exact in binary floating point, so any change to legacy's arithmetic is caught.</para>
/// </summary>
public class ProjectCostEngineService_Tests
{
    private const int TenantId = 1;
    private static readonly DateTime PeriodEnd = new DateTime(2024, 1, 31);

    #region << Pre-flight validation (legacy Main, lines 219-275) >>

    [Fact]
    public async Task Should_Throw_When_PayInDate_Is_Not_Set()
    {
        var harness = new Harness();
        var service = harness.Build();

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            service.RunAsync(new ProjectCostEngineRunInput
            {
                PayrollRunType = PayrollRunType.Regular,
                PayrollAreaName = "PA"
            }));

        ex.Message.ShouldContain("No Pay Date defined");
    }

    [Fact]
    public async Task Should_Throw_When_OffCycle_Run_Has_No_Payment_Date()
    {
        var harness = new Harness();
        var service = harness.Build();

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            service.RunAsync(new ProjectCostEngineRunInput
            {
                PayrollRunType = PayrollRunType.OffCycle,
                PayInDate = PeriodEnd,
                PayrollAreaName = "PA"
            }));

        ex.Message.ShouldContain("Offcycle Payment Date");
    }

    [Fact]
    public async Task Should_Throw_When_Payroll_Control_Record_Not_Found()
    {
        var harness = new Harness();
        harness.PayrollControlRecords.Clear();
        var service = harness.Build();

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => service.RunAsync(RegularInput()));

        ex.Message.ShouldContain("Payroll Control Record not found");
    }

    [Fact]
    public async Task Should_Throw_When_Payroll_Calendar_Missing_For_Period()
    {
        var harness = new Harness();
        harness.PayrollCalendars.Clear();
        var service = harness.Build();

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => service.RunAsync(RegularInput()));

        ex.Message.ShouldContain("Payroll Calender");
    }

    [Fact]
    public async Task Should_Throw_When_Control_Record_Is_Not_Locked()
    {
        var harness = new Harness();
        harness.PayrollControlRecords[0].PeriodLocked = false;
        var service = harness.Build();

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => service.RunAsync(RegularInput()));

        ex.Message.ShouldContain("not locked or is not current");
    }

    /// <summary>
    /// Legacy has NO off-cycle arm at all (lines 244-262): an off-cycle run validates and then does
    /// nothing. Guarding this so a future refactor cannot quietly "fix" it.
    /// </summary>
    [Fact]
    public async Task OffCycle_Run_Should_Do_Nothing()
    {
        var harness = new Harness();
        var service = harness.Build();

        var result = await service.RunAsync(new ProjectCostEngineRunInput
        {
            PayrollRunType = PayrollRunType.OffCycle,
            PayInDate = PeriodEnd,
            PayrollAreaName = "PA",
            OffCyclePaymentDate = PeriodEnd
        });

        result.EmployeeResults.ShouldBeEmpty();
        harness.ProjectRegisters.ShouldBeEmpty();
        harness.ProjectLogs.ShouldBeEmpty();
    }

    #endregion

    #region << ActualDays costing (legacy lines 683-1234) >>

    /// <summary>
    /// Legacy lines 790-806: a basic-pay wage type is pro-rated as
    /// <c>(Amount / Number) * actualWorkingDays</c>. 300/30 * 10 == 100 exactly.
    /// </summary>
    [Fact]
    public async Task Should_Prorate_BasicPay_WageType_By_Actual_Working_Days()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);
        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.EmployeeResults.Count.ShouldBe(1);
        result.EmployeeResults[0].Succeeded.ShouldBeTrue();

        harness.ProjectRegisters.Count.ShouldBe(1);
        harness.ProjectRegisters[0].Period.ShouldBe(PeriodEnd);

        harness.ProjectRegisterDetails.Count.ShouldBe(1);
        var detail = harness.ProjectRegisterDetails[0];
        detail.ProjectId.ShouldBe(Harness.ProjectId);
        detail.WageTypeId.ShouldBe(Harness.BasicWageTypeId);
        detail.Number.ShouldBe(10);
        detail.Amount.ShouldBe(100);
    }

    /// <summary>
    /// Public holidays and rest days count as worked days (legacy lines 794-799): 8 working + 1 holiday +
    /// 1 rest = 10 -> 300/30*10 = 100.
    /// </summary>
    [Fact]
    public async Task Should_Count_PublicHoliday_And_RestDay_As_Worked_Days()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(8);
        harness.TimeRegisters.Add(new TimeRegister { Id = 100, EmployeeId = Harness.EmployeeId, DayDate = new DateTime(2024, 1, 20), PublicHoliday = 1 });
        harness.TimeRegisters.Add(new TimeRegister { Id = 101, EmployeeId = Harness.EmployeeId, DayDate = new DateTime(2024, 1, 21), RestDay = 1 });
        var service = harness.Build();

        await service.RunAsync(RegularInput());

        harness.ProjectRegisterDetails.Count.ShouldBe(1);
        harness.ProjectRegisterDetails[0].Number.ShouldBe(10);
        harness.ProjectRegisterDetails[0].Amount.ShouldBe(100);
    }

    /// <summary>Legacy lines 966-967: nothing is written when both figures are zero.</summary>
    [Fact]
    public async Task Should_Write_No_Detail_When_There_Are_No_Worked_Days()
    {
        var harness = new Harness();
        var service = harness.Build();

        await service.RunAsync(RegularInput());

        harness.ProjectRegisters.Count.ShouldBe(1);
        harness.ProjectRegisterDetails.ShouldBeEmpty();
    }

    /// <summary>
    /// Legacy lines 546-556: re-running a period REUSES the existing register and wipes its detail rows
    /// first, so figures never double up.
    /// </summary>
    [Fact]
    public async Task Should_Reuse_Existing_Register_And_Replace_Its_Details()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);

        harness.ProjectRegisters.Add(new ProjectRegister
        {
            Id = 77,
            TenantId = TenantId,
            EmployeeId = Harness.EmployeeId,
            Period = PeriodEnd
        });
        harness.ProjectRegisterDetails.Add(new ProjectRegisterDetail
        {
            Id = 88,
            TenantId = TenantId,
            ProjectRegisterId = 77,
            ProjectId = Harness.ProjectId,
            WageTypeId = Harness.BasicWageTypeId,
            Number = 999,
            Amount = 999
        });

        var service = harness.Build();

        await service.RunAsync(RegularInput());

        harness.ProjectRegisters.Count.ShouldBe(1);
        harness.ProjectRegisters[0].Id.ShouldBe(77);

        harness.ProjectRegisterDetails.Count.ShouldBe(1);
        harness.ProjectRegisterDetails[0].Number.ShouldBe(10);
        harness.ProjectRegisterDetails[0].Amount.ShouldBe(100);
    }

    #endregion

    #region << Percentage costing (legacy lines 1237-1272) >>

    /// <summary>
    /// Legacy lines 1264-1265: figures are split by the detail's Percentage, with no day counting at all.
    /// 30 * 40 / 100 == 12, 300 * 40 / 100 == 120.
    /// </summary>
    [Fact]
    public async Task Should_Split_By_Percentage_When_CostingMethod_Is_Percentage()
    {
        var harness = new Harness();
        harness.EmployeeProjects[0].CostingMethod = ProjectCostingMethod.Percentage;
        harness.EmployeeProjectDetails[0].Percentage = 40;
        // Deliberately no TimeRegister rows: the percentage path must not read them.
        var service = harness.Build();

        await service.RunAsync(RegularInput());

        harness.ProjectRegisterDetails.Count.ShouldBe(1);
        harness.ProjectRegisterDetails[0].Number.ShouldBe(12);
        harness.ProjectRegisterDetails[0].Amount.ShouldBe(120);
    }

    /// <summary>Legacy lines 1276-1279: the ActualHours branch is an empty block -- no rows, no error.</summary>
    [Fact]
    public async Task Should_Produce_Nothing_For_ActualHours_CostingMethod()
    {
        var harness = new Harness();
        harness.EmployeeProjects[0].CostingMethod = ProjectCostingMethod.ActualHours;
        harness.AddTimeRegisterWorkingDays(10);
        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.EmployeeResults[0].Succeeded.ShouldBeTrue();
        harness.ProjectRegisterDetails.ShouldBeEmpty();
    }

    #endregion

    #region << Per-employee isolation and abort (legacy lines 397-400, 598-622) >>

    /// <summary>
    /// Legacy lines 527-531: a missing SalaryRegister fails only THAT employee, and the failure is recorded
    /// as a ProjectLog row with Status 1 rather than aborting the run.
    /// </summary>
    [Fact]
    public async Task Should_Log_Failure_Per_Employee_Instead_Of_Aborting()
    {
        var harness = new Harness();
        harness.SalaryRegisters.Clear();
        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.Aborted.ShouldBeFalse();
        result.EmployeeResults.Count.ShouldBe(1);
        result.EmployeeResults[0].Succeeded.ShouldBeFalse();
        result.EmployeeResults[0].Message.ShouldContain("Salary Register not found");

        harness.ProjectLogs.Count.ShouldBe(1);
        harness.ProjectLogs[0].Status.ShouldBe(1);
    }

    /// <summary>Legacy lines 397-400: any ProjectLog row carrying StopFlag aborts the whole loop.</summary>
    [Fact]
    public async Task Should_Abort_When_A_ProjectLog_Row_Has_StopFlag()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);
        harness.ProjectLogs.Add(new ProjectLog
        {
            Id = 1,
            TenantId = TenantId,
            EmployeeId = Harness.EmployeeId,
            StopFlag = true,
            RunTimeStamp = DateTime.Now
        });

        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.Aborted.ShouldBeTrue();
        harness.ProjectRegisters.ShouldBeEmpty();
    }

    /// <summary>
    /// Legacy lines 394-395: an employee whose contract ended before the period starts is skipped silently
    /// -- no ProjectLog row at all -- unless the run is an End Of Service off-cycle.
    /// </summary>
    [Fact]
    public async Task Should_Skip_Employee_Whose_Contract_Ended_Before_The_Period()
    {
        var harness = new Harness();
        harness.EmployeeContracts[0].EndDate = new DateTime(2023, 6, 30);
        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.EmployeeResults.ShouldBeEmpty();
        harness.ProjectLogs.ShouldBeEmpty();
        harness.ProjectRegisters.ShouldBeEmpty();
    }

    #endregion

    #region << Execution log (legacy lines 314-346, 638-670) >>

    [Fact]
    public async Task Should_Write_An_ExecutionLog_Row_For_The_Run()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);
        var service = harness.Build();

        await service.RunAsync(RegularInput());

        harness.ExecutionLogs.Count.ShouldBe(1);
        var log = harness.ExecutionLogs[0];
        log.Module.ShouldBe(ExecutionLogModule.ProjectEngine);
        log.ScreenName.ShouldBe("HCM10.Web.UserControls.ProjectManagement.uscProjectCosting");
        log.Parameters.ShouldContain("Payroll Area : PA");
        log.Parameters.ShouldContain("Period : 31/01/2024");
        log.ExecutionStatus.ShouldBe(ExecutionLogStatus.SUCCESS);
        log.TotalRecords.ShouldBe(1);
        log.Successfull.ShouldBe(1);
    }

    #endregion

    private static ProjectCostEngineRunInput RegularInput()
    {
        return new ProjectCostEngineRunInput
        {
            PayrollRunType = PayrollRunType.Regular,
            PayInDate = PeriodEnd,
            PayrollAreaName = "PA"
        };
    }

    /// <summary>
    /// Minimal but complete data set for one employee on one project for January 2024, with every guard in
    /// <c>ProcessEmployeeAsync</c> satisfied. Individual tests mutate the lists before calling
    /// <see cref="Build"/>.
    /// </summary>
    private sealed class Harness
    {
        public const int EmployeeId = 1;
        public const int ProjectId = 5;
        public const int BasicWageTypeId = 10;

        public readonly List<PayrollArea> PayrollAreas = new List<PayrollArea>();
        public readonly List<PayrollControlRecord> PayrollControlRecords = new List<PayrollControlRecord>();
        public readonly List<PayrollCalendar> PayrollCalendars = new List<PayrollCalendar>();
        public readonly List<Employee> Employees = new List<Employee>();
        public readonly List<EmployeeContract> EmployeeContracts = new List<EmployeeContract>();
        public readonly List<EmployeeOrgAssignment> EmployeeOrgAssignments = new List<EmployeeOrgAssignment>();
        public readonly List<EmployeePayrollStatus> EmployeePayrollStatuses = new List<EmployeePayrollStatus>();
        public readonly List<EmployeeAbsence> EmployeeAbsences = new List<EmployeeAbsence>();
        public readonly List<EmployeeOvertime> EmployeeOvertimes = new List<EmployeeOvertime>();
        public readonly List<SalaryRegister> SalaryRegisters = new List<SalaryRegister>();
        public readonly List<SalaryRegisterDetail> SalaryRegisterDetails = new List<SalaryRegisterDetail>();
        public readonly List<WageType> WageTypes = new List<WageType>();
        public readonly List<AbsenceType> AbsenceTypes = new List<AbsenceType>();
        public readonly List<TicketType> TicketTypes = new List<TicketType>();
        public readonly List<TimeRegister> TimeRegisters = new List<TimeRegister>();
        public readonly List<ExecutionLog> ExecutionLogs = new List<ExecutionLog>();
        public readonly List<ProjectLog> ProjectLogs = new List<ProjectLog>();
        public readonly List<ProjectRegister> ProjectRegisters = new List<ProjectRegister>();
        public readonly List<ProjectRegisterDetail> ProjectRegisterDetails = new List<ProjectRegisterDetail>();
        public readonly List<EmployeeProject> EmployeeProjects = new List<EmployeeProject>();
        public readonly List<EmployeeProjectDetail> EmployeeProjectDetails = new List<EmployeeProjectDetail>();
        public readonly List<ProjectWageType> ProjectWageTypes = new List<ProjectWageType>();

        public Harness()
        {
            var payrollArea = new PayrollArea { Id = 1, TenantId = TenantId, Code = "PA", Name = "PA" };
            PayrollAreas.Add(payrollArea);

            PayrollControlRecords.Add(new PayrollControlRecord
            {
                Id = 1,
                TenantId = TenantId,
                PayrollAreaId = payrollArea.Id,
                PayrollArea = payrollArea,
                PayrollMonth = 1,
                PayrollYear = 2024,
                CalendarMonthStart = new DateTime(2024, 1, 1),
                CalendarMonthEnd = PeriodEnd,
                PeriodLocked = true
            });

            PayrollCalendars.Add(new PayrollCalendar { Id = 1, TenantId = TenantId, PayrollMonth = 1, PayrollYear = 2024 });

            Employees.Add(new Employee
            {
                Id = EmployeeId,
                TenantId = TenantId,
                Code = "E1",
                FirstNameEn = "E",
                LastNameEn = "One",
                FullNameEn = "E One"
            });

            EmployeeContracts.Add(new EmployeeContract
            {
                Id = 1,
                TenantId = TenantId,
                EmployeeId = EmployeeId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31)
            });

            EmployeeOrgAssignments.Add(new EmployeeOrgAssignment
            {
                Id = 1,
                TenantId = TenantId,
                EmployeeId = EmployeeId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                PayrollAreaId = payrollArea.Id,
                PayrollArea = payrollArea
            });

            EmployeePayrollStatuses.Add(new EmployeePayrollStatus
            {
                Id = 1,
                TenantId = TenantId,
                EmployeeId = EmployeeId,
                DoNoAccountAfter = new DateTime(1, 1, 1),
                NextTimeEvaluationFrom = new DateTime(1, 1, 1)
            });

            SalaryRegisters.Add(new SalaryRegister
            {
                Id = 1,
                TenantId = TenantId,
                EmployeeId = EmployeeId,
                ForPeriod = PeriodEnd,
                InPeriod = PeriodEnd,
                PayrollRunType = PayrollRunType.Regular
            });

            WageTypes.Add(new WageType
            {
                Id = BasicWageTypeId,
                TenantId = TenantId,
                Code = "1000",
                NameEn = "Basic",
                NameAr = "Basic",
                BillingType = BillingType.Earning,
                IncludeInBasicPay = true,
                PayrollOrder = 1
            });

            SalaryRegisterDetails.Add(new SalaryRegisterDetail
            {
                Id = 1,
                TenantId = TenantId,
                SalaryRegisterId = 1,
                WageTypeId = BasicWageTypeId,
                Number = 30,
                Amount = 300,
                Rate = 10
            });

            EmployeeProjects.Add(new EmployeeProject
            {
                Id = 1,
                TenantId = TenantId,
                EmployeeId = EmployeeId,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = PeriodEnd,
                CostingMethod = ProjectCostingMethod.ActualDays
            });

            EmployeeProjectDetails.Add(new EmployeeProjectDetail
            {
                Id = 1,
                TenantId = TenantId,
                EmployeeProjectId = 1,
                ProjectId = ProjectId,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = PeriodEnd,
                Percentage = 100
            });

            ProjectWageTypes.Add(new ProjectWageType
            {
                Id = 1,
                TenantId = TenantId,
                ProjectId = ProjectId,
                WageTypeId = BasicWageTypeId
            });
        }

        public void AddTimeRegisterWorkingDays(int days)
        {
            for (var i = 0; i < days; i++)
            {
                TimeRegisters.Add(new TimeRegister
                {
                    Id = TimeRegisters.Count + 1,
                    TenantId = TenantId,
                    EmployeeId = EmployeeId,
                    DayDate = new DateTime(2024, 1, 1).AddDays(i),
                    ActualWorkingDay = 1
                });
            }
        }

        public ProjectCostEngineService Build()
        {
            var sysSettings = Substitute.For<ISysSettingsAccessor>();
            sysSettings.GetSecuritySettingsAsync().Returns(Task.FromResult<ManageSecuritySettings>(null));
            sysSettings.GetProjectCostingSettingsAsync().Returns(Task.FromResult<ManageProjectCostingSettings>(null));
            sysSettings.GetTimeSettingsAsync().Returns(Task.FromResult<ManageTimeSettings>(null));

            var rounding = Substitute.For<IPayrollRoundingService>();
            rounding.GetCurrencyDecimalRoundingAsync(Arg.Any<int>()).Returns(Task.FromResult(3));

            var service = new ProjectCostEngineService(
                ProjectCostEngineFakes.RepoOver(Employees),
                ProjectCostEngineFakes.RepoOver(EmployeeContracts),
                ProjectCostEngineFakes.RepoOver(EmployeeOrgAssignments),
                ProjectCostEngineFakes.RepoOver(EmployeePayrollStatuses),
                ProjectCostEngineFakes.RepoOver(EmployeeAbsences),
                ProjectCostEngineFakes.RepoOver(EmployeeOvertimes),
                ProjectCostEngineFakes.RepoOver(SalaryRegisters),
                ProjectCostEngineFakes.RepoOver(SalaryRegisterDetails),
                ProjectCostEngineFakes.RepoOver(PayrollControlRecords),
                ProjectCostEngineFakes.RepoOver(PayrollCalendars),
                ProjectCostEngineFakes.RepoOver(WageTypes),
                ProjectCostEngineFakes.RepoOver(AbsenceTypes),
                ProjectCostEngineFakes.RepoOver(TicketTypes),
                ProjectCostEngineFakes.RepoOver(TimeRegisters),
                ProjectCostEngineFakes.RepoOver(ExecutionLogs),
                ProjectCostEngineFakes.RepoOver(ProjectLogs),
                ProjectCostEngineFakes.RepoOver(ProjectRegisters),
                ProjectCostEngineFakes.RepoOver(ProjectRegisterDetails),
                ProjectCostEngineFakes.RepoOver(EmployeeProjects),
                ProjectCostEngineFakes.RepoOver(EmployeeProjectDetails),
                ProjectCostEngineFakes.RepoOver(ProjectWageTypes),
                sysSettings,
                rounding,
                new NullProjectCostOvertimeWageTypeResolver());

            service.UnitOfWorkManager = ProjectCostEngineFakes.UnitOfWorkManager();

            return service;
        }
    }
}
