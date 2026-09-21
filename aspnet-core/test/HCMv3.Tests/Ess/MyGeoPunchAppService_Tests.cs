using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Self-service geo punch -- the port of legacy ESS "Geo Punching".
///
/// <para>
/// The tests to read first are the ones where the server refuses something the legacy screen only greyed
/// out or took on trust: a button that is not open, a work location the employee may not use, and a
/// distance outside the radius. Legacy sent the distance from the browser and recorded whatever arrived; the
/// port measures it itself, so each of those is asserted against a request the screen would never send.
/// </para>
///
/// <para>
/// The button sequence itself (split shifts, overtime) is covered in <c>GeoPunchRules_Tests</c>; these
/// tests use an ordinary shift, whose buttons do not depend on the time of day the suite runs at.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyGeoPunchAppService_Tests : AppTestBase
{
    private readonly IMyGeoPunchAppService _geoPunch;

    // Kuwait City, and a point ~111 m due north of it.
    private const double SiteLat = 29.3759;
    private const double SiteLng = 47.9774;
    private const double NearbyLat = 29.3769;

    // A 1x1 transparent PNG.
    private const string TinyPng =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

    public MyGeoPunchAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _geoPunch = Resolve<IMyGeoPunchAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- seeding

    private int CreateEmployee(string code, long? userId = null)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = code,
                LastNameEn = "T",
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code + " T",
                FullNameAr = "ت ت",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                AccessID = "PIN-" + code,
                UserId = userId,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    /// <summary>The four punch types the geo punch buttons map to, by their fixed codes.</summary>
    private void CreatePunchTypes()
    {
        UsingDbContext(context =>
        {
            foreach (var (code, name) in new[]
                     {
                         (TransTypeCodes.ClockIn, "Clock In"),
                         (TransTypeCodes.ClockOut, "Clock Out"),
                         (TransTypeCodes.OvertimeIn, "OT In"),
                         (TransTypeCodes.OvertimeOut, "OT Out")
                     })
            {
                context.Set<TransType>().Add(new TransType { TenantId = TenantId, Code = code, NameEn = name, NameAr = name });
            }
        });
    }

    /// <summary>Same ordinary 08:00-17:00 duty on every day of the week.</summary>
    private void GiveEmployeeADuty(int employeeId)
    {
        Duty duty = null;
        UsingDbContext(context =>
        {
            duty = context.Set<Duty>().Add(new Duty
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Day shift",
                NameAr = "Day shift",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            }).Entity;
        });

        DutyPeriod dutyPeriod = null;
        UsingDbContext(context =>
        {
            dutyPeriod = context.Set<DutyPeriod>().Add(new DutyPeriod
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Standard week",
                NameAr = "Standard week",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                SundayDutyId = duty.Id,
                MondayDutyId = duty.Id,
                TuesdayDutyId = duty.Id,
                WednesdayDutyId = duty.Id,
                ThursdayDutyId = duty.Id,
                FridayDutyId = duty.Id,
                SaturdayDutyId = duty.Id
            }).Entity;
        });

        UsingDbContext(context =>
        {
            context.Set<EmployeeDutyPeriod>().Add(new EmployeeDutyPeriod
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DutyPeriodId = dutyPeriod.Id,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31)
            });
        });
    }

    /// <summary>An employee linked to the signed-in user, with a duty and the four punch types.</summary>
    private int CreateMe()
    {
        var employeeId = CreateEmployee("GEO-" + Guid.NewGuid().ToString("N").Substring(0, 6), AbpSession.UserId);
        GiveEmployeeADuty(employeeId);
        CreatePunchTypes();

        return employeeId;
    }

    private int CreateWorkLocation(string name, double range = 100)
    {
        WorkLocation workLocation = null;
        UsingDbContext(context =>
        {
            workLocation = context.Set<WorkLocation>().Add(new WorkLocation
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = name,
                NameAr = name,
                Latitude = SiteLat,
                Longitude = SiteLng,
                GeoRange = range,
                IsActive = true
            }).Entity;
        });

        return workLocation.Id;
    }

    /// <summary>Current org assignment: own work location and a sub group that does or does not pay overtime.</summary>
    private void AssignEmployee(int employeeId, int? workLocationId, bool payOvertime)
    {
        UsingDbContext(context =>
        {
            var group = context.Set<EmployeeGroup>().Add(new EmployeeGroup
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Group",
                NameAr = "Group"
            }).Entity;
            context.SaveChanges();

            var subGroup = context.Set<EmployeeSubGroup>().Add(new EmployeeSubGroup
            {
                TenantId = TenantId,
                EmployeeGroupId = group.Id,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Sub group",
                NameAr = "Sub group",
                PayOvertime = payOvertime
            }).Entity;
            context.SaveChanges();

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = new DateTime(9999, 12, 31),
                WorkLocationId = workLocationId,
                EmployeeSubGroupId = subGroup.Id
            });
        });
    }

    private void GiveTimePolicy(int employeeId, Action<TimePolicy> configure)
    {
        UsingDbContext(context =>
        {
            var policy = new TimePolicy
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Policy",
                NameAr = "Policy"
            };
            configure(policy);
            context.Set<TimePolicy>().Add(policy);
            context.SaveChanges();

            context.Set<EmployeeTimePolicy>().Add(new EmployeeTimePolicy
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                TimePolicyId = policy.Id,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = new DateTime(9999, 12, 31)
            });
        });
    }

    private void GiveExtendedWorkLocation(int employeeId, int workLocationId, DateTime start, DateTime end)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeExtdWorkLocation>().Add(new EmployeeExtdWorkLocation
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                WorkLocationId = workLocationId,
                StartDate = start,
                EndDate = end
            });
        });
    }

    private void SetSelfServiceSettings(Action<ManageSelfServiceSettings> configure)
    {
        UsingDbContext(context =>
        {
            var settings = new ManageSelfServiceSettings { TenantId = TenantId };
            configure(settings);
            context.Set<ManageSelfServiceSettings>().Add(settings);
        });
    }

    /// <summary>Plants one of today's punches directly, as a device would have.</summary>
    private void PlantPunchToday(int employeeId, string code, int hour)
    {
        UsingDbContext(context =>
        {
            var transType = context.Set<TransType>().First(t => t.Code == code);
            context.Set<TimeEvent>().Add(new TimeEvent
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                TransTypeId = transType.Id,
                TransDate = DateTime.Today,
                TransTime = DateTime.Today.AddHours(hour),
                TransSource = TransSource.Machine,
                RecordSource = Random.Shared.Next(1, int.MaxValue)
            });
        });
    }

    private static RecordMyGeoPunchInput Punch(string code, int? workLocationId = null, double lat = SiteLat, string selfie = null) =>
        new()
        {
            TransTypeCode = code,
            Latitude = lat,
            Longitude = SiteLng,
            WorkLocationId = workLocationId,
            Selfie = selfie
        };

    private TimeEvent LoadTimeEvent(int id)
    {
        TimeEvent timeEvent = null;
        UsingDbContext(context => timeEvent = context.Set<TimeEvent>().First(e => e.Id == id));

        return timeEvent;
    }

    // ---------------------------------------------------------------- context

    [Fact]
    public async Task Should_Refuse_A_User_With_No_Employee_Record()
    {
        await Should.ThrowAsync<UserFriendlyException>(() => _geoPunch.GetMyGeoPunchContext());
    }

    [Fact]
    public async Task Should_Offer_Only_Clock_In_At_The_Start_Of_The_Day()
    {
        CreateMe();

        var context = await _geoPunch.GetMyGeoPunchContext();

        context.CanClockIn.ShouldBeTrue();
        context.CanClockOut.ShouldBeFalse();
        context.HasDutyPeriodToday.ShouldBeTrue();
        context.TodaysPunches.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Be_An_Open_Location_Punch_When_No_Policy_Restricts_Location()
    {
        CreateMe();

        var context = await _geoPunch.GetMyGeoPunchContext();

        context.IsOpenLocation.ShouldBeTrue();
        context.WorkLocations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Report_No_Duty_Period_Today()
    {
        CreateEmployee("GEO-NODUTY", AbpSession.UserId);
        CreatePunchTypes();

        var context = await _geoPunch.GetMyGeoPunchContext();

        context.HasDutyPeriodToday.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Carry_The_Tenant_Screen_Settings()
    {
        CreateMe();
        SetSelfServiceSettings(s =>
        {
            s.AutoLogoutAfterPunch = true;
            s.HideGeolocationInfo = true;
            s.HideGeolocationPunchInfo = true;
            s.AllowPunchOutsideRange = true;
        });

        var context = await _geoPunch.GetMyGeoPunchContext();

        context.AutoLogoutAfterPunch.ShouldBeTrue();
        context.HideLocationInfo.ShouldBeTrue();
        context.HidePunchList.ShouldBeTrue();
        context.AllowPunchOutsideRange.ShouldBeTrue();
    }

    // ---------------------------------------------------------------- recording

    [Fact]
    public async Task Should_Record_An_Open_Location_Clock_In_As_An_Ordinary_Punch()
    {
        var meId = CreateMe();

        var result = await _geoPunch.RecordMyGeoPunch(Punch(TransTypeCodes.ClockIn));

        var timeEvent = LoadTimeEvent(result.Id);
        timeEvent.EmployeeId.ShouldBe(meId);
        timeEvent.PIN.ShouldStartWith("PIN-GEO-");
        timeEvent.TransSource.ShouldBe(TransSource.ESS);
        timeEvent.TransDate.ShouldBe(DateTime.Today);
        timeEvent.Latitude.ShouldBe(SiteLat);
        timeEvent.Longitude.ShouldBe(SiteLng);
        timeEvent.PunchInRange.ShouldBeTrue();
        timeEvent.WorkLocationId.ShouldBeNull();

        // No workflow: the Time Engine must count it at once.
        timeEvent.WorkflowStatus.ShouldBeNull();
        timeEvent.WorkflowRequestId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Move_On_To_Clock_Out_After_Clocking_In()
    {
        CreateMe();

        await _geoPunch.RecordMyGeoPunch(Punch(TransTypeCodes.ClockIn));
        var context = await _geoPunch.GetMyGeoPunchContext();

        context.CanClockIn.ShouldBeFalse();
        context.CanClockOut.ShouldBeTrue();
        context.TodaysPunches.Count.ShouldBe(1);
        context.TodaysPunches[0].TransTypeCode.ShouldBe(TransTypeCodes.ClockIn);
    }

    [Fact]
    public async Task Should_Refuse_A_Button_That_Is_Not_Open()
    {
        // Legacy only greyed the button out; a replayed request punched anyway.
        CreateMe();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _geoPunch.RecordMyGeoPunch(Punch(TransTypeCodes.ClockOut)));
    }

    [Fact]
    public async Task Should_Refuse_A_Second_Clock_In()
    {
        var meId = CreateMe();
        PlantPunchToday(meId, TransTypeCodes.ClockIn, 8);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _geoPunch.RecordMyGeoPunch(Punch(TransTypeCodes.ClockIn)));
    }

    [Fact]
    public async Task Should_Refuse_When_No_Duty_Period_Covers_Today()
    {
        CreateEmployee("GEO-NODUTY2", AbpSession.UserId);
        CreatePunchTypes();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _geoPunch.RecordMyGeoPunch(Punch(TransTypeCodes.ClockIn)));
    }

    [Fact]
    public async Task Should_Refuse_An_Unknown_Punch_Code()
    {
        CreateMe();

        await Should.ThrowAsync<UserFriendlyException>(() => _geoPunch.RecordMyGeoPunch(Punch("9")));
    }

    // ---------------------------------------------------------------- work locations and range

    [Fact]
    public async Task Should_Offer_The_Own_Work_Location_When_The_Policy_Restricts_Clock_In_To_It()
    {
        var meId = CreateMe();
        var siteId = CreateWorkLocation("Head office");
        AssignEmployee(meId, siteId, payOvertime: false);
        GiveTimePolicy(meId, p => p.AllowPunchInFromWorkLocationOnly = true);

        var context = await _geoPunch.GetMyGeoPunchContext();

        context.IsOpenLocation.ShouldBeFalse();
        context.DefaultWorkLocationId.ShouldBe(siteId);
        context.WorkLocations.Single().Id.ShouldBe(siteId);
        context.WorkLocations.Single().IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Record_An_In_Range_Punch_Against_The_Work_Location()
    {
        var meId = CreateMe();
        var siteId = CreateWorkLocation("Head office", range: 150);
        AssignEmployee(meId, siteId, payOvertime: false);
        GiveTimePolicy(meId, p => p.AllowPunchInFromWorkLocationOnly = true);

        var result = await _geoPunch.RecordMyGeoPunch(Punch(TransTypeCodes.ClockIn, siteId, NearbyLat));

        result.PunchInRange.ShouldBeTrue();
        result.DistanceMeters.ShouldBe(111.19, 0.1);
        var timeEvent = LoadTimeEvent(result.Id);
        timeEvent.WorkLocationId.ShouldBe(siteId);
        timeEvent.PunchInRange.ShouldBeTrue();
        timeEvent.RangeDifference.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Refuse_An_Out_Of_Range_Punch()
    {
        // The distance is measured on the server; nothing the client says about it is used.
        var meId = CreateMe();
        var siteId = CreateWorkLocation("Head office", range: 50);
        AssignEmployee(meId, siteId, payOvertime: false);
        GiveTimePolicy(meId, p => p.AllowPunchInFromWorkLocationOnly = true);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _geoPunch.RecordMyGeoPunch(Punch(TransTypeCodes.ClockIn, siteId, NearbyLat)));
    }

    [Fact]
    public async Task Should_Record_An_Out_Of_Range_Punch_When_The_Tenant_Allows_It()
    {
        var meId = CreateMe();
        var siteId = CreateWorkLocation("Head office", range: 50);
        AssignEmployee(meId, siteId, payOvertime: false);
        GiveTimePolicy(meId, p => p.AllowPunchInFromWorkLocationOnly = true);
        SetSelfServiceSettings(s => s.AllowPunchOutsideRange = true);

        var result = await _geoPunch.RecordMyGeoPunch(Punch(TransTypeCodes.ClockIn, siteId, NearbyLat));

        var timeEvent = LoadTimeEvent(result.Id);
        timeEvent.PunchInRange.ShouldBeFalse();
        timeEvent.RangeDifference.ShouldBe(61.19, 0.1);
    }

    [Fact]
    public async Task Should_Require_A_Work_Location_When_One_Is_Offered()
    {
        var meId = CreateMe();
        var siteId = CreateWorkLocation("Head office");
        AssignEmployee(meId, siteId, payOvertime: false);
        GiveTimePolicy(meId, p => p.AllowPunchInFromWorkLocationOnly = true);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _geoPunch.RecordMyGeoPunch(Punch(TransTypeCodes.ClockIn)));
    }

    [Fact]
    public async Task Should_Refuse_A_Work_Location_That_Is_Not_Offered()
    {
        var meId = CreateMe();
        var siteId = CreateWorkLocation("Head office");
        var elsewhereId = CreateWorkLocation("Elsewhere");
        AssignEmployee(meId, siteId, payOvertime: false);
        GiveTimePolicy(meId, p => p.AllowPunchInFromWorkLocationOnly = true);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _geoPunch.RecordMyGeoPunch(Punch(TransTypeCodes.ClockIn, elsewhereId)));
    }

    [Fact]
    public async Task Should_Offer_Current_Extended_Work_Locations_Including_On_Their_Last_Day()
    {
        var meId = CreateMe();
        var currentId = CreateWorkLocation("Client site");
        var endsTodayId = CreateWorkLocation("Ends today");
        var expiredId = CreateWorkLocation("Expired");
        GiveExtendedWorkLocation(meId, currentId, DateTime.Today.AddDays(-5), DateTime.Today.AddDays(5));
        GiveExtendedWorkLocation(meId, endsTodayId, DateTime.Today.AddDays(-5), DateTime.Today);
        GiveExtendedWorkLocation(meId, expiredId, DateTime.Today.AddDays(-5), DateTime.Today.AddDays(-1));
        GiveTimePolicy(meId, p => p.AllowPunchInFromExtWorklocation = true);

        var context = await _geoPunch.GetMyGeoPunchContext();

        context.WorkLocations.Select(w => w.Id).ShouldBe(new[] { currentId, endsTodayId }, ignoreOrder: true);
        context.DefaultWorkLocationId.ShouldBeNull();
    }

    [Fact]
    public async Task Check_Location_Should_Report_Distance_And_Verdict()
    {
        var meId = CreateMe();
        var siteId = CreateWorkLocation("Head office", range: 50);
        AssignEmployee(meId, siteId, payOvertime: false);
        GiveTimePolicy(meId, p => p.AllowPunchInFromWorkLocationOnly = true);

        var check = await _geoPunch.CheckMyGeoPunchLocation(new CheckMyGeoPunchLocationInput
        {
            Latitude = NearbyLat,
            Longitude = SiteLng,
            WorkLocationId = siteId
        });

        check.DistanceMeters.ShouldBe(111.19, 0.1);
        check.RangeMeters.ShouldBe(50);
        check.Verdict.ShouldBe(GeoPunchRangeVerdict.OutOfRangeRefused);
    }

    // ---------------------------------------------------------------- overtime

    [Fact]
    public async Task Should_Refuse_Overtime_When_The_Sub_Group_Does_Not_Pay_It()
    {
        var meId = CreateMe();
        AssignEmployee(meId, null, payOvertime: false);
        PlantPunchToday(meId, TransTypeCodes.ClockIn, 8);
        PlantPunchToday(meId, TransTypeCodes.ClockOut, 17);

        var context = await _geoPunch.GetMyGeoPunchContext();
        context.IsOvertimeAllowed.ShouldBeFalse();
        context.CanOvertimeIn.ShouldBeFalse();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _geoPunch.RecordMyGeoPunch(Punch(TransTypeCodes.OvertimeIn)));
    }

    [Fact]
    public async Task Should_Record_Overtime_In_After_The_Shift_When_The_Sub_Group_Pays_It()
    {
        var meId = CreateMe();
        AssignEmployee(meId, null, payOvertime: true);
        PlantPunchToday(meId, TransTypeCodes.ClockIn, 8);
        PlantPunchToday(meId, TransTypeCodes.ClockOut, 17);

        var context = await _geoPunch.GetMyGeoPunchContext();
        context.CanOvertimeIn.ShouldBeTrue();

        var result = await _geoPunch.RecordMyGeoPunch(Punch(TransTypeCodes.OvertimeIn));

        result.Id.ShouldBeGreaterThan(0);
    }

    // ---------------------------------------------------------------- selfie

    [Fact]
    public async Task Should_Require_A_Selfie_When_The_Tenant_And_Policy_Ask_For_One()
    {
        var meId = CreateMe();
        GiveTimePolicy(meId, p => p.IsFaceRequired = true);
        SetSelfServiceSettings(s => s.AllowGeolocationSelfie = true);

        (await _geoPunch.GetMyGeoPunchContext()).RequiresSelfie.ShouldBeTrue();
        await Should.ThrowAsync<UserFriendlyException>(() =>
            _geoPunch.RecordMyGeoPunch(Punch(TransTypeCodes.ClockIn)));
    }

    [Fact]
    public async Task Should_Default_To_Face_Required_When_The_Employee_Has_No_Time_Policy()
    {
        // Legacy's default: IsFaceRequired starts TRUE and only a policy turns it off.
        CreateMe();
        SetSelfServiceSettings(s => s.AllowGeolocationSelfie = true);

        (await _geoPunch.GetMyGeoPunchContext()).RequiresSelfie.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Require_A_Selfie_When_The_Policy_Does_Not_Need_A_Face()
    {
        var meId = CreateMe();
        GiveTimePolicy(meId, p => p.IsFaceRequired = false);
        SetSelfServiceSettings(s => s.AllowGeolocationSelfie = true);

        (await _geoPunch.GetMyGeoPunchContext()).RequiresSelfie.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Store_The_Selfie_And_Return_It_To_Its_Owner()
    {
        var meId = CreateMe();
        GiveTimePolicy(meId, p => p.IsFaceRequired = true);
        SetSelfServiceSettings(s => s.AllowGeolocationSelfie = true);

        var result = await _geoPunch.RecordMyGeoPunch(Punch(TransTypeCodes.ClockIn, selfie: TinyPng));

        var timeEvent = LoadTimeEvent(result.Id);
        Guid.TryParse(timeEvent.GeoSelfiePath, out var selfieId).ShouldBeTrue();
        UsingDbContext(context => context.Set<BinaryObject>().Any(b => b.Id == selfieId).ShouldBeTrue());

        var selfie = await _geoPunch.GetMyGeoPunchSelfie(new EntityDto(result.Id));
        selfie.DataUrl.ShouldBe(TinyPng);

        (await _geoPunch.GetMyGeoPunchContext()).TodaysPunches.Single().HasSelfie.ShouldBeTrue();
    }

    [Theory]
    [InlineData("not a data url")]
    [InlineData("data:text/plain;base64,aGVsbG8=")]
    [InlineData("data:image/png;base64,@@@not-base64@@@")]
    public async Task Should_Refuse_A_Selfie_That_Is_Not_An_Image(string selfie)
    {
        var meId = CreateMe();
        GiveTimePolicy(meId, p => p.IsFaceRequired = true);
        SetSelfServiceSettings(s => s.AllowGeolocationSelfie = true);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _geoPunch.RecordMyGeoPunch(Punch(TransTypeCodes.ClockIn, selfie: selfie)));
    }

    [Fact]
    public async Task Should_Not_Return_Somebody_Elses_Selfie()
    {
        CreateMe();
        var otherId = CreateEmployee("GEO-OTHER");
        TimeEvent planted = null;
        UsingDbContext(context =>
        {
            var file = context.Set<BinaryObject>().Add(new BinaryObject(TenantId, new byte[] { 1, 2, 3 }, "GeoPunchSelfie|image/png")).Entity;
            planted = context.Set<TimeEvent>().Add(new TimeEvent
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                TransTypeId = context.Set<TransType>().First(t => t.Code == TransTypeCodes.ClockIn).Id,
                TransDate = DateTime.Today,
                TransTime = DateTime.Today.AddHours(8),
                TransSource = TransSource.ESS,
                Latitude = SiteLat,
                Longitude = SiteLng,
                GeoSelfiePath = file.Id.ToString()
            }).Entity;
        });

        var selfie = await _geoPunch.GetMyGeoPunchSelfie(new EntityDto(planted.Id));

        selfie.DataUrl.ShouldBeNull();
    }

    // ---------------------------------------------------------------- the missing-punch list

    [Fact]
    public async Task A_Geo_Punch_Should_Not_Appear_In_The_Missing_Punch_Request_List()
    {
        CreateMe();
        await _geoPunch.RecordMyGeoPunch(Punch(TransTypeCodes.ClockIn));

        var requests = await Resolve<IMyPunchAppService>().GetMyPunches(new GetMyRequestsInput
        {
            IncludeHistory = true,
            MaxResultCount = 50,
            SkipCount = 0
        });

        requests.Items.ShouldBeEmpty();
    }
}
