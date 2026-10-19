using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Legacy <c>HCM10.Module/BusinessObjects/PersonnelAdministration/EmployeeAttendance.cs</c>:
/// <c>UpdateAttendanceQuota</c> (344-442), <c>GetHoursToAttendanceDay</c> (465-489), <c>GetPlannedminutes</c>
/// (490-573), <c>OnDeleting</c> (309-343) and <c>AddEmployeeAbsQuota</c> (652-733). Every expected value below is
/// worked by hand from those lines.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeAttendanceCalculationService_Tests : AppTestBase
{
    private static readonly DateTime JoiningDate = new(2020, 1, 1);

    /// <summary>A Monday; the standard week below makes it a working day.</summary>
    private static readonly DateTime Monday = new(2024, 4, 1);

    private readonly IEmployeeAttendanceCalculationService _service;
    private readonly IEmployeeAttendancesAppService _appService;

    public EmployeeAttendanceCalculationService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _service = Resolve<IEmployeeAttendanceCalculationService>();
        _appService = Resolve<IEmployeeAttendancesAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    #region Fixtures

    private int CreateEmployee(string code)
    {
        return UsingDbContext(context =>
        {
            var nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
            context.SaveChanges();

            var employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = JoiningDate,
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
            context.SaveChanges();
            return employee.Id;
        });
    }

    /// <summary>
    /// Every day of the week is <paramref name="start"/>-<paramref name="end"/> except Friday, a weekly off.
    /// Time settings allow attendance on a week-off/rest day unless a test says otherwise.
    /// </summary>
    private void GiveDuty(int employeeId, TimeSpan start, TimeSpan end, bool nightShift = false, TimeSpan? breakStart = null, TimeSpan? breakEnd = null)
    {
        UsingDbContext(context =>
        {
            int? breakShiftId = null;
            if (breakStart.HasValue)
            {
                var breakShift = context.Set<BreakShift>().Add(new BreakShift
                {
                    TenantId = TenantId,
                    Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                    NameEn = "Lunch",
                    NameAr = "Lunch",
                    StartTime = breakStart.Value,
                    EndTime = breakEnd.Value
                }).Entity;
                context.SaveChanges();
                breakShiftId = breakShift.Id;
            }

            var working = context.Set<Duty>().Add(new Duty
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Work",
                NameAr = "Work",
                StartTime = start,
                EndTime = end,
                IsNightShift = nightShift,
                BreakShiftId = breakShiftId
            }).Entity;
            var off = context.Set<Duty>().Add(new Duty
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Friday Off",
                NameAr = "Friday Off",
                IsWeeklyOff = true
            }).Entity;
            context.SaveChanges();

            var period = context.Set<DutyPeriod>().Add(new DutyPeriod
            {
                TenantId = TenantId,
                NameEn = "KW",
                NameAr = "KW",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2035, 12, 31),
                SundayDutyId = working.Id,
                MondayDutyId = working.Id,
                TuesdayDutyId = working.Id,
                WednesdayDutyId = working.Id,
                ThursdayDutyId = working.Id,
                FridayDutyId = off.Id,
                SaturdayDutyId = working.Id
            }).Entity;
            context.SaveChanges();

            context.Set<EmployeeDutyPeriod>().Add(new EmployeeDutyPeriod
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DutyPeriodId = period.Id,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2035, 12, 31)
            });
        });

        SetTimeSettings(weekOffAllowed: true);
    }

    private void SetTimeSettings(bool weekOffAllowed)
    {
        UsingDbContext(context =>
        {
            var settings = context.Set<ManageTimeSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                           ?? context.Set<ManageTimeSettings>().Add(new ManageTimeSettings { TenantId = TenantId }).Entity;
            settings.EnableAttendanceOnWeekOff = weekOffAllowed;
            settings.EnableAttendanceOnRestDay = true;
        });
    }

    private void SetDefaultWorkingHours(double hours)
    {
        UsingDbContext(context =>
        {
            context.Set<ManagePayrollSettings>().Add(new ManagePayrollSettings
            {
                TenantId = TenantId,
                DefaultWorkingHours = hours
            });
        });
    }

    private int CreateAbsenceType(string code, AccrualPeriod accrualPeriod = AccrualPeriod.WorkYear,
        QuotaType quotaType = QuotaType.None, CustomStartMonth customStartMonth = CustomStartMonth.None, bool reInitialize = false)
    {
        return UsingDbContext(context =>
        {
            var type = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = "Leave " + code,
                NameAr = "Leave " + code,
                AccrualPeriod = accrualPeriod,
                QuotaType = quotaType,
                CustomStartMonth = customStartMonth,
                ReInitialize = reInitialize
            }).Entity;
            context.SaveChanges();
            return type.Id;
        });
    }

    private int CreateAttendanceType(string code, int? linkLeaveQuotaId = null, bool haveQuotaAccrual = false)
    {
        return UsingDbContext(context =>
        {
            var type = context.Set<AttendanceType>().Add(new AttendanceType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code,
                NameAr = code,
                HaveQuotaAccrual = haveQuotaAccrual,
                LinkLeaveQuotaId = linkLeaveQuotaId
            }).Entity;
            context.SaveChanges();
            return type.Id;
        });
    }

    /// <summary>A quota row whose entitlement AND deduction periods cover <see cref="JoiningDate"/>.</summary>
    private int CreateQuota(int employeeId, int absenceTypeId, double accrued,
        DateTime? from = null, DateTime? to = null)
    {
        return UsingDbContext(context =>
        {
            var quota = context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                EntitlementFrom = from ?? JoiningDate,
                EntitlementTo = to ?? new DateTime(9999, 12, 31),
                DeductionFrom = from ?? JoiningDate,
                DeductionTo = to ?? new DateTime(9999, 12, 31),
                QuotaAccrued = accrued
            }).Entity;
            context.SaveChanges();
            return quota.Id;
        });
    }

    private double AccruedOn(int quotaId) =>
        UsingDbContext(context => context.Set<EmployeeAbsenceQuota>().Single(q => q.Id == quotaId).QuotaAccrued);

    private System.Collections.Generic.List<EmployeeAbsenceQuota> QuotasOf(int employeeId, int absenceTypeId) =>
        UsingDbContext(context => context.Set<EmployeeAbsenceQuota>()
            .Where(q => q.EmployeeId == employeeId && q.AbsenceTypeId == absenceTypeId)
            .OrderBy(q => q.Id)
            .ToList());

    private Task SaveDayAttendanceAsync(int employeeId, int attendanceTypeId, DateTime start, DateTime end, int? id = null) =>
        _appService.CreateOrEdit(new CreateOrEditEmployeeAttendanceDto
        {
            Id = id,
            EmployeeId = employeeId,
            AttendanceTypeId = attendanceTypeId,
            AttendanceUnit = AttendanceUnit.Period,
            StartDate = start,
            EndDate = end
        });

    private async Task<EmployeeAttendanceDto> OnlyAttendanceAsync(int employeeId) =>
        (await _appService.GetAll(new GetAllEmployeeAttendancesInput { EmployeeId = employeeId })).Items.Single();

    #endregion

    #region TotalAttendDays -- Period / Hours (legacy lines 348-381)

    [Fact]
    public async Task Period_With_Times_Should_Derive_Hours_And_Day_Fraction_From_Duty()
    {
        // Duty 08:00-16:00 = 480 planned minutes. Times 09:00-13:00 -> hours 4 (line 353), overwriting the 7 typed;
        // days = 240 / 480 = 0.5 (line 480).
        var employeeId = CreateEmployee("EAC100");
        GiveDuty(employeeId, new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0));
        var typeId = CreateAttendanceType("EAC100");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeAttendanceDto
        {
            EmployeeId = employeeId,
            AttendanceTypeId = typeId,
            AttendanceUnit = AttendanceUnit.Period,
            StartDate = Monday,
            EndDate = Monday,
            StartTime = Monday.AddHours(9),
            EndTime = Monday.AddHours(13),
            AttendanceHours = 7
        });

        var row = await OnlyAttendanceAsync(employeeId);
        row.AttendanceHours.ShouldBe(4);
        row.TotalAttendDays.ShouldBe(0.5);
    }

    [Fact]
    public async Task Period_With_Times_Should_Cap_At_One_Day()
    {
        // 08:00-18:00 = 10 h = 600 min over 480 -> 1.25, capped to 1 (lines 358-359).
        var employeeId = CreateEmployee("EAC110");
        GiveDuty(employeeId, new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0));
        var typeId = CreateAttendanceType("EAC110");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeAttendanceDto
        {
            EmployeeId = employeeId,
            AttendanceTypeId = typeId,
            AttendanceUnit = AttendanceUnit.Period,
            StartDate = Monday,
            EndDate = Monday,
            StartTime = Monday.AddHours(8),
            EndTime = Monday.AddHours(18)
        });

        var row = await OnlyAttendanceAsync(employeeId);
        row.AttendanceHours.ShouldBe(10);
        row.TotalAttendDays.ShouldBe(1);
    }

    [Fact]
    public async Task Period_Without_Times_Should_Count_Calendar_Days()
    {
        // Line 365: EndDate - StartDate + 1. No duty needed.
        var employeeId = CreateEmployee("EAC120");
        var typeId = CreateAttendanceType("EAC120");

        await SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday.AddDays(2));

        (await OnlyAttendanceAsync(employeeId)).TotalAttendDays.ShouldBe(3);
    }

    [Fact]
    public async Task Hours_Unit_Should_Not_Be_Capped_At_One_Day()
    {
        // LEGACY ODDITY (lines 374-379): unlike Period, the Hours unit is floored at 0 but never capped at 1.
        // 10 h over 480 min = 1.25. Kept as-is under the parity rule.
        var employeeId = CreateEmployee("EAC130");
        GiveDuty(employeeId, new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0));
        var typeId = CreateAttendanceType("EAC130");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeAttendanceDto
        {
            EmployeeId = employeeId,
            AttendanceTypeId = typeId,
            AttendanceUnit = AttendanceUnit.Hours,
            StartDate = Monday,
            EndDate = Monday,
            AttendanceHours = 10
        });

        (await OnlyAttendanceAsync(employeeId)).TotalAttendDays.ShouldBe(1.25);
    }

    [Fact]
    public async Task Planned_Minutes_Should_Exclude_The_Break()
    {
        // Lines 560-561: (12:00 - 08:00) + (16:00 - 13:00) = 240 + 180 = 420.
        var employeeId = CreateEmployee("EAC140");
        GiveDuty(employeeId, new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0),
            breakStart: new TimeSpan(12, 0, 0), breakEnd: new TimeSpan(13, 0, 0));

        (await _service.GetPlannedMinutesAsync(employeeId, Monday)).ShouldBe(420);
        // 3.5 h = 210 / 420 = 0.5.
        (await _service.GetHoursToAttendanceDayAsync(employeeId, Monday, 3.5)).ShouldBe(0.5);
    }

    [Fact]
    public async Task Planned_Minutes_Should_Run_Past_Midnight_For_A_Night_Shift()
    {
        // Lines 534-536: 22:00 to 06:00 next day = 480.
        var employeeId = CreateEmployee("EAC150");
        GiveDuty(employeeId, new TimeSpan(22, 0, 0), new TimeSpan(6, 0, 0), nightShift: true);

        (await _service.GetPlannedMinutesAsync(employeeId, Monday)).ShouldBe(480);
    }

    [Fact]
    public async Task Zero_Planned_Minutes_Should_Fall_Back_To_Default_Working_Hours()
    {
        // Lines 475-476 (Zoho #2748): 08:00-08:00 plans 0 minutes -> DefaultWorkingHours 6 = 360 min.
        // 3 h = 180 / 360 = 0.5.
        var employeeId = CreateEmployee("EAC160");
        GiveDuty(employeeId, new TimeSpan(8, 0, 0), new TimeSpan(8, 0, 0));
        SetDefaultWorkingHours(6);

        (await _service.GetHoursToAttendanceDayAsync(employeeId, Monday, 3)).ShouldBe(0.5);
    }

    [Fact]
    public async Task Negative_Shift_Length_Should_Also_Fall_Back_To_Default_Working_Hours()
    {
        // Lines 564-565 floor a non-night 16:00-08:00 duty at 0, and 475-476 then use DefaultWorkingHours 8.
        // 2 h = 120 / 480 = 0.25.
        var employeeId = CreateEmployee("EAC165");
        GiveDuty(employeeId, new TimeSpan(16, 0, 0), new TimeSpan(8, 0, 0));
        SetDefaultWorkingHours(8);

        (await _service.GetPlannedMinutesAsync(employeeId, Monday)).ShouldBe(0);
        (await _service.GetHoursToAttendanceDayAsync(employeeId, Monday, 2)).ShouldBe(0.25);
    }

    [Fact]
    public async Task Day_Fraction_Should_Be_Rounded_To_Three_Places()
    {
        // Line 480: 60 / 420 = 0.142857... -> 0.143.
        var employeeId = CreateEmployee("EAC170");
        GiveDuty(employeeId, new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0),
            breakStart: new TimeSpan(12, 0, 0), breakEnd: new TimeSpan(13, 0, 0));

        (await _service.GetHoursToAttendanceDayAsync(employeeId, Monday, 1)).ShouldBe(0.143);
    }

    [Fact]
    public async Task Hours_Unit_Without_A_Duty_Should_Be_Refused()
    {
        // Lines 498-499: no duty for the day -> refused.
        var employeeId = CreateEmployee("EAC180");
        var typeId = CreateAttendanceType("EAC180");
        SetTimeSettings(weekOffAllowed: true);

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(new CreateOrEditEmployeeAttendanceDto
        {
            EmployeeId = employeeId,
            AttendanceTypeId = typeId,
            AttendanceUnit = AttendanceUnit.Hours,
            StartDate = Monday,
            EndDate = Monday,
            AttendanceHours = 2
        }));
    }

    [Fact]
    public async Task Planned_Minutes_Should_Refuse_A_Week_Off_Unless_Allowed()
    {
        // Lines 502-515.
        var employeeId = CreateEmployee("EAC190");
        GiveDuty(employeeId, new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0));
        SetTimeSettings(weekOffAllowed: false);
        var friday = Monday.AddDays(4);

        var exception = await Should.ThrowAsync<UserFriendlyException>(() => _service.GetPlannedMinutesAsync(employeeId, friday));
        exception.Message.ShouldContain("Friday Off");
    }

    [Fact]
    public async Task Day_Unit_Should_Leave_TotalAttendDays_Untouched_In_The_Service()
    {
        // Lines 368-371: the Day branch is empty in legacy. (The admin screen applies HCMv3's own Day rule.)
        var attendance = new EmployeeAttendance { AttendanceUnit = AttendanceUnit.Day, TotalAttendDays = 4 };

        await _service.ApplyTotalAttendDaysAsync(attendance);

        attendance.TotalAttendDays.ShouldBe(4);
    }

    [Fact]
    public async Task Edit_Output_Should_Carry_TotalAttendDays_For_Display()
    {
        var employeeId = CreateEmployee("EAC195");
        var typeId = CreateAttendanceType("EAC195");
        await SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday.AddDays(1));

        var forEdit = await _appService.GetEmployeeAttendanceForEdit(new EntityDto((await OnlyAttendanceAsync(employeeId)).Id));

        forEdit.EmployeeAttendance.TotalAttendDays.ShouldBe(2);
    }

    #endregion

    #region Quota accrual (legacy lines 383-436, 309-343, 652-733)

    [Fact]
    public async Task Should_Accrue_TotalAttendDays_Into_Linked_Quota()
    {
        // Line 431: QuotaAccrued 5 + 3 days = 8.
        var employeeId = CreateEmployee("EAQ100");
        var leaveId = CreateAbsenceType("CMP100");
        var quotaId = CreateQuota(employeeId, leaveId, accrued: 5);
        var typeId = CreateAttendanceType("EAQ100", leaveId, haveQuotaAccrual: true);

        await SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday.AddDays(2));

        AccruedOn(quotaId).ShouldBe(8);
    }

    [Fact]
    public async Task Should_Not_Accrue_When_Type_Has_No_Quota_Accrual()
    {
        var employeeId = CreateEmployee("EAQ110");
        var leaveId = CreateAbsenceType("CMP110");
        var quotaId = CreateQuota(employeeId, leaveId, accrued: 5);
        var typeId = CreateAttendanceType("EAQ110", leaveId, haveQuotaAccrual: false);

        await SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday.AddDays(2));

        AccruedOn(quotaId).ShouldBe(5);
    }

    [Fact]
    public async Task Should_Refuse_Accrual_Type_Without_Linked_Leave_Type()
    {
        // Lines 416-417.
        var employeeId = CreateEmployee("EAQ120");
        var typeId = CreateAttendanceType("EAQ120", linkLeaveQuotaId: null, haveQuotaAccrual: true);

        await Should.ThrowAsync<UserFriendlyException>(() => SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday));
    }

    [Fact]
    public async Task Should_Create_Work_Year_Quota_When_None_Exists()
    {
        // Lines 421-422, 666-675: EntitlementFrom = JoiningDate, open-ended to 9999-12-31, deduction the same
        // (dedStartMonth hard-coded 0, line 658); QuotaAccrued = 0 + 2 days.
        var employeeId = CreateEmployee("EAQ130");
        var leaveId = CreateAbsenceType("CMP130", AccrualPeriod.WorkYear);
        var typeId = CreateAttendanceType("EAQ130", leaveId, haveQuotaAccrual: true);

        await SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday.AddDays(1));

        var quota = QuotasOf(employeeId, leaveId).Single();
        quota.EntitlementFrom.ShouldBe(JoiningDate);
        quota.EntitlementTo.ShouldBe(new DateTime(9999, 12, 31));
        quota.DeductionFrom.ShouldBe(JoiningDate);
        quota.DeductionTo.ShouldBe(new DateTime(9999, 12, 31));
        quota.QuotaAccrued.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Create_Custom_Quota_From_Start_Month_Of_Joining_Year()
    {
        // Lines 700-712: CustomStartMonth March -> 2020-03-01 (joining year 2020).
        var employeeId = CreateEmployee("EAQ140");
        var leaveId = CreateAbsenceType("CMP140", AccrualPeriod.Custom, customStartMonth: CustomStartMonth.March);
        var typeId = CreateAttendanceType("EAQ140", leaveId, haveQuotaAccrual: true);

        await SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday);

        var quota = QuotasOf(employeeId, leaveId).Single();
        quota.EntitlementFrom.ShouldBe(new DateTime(2020, 3, 1));
        quota.QuotaAccrued.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Refuse_Creating_Quota_For_Unsupported_Accrual_Period()
    {
        // Line 721: only WorkYear, CalendarYear and Custom are handled.
        var employeeId = CreateEmployee("EAQ150");
        var leaveId = CreateAbsenceType("CMP150", AccrualPeriod.FiscalYear);
        var typeId = CreateAttendanceType("EAQ150", leaveId, haveQuotaAccrual: true);

        await Should.ThrowAsync<UserFriendlyException>(() => SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday));
    }

    [Fact]
    public async Task ReInitialize_Type_Should_Create_A_New_Undated_Quota_Row_On_Every_Save()
    {
        // LEGACY ODDITY (line 663): with ReInitialize on, AddEmployeeAbsQuota sets no dates, so the next lookup
        // (deduction period covering the joining date, line 419) never finds the row again and another is
        // created. Kept as-is under the parity rule.
        var employeeId = CreateEmployee("EAQ160");
        var leaveId = CreateAbsenceType("CMP160", reInitialize: true);
        var typeId = CreateAttendanceType("EAQ160", leaveId, haveQuotaAccrual: true);

        await SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday);
        await SaveDayAttendanceAsync(employeeId, typeId, Monday.AddDays(7), Monday.AddDays(7));

        var quotas = QuotasOf(employeeId, leaveId);
        quotas.Count.ShouldBe(2);
        quotas.ShouldAllBe(q => q.DeductionFrom == default(DateTime) && q.QuotaAccrued == 1);
    }

    [Fact]
    public async Task Should_Use_Quota_Covering_Joining_Date_Not_Attendance_Date()
    {
        // LEGACY ODDITY (lines 394-395, 419): the quota is found through the employee's JOINING date (the
        // attendance date is commented out). A 2024 row exists, but the 2024 attendance accrues into the row
        // covering 2020-01-01. Kept as-is under the parity rule.
        var employeeId = CreateEmployee("EAQ170");
        var leaveId = CreateAbsenceType("CMP170");
        var joiningRow = CreateQuota(employeeId, leaveId, accrued: 0, from: JoiningDate, to: new DateTime(2020, 12, 31));
        var currentRow = CreateQuota(employeeId, leaveId, accrued: 0, from: new DateTime(2024, 1, 1), to: new DateTime(2024, 12, 31));
        var typeId = CreateAttendanceType("EAQ170", leaveId, haveQuotaAccrual: true);

        await SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday);

        AccruedOn(joiningRow).ShouldBe(1);
        AccruedOn(currentRow).ShouldBe(0);
    }

    [Fact]
    public async Task Annual_Group_Linked_Type_Should_Accrue_Into_Code_1_Quota()
    {
        // LEGACY ODDITY (EmployeeAbsenceQuota.GetEmployeeAbsenceQuota, 5-arg overload): a linked type in the
        // AnnualLeave quota group accrues into the Code "1" type's row, not its own.
        var employeeId = CreateEmployee("EAQ180");
        var annualId = CreateAbsenceType("1");
        var groupedId = CreateAbsenceType("AL2", quotaType: QuotaType.AnnualLeave);
        var annualRow = CreateQuota(employeeId, annualId, accrued: 10);
        var ownRow = CreateQuota(employeeId, groupedId, accrued: 10);
        var typeId = CreateAttendanceType("EAQ180", groupedId, haveQuotaAccrual: true);

        await SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday.AddDays(1));

        AccruedOn(annualRow).ShouldBe(12);
        AccruedOn(ownRow).ShouldBe(10);
    }

    [Fact]
    public async Task Edit_Should_Give_Back_Previous_Days_Then_Accrue_New_Days()
    {
        // Lines 403 then 431: 5 + 3 = 8, edited to 1 day -> 8 - 3 + 1 = 6.
        var employeeId = CreateEmployee("EAQ190");
        var leaveId = CreateAbsenceType("CMP190");
        var quotaId = CreateQuota(employeeId, leaveId, accrued: 5);
        var typeId = CreateAttendanceType("EAQ190", leaveId, haveQuotaAccrual: true);
        await SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday.AddDays(2));

        await SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday, id: (await OnlyAttendanceAsync(employeeId)).Id);

        AccruedOn(quotaId).ShouldBe(6);
    }

    [Fact]
    public async Task Delete_Should_Give_Back_Accrued_Days()
    {
        // Line 330.
        var employeeId = CreateEmployee("EAQ200");
        var leaveId = CreateAbsenceType("CMP200");
        var quotaId = CreateQuota(employeeId, leaveId, accrued: 5);
        var typeId = CreateAttendanceType("EAQ200", leaveId, haveQuotaAccrual: true);
        await SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday.AddDays(2));

        await _appService.Delete(new EntityDto((await OnlyAttendanceAsync(employeeId)).Id));

        AccruedOn(quotaId).ShouldBe(5);
    }

    [Fact]
    public async Task Give_Back_Should_Run_For_A_Linked_Type_Even_Without_Quota_Accrual()
    {
        // LEGACY ODDITY (lines 318-331, 390): giving back tests only LinkLeaveQuota, not HaveQuotaAccrual. A row
        // of a linked-but-not-accruing type accrued nothing on save, yet its delete takes its days off the
        // quota (5 - 2 = 3, unfloored). Kept as-is under the parity rule.
        var employeeId = CreateEmployee("EAQ210");
        var leaveId = CreateAbsenceType("CMP210");
        var quotaId = CreateQuota(employeeId, leaveId, accrued: 5);
        var typeId = CreateAttendanceType("EAQ210", leaveId, haveQuotaAccrual: false);
        await SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday.AddDays(1));
        AccruedOn(quotaId).ShouldBe(5);

        await _appService.Delete(new EntityDto((await OnlyAttendanceAsync(employeeId)).Id));

        AccruedOn(quotaId).ShouldBe(3);
    }

    [Fact]
    public async Task Give_Back_Should_Refuse_When_Linked_Quota_Row_Is_Missing()
    {
        // Lines 398-399 / 325-326: linked type, but no row whose entitlement covers the joining date.
        var employeeId = CreateEmployee("EAQ220");
        var leaveId = CreateAbsenceType("CMP220");
        var typeId = CreateAttendanceType("EAQ220", leaveId, haveQuotaAccrual: false);
        await SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.Delete(new EntityDto(OnlyAttendanceIdSync(employeeId))));
    }

    [Fact]
    public async Task Delete_Of_Unlinked_Type_Should_Succeed()
    {
        // Deliberate difference, see EmployeeAttendanceCalculationService remarks: legacy OnDeleting (316-326)
        // crashes with a NullReferenceException here; the save path's own null test (line 390) is applied.
        var employeeId = CreateEmployee("EAQ230");
        var typeId = CreateAttendanceType("EAQ230");
        await SaveDayAttendanceAsync(employeeId, typeId, Monday, Monday);

        await _appService.Delete(new EntityDto((await OnlyAttendanceAsync(employeeId)).Id));

        (await _appService.GetAll(new GetAllEmployeeAttendancesInput { EmployeeId = employeeId })).TotalCount.ShouldBe(0);
    }

    private int OnlyAttendanceIdSync(int employeeId) =>
        UsingDbContext(context => context.Set<EmployeeAttendance>().Single(a => a.EmployeeId == employeeId).Id);

    #endregion
}
