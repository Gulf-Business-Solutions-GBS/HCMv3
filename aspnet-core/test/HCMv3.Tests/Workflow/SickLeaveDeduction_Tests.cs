using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// The sick-leave breakdown shown before applying -- legacy <c>EmployeeAbsence.GetSickLeaveUnpaidInfo</c>.
///
/// <para>
/// The slabs are cumulative ceilings on the sick days already taken this year, so the same request is paid
/// differently depending on what the employee has already used. Payroll applies these rules independently
/// (<see cref="HCMv3.Tests.PayrollManagement.EmployeeLeavePayCalculationServiceTests"/>); this only explains
/// them to the employee.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class SickLeaveDeduction_Tests : EssLeaveWorkflowTestBase
{
    private readonly ISickLeaveDeductionPreview _preview;

    public SickLeaveDeduction_Tests()
    {
        _preview = Resolve<ISickLeaveDeductionPreview>();
    }

    private static DateTime Soon => DateTime.Today.AddDays(30);

    /// <summary>Kuwait's Article 69 shape: first 10 days 25% deducted, next 10 50%, next 10 75%, next 30 100%.</summary>
    private int GivenSickLeaveSlabs(int employeeId, params (double UpTo, double Deduction)[] slabs)
    {
        var subGroupId = UsingDbContext(context =>
        {
            var group = context.Set<EmployeeGroup>().Add(new EmployeeGroup
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "G",
                NameAr = "G"
            }).Entity;

            context.SaveChanges();

            var subGroup = context.Set<EmployeeSubGroup>().Add(new EmployeeSubGroup
            {
                TenantId = TenantId,
                EmployeeGroupId = group.Id,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "SG",
                NameAr = "SG"
            }).Entity;

            context.SaveChanges();
            return subGroup.Id;
        });

        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                EmployeeSubGroupId = subGroupId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2035, 12, 31)
            });
        });

        var sickTypeId = UsingDbContext(context =>
        {
            var existing = context.Set<AbsenceType>().FirstOrDefault(t => t.Code == "3");
            if (existing != null)
            {
                return existing.Id;
            }

            var paidSick = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = "3",
                NameEn = "Sick Leave",
                NameAr = "Sick Leave",
                IsPaid = true,
                CanApplyAgain = true,
                AllowDuringProbation = true,
                AllowBackDatedDays = 3650
            }).Entity;

            context.SaveChanges();
            return paidSick.Id;
        });

        UsingDbContext(context =>
        {
            var rule = context.Set<AbsenceDeductionRule>().Add(new AbsenceDeductionRule
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                RuleName = "Sick leave"
            }).Entity;

            context.SaveChanges();

            foreach (var (upTo, deduction) in slabs)
            {
                context.Set<AbsenceDedRuleDetail>().Add(new AbsenceDedRuleDetail
                {
                    TenantId = TenantId,
                    AbsenceDeductionRuleId = rule.Id,
                    StartDate = new DateTime(2020, 1, 1),
                    EndDate = new DateTime(2035, 12, 31),
                    DaysCount = upTo,
                    DeductionUnit = DeductionUnit.Percentage,
                    DeductionNumber = deduction
                });
            }

            context.Set<EmployeeSubGroupAbsence>().Add(new EmployeeSubGroupAbsence
            {
                TenantId = TenantId,
                EmployeeSubGroupId = subGroupId,
                AbsenceTypeId = sickTypeId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2035, 12, 31),
                TotalDays = 75,
                AbsenceDeductionRuleId = rule.Id
            });

            context.SaveChanges();
        });

        return sickTypeId;
    }

    /// <summary>The unpaid sick leave type the employee actually applies for (legacy code "5").</summary>
    private int SickLeaveUnpaidType() =>
        CreateAbsenceType(t =>
        {
            t.Code = "5";
            t.IsPaid = false;
            t.IncludeWeekend = true;
            t.IncludePublicHoliday = true;
            t.IncludeRestDay = true;
        }, nameEn: "Sick Leave Unpaid");

    [Fact]
    public async Task A_Short_Sick_Leave_Should_Sit_Entirely_In_The_First_Slab()
    {
        var employeeId = CreateEmployee("SLD100");
        GivenSickLeaveSlabs(employeeId, (10, 25), (20, 50), (30, 75));
        var typeId = SickLeaveUnpaidType();

        var segments = await _preview.GetSegmentsAsync(employeeId, typeId, Soon, Soon.AddDays(4));

        segments.Count.ShouldBe(1);
        segments[0].FromDate.ShouldBe(Soon);
        segments[0].ToDate.ShouldBe(Soon.AddDays(4));
        segments[0].DeductionNumber.ShouldBe(25);
    }

    [Fact]
    public async Task A_Sick_Leave_Crossing_A_Slab_Should_Be_Split_At_The_Boundary()
    {
        var employeeId = CreateEmployee("SLD110");
        GivenSickLeaveSlabs(employeeId, (10, 25), (20, 50), (30, 75));
        var typeId = SickLeaveUnpaidType();

        // 14 days from a standing start: the first 10 at 25%, the next 4 at 50%.
        var segments = await _preview.GetSegmentsAsync(employeeId, typeId, Soon, Soon.AddDays(13));

        segments.Count.ShouldBe(2);
        segments[0].ToDate.ShouldBe(Soon.AddDays(9));
        segments[0].DeductionNumber.ShouldBe(25);
        segments[1].FromDate.ShouldBe(Soon.AddDays(10));
        segments[1].ToDate.ShouldBe(Soon.AddDays(13));
        segments[1].DeductionNumber.ShouldBe(50);
    }

    [Fact]
    public async Task Days_Already_Taken_This_Year_Should_Move_The_Starting_Slab()
    {
        var employeeId = CreateEmployee("SLD120");
        GivenSickLeaveSlabs(employeeId, (10, 25), (20, 50), (30, 75));
        var typeId = SickLeaveUnpaidType();

        // Eight sick days already recorded this year, so a new 5-day leave starts in the first slab and crosses
        // into the second after two days.
        GivenSickDaysAlreadyTaken(employeeId, days: 8, before: Soon);

        var segments = await _preview.GetSegmentsAsync(employeeId, typeId, Soon, Soon.AddDays(4));

        segments.Count.ShouldBe(2);
        segments[0].DeductionNumber.ShouldBe(25);
        segments[0].ToDate.ShouldBe(Soon.AddDays(1));
        segments[1].DeductionNumber.ShouldBe(50);
    }

    [Fact]
    public async Task Days_Past_The_Last_Slab_Should_Be_Fully_Deducted()
    {
        var employeeId = CreateEmployee("SLD130");
        GivenSickLeaveSlabs(employeeId, (10, 25), (20, 50));
        var typeId = SickLeaveUnpaidType();

        var segments = await _preview.GetSegmentsAsync(employeeId, typeId, Soon, Soon.AddDays(24));

        segments.Last().IsBeyondTheLastSlab.ShouldBeTrue();
        segments.Last().DeductionNumber.ShouldBe(100);
        segments.Last().ToDate.ShouldBe(Soon.AddDays(24));
    }

    [Fact]
    public async Task An_Employee_Already_Past_The_Last_Slab_Should_Get_Nothing_Paid()
    {
        var employeeId = CreateEmployee("SLD140");
        GivenSickLeaveSlabs(employeeId, (10, 25), (20, 50));
        var typeId = SickLeaveUnpaidType();
        GivenSickDaysAlreadyTaken(employeeId, days: 20, before: Soon);

        var segments = await _preview.GetSegmentsAsync(employeeId, typeId, Soon, Soon.AddDays(3));

        segments.Count.ShouldBe(1);
        segments[0].IsBeyondTheLastSlab.ShouldBeTrue();
        segments[0].DeductionNumber.ShouldBe(100);
    }

    [Fact]
    public async Task Any_Other_Leave_Type_Should_Have_No_Breakdown()
    {
        var employeeId = CreateEmployee("SLD150");
        GivenSickLeaveSlabs(employeeId, (10, 25));
        var annualId = CreateAbsenceType(t => t.Code = "1", nameEn: "Annual");

        (await _preview.GetSegmentsAsync(employeeId, annualId, Soon, Soon.AddDays(3))).ShouldBeEmpty();
    }

    [Fact]
    public async Task No_Deduction_Rule_Should_Mean_No_Breakdown()
    {
        var employeeId = CreateEmployee("SLD160");
        var typeId = SickLeaveUnpaidType();

        (await _preview.GetSegmentsAsync(employeeId, typeId, Soon, Soon.AddDays(3))).ShouldBeEmpty();
    }

    /// <summary>Sick days the time engine has already recorded this accrual year.</summary>
    private void GivenSickDaysAlreadyTaken(int employeeId, int days, DateTime before) =>
        UsingDbContext(context =>
        {
            for (var i = 1; i <= days; i++)
            {
                context.Set<TimeRegister>().Add(new TimeRegister
                {
                    TenantId = TenantId,
                    EmployeeId = employeeId,
                    DayDate = before.AddDays(-i),
                    SickLeaveUnpaidDay = 1
                });
            }
        });
}
