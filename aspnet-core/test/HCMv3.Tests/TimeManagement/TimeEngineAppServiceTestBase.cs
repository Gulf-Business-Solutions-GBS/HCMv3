using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Timing;
using Castle.MicroKernel.Registration;
using HCMv3.EngineRuns;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using NSubstitute;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// Shared arrangement for the Time Engine screen's app-service tests. The service is resolved from the container
/// (so ABP's authorization and unit-of-work interceptors are in the call path), with the job manager and the engine
/// replaced by doubles: queueing is observed, never executed. Everything else -- payroll areas, control records,
/// employees, progress rows, time logs -- is real data in the test database.
/// </summary>
public abstract class TimeEngineAppServiceTestBase : AppTestBase
{
    protected readonly IBackgroundJobManager BackgroundJobManager = Substitute.For<IBackgroundJobManager>();
    protected readonly ITimeEngineService TimeEngineService = Substitute.For<ITimeEngineService>();
    protected readonly ITimeEngineAppService TimeEngineAppService;

    protected TimeEngineJobArgs EnqueuedArgs;

    private int? _nationalityId;

    protected TimeEngineAppServiceTestBase()
    {
        LoginAsDefaultTenantAdmin();

        BackgroundJobManager
            .EnqueueAsync<TimeEngineBackgroundJob, TimeEngineJobArgs>(
                Arg.Do<TimeEngineJobArgs>(args => EnqueuedArgs = args),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>())
            .Returns(Task.FromResult("job-1"));

        LocalIocManager.IocContainer.Register(
            Component.For<IBackgroundJobManager>().Instance(BackgroundJobManager).IsDefault());
        LocalIocManager.IocContainer.Register(
            Component.For<ITimeEngineService>().Instance(TimeEngineService).IsDefault());

        TimeEngineAppService = Resolve<ITimeEngineAppService>();
    }

    protected int TenantId => AbpSession.TenantId ?? 1;

    protected void SetTimeSettings(int futureEvaluationInDays, DateTime earliestEvaluationDate)
    {
        UsingDbContext(context =>
        {
            var settings = context.Set<ManageTimeSettings>().FirstOrDefault(s => s.TenantId == TenantId);
            if (settings == null)
            {
                settings = context.Set<ManageTimeSettings>().Add(new ManageTimeSettings { TenantId = TenantId }).Entity;
            }

            settings.FutureEvaluationInDays = futureEvaluationInDays;
            settings.EarliestEvaluationDate = earliestEvaluationDate;
        });
    }

    /// <summary>A payroll area, with a control record ending on <paramref name="calendarMonthEnd"/> when given.</summary>
    protected PayrollArea CreatePayrollArea(string name, DateTime? calendarMonthEnd)
    {
        return UsingDbContext(context =>
        {
            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Dinar",
                NameAr = "Dinar",
                DecimalCount = 3,
                CurrencyShortFormEn = "KWD",
                CurrencyShortFormAr = "KWD",
                CurrencyDenominationFormEn = "Fils",
                CurrencyDenominationFormAr = "Fils"
            }).Entity;
            var period = context.Set<PayrollPeriod>().Add(new PayrollPeriod
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Monthly",
                NameAr = "Monthly"
            }).Entity;
            context.SaveChanges();

