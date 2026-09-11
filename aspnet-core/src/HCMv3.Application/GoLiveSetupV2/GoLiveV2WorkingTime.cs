using System;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GoLiveSetupV2;

/// <summary>
/// The working time the time engine needs before it can work out a single day, as the customer chose it for V2:
/// planned time with no punches, 08:00 to 17:00, Friday the weekly off.
///
/// <para>For the company, once: the four settings records the engine reads (left at their defaults -- no
/// autorun, no approvals, no roster, security switches off), an empty holiday calendar, a working-day duty and a
/// weekly-off duty, a duty period laying them over the week, and a planned-time policy. For each employee: that
/// duty period and that policy from the joining date. Everything is looked up first, so a second load reuses it,
/// and goes through its own service.</para>
///
/// <para><b>Why planned time with no sign-in.</b> A new company has no punches. A policy that expects them marks
/// every working day absent; planned time counts the planned day as worked.</para>
/// </summary>
public class GoLiveV2WorkingTime : AbpServiceBase, ITransientDependency
{
    public const string WorkingDutyName = "Normal Day 08:00-17:00";
    public const string WeeklyOffDutyName = "Weekly Off";
    public const string DutyPeriodName = "Saturday-Thursday 08:00-17:00, Friday Off";
    public const string TimePolicyName = "Planned Time, No Punches";
    public const string HolidayCalendarName = "Default Holiday Calendar";

    private static readonly TimeSpan DayStart = new(8, 0, 0);
    private static readonly TimeSpan DayEnd = new(17, 0, 0);
    private static readonly DateTime ValidFrom = new(1900, 1, 1);
    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly IManageTimeSettingsAppService _timeSettings;
    private readonly IManagePayrollSettingsAppService _payrollSettings;
    private readonly IManagePersonnelSettingsAppService _personnelSettings;
    private readonly IManageSecuritySettingsAppService _securitySettings;
    private readonly IHolidayCalendarsAppService _holidayCalendars;
    private readonly IDutiesAppService _duties;
    private readonly IDutyPeriodsAppService _dutyPeriods;
    private readonly ITimePoliciesAppService _timePolicies;
    private readonly IEmployeeDutyPeriodsAppService _employeeDutyPeriods;
    private readonly IEmployeeTimePoliciesAppService _employeeTimePolicies;
    private readonly IRepository<ManageTimeSettings> _timeSettingsRepository;
    private readonly IRepository<ManagePayrollSettings> _payrollSettingsRepository;
    private readonly IRepository<ManagePersonnelSettings> _personnelSettingsRepository;
    private readonly IRepository<ManageSecuritySettings> _securitySettingsRepository;
    private readonly IRepository<HolidayCalendar> _holidayCalendarRepository;
    private readonly IRepository<Duty> _dutyRepository;
    private readonly IRepository<DutyPeriod> _dutyPeriodRepository;
    private readonly IRepository<TimePolicy> _timePolicyRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<EmployeeTimePolicy> _employeeTimePolicyRepository;

    public GoLiveV2WorkingTime(
        IManageTimeSettingsAppService timeSettings,
        IManagePayrollSettingsAppService payrollSettings,
        IManagePersonnelSettingsAppService personnelSettings,
        IManageSecuritySettingsAppService securitySettings,
        IHolidayCalendarsAppService holidayCalendars,
        IDutiesAppService duties,
        IDutyPeriodsAppService dutyPeriods,
        ITimePoliciesAppService timePolicies,
        IEmployeeDutyPeriodsAppService employeeDutyPeriods,
        IEmployeeTimePoliciesAppService employeeTimePolicies,
        IRepository<ManageTimeSettings> timeSettingsRepository,
        IRepository<ManagePayrollSettings> payrollSettingsRepository,
        IRepository<ManagePersonnelSettings> personnelSettingsRepository,
        IRepository<ManageSecuritySettings> securitySettingsRepository,
        IRepository<HolidayCalendar> holidayCalendarRepository,
        IRepository<Duty> dutyRepository,
        IRepository<DutyPeriod> dutyPeriodRepository,
        IRepository<TimePolicy> timePolicyRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<EmployeeTimePolicy> employeeTimePolicyRepository)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;

