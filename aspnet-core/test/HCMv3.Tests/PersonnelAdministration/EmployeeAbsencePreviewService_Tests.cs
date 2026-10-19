using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// HR leave form's calculated read-only fields -- legacy <c>HCM10.Module/BusinessObjects/PersonnelAdministration/
/// EmployeeAbsence.cs</c>: <c>UpdateCalendarDays</c> 2911-2917, <c>UpdateAbsenceHours</c> 2935-2941,
/// <c>RecalculateTotalLeaveDays</c> 2942-2969, <c>CurrentBalance</c> 408-437, <c>GetOtherLeaveQuota</c> 3158-3258,
/// <c>Paid_CalendarDays</c>/<c>DeductFromQuota</c> 820-866, <c>Unpaid_CalendarDays</c>/<c>UnpaidDays</c> 926-972.
/// Expected values are worked by hand from those lines.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeAbsencePreviewService_Tests : AppTestBase
{
    private static readonly DateTime JoiningDate = new(2020, 1, 1);

    /// <summary>Sunday 2026-02-01 to Saturday 2026-02-07: seven days, Friday 2026-02-06 a weekly off.</summary>
    private static readonly DateTime WeekStart = new(2026, 2, 1);

    private static readonly DateTime WeekEnd = new(2026, 2, 7);

    private readonly IEmployeeAbsencePreviewService _service;

    public EmployeeAbsencePreviewService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _service = Resolve<IEmployeeAbsencePreviewService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    #region Fixtures

    private int CreateEmployee(string code, bool withWeek = true)
    {
        var employeeId = UsingDbContext(context =>
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
                NationalityId = nationality.Id
            }).Entity;
            context.SaveChanges();
            return employee.Id;
        });

        if (withWeek)
        {
            GiveStandardWeek(employeeId);
        }

        return employeeId;
    }

    /// <summary>08:00-17:00 (540 minutes, no break) every day except Friday, a weekly off.</summary>
    private void GiveStandardWeek(int employeeId)
    {
        UsingDbContext(context =>
        {
            var working = context.Set<Duty>().Add(new Duty
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Work",
                NameAr = "Work",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            }).Entity;
            var off = context.Set<Duty>().Add(new Duty
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Off",
                NameAr = "Off",
                IsWeeklyOff = true
            }).Entity;
            context.SaveChanges();

            var period = context.Set<DutyPeriod>().Add(new DutyPeriod
            {
                TenantId = TenantId,
                NameEn = "DP",
                NameAr = "DP",
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
    }

    private int CreateAbsenceType(string code, bool haveQuotaAccrual = false, QuotaType quotaType = QuotaType.None,
        bool includeWeekend = false, double totalDaysAllowed = 0, bool reInitialize = false)
    {
        return UsingDbContext(context =>
        {
            var type = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = "Leave " + code,
                NameAr = "Leave " + code,
                HaveQuotaAccrual = haveQuotaAccrual,
                QuotaType = quotaType,
                IncludeWeekend = includeWeekend,
                TotalDaysAllowed = totalDaysAllowed,
                AccrualPeriod = AccrualPeriod.WorkYear,
                ReInitialize = reInitialize,
                ReInitializeAfter = 1
            }).Entity;
            context.SaveChanges();
            return type.Id;
        });
    }

    private void CreateQuota(int employeeId, int absenceTypeId, DateTime deductionFrom, DateTime deductionTo,
        double accrued, double used = 0, double adjusted = 0, double forfeited = 0)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                EntitlementFrom = deductionFrom,
                EntitlementTo = deductionTo,
                DeductionFrom = deductionFrom,
                DeductionTo = deductionTo,
                QuotaAccrued = accrued,
                QuotaUsed = used,
                QuotaAdjusted = adjusted,
                QuotaForfeited = forfeited
            });
        });
    }

    private void CreateLeave(int employeeId, int absenceTypeId, DateTime start, DateTime end, double totalLeaveDays)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                StartDate = start,
                EndDate = end,
                InitialEndDate = end,
                TotalLeaveDays = totalLeaveDays
            });
        });
    }

    private Task<EmployeeAbsencePreview> PreviewAsync(int employeeId, int absenceTypeId, DateTime start, DateTime end,
        Action<EmployeeAbsencePreviewRequest> configure = null)
    {
        var request = new EmployeeAbsencePreviewRequest
        {
            EmployeeId = employeeId,
            AbsenceTypeId = absenceTypeId,
            StartDate = start,
            EndDate = end
        };
        configure?.Invoke(request);
        return _service.CalculateAsync(request);
    }

    #endregion

    #region CalendarDays / AbsenceHours / TotalLeaveDays

    [Fact]
    public async Task Should_Count_Calendar_Days_And_Leave_Days_Less_Week_Off()
    {
        // 2911-2917: 7 calendar days. 2942-2969 (full day): 7 - 1 Friday off = 6.
        var employeeId = CreateEmployee("EAP100");
        var typeId = CreateAbsenceType("EAP100");

        var preview = await PreviewAsync(employeeId, typeId, WeekStart, WeekEnd);

        preview.CalendarDays.ShouldBe(7);
        preview.AbsenceHours.ShouldBe(0);
        preview.TotalLeaveDays.ShouldBe(6);
    }

    [Fact]
    public async Task Should_Derive_Hours_From_Times_And_Count_An_Hourly_Leave_As_A_Day_Fraction()
    {
        // 2935-2941: 09:00-13:00 = 4 h. 2959-2965: 240 / 540 planned minutes = 0.444 (rounded 3), no week-off.
        var employeeId = CreateEmployee("EAP110");
        var typeId = CreateAbsenceType("EAP110");
        var monday = new DateTime(2026, 2, 2);

        var preview = await PreviewAsync(employeeId, typeId, monday, monday, r =>
        {
            r.StartTime = monday.AddHours(9);
            r.EndTime = monday.AddHours(13);
        });

        preview.CalendarDays.ShouldBe(1);
        preview.AbsenceHours.ShouldBe(4);
        preview.TotalLeaveDays.ShouldBe(0.444);
    }

    [Fact]
    public async Task Should_Return_Zeros_Without_Dates()
    {
        var employeeId = CreateEmployee("EAP120");
        var typeId = CreateAbsenceType("EAP120");

        var preview = await PreviewAsync(employeeId, typeId, default, default);

        preview.CalendarDays.ShouldBe(0);
        preview.TotalLeaveDays.ShouldBe(0);
    }

    #endregion

    #region CurrentBalance

    [Fact]
    public async Task Accrual_Type_Balance_Should_Be_Quota_Balance()
    {
        // 418-422 + EmployeeAbsenceQuota.QuotaBalance (line 123): 30 - (5 + 1 + 2) = 22.
        var employeeId = CreateEmployee("EAP200");
        var typeId = CreateAbsenceType("EAP200", haveQuotaAccrual: true);
        CreateQuota(employeeId, typeId, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), accrued: 30, used: 5, adjusted: 1, forfeited: 2);

        var preview = await PreviewAsync(employeeId, typeId, WeekStart, WeekEnd);

        preview.CurrentBalance.ShouldBe(22);
    }

    [Fact]
    public async Task Accrual_Type_Balance_Should_Need_A_Deduction_Period_Covering_The_Whole_Leave()
    {
        // LEGACY ODDITY (EmployeeAbsenceQuota.GetEmployeeAbsenceQuota 5-arg: DeductionFrom <= start && DeductionTo
        // >= end): a quota row ending inside the leave is not found, so the balance shows 0. Kept as-is.
        var employeeId = CreateEmployee("EAP210");
        var typeId = CreateAbsenceType("EAP210", haveQuotaAccrual: true);
        CreateQuota(employeeId, typeId, new DateTime(2026, 1, 1), new DateTime(2026, 2, 3), accrued: 30);

        var preview = await PreviewAsync(employeeId, typeId, WeekStart, WeekEnd);

        preview.CurrentBalance.ShouldBe(0);
    }

    [Fact]
    public async Task Annual_Group_Accrual_Type_Should_Show_The_Code_1_Balance()
    {
        // LEGACY ODDITY (GetEmployeeAbsenceQuota 5-arg): an AnnualLeave-group type reads the Code "1" row.
        var employeeId = CreateEmployee("EAP220");
        var annualId = CreateAbsenceType("1", haveQuotaAccrual: true);
        var groupedId = CreateAbsenceType("AL2", haveQuotaAccrual: true, quotaType: QuotaType.AnnualLeave);
        CreateQuota(employeeId, annualId, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), accrued: 25);
        CreateQuota(employeeId, groupedId, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), accrued: 3);

        var preview = await PreviewAsync(employeeId, groupedId, WeekStart, WeekEnd);

        preview.CurrentBalance.ShouldBe(25);
    }

    [Fact]
    public async Task Non_Accrual_Type_Balance_Should_Be_Allowance_Less_Leave_Taken_In_The_Period()
    {
        // GetOtherLeaveQuota (3158-3258). Allowance 10 (type TotalDaysAllowed, no salary profile, line 3180).
        // Accrual period (ReInitialize yearly from joining 2020-01-01): 2026-01-01 .. 2026-12-31.
        // Used (3195-3200): Jan 10-12 (3) + Dec 30 - Jan 2 (4) = 7; a June 2025 leave is outside.
        // Straddling the start (3203-3220): Dec 30-31 = 2 calendar days, neither a Friday -> 7 - 2 = 5.
        // Balance 10 - 5 = 5.
        var employeeId = CreateEmployee("EAP230");
        var typeId = CreateAbsenceType("EAP230", totalDaysAllowed: 10, reInitialize: true);
        CreateLeave(employeeId, typeId, new DateTime(2026, 1, 10), new DateTime(2026, 1, 12), 3);
        CreateLeave(employeeId, typeId, new DateTime(2025, 12, 30), new DateTime(2026, 1, 2), 4);
        CreateLeave(employeeId, typeId, new DateTime(2025, 6, 1), new DateTime(2025, 6, 5), 5);

        var preview = await PreviewAsync(employeeId, typeId, WeekStart, WeekEnd);

        preview.CurrentBalance.ShouldBe(5);
    }

    [Fact]
    public async Task Non_Accrual_Type_With_No_Allowance_Should_Show_Zero()
    {
        // Line 3182: an allowance of 0 is returned as is (nothing is subtracted).
        var employeeId = CreateEmployee("EAP240");
        var typeId = CreateAbsenceType("EAP240", totalDaysAllowed: 0);
        CreateLeave(employeeId, typeId, new DateTime(2026, 1, 10), new DateTime(2026, 1, 12), 3);

        var preview = await PreviewAsync(employeeId, typeId, WeekStart, WeekEnd);

        preview.CurrentBalance.ShouldBe(0);
    }

    [Fact]
    public async Task Sick_Leave_Balance_Should_Also_Subtract_Opening_Sick_Leave_Transfers()
    {
        // Lines 3241-3251: type code "3" adds TimeTransfer rows of transfer type "2" in the period to "used".
        // Allowance 15, one leave of 2 days, opening transfer of 4 -> 15 - 6 = 9.
        var employeeId = CreateEmployee("EAP250");
        var sickId = CreateAbsenceType("3", totalDaysAllowed: 15, reInitialize: true);
        CreateLeave(employeeId, sickId, new DateTime(2026, 1, 10), new DateTime(2026, 1, 11), 2);
        UsingDbContext(context =>
        {
            var transferType = context.Set<TimeTransferType>().Add(new TimeTransferType
            {
                TenantId = TenantId,
                Code = "2",
                NameEn = "Opening Sick Leave",
                NameAr = "Opening Sick Leave"
            }).Entity;
            context.SaveChanges();

            context.Set<TimeTransfer>().Add(new TimeTransfer
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                TransferTypeId = transferType.Id,
                TransferDate = new DateTime(2026, 1, 1),
                TransferCount = 4
            });
        });

        var preview = await PreviewAsync(employeeId, sickId, WeekStart, WeekEnd);

        preview.CurrentBalance.ShouldBe(9);
    }

    #endregion

    #region Paid / Unpaid split

    [Fact]
    public async Task DeductFromQuota_Should_Exclude_Days_By_The_Annual_Type_Rules()
    {
        // 820-835: Feb 8-14 = 7 calendar days. 839-866: less the Code "1" type's exclusions (weekend excluded ->
        // Friday Feb 13) = 6, even though the leave's own type counts weekends.
        var employeeId = CreateEmployee("EAP300");
        CreateAbsenceType("1", includeWeekend: false);
        var typeId = CreateAbsenceType("EAP300", includeWeekend: true);

        var preview = await PreviewAsync(employeeId, typeId, WeekStart, WeekEnd, r =>
        {
            r.ReturnDate = new DateTime(2026, 2, 15);
            r.PaidStartDate = new DateTime(2026, 2, 8);
            r.PaidEndDate = new DateTime(2026, 2, 14);
        });

        preview.PaidCalendarDays.ShouldBe(7);
        preview.DeductFromQuota.ShouldBe(6);
    }

    [Fact]
    public async Task UnpaidDays_Should_Exclude_Days_By_The_Unpaid_Type_Rules()
    {
        // 926-941 / 944-972: Feb 8-14 = 7; the Code "2" type counts weekends -> nothing excluded -> 7.
        var employeeId = CreateEmployee("EAP310");
        CreateAbsenceType("2", includeWeekend: true);
        var typeId = CreateAbsenceType("EAP310");

        var preview = await PreviewAsync(employeeId, typeId, WeekStart, WeekEnd, r =>
        {
            r.ReturnDate = new DateTime(2026, 2, 15);
            r.UnpaidStartDate = new DateTime(2026, 2, 8);
            r.UnpaidEndDate = new DateTime(2026, 2, 14);
        });

        preview.UnpaidCalendarDays.ShouldBe(7);
        preview.UnpaidDays.ShouldBe(7);
    }

    [Fact]
    public async Task Paid_And_Unpaid_Days_Should_Stay_Zero_Until_A_Return_Date_Is_Entered()
    {
        // LEGACY ODDITY (lines 845, 950: ReturnDate.Year != 1): without a return date DeductFromQuota and UnpaidDays
        // stay 0 while the calendar-day counts beside them (826, 932) are still shown. Kept as-is.
        var employeeId = CreateEmployee("EAP320");
        CreateAbsenceType("1");
        CreateAbsenceType("2");
        var typeId = CreateAbsenceType("EAP320");

        var preview = await PreviewAsync(employeeId, typeId, WeekStart, WeekEnd, r =>
        {
            r.PaidStartDate = new DateTime(2026, 2, 8);
            r.PaidEndDate = new DateTime(2026, 2, 14);
            r.UnpaidStartDate = new DateTime(2026, 2, 15);
            r.UnpaidEndDate = new DateTime(2026, 2, 16);
        });

        preview.PaidCalendarDays.ShouldBe(7);
        preview.DeductFromQuota.ShouldBe(0);
        preview.UnpaidCalendarDays.ShouldBe(2);
        preview.UnpaidDays.ShouldBe(0);
    }

    [Fact]
    public async Task Paid_Days_Should_Be_Zero_When_Only_One_Paid_Date_Is_Given()
    {
        // Lines 826 / 845: both dates required.
        var employeeId = CreateEmployee("EAP330");
        CreateAbsenceType("1");
        var typeId = CreateAbsenceType("EAP330");

        var preview = await PreviewAsync(employeeId, typeId, WeekStart, WeekEnd, r =>
        {
            r.ReturnDate = new DateTime(2026, 2, 15);
            r.PaidStartDate = new DateTime(2026, 2, 8);
        });

        preview.PaidCalendarDays.ShouldBe(0);
        preview.DeductFromQuota.ShouldBe(0);
    }

    #endregion

    #region Sick leave info

    [Fact]
    public async Task Sick_Leave_Info_Should_Be_Empty_For_Other_Types()
    {
        // Line 1017: only type code "5" (sick leave unpaid).
        var employeeId = CreateEmployee("EAP400");
        var typeId = CreateAbsenceType("EAP400");

        var preview = await PreviewAsync(employeeId, typeId, WeekStart, WeekEnd);

        preview.SickLeaveUnpaidInfo.ShouldBe(string.Empty);
        preview.SickLeaveSegments.ShouldBeEmpty();
    }

    #endregion

    #region App service endpoint

    [Fact]
    public async Task GetAbsencePreview_Should_Return_The_Figures()
    {
        var employeeId = CreateEmployee("EAP500");
        var typeId = CreateAbsenceType("EAP500");

        var result = await Resolve<IEmployeeAbsencesAppService>().GetAbsencePreview(new GetEmployeeAbsencePreviewInput
        {
            EmployeeId = employeeId,
            AbsenceTypeId = typeId,
            StartDate = WeekStart,
            EndDate = WeekEnd
        });

        result.CalendarDays.ShouldBe(7);
        result.TotalLeaveDays.ShouldBe(6);
        result.SickLeaveSegments.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAbsencePreview_Should_Refuse_Politely_When_No_Duty_Is_Set_Up_For_An_Hourly_Leave()
    {
        // The engine's "duty not found" (InvalidOperationException) becomes a message the form can show.
        var employeeId = CreateEmployee("EAP510", withWeek: false);
        var typeId = CreateAbsenceType("EAP510");
        var monday = new DateTime(2026, 2, 2);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            Resolve<IEmployeeAbsencesAppService>().GetAbsencePreview(new GetEmployeeAbsencePreviewInput
            {
                EmployeeId = employeeId,
                AbsenceTypeId = typeId,
                StartDate = monday,
                EndDate = monday,
                StartTime = monday.AddHours(9),
                EndTime = monday.AddHours(11)
            }));
    }

    #endregion
}
