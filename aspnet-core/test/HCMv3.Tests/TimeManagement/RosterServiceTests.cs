using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class RosterServiceTests : AppTestBase
{
    private readonly IRosterService _rosterService;

    public RosterServiceTests()
    {
        LoginAsDefaultTenantAdmin();
        _rosterService = Resolve<IRosterService>();
    }

    private int CreateEmployee(string firstName)
    {
        return UsingDbContext(context =>
        {
            var nationality = new Nationality { TenantId = AbpSession.TenantId ?? 1, NameEn = "Local", NameAr = "محلي" };
            context.Set<Nationality>().Add(nationality);
            context.SaveChanges();

            var employee = new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                FirstNameEn = firstName,
                LastNameEn = "Doe",
                FirstNameAr = firstName,
                LastNameAr = "Doe",
                FullNameEn = firstName,
                FullNameAr = firstName,
                NationalityId = nationality.Id
            };
            context.Set<Employee>().Add(employee);
            context.SaveChanges();
            return employee.Id;
        });
    }

    private int CreateDuty(string code)
    {
        return UsingDbContext(context =>
        {
            var entity = new Duty
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            };
            context.Set<Duty>().Add(entity);
            context.SaveChanges();
            return entity.Id;
        });
    }

    private int CreateDutyPeriod(int workingDutyId, int weeklyOffDutyId)
    {
        return UsingDbContext(context =>
        {
            var entity = new DutyPeriod
            {
                TenantId = AbpSession.TenantId ?? 1,
                NameEn = "DP",
                NameAr = "DP",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                SundayDutyId = workingDutyId,
                MondayDutyId = workingDutyId,
                TuesdayDutyId = workingDutyId,
                WednesdayDutyId = workingDutyId,
                ThursdayDutyId = workingDutyId,
                FridayDutyId = weeklyOffDutyId,
                SaturdayDutyId = workingDutyId
            };
            context.Set<DutyPeriod>().Add(entity);
            context.SaveChanges();
            return entity.Id;
        });
    }

    private int CreateRosterDutyPeriod(int weekCount, params int[] weekDutyIds)
    {
        return UsingDbContext(context =>
        {
            var entity = new DutyPeriod
            {
                TenantId = AbpSession.TenantId ?? 1,
                NameEn = "Roster DP",
                NameAr = "Roster DP",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31)
            };
            context.Set<DutyPeriod>().Add(entity);
            context.SaveChanges();

            for (var week = 1; week <= weekCount; week++)
            {
                var dutyId = weekDutyIds[week - 1];
                context.Set<DutyPeriodDetail>().Add(new DutyPeriodDetail
                {
                    TenantId = AbpSession.TenantId ?? 1,
                    DutyPeriodId = entity.Id,
                    Week = week,
                    Day1DutyId = dutyId,
                    Day2DutyId = dutyId,
                    Day3DutyId = dutyId,
                    Day4DutyId = dutyId,
                    Day5DutyId = dutyId,
                    Day6DutyId = dutyId,
                    Day7DutyId = dutyId
                });
            }
            context.SaveChanges();
            return entity.Id;
        });
    }

    private void AssignEmployeeDutyPeriod(int employeeId, int dutyPeriodId, DateTime startDate, DateTime endDate, int startPoint = 0)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeDutyPeriod>().Add(new EmployeeDutyPeriod
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                DutyPeriodId = dutyPeriodId,
                StartDate = startDate,
                EndDate = endDate,
                StartPoint = startPoint
            });
            context.SaveChanges();
        });
    }

    private void AssignEmployeeSubstituteDuty(int employeeId, int dutyId, DateTime substituteDate)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeSubstituteDuty>().Add(new EmployeeSubstituteDuty
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                DutyId = dutyId,
                SubstituteDate = substituteDate
            });
            context.SaveChanges();
        });
    }

    private int CreateRosterGroup(string code)
    {
        return UsingDbContext(context =>
        {
            var entity = new RosterGroup { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
            context.Set<RosterGroup>().Add(entity);
            context.SaveChanges();
            return entity.Id;
        });
    }

    private void AssignRosterGroupDetail(int rosterGroupId, int employeeId, DateTime startDate, DateTime endDate)
    {
        UsingDbContext(context =>
        {
            context.Set<RosterGroupDetail>().Add(new RosterGroupDetail
            {
                TenantId = AbpSession.TenantId ?? 1,
                RosterGroupId = rosterGroupId,
                EmployeeId = employeeId,
                StartDate = startDate,
                EndDate = endDate
            });
            context.SaveChanges();
        });
    }

    private void AssignRosterGroupSchedule(int rosterGroupId, int dutyPeriodId, DateTime startDate, DateTime endDate, int startPoint)
    {
        UsingDbContext(context =>
        {
            context.Set<RosterGroupSchedule>().Add(new RosterGroupSchedule
            {
                TenantId = AbpSession.TenantId ?? 1,
                RosterGroupId = rosterGroupId,
                DutyPeriodId = dutyPeriodId,
                StartDate = startDate,
                EndDate = endDate,
                StartPoint = startPoint
            });
            context.SaveChanges();
        });
    }

    [Fact]
    public async Task GetRosterDutyAsync_Should_Prioritize_EmployeeSubstituteDuty_Over_Everything_Else()
    {
        var employeeId = CreateEmployee("Sub");
        var workingDutyId = CreateDuty("WORK");
        var weeklyOffDutyId = CreateDuty("WKOFF");
        var dutyPeriodId = CreateDutyPeriod(workingDutyId, weeklyOffDutyId);
        AssignEmployeeDutyPeriod(employeeId, dutyPeriodId, new DateTime(2020, 1, 1), new DateTime(2030, 12, 31));

        var substituteDutyId = CreateDuty("SUBST");
        var substituteDate = new DateTime(2026, 2, 2); // Monday, normally a working day
        AssignEmployeeSubstituteDuty(employeeId, substituteDutyId, substituteDate);

        var duty = await _rosterService.GetRosterDutyAsync(employeeId, substituteDate);

        duty.ShouldNotBeNull();
        duty.Id.ShouldBe(substituteDutyId);
    }

    [Fact]
    public async Task GetRosterDutyAsync_Should_Resolve_DayOfWeek_Duty_From_EmployeeDutyPeriod_When_No_Substitute()
    {
        var employeeId = CreateEmployee("Emp");
        var workingDutyId = CreateDuty("WORK2");
        var weeklyOffDutyId = CreateDuty("WKOFF2");
        var dutyPeriodId = CreateDutyPeriod(workingDutyId, weeklyOffDutyId);
        AssignEmployeeDutyPeriod(employeeId, dutyPeriodId, new DateTime(2020, 1, 1), new DateTime(2030, 12, 31));

        var friday = new DateTime(2026, 2, 6); // Friday -> weekly off in CreateDutyPeriod
        var duty = await _rosterService.GetRosterDutyAsync(employeeId, friday);

        duty.ShouldNotBeNull();
        duty.Id.ShouldBe(weeklyOffDutyId);
    }

    [Fact]
    public async Task GetRosterDutyPeriodAsync_Should_Resolve_Via_RosterGroup_When_No_EmployeeDutyPeriod()
    {
        var employeeId = CreateEmployee("RosterEmp");
        var dutyId = CreateDuty("RDUTY");
        var dutyPeriodId = CreateRosterDutyPeriod(1, dutyId);

        var rosterGroupId = CreateRosterGroup("RG1");
        AssignRosterGroupDetail(rosterGroupId, employeeId, new DateTime(2020, 1, 1), new DateTime(2030, 12, 31));
        AssignRosterGroupSchedule(rosterGroupId, dutyPeriodId, new DateTime(2026, 1, 1), new DateTime(2030, 12, 31), startPoint: 1);

        var resolvedDutyPeriod = await _rosterService.GetRosterDutyPeriodAsync(employeeId, new DateTime(2026, 2, 2));

        resolvedDutyPeriod.ShouldNotBeNull();
        resolvedDutyPeriod.Id.ShouldBe(dutyPeriodId);
    }

    [Fact]
    public async Task GetRosterDutyAsync_Should_Resolve_Duty_Via_MultiWeek_Rotation_From_RosterGroupSchedule()
    {
        var employeeId = CreateEmployee("RotationEmp");

        var week1DutyId = CreateDuty("W1");
        var week2DutyId = CreateDuty("W2");
        var dutyPeriodId = CreateRosterDutyPeriod(2, week1DutyId, week2DutyId);

        var rosterGroupId = CreateRosterGroup("RG2");
        AssignRosterGroupDetail(rosterGroupId, employeeId, new DateTime(2020, 1, 1), new DateTime(2030, 12, 31));

        var scheduleStartDate = new DateTime(2026, 1, 1); // Thursday
        AssignRosterGroupSchedule(rosterGroupId, dutyPeriodId, scheduleStartDate, new DateTime(2030, 12, 31), startPoint: 1);

        // First day of the cycle (StartPoint = 1) resolves to Week 1's duty.
        var dutyDay1 = await _rosterService.GetRosterDutyAsync(employeeId, scheduleStartDate);
        dutyDay1.ShouldNotBeNull();
        dutyDay1.Id.ShouldBe(week1DutyId);

        // 8th day (dutyCycle = 14 days across 2 weeks) rolls into Week 2's duty.
        var dutyDay8 = await _rosterService.GetRosterDutyAsync(employeeId, scheduleStartDate.AddDays(7));
        dutyDay8.ShouldNotBeNull();
        dutyDay8.Id.ShouldBe(week2DutyId);
    }

    [Fact]
    public async Task GetRosterDutyAsync_Should_Resolve_Duty_Via_MultiWeek_Rotation_From_EmployeeDutyPeriod_When_StartPoint_Set()
    {
        var employeeId = CreateEmployee("EmpRotation");

        var week1DutyId = CreateDuty("EW1");
        var week2DutyId = CreateDuty("EW2");
        var dutyPeriodId = CreateRosterDutyPeriod(2, week1DutyId, week2DutyId);

        var startDate = new DateTime(2026, 1, 1);
        AssignEmployeeDutyPeriod(employeeId, dutyPeriodId, startDate, new DateTime(2030, 12, 31), startPoint: 1);

        // First day of the cycle (StartPoint = 1) resolves to Week 1's duty.
        var dutyDay1 = await _rosterService.GetRosterDutyAsync(employeeId, startDate);
        dutyDay1.ShouldNotBeNull();
        dutyDay1.Id.ShouldBe(week1DutyId);

        // 8th day (dutyCycle = 14 days across 2 weeks) rolls into Week 2's duty.
        var dutyDay8 = await _rosterService.GetRosterDutyAsync(employeeId, startDate.AddDays(7));
        dutyDay8.ShouldNotBeNull();
        dutyDay8.Id.ShouldBe(week2DutyId);
    }

    [Fact]
    public async Task GetRosterDutyAsync_Should_Fall_Back_To_DayOfWeek_Duty_From_EmployeeDutyPeriod_When_StartPoint_Not_Set()
    {
        var employeeId = CreateEmployee("EmpNoRotation");
        var workingDutyId = CreateDuty("NW1");
        var weeklyOffDutyId = CreateDuty("NWOFF");
        var dutyPeriodId = CreateDutyPeriod(workingDutyId, weeklyOffDutyId);
        AssignEmployeeDutyPeriod(employeeId, dutyPeriodId, new DateTime(2020, 1, 1), new DateTime(2030, 12, 31), startPoint: 0);

        var friday = new DateTime(2026, 2, 6); // Friday -> weekly off in CreateDutyPeriod
        var duty = await _rosterService.GetRosterDutyAsync(employeeId, friday);

        duty.ShouldNotBeNull();
        duty.Id.ShouldBe(weeklyOffDutyId);
    }

    [Fact]
    public async Task GetRosterDutyAsync_Should_Return_Null_When_No_Assignment_Found()
    {
        var employeeId = CreateEmployee("NoDutyEmp");

        var duty = await _rosterService.GetRosterDutyAsync(employeeId, new DateTime(2026, 2, 2));

        duty.ShouldBeNull();
    }
}