        _timeSettings = timeSettings;
        _payrollSettings = payrollSettings;
        _personnelSettings = personnelSettings;
        _securitySettings = securitySettings;
        _holidayCalendars = holidayCalendars;
        _duties = duties;
        _dutyPeriods = dutyPeriods;
        _timePolicies = timePolicies;
        _employeeDutyPeriods = employeeDutyPeriods;
        _employeeTimePolicies = employeeTimePolicies;
        _timeSettingsRepository = timeSettingsRepository;
        _payrollSettingsRepository = payrollSettingsRepository;
        _personnelSettingsRepository = personnelSettingsRepository;
        _securitySettingsRepository = securitySettingsRepository;
        _holidayCalendarRepository = holidayCalendarRepository;
        _dutyRepository = dutyRepository;
        _dutyPeriodRepository = dutyPeriodRepository;
        _timePolicyRepository = timePolicyRepository;
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _employeeTimePolicyRepository = employeeTimePolicyRepository;
    }

    /// <summary>The company's working time. Returns what every employee is then given, and whether any of it was new.</summary>
    public async Task<GoLiveV2WorkingTimeIds> EnsureCompanyAsync()
    {
        var ids = new GoLiveV2WorkingTimeIds();

        ids.Created |= await EnsureSettingsAsync();

        var calendarId = await EnsureAsync(
            () => _holidayCalendarRepository.FirstOrDefaultAsync(c => c.NameEn == HolidayCalendarName),
            () => _holidayCalendars.CreateOrEdit(new CreateOrEditHolidayCalendarDto
            {
                Code = "DEFAULT",
                NameEn = HolidayCalendarName,
                NameAr = HolidayCalendarName,
            }),
            c => c.Id,
            ids);

        var workingDutyId = await EnsureAsync(
            () => _dutyRepository.FirstOrDefaultAsync(d => d.NameEn == WorkingDutyName),
            () => _duties.CreateOrEdit(new CreateOrEditDutyDto
            {
                Code = "DAY-0800-1700",
                NameEn = WorkingDutyName,
                NameAr = WorkingDutyName,
                StartDate = ValidFrom,
                EndDate = OpenEnded,
                StartTime = DayStart,
                EndTime = DayEnd,
            }),
            d => d.Id,
            ids);

        var weeklyOffDutyId = await EnsureAsync(
            () => _dutyRepository.FirstOrDefaultAsync(d => d.NameEn == WeeklyOffDutyName),
            () => _duties.CreateOrEdit(new CreateOrEditDutyDto
            {
                Code = "WEEKLY-OFF",
                NameEn = WeeklyOffDutyName,
                NameAr = WeeklyOffDutyName,
                StartDate = ValidFrom,
                EndDate = OpenEnded,
                IsWeeklyOff = true,
            }),
            d => d.Id,
            ids);

        ids.DutyPeriodId = await EnsureAsync(
            () => _dutyPeriodRepository.FirstOrDefaultAsync(p => p.NameEn == DutyPeriodName),
            () => _dutyPeriods.CreateOrEdit(new CreateOrEditDutyPeriodDto
            {
                Code = "SAT-THU-FRI-OFF",
                NameEn = DutyPeriodName,
                NameAr = DutyPeriodName,
                StartDate = ValidFrom,
                EndDate = OpenEnded,
                HolidayCalendarId = calendarId,
                SaturdayDutyId = workingDutyId,
                SundayDutyId = workingDutyId,
                MondayDutyId = workingDutyId,
                TuesdayDutyId = workingDutyId,
                WednesdayDutyId = workingDutyId,
                ThursdayDutyId = workingDutyId,
                FridayDutyId = weeklyOffDutyId,
            }),
            p => p.Id,
            ids);

        ids.TimePolicyId = await EnsureAsync(
            () => _timePolicyRepository.FirstOrDefaultAsync(p => p.NameEn == TimePolicyName),
            () => _timePolicies.CreateOrEdit(new CreateOrEditTimePolicyDto
            {
                Code = "PLANNED-NO-PUNCH",
                NameEn = TimePolicyName,
                NameAr = TimePolicyName,
                TimeMgmtStatus = TimeMgmtStatus.PlannedTime,
                ShouldSignIn = false,
                ShouldSignOut = false,
                ShouldBreakSignIn = false,
                ShouldBreakSignOut = false,
            }),
            p => p.Id,
            ids);

        return ids;
    }

    /// <summary>An employee's duty period and time policy from the joining date -- unless they already have some.</summary>
    public async Task EnsureEmployeeAsync(Employee employee, GoLiveV2WorkingTimeIds ids)
    {
        if (!await _employeeDutyPeriodRepository.GetAll().AnyAsync(p => p.EmployeeId == employee.Id))
        {
            await _employeeDutyPeriods.CreateOrEdit(new CreateOrEditEmployeeDutyPeriodDto
            {
                EmployeeId = employee.Id,
                DutyPeriodId = ids.DutyPeriodId,
                StartDate = employee.JoiningDate,
            });
            await CurrentUnitOfWork.SaveChangesAsync();
        }

        if (!await _employeeTimePolicyRepository.GetAll().AnyAsync(p => p.EmployeeId == employee.Id))
        {
            await _employeeTimePolicies.CreateOrEdit(new CreateOrEditEmployeeTimePolicyDto
            {
                EmployeeId = employee.Id,
                TimePolicyId = ids.TimePolicyId,
                StartDate = employee.JoiningDate,
            });
            await CurrentUnitOfWork.SaveChangesAsync();
        }
    }

    /// <summary>Each of the four at its defaults, through its own screen's service (an Id of 0 creates).</summary>
    private async Task<bool> EnsureSettingsAsync()
    {
        var created = false;

        if (!await _timeSettingsRepository.GetAll().AnyAsync())
        {
            await _timeSettings.CreateOrEditTimeSettings(new TimeSettingsEditDto());
            created = true;
        }

        if (!await _payrollSettingsRepository.GetAll().AnyAsync())
        {
            await _payrollSettings.CreateOrEditPayrollSettings(new PayrollSettingsEditDto());
            created = true;
        }

        if (!await _personnelSettingsRepository.GetAll().AnyAsync())
        {
            await _personnelSettings.CreateOrEditPersonnelSettings(new PersonnelSettingsEditDto());
            created = true;
        }

        if (!await _securitySettingsRepository.GetAll().AnyAsync())
        {
            await _securitySettings.CreateOrEditSecuritySettings(new ManageSecuritySettingsEditDto());
            created = true;
        }

        if (created)
        {
            await CurrentUnitOfWork.SaveChangesAsync();
        }

        return created;
    }

    /// <summary>Find it; if missing, create it through its service and find it again. Services return no id.</summary>
    private async Task<int> EnsureAsync<TEntity>(
        Func<Task<TEntity>> find,
        Func<Task> create,
        Func<TEntity, int> idOf,
        GoLiveV2WorkingTimeIds ids)
        where TEntity : class
    {
        var existing = await find();
        if (existing != null)
        {
            return idOf(existing);
        }

        await create();
        await CurrentUnitOfWork.SaveChangesAsync();
        ids.Created = true;

        return idOf(await find());
    }
}

/// <summary>What each loaded employee is given, and whether the company's working time had to be created.</summary>
public class GoLiveV2WorkingTimeIds
{
    public int DutyPeriodId { get; set; }

    public int TimePolicyId { get; set; }

    public bool Created { get; set; }
}
