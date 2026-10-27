using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Ess.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.SystemSettings;
using HCMv3.TravelManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// The leave-policy rules legacy applied to every leave, wherever it came from: the ESS request
/// (<c>WorkFlowServices.SaveWorkflowRequest</c>) and the leave row itself
/// (<c>EmployeeAbsence.PerformValidations</c> / <c>UpdateQuotaInformation</c>).
///
/// <para>
/// Each test configures one flag on the absence type and proves the rule fires, because a rule that exists but
/// is never reached is the failure mode worth guarding against: they all live in one service now, called from
/// both doors.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class LeaveRules_Tests : EssLeaveWorkflowTestBase
{
    private int _meId;

    private int GivenAnEmployee(string code = "RUL100")
    {
        _meId = CreateCurrentUserEmployee(code);
        EnableLeaveWorkflowWithApprover(code + "APP");
        return _meId;
    }

    private Task<ApplyForLeaveResultDto> ApplyAsync(int typeId, DateTime start, int days = 3) =>
        _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId,
            StartDate = start,
            EndDate = start.AddDays(days - 1)
        });

    private static DateTime Soon => DateTime.Today.AddDays(30);

    // ------------------------------------------------------------------------------------------ dates and days

    [Fact]
    public async Task Should_Refuse_A_Leave_Backdated_Further_Than_The_Type_Allows()
    {
        GivenAnEmployee();
        var typeId = CreateAbsenceType(t => t.AllowBackDatedDays = 0);
        CreateQuota(_meId, typeId, accrued: 30);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            ApplyAsync(typeId, DateTime.Today.AddDays(-5)));

        exception.Message.ShouldContain("backdated");
    }

    [Fact]
    public async Task Should_Allow_A_Leave_Inside_The_Backdating_Window()
    {
        GivenAnEmployee("RUL101");
        var typeId = CreateAbsenceType(t => t.AllowBackDatedDays = 15);
        CreateQuota(_meId, typeId, accrued: 30);

        var result = await ApplyAsync(typeId, DateTime.Today.AddDays(-5));

        result.EmployeeAbsenceId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Refuse_A_Leave_Type_That_Is_Not_Allowed_During_Probation()
    {
        GivenAnEmployee("RUL110");
        var typeId = CreateAbsenceType(t => t.AllowDuringProbation = false);
        CreateQuota(_meId, typeId, accrued: 30);
        SetProbationEndDate(_meId, DateTime.Today.AddMonths(2));

        await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, Soon));
    }

    [Fact]
    public async Task Should_Refuse_A_Leave_Starting_On_A_Week_Off()
    {
        // Legacy ValidationService: a leave may not start on a week-off unless the duty allows it.
        GivenAnEmployee("RUL120");
        var typeId = CreateAbsenceType(t => t.IncludeWeekend = false);
        CreateQuota(_meId, typeId, accrued: 30);
        SetupFridaySaturdayWeekend(_meId);

        var friday = NextSunday(Soon).AddDays(5);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, friday));

        exception.Message.ShouldContain(friday.ToString("dd/MM/yyyy"));
    }

    [Fact]
    public async Task Should_Refuse_A_Leave_Made_Up_Only_Of_Week_Offs()
    {
        // Legacy "Leave days should atleast a single Day." -- Friday to Saturday on a Fri/Sat weekend is no leave.
        GivenAnEmployee("RUL130");
        var typeId = CreateAbsenceType(t =>
        {
            t.IncludeWeekend = false;

            // So the start-day rule does not fire first: this test is about the day count.
            t.AllowBackDatedDays = 15;
        });
        CreateQuota(_meId, typeId, accrued: 30);
        SetupFridaySaturdayWeekend(_meId);
        AllowTransactionsOnAnyDay();

        var friday = NextSunday(Soon).AddDays(5);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            ApplyAsync(typeId, friday, days: 2));

        exception.Message.ShouldContain("no leave days");
    }

    [Fact]
    public async Task Should_Refuse_A_Leave_Starting_On_A_Day_The_Type_Blocks()
    {
        GivenAnEmployee("RUL140");
        var typeId = CreateAbsenceType(t => t.DontAllowToStartOnSunday = true);
        CreateQuota(_meId, typeId, accrued: 30);

        var sunday = NextSunday(Soon);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, sunday));

        exception.Message.ShouldContain("Sunday");
    }

    // ------------------------------------------------------------------------------------------ length and repeats

    [Fact]
    public async Task Should_Refuse_More_Days_Than_The_Type_Allows_At_A_Time()
    {
        GivenAnEmployee("RUL150");
        var typeId = CreateAbsenceType(t => { t.ApplyMaxLeave = true; t.MaxLimitInDays = 2; });
        CreateQuota(_meId, typeId, accrued: 30);

        await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, Soon, days: 5));
    }

    [Fact]
    public async Task Should_Allow_More_Than_The_Maximum_When_The_Excess_May_Become_Unpaid_Leave()
    {
        // Legacy exempts a type flagged CreateUnpaidAboveMaxLimitDays: the excess is meant to become unpaid leave.
        GivenAnEmployee("RUL151");
        var typeId = CreateAbsenceType(t =>
        {
            t.ApplyMaxLeave = true;
            t.MaxLimitInDays = 2;
            t.CreateUnpaidAboveMaxLimitDays = true;
        });
        CreateQuota(_meId, typeId, accrued: 30);

        (await ApplyAsync(typeId, Soon, days: 5)).EmployeeAbsenceId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Refuse_Fewer_Days_Than_The_Type_Requires()
    {
        GivenAnEmployee("RUL160");
        var typeId = CreateAbsenceType(t => { t.ApplyMinLeave = true; t.MinLimitInDays = 5; });
        CreateQuota(_meId, typeId, accrued: 30);

        await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, Soon, days: 2));
    }

    [Fact]
    public async Task Should_Refuse_A_Second_Leave_Of_A_Once_Only_Type()
    {
        GivenAnEmployee("RUL170");
        var typeId = CreateAbsenceType(t => t.CanApplyAgain = false);
        CreateQuota(_meId, typeId, accrued: 30);

        await ApplyAsync(typeId, Soon);

        await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, Soon.AddDays(10)));
    }

    [Fact]
    public async Task Should_Refuse_A_Second_Leave_Before_The_Types_Gap_Has_Passed()
    {
        GivenAnEmployee("RUL180");
        var typeId = CreateAbsenceType(t => t.CanApplyAfterDays = 30);
        CreateQuota(_meId, typeId, accrued: 30);

        await ApplyAsync(typeId, Soon);

        // The first leave ends on Soon+2; 30 days must pass before the next one.
        await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, Soon.AddDays(10)));

        (await ApplyAsync(typeId, Soon.AddDays(40))).EmployeeAbsenceId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Refuse_A_Leave_Asked_For_With_Too_Little_Notice()
    {
        GivenAnEmployee("RUL190");
        var typeId = CreateAbsenceType(t => t.CanApplyBeforeDays = 14);
        CreateQuota(_meId, typeId, accrued: 30);

        await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, DateTime.Today.AddDays(3)));

        (await ApplyAsync(typeId, DateTime.Today.AddDays(20))).EmployeeAbsenceId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Refuse_A_Leave_Before_The_Employee_Has_Served_Long_Enough()
    {
        GivenAnEmployee("RUL200");
        var typeId = CreateAbsenceType(t => t.MinActiveDaysInService = 20000);
        CreateQuota(_meId, typeId, accrued: 30);

        await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, Soon));
    }

    // ------------------------------------------------------------------------------------------ quota

    [Fact]
    public async Task Should_Refuse_An_Accrual_Type_With_No_Quota_Covering_The_Dates()
    {
        // Legacy "Leave Quota not found within the Deduction From - to Dates".
        GivenAnEmployee("RUL210");
        var typeId = CreateAbsenceType(t => t.HaveQuotaAccrual = true);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, Soon));

        exception.Message.ShouldContain("quota");
    }

    [Fact]
    public async Task Should_Allow_An_Overdraw_Within_The_Types_Negative_Balance_Limit()
    {
        GivenAnEmployee("RUL220");
        // The yearly entitlement matters: legacy allows an overdraw only up to what will have accrued by the
        // start date (capped at the limit), not the flat limit.
        var typeId = CreateAbsenceType(t => { t.EnableNegativeBalance = true; t.NegativeBalanceLimit = 5; t.TotalDaysAllowed = 30; });
        CreateQuota(_meId, typeId, accrued: 2);

        (await ApplyAsync(typeId, Soon, days: 5)).EmployeeAbsenceId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Still_Refuse_An_Overdraw_Beyond_The_Negative_Balance_Limit()
    {
        GivenAnEmployee("RUL221");
        var typeId = CreateAbsenceType(t => { t.EnableNegativeBalance = true; t.NegativeBalanceLimit = 1; t.TotalDaysAllowed = 30; });
        CreateQuota(_meId, typeId, accrued: 2);

        await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, Soon, days: 5));
    }

    [Fact]
    public async Task Should_Refuse_Unpaid_Leave_While_Annual_Leave_Remains()
    {
        GivenAnEmployee("RUL230");
        var annualId = CreateAbsenceType(t => t.Code = "1", nameEn: "Annual Paid");
        var unpaidId = CreateAbsenceType(t => t.Code = "2", nameEn: "Unpaid");
        CreateQuota(_meId, annualId, accrued: 10);
        SetRestrictUnpaidWhileAnnualAvailable(true);

        await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(unpaidId, Soon));

        SetRestrictUnpaidWhileAnnualAvailable(false);
        (await ApplyAsync(unpaidId, Soon)).EmployeeAbsenceId.ShouldBeGreaterThan(0);
    }

    // ------------------------------------------------------------------------------------------ other records

    [Fact]
    public async Task Should_Refuse_A_Leave_Over_Days_The_Employee_Is_Travelling()
    {
        GivenAnEmployee("RUL240");
        var typeId = CreateAbsenceType(t => t.CanApplyAgain = true);
        CreateQuota(_meId, typeId, accrued: 30);
        CreateTravel(_meId, Soon, Soon.AddDays(4));

        await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, Soon));
    }

    [Fact]
    public async Task Should_Refuse_A_Leave_Running_Past_The_Last_Day_In_Service()
    {
        GivenAnEmployee("RUL250");
        var typeId = CreateAbsenceType(t => t.CanApplyAgain = true);
        CreateQuota(_meId, typeId, accrued: 30);
        CreateSeparation(_meId, lastDayInService: Soon.AddDays(1), SeparationType.Resignation);

        await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, Soon, days: 5));
    }

    [Fact]
    public async Task Should_Refuse_A_Termination_Notice_Only_Type_When_There_Is_No_Exit_Record()
    {
        GivenAnEmployee("RUL260");
        var typeId = CreateAbsenceType(t => t.AllowInTerminationNoticeOnly = true);
        CreateQuota(_meId, typeId, accrued: 30);

        await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, Soon));
    }

    [Fact]
    public async Task Should_Refuse_A_Type_That_Is_Not_In_The_Employees_Salary_Profile()
    {
        GivenAnEmployee("RUL270");
        var typeId = CreateAbsenceType(t => t.IncludeInSalaryProfile = true);
        CreateQuota(_meId, typeId, accrued: 30);

        await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, Soon));
    }

    // ------------------------------------------------------------------------------------------ the other door

    [Fact]
    public async Task The_Same_Rules_Should_Apply_When_HR_Keys_The_Leave_In_Directly()
    {
        // The point of one rule service: HR's own screen cannot bypass what ESS enforces. Legacy ran the rules
        // on the row as well as on the request for exactly this reason.
        GivenAnEmployee("RUL280");
        var typeId = CreateAbsenceType(t => { t.ApplyMaxLeave = true; t.MaxLimitInDays = 2; });
        CreateQuota(_meId, typeId, accrued: 30);

        var admin = Resolve<IEmployeeAbsencesAppService>();

        await Assert.ThrowsAsync<UserFriendlyException>(() => admin.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
        {
            EmployeeId = _meId,
            AbsenceTypeId = typeId,
            StartDate = Soon,
            EndDate = Soon.AddDays(4)
        }));
    }

    // ------------------------------------------------------------------------------------------ arrange helpers

    private void SetProbationEndDate(int employeeId, DateTime probationEnd) =>
        UsingDbContext(context => context.Set<Employee>().Single(e => e.Id == employeeId).ProbationEndDate = probationEnd);

    private void SetRestrictUnpaidWhileAnnualAvailable(bool restrict) =>
        UsingDbContext(context =>
        {
            var settings = context.Set<ManageTimeSettings>().FirstOrDefault();
            if (settings == null)
            {
                context.Set<ManageTimeSettings>().Add(new ManageTimeSettings
                {
                    TenantId = TenantId,
                    RestrictUnpaidleaveIfAnnualleaveAvailable = restrict
                });
            }
            else
            {
                settings.RestrictUnpaidleaveIfAnnualleaveAvailable = restrict;
            }
        });

    /// <summary>Lets a leave start or end on a week-off, so a test can reach a later rule.</summary>
    private void AllowTransactionsOnAnyDay() =>
        UsingDbContext(context =>
        {
            foreach (var duty in context.Set<HCMv3.TimeManagement.Duty>().ToList())
            {
                duty.AllowTransStart = true;
                duty.AllowTransEnd = true;
            }
        });

    private void CreateTravel(int employeeId, DateTime start, DateTime end) =>
        UsingDbContext(context =>
        {
            var travelType = context.Set<TravelType>().Add(new TravelType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Business",
                NameAr = "Business"
            }).Entity;

            context.SaveChanges();

            context.Set<Travel>().Add(new Travel
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                TravelTypeId = travelType.Id,
                StartDate = start,
                EndDate = end
            });
        });

    private void CreateSeparation(int employeeId, DateTime lastDayInService, SeparationType separationType) =>
        UsingDbContext(context =>
        {
            var reason = context.Set<SeparationReason>().Add(new SeparationReason
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Reason",
                NameAr = "Reason"
            }).Entity;

            context.SaveChanges();

            context.Set<EmployeeSeparation>().Add(new EmployeeSeparation
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                SubmissionDate = DateTime.Today,
                LastDayInService = lastDayInService,
                PlannedLastDayInService = lastDayInService,
                SeparationType = separationType,
                SeparationReasonId = reason.Id
            });
        });
}