            var area = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = name,
                CurrencyId = currency.Id,
                PayrollPeriodId = period.Id,
                CutOffDay = 25
            }).Entity;
            context.SaveChanges();

            if (calendarMonthEnd.HasValue)
            {
                context.Set<PayrollControlRecord>().Add(new PayrollControlRecord
                {
                    TenantId = TenantId,
                    PayrollAreaId = area.Id,
                    PayrollMonth = calendarMonthEnd.Value.Month,
                    PayrollYear = calendarMonthEnd.Value.Year,
                    CalendarMonthStart = new DateTime(calendarMonthEnd.Value.Year, calendarMonthEnd.Value.Month, 1),
                    CalendarMonthEnd = calendarMonthEnd.Value
                });
                context.SaveChanges();
            }

            return area;
        });
    }

    /// <summary>
    /// An employee, assigned to <paramref name="payrollAreaId"/> from <paramref name="assignedFrom"/> to
    /// <paramref name="assignedTo"/> (defaults: 2019-01-01 to 9999-12-31) when an area is given.
    /// </summary>
    protected int CreateEmployee(
        string code,
        int? payrollAreaId,
        EmployeeStatus status = EmployeeStatus.Active,
        DateTime? assignedFrom = null,
        DateTime? assignedTo = null,
        string firstNameAr = "موظف")
    {
        return UsingDbContext(context =>
        {
            if (_nationalityId == null)
            {
                var nationality = context.Set<Nationality>().Add(new Nationality
                {
                    TenantId = TenantId,
                    Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                    NameEn = "Kuwaiti",
                    NameAr = "Kuwaiti"
                }).Entity;
                context.SaveChanges();
                _nationalityId = nationality.Id;
            }

            var employee = context.Employees.Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "Employee",
                LastNameEn = code,
                FullNameEn = "Employee " + code,
                FirstNameAr = firstNameAr,
                LastNameAr = code,
                FullNameAr = firstNameAr + " " + code,
                NationalityId = _nationalityId.Value,
                JoiningDate = new DateTime(2019, 1, 1),
                Status = status
            }).Entity;
            context.SaveChanges();

            if (payrollAreaId.HasValue)
            {
                context.EmployeeOrgAssignments.Add(new EmployeeOrgAssignment
                {
                    TenantId = TenantId,
                    EmployeeId = employee.Id,
                    StartDate = assignedFrom ?? new DateTime(2019, 1, 1),
                    EndDate = assignedTo ?? new DateTime(9999, 12, 31),
                    PayrollAreaId = payrollAreaId.Value
                });
                context.SaveChanges();
            }

            return employee.Id;
        });
    }

    /// <summary>A Regular salary register for <paramref name="period"/> -- what makes an employee "Processed".</summary>
    protected void AddRegularRegister(int employeeId, DateTime period, PayrollRunType runType = PayrollRunType.Regular)
    {
        UsingDbContext(context =>
        {
            context.SalaryRegisters.Add(new SalaryRegister
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                ForPeriod = period,
                InPeriod = period,
                PayrollRunType = runType
            });
        });
    }

    protected long SeedEngineRun(
        string runKey,
        EngineRunStatus status = EngineRunStatus.Completed,
        DateTime? startedAt = null,
        DateTime? completedAt = null,
        int total = 0,
        EngineType engineType = EngineType.TimeEngine)
    {
        return UsingDbContext(context =>
        {
            var started = startedAt ?? Clock.Now.AddMinutes(-10);
            var run = context.EngineRuns.Add(new EngineRun
            {
                TenantId = TenantId,
                EngineType = engineType,
                RunKey = runKey ?? Guid.NewGuid().ToString("N"),
                Status = status,
                TotalEmployees = total,
                StartedAt = started,
                LastHeartbeatAt = completedAt ?? Clock.Now,
                CompletedAt = status == EngineRunStatus.Running ? null : (completedAt ?? Clock.Now)
            }).Entity;
            context.SaveChanges();
            return run.Id;
        });
    }

    protected void SeedEngineRunEmployee(long runId, int sequenceNo, int employeeId, string code, bool succeeded, string message)
    {
        UsingDbContext(context =>
        {
            context.EngineRunEmployees.Add(new EngineRunEmployee
            {
                TenantId = TenantId,
                EngineRunId = runId,
                SequenceNo = sequenceNo,
                EmployeeId = employeeId,
                EmployeeCode = code,
                EmployeeName = "Employee " + code,
                Succeeded = succeeded,
                Message = message,
                StartedAt = Clock.Now.AddMinutes(-5),
                CompletedAt = Clock.Now.AddMinutes(-5),
                ElapsedMilliseconds = 1000
            });
        });
    }

    protected void SeedTimeLog(int? employeeId, DateTime dayDate, int status, string message, DateTime runTimeStamp)
    {
        UsingDbContext(context =>
        {
            context.TimeLogs.Add(new TimeLog
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DayDate = dayDate,
                Status = status,
                Message = message,
                RunTimeStamp = runTimeStamp,
                NoOfEmployee = 1
            });
        });
    }

    protected void SeedExecutionLog(string userName, DateTime actionDateTime, string parameters)
    {
        UsingDbContext(context =>
        {
            context.ExecutionLogs.Add(new ExecutionLog
            {
                TenantId = TenantId,
                UserName = userName,
                ActionDateTime = actionDateTime,
                Action = ExecutionLogAction.RUN,
                Module = ExecutionLogModule.TimeEngine,
                ScreenName = "HCM10.Web.UserControls.PayrollManagement.uscTimeEngine",
                Parameters = parameters,
                ExecutionStatus = ExecutionLogStatus.SUCCESS
            });
        });
    }
}
