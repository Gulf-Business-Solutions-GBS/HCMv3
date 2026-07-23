using HCMv3.GeneralMasterInfo;
using HCMv3.TimeManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the TimeManagement app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations
/// are left for a follow-up), so the new TimeManagement entities have no table in the shared test Sqlite
/// database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/
/// ToListAsync/AnyAsync require an async-capable LINQ provider — a plain <c>List&lt;T&gt;.AsQueryable()</c>
/// does not support them), each test spins up its own private in-memory Sqlite connection against this
/// context and wires fake <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances
/// (see <see cref="FakeRepository"/>) on top of it via NSubstitute — not via the real Abp repository/DI
/// pipeline, which is untouched.
/// </summary>
public class TimeManagementTestDbContext : DbContext
{
    public DbSet<TimeType> TimeTypes { get; set; }
    public DbSet<TransType> TransTypes { get; set; }
    public DbSet<TimeTransferType> TimeTransferTypes { get; set; }
    public DbSet<BreakShift> BreakShifts { get; set; }
    public DbSet<Duty> Duties { get; set; }
    public DbSet<DailyDuty> DailyDuties { get; set; }
    public DbSet<HolidayCalendar> HolidayCalendars { get; set; }
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<HolidayPeriod> HolidayPeriods { get; set; }
    public DbSet<TimePolicy> TimePolicies { get; set; }
    public DbSet<OvertimeType> OvertimeTypes { get; set; }

    // Wave 2
    public DbSet<DutyPeriod> DutyPeriods { get; set; }
    public DbSet<DutyPeriodDetail> DutyPeriodDetails { get; set; }
    public DbSet<RosterDutyPeriod> RosterDutyPeriods { get; set; }
    public DbSet<RosterDutyPeriodDetail> RosterDutyPeriodDetails { get; set; }
    public DbSet<RosterDutySchedule> RosterDutySchedules { get; set; }
    public DbSet<RosterGroup> RosterGroups { get; set; }
    public DbSet<RosterGroupDetail> RosterGroupDetails { get; set; }
    public DbSet<RosterGroupSchedule> RosterGroupSchedules { get; set; }
    public DbSet<RosterDailyDuty> RosterDailyDuties { get; set; }
    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }
    public DbSet<HCMv3.OrganizationManagement.WorkLocation> WorkLocations { get; set; }

    // Wave 3 (employee-assignment records)
    public DbSet<EmployeeDutyPeriod> EmployeeDutyPeriods { get; set; }
    public DbSet<EmployeeTimePolicy> EmployeeTimePolicies { get; set; }
    public DbSet<EmployeeSubstituteDuty> EmployeeSubstituteDuties { get; set; }
    public DbSet<EmployeeDutySchedule> EmployeeDutySchedules { get; set; }
    public DbSet<WorkLocationDutyPeriod> WorkLocationDutyPeriods { get; set; }
    public DbSet<EmployeeTimesheet> EmployeeTimesheets { get; set; }

    // Wave 4 (raw event/log data + calculated register data)
    public DbSet<TimeEvent> TimeEvents { get; set; }
    public DbSet<TimeAttAPI> TimeAttAPIs { get; set; }
    public DbSet<TimeLog> TimeLogs { get; set; }
    public DbSet<TimeRegister> TimeRegisters { get; set; }
    public DbSet<MonthlyTimeRegister> MonthlyTimeRegisters { get; set; }
    public DbSet<MonthlyTimeRegisterDetail> MonthlyTimeRegisterDetails { get; set; }
    public DbSet<YearlyTimeRegister> YearlyTimeRegisters { get; set; }
    public DbSet<YearlyTimeRegisterDetail> YearlyTimeRegisterDetails { get; set; }
    public DbSet<SimulationTimeRegister> SimulationTimeRegisters { get; set; }
    public DbSet<SimulationTimeRegisterDetail> SimulationTimeRegisterDetails { get; set; }
    public DbSet<TimeTransfer> TimeTransfers { get; set; }

    public TimeManagementTestDbContext(DbContextOptions<TimeManagementTestDbContext> options) : base(options)
    {
    }
}
