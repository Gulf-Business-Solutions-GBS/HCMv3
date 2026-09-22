using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Shared set-up for the ESS leave lifecycle tests (apply, return, cancel): an employee linked to the
/// signed-in user, a one-step leave approval chain, quota, and optionally a Friday/Saturday week.
/// </summary>
public abstract class EssLeaveWorkflowTestBase : AppTestBase
{
    protected readonly IMyLeaveAppService _myLeaveAppService;
    protected readonly IWorkflowApprovalService _approvalService;

    protected EssLeaveWorkflowTestBase()
    {
        LoginAsDefaultTenantAdmin();
        _myLeaveAppService = Resolve<IMyLeaveAppService>();
        _approvalService = Resolve<IWorkflowApprovalService>();

        // These tests book leave a month ahead and return from it, which legacy refuses unless future-dated
        // returns are switched on. The rule itself has its own test below.
        SetAllowFutureDatedReturn(true);
    }

    protected void SetAllowFutureDatedReturn(bool allow)
    {
        UsingDbContext(context =>
        {
            var settings = context.Set<ManageTimeSettings>().FirstOrDefault();
            if (settings == null)
            {
                context.Set<ManageTimeSettings>().Add(new ManageTimeSettings { TenantId = TenantId, AllowFutureDatedReturnFromLeave = allow });
            }
            else
            {
                settings.AllowFutureDatedReturnFromLeave = allow;
            }
        });
    }

    /// <summary>Legacy hard-codes unpaid leave as absence type code "2"; late days beyond the balance land there.</summary>
    protected int EnsureUnpaidLeaveType()
    {
        return UsingDbContext(context =>
        {
            var existing = context.Set<AbsenceType>().FirstOrDefault(t => t.Code == "2");
            if (existing != null)
            {
                return existing.Id;
            }

            var unpaid = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = "2",
                NameEn = "Unpaid",
                NameAr = "Unpaid",
                IncludeWeekend = true,
                IncludePublicHoliday = true,
                IncludeRestDay = true
            }).Entity;

            context.SaveChanges();
            return unpaid.Id;
        });
    }

    /// <summary>Kuwait week: Friday and Saturday weekly-off, every other day a working day.</summary>
    protected void SetupFridaySaturdayWeekend(int employeeId)
    {
        UsingDbContext(context => EssLeaveTestDuty.ClearDutyPeriods(context, employeeId));

        UsingDbContext(context =>
        {
            var working = context.Set<Duty>().Add(new Duty
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Work",
                NameAr = "Work",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(16, 0, 0)
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
                SaturdayDutyId = off.Id
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

    protected static DateTime NextSunday(DateTime from)
    {
        var date = from.Date;
        while (date.DayOfWeek != DayOfWeek.Sunday)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    protected EmployeeAbsenceQuota QuotaRow(int employeeId, int absenceTypeId)
    {
        return UsingDbContext(context => context.Set<EmployeeAbsenceQuota>()
            .IgnoreQueryFilters()
            .Single(q => q.EmployeeId == employeeId && q.AbsenceTypeId == absenceTypeId));
    }

    protected System.Collections.Generic.List<EmployeeAbsence> LeaveRowsOf(int employeeId)
    {
        return UsingDbContext(context => context.Set<EmployeeAbsence>()
            .IgnoreQueryFilters()
            .Where(a => a.EmployeeId == employeeId && !a.IsDeleted)
            .OrderBy(a => a.StartDate)
            .ToList());
    }

    protected int TenantId => AbpSession.TenantId ?? 1;

    protected int CreateEmployee(string code, long? userId = null)
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
                UserId = userId,
                NationalityId = nationality.Id
            }).Entity;
        });

        UsingDbContext(context => EssLeaveTestDuty.AssignAllWorkingWeek(context, TenantId, employee.Id));

        return employee.Id;
    }

    protected int CreateCurrentUserEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    protected int CreateAbsenceType(bool allowToReturnEarly = true, string nameEn = "Annual")
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameEn,
                AllowToReturnEarly = allowToReturnEarly
            }).Entity;
        });

        return absenceType.Id;
    }

    protected void CreateQuota(int employeeId, int absenceTypeId, double accrued)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                EntitlementFrom = DateTime.Today.AddYears(-1),
                EntitlementTo = DateTime.Today.AddYears(2),
                DeductionFrom = DateTime.Today.AddYears(-1),
                DeductionTo = DateTime.Today.AddYears(2),
                QuotaAccrued = accrued,
                QuotaUsed = 0
            });
        });
    }

    /// <summary>A one-step leave chain with a named approver, and the catalog row switched on.</summary>
    protected int EnableLeaveWorkflowWithApprover(string approverCode)
    {
        var approverId = CreateEmployee(approverCode);

        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Approver",
                NameAr = "Approver"
            }).Entity;

            var master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = "LEAVE-RET",
                NameEn = "Standard Leave",
                NameAr = "Standard Leave",
                WorkflowSteps = 1
            }).Entity;

            context.SaveChanges();

            context.Set<RequestDetail>().Add(new RequestDetail
            {
                TenantId = TenantId,
                RequestMasterId = master.Id,
                Step = 1,
                WorkflowRoleId = role.Id,
                ApproverPerson = WorkflowApproverPersonType.Person,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = true,
                CanSendBack = true
            });

            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = approverId,
                WorkflowRoleId = role.Id,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2)
            });

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeAbsence");
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    protected async Task<int> CreateApprovedLeaveAsync(int absenceTypeId, DateTime start, int days, int approverId)
    {
        var result = await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = absenceTypeId,
            StartDate = start,
            EndDate = start.AddDays(days - 1),
            Comments = "original"
        });

        await _approvalService.ApproveAsync(result.WorkflowRequestId.Value, approverId, "ok");

        return result.EmployeeAbsenceId;
    }

    protected EmployeeAbsence LeaveRow(int leaveId)
    {
        return UsingDbContext(context => context.Set<EmployeeAbsence>()
            .IgnoreQueryFilters()
            .Single(a => a.Id == leaveId));
    }
}
