using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// The deduction window on a quota the time engine creates.
///
/// <para>
/// Legacy set it on every quota it created (<c>Program.cs</c> 596-597, 696-697, 1124-1125). A quota without
/// one accrues a balance nobody can use: the leave rules look for a quota whose DEDUCTION dates cover the
/// leave, and refuse with "No leave quota covering these dates" when there is none.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineService_QuotaDeductionWindow_Tests
{
    private static readonly DateTime Day = new DateTime(2024, 6, 1);
    private static readonly DateTime EntitlementFrom = new DateTime(2020, 1, 1);
    private static readonly DateTime EntitlementTo = new DateTime(9999, 12, 31);
    private static readonly DateTime OpenEnded = new DateTime(9999, 12, 31);

    private const int EmployeeId = 300;
    private const int SubGroupId = 400;
    private const int AnnualLeaveTypeId = 61;
    private const int SickLeaveTypeId = 62;

    private readonly TimeEngineServiceTestHarness _harness = new TimeEngineServiceTestHarness();

    private readonly Employee _employee = new Employee
    {
        Id = EmployeeId,
        TenantId = 1,
        FirstNameEn = "Test",
        LastNameEn = "Employee",
        FirstNameAr = "Test",
        LastNameAr = "Employee",
        FullNameEn = "Test Employee",
        FullNameAr = "Test Employee",
        JoiningDate = EntitlementFrom
    };

    private readonly EmployeeSubGroup _subGroup = new EmployeeSubGroup { Id = SubGroupId, TenantId = 1 };

    public TimeEngineService_QuotaDeductionWindow_Tests()
    {
        _harness.AccrualService
            .GetLeaveAccrualStartEndAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult((EntitlementFrom, EntitlementTo)));
    }

    [Fact]
    public async Task Should_Open_An_Annual_Leave_Quota_For_Deduction_After_The_Configured_Months()
    {
        var context = Context(leaveDeductionStartMonth: 6);
        context.AnnualLeaveSubGroupAbsence = SubGroupAbsence(AnnualLeaveTypeId);

        await _harness.InvokeAsync("EnsureAnnualLeaveQuotaAsync", context, _subGroup);

        var quota = _harness.Store<EmployeeAbsenceQuota>().ShouldHaveSingleItem();
        quota.EntitlementFrom.ShouldBe(EntitlementFrom);
        quota.DeductionFrom.ShouldBe(new DateTime(2020, 7, 1));
        quota.DeductionTo.ShouldBe(OpenEnded);
    }

    [Fact]
    public async Task Should_Open_An_Annual_Leave_Quota_From_Its_Entitlement_Date_When_No_Delay_Is_Configured()
    {
        var context = Context(leaveDeductionStartMonth: 0);
        context.AnnualLeaveSubGroupAbsence = SubGroupAbsence(AnnualLeaveTypeId);

        await _harness.InvokeAsync("EnsureAnnualLeaveQuotaAsync", context, _subGroup);

        var quota = _harness.Store<EmployeeAbsenceQuota>().ShouldHaveSingleItem();
        quota.DeductionFrom.ShouldBe(EntitlementFrom);
        quota.DeductionTo.ShouldBe(OpenEnded);
    }

    [Fact]
    public async Task Should_Open_Every_Other_Accrual_Leave_Quota_From_Its_Entitlement_Date()
    {
        // The delay is for annual leave only: legacy wrote EntitlmentFrom itself for the rest.
        var context = Context(leaveDeductionStartMonth: 6);
        context.MasterData.AnnualLeaveTypes = new List<AbsenceType>
        {
            new AbsenceType { Id = AnnualLeaveTypeId, TenantId = 1, Code = "1", NameEn = "Annual", NameAr = "Annual" },
            new AbsenceType { Id = SickLeaveTypeId, TenantId = 1, Code = "3", NameEn = "Sick", NameAr = "Sick" }
        };
        context.MasterData.EmployeeSubGroupAbsences = new List<EmployeeSubGroupAbsence> { SubGroupAbsence(SickLeaveTypeId) };

        await _harness.InvokeAsync("EnsureAccrualLeaveQuotasAsync", context, _subGroup);

        var quota = _harness.Store<EmployeeAbsenceQuota>().ShouldHaveSingleItem();
        quota.AbsenceTypeId.ShouldBe(SickLeaveTypeId);
        quota.DeductionFrom.ShouldBe(EntitlementFrom);
        quota.DeductionTo.ShouldBe(OpenEnded);
    }

    [Fact]
    public async Task Should_Open_A_Gratuity_Quota_From_Its_Entitlement_Date()
    {
        var context = Context(leaveDeductionStartMonth: 6);
        context.MasterData.EmployeeSubGroupGratuities = new List<EmployeeSubGroupGratuity>
        {
            new EmployeeSubGroupGratuity
            {
                TenantId = 1,
                EmployeeSubGroupId = SubGroupId,
                StartDate = Day.AddYears(-1),
                EndDate = Day.AddYears(1)
            }
        };

        await _harness.InvokeAsync("EnsureGratuityQuotaAsync", context, _subGroup);

        var quota = _harness.Store<EmployeeGratuityQuota>().ShouldHaveSingleItem();
        quota.DeductionFrom.ShouldBe(EntitlementFrom);
        quota.DeductionTo.ShouldBe(OpenEnded);
    }

    private static EmployeeSubGroupAbsence SubGroupAbsence(int absenceTypeId)
    {
        return new EmployeeSubGroupAbsence
        {
            TenantId = 1,
            EmployeeSubGroupId = SubGroupId,
            AbsenceTypeId = absenceTypeId,
            StartDate = Day.AddYears(-1),
            EndDate = Day.AddYears(1),
            ChangeYear = 99
        };
    }

    private TimeRegisterDayContext Context(int leaveDeductionStartMonth)
    {
        return new TimeRegisterDayContext
        {
            DayDate = Day,
            Employee = _employee,
            TimeRegister = new TimeRegister { TenantId = 1, EmployeeId = EmployeeId, DayDate = Day },
            Settings = new TimeEngineSettings { LeaveDeductionStartMonth = leaveDeductionStartMonth },
            MasterData = new TimeEngineMasterData { AnnualLeaveTypeId = AnnualLeaveTypeId },
            EmployeeContext = new TimeEngineEmployeeContext
            {
                Employee = _employee,
                EmployeeSubGroup = _subGroup,
                StartDate = Day,
                EndDate = Day
            }
        };
    }
}
