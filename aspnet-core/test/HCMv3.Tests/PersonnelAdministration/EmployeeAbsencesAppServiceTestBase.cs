using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using HCMv3.Tests.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Fixtures for HR's own leave screen (<see cref="IEmployeeAbsencesAppService"/>): employees with a working week,
/// leave types, quota rows, salary profiles and the time / payroll settings the form depends on.
/// </summary>
public abstract class EmployeeAbsencesAppServiceTestBase : AppTestBase
{
    protected IEmployeeAbsencesAppService AppService { get; private set; }

    protected EmployeeAbsencesAppServiceTestBase()
    {
        LoginAsDefaultTenantAdmin();
    }

    /// <summary>Resolved lazily so a test class can replace a dependency in its constructor first.</summary>
    protected IEmployeeAbsencesAppService Service => AppService ??= Resolve<IEmployeeAbsencesAppService>();

    protected int TenantId => AbpSession.TenantId ?? 1;

    /// <summary>
    /// An employee. <paramref name="fridayOff"/> gives them 08:00-17:00 every day with Friday a weekly off (the
    /// preview tests' week); otherwise every day is an 08:00-16:00 working day (<see cref="EssLeaveTestDuty"/>).
    /// </summary>
    protected int CreateEmployee(string code, bool fridayOff = false, Gender gender = Gender.Male)
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
                Gender = gender,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
            context.SaveChanges();
            return employee.Id;
        });

        if (fridayOff)
        {
            GiveWeek(employeeId, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), fridayOff: true);
        }
        else
        {
            UsingDbContext(context => EssLeaveTestDuty.AssignAllWorkingWeek(context, TenantId, employeeId));
        }

        return employeeId;
    }

    /// <summary>A working week of the given duty, optionally with a break shift, Friday off when asked.</summary>
    protected void GiveWeek(int employeeId, TimeSpan start, TimeSpan end, bool fridayOff, TimeSpan? breakStart = null, TimeSpan? breakEnd = null)
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
                    StartDate = new DateTime(2020, 1, 1),
                    EndDate = new DateTime(2035, 12, 31),
                    StartTime = breakStart.Value,
                    EndTime = breakEnd!.Value
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
                BreakShiftId = breakShiftId
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
                FridayDutyId = fridayOff ? off.Id : working.Id,
                SaturdayDutyId = working.Id
            }).Entity;
            context.SaveChanges();

            EssLeaveTestDuty.ClearDutyPeriods(context, employeeId);

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

    /// <summary>
    /// A leave type HR may book freely: repeatable, allowed in probation, back-dating open. Further settings via
    /// <paramref name="configure"/>.
    /// </summary>
    protected int CreateAbsenceType(string code, Action<AbsenceType> configure = null, string nameEn = null)
    {
        return UsingDbContext(context =>
        {
            var type = new AbsenceType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = nameEn ?? "Leave " + code,
                NameAr = nameEn ?? "Leave " + code,
                IsActive = true,
                CanApplyAgain = true,
                AllowDuringProbation = true,
                AllowBackDatedDays = 3650,
                AllowToReturnEarly = true,
                AccrualPeriod = AccrualPeriod.WorkYear
            };
            configure?.Invoke(type);
            context.Set<AbsenceType>().Add(type);
            context.SaveChanges();
            return type.Id;
        });
    }

    /// <summary>Partial leave allowed, with hourly limits wide enough not to get in the way.</summary>
    protected static void Partial(AbsenceType type)
    {
        type.AllowPartialLeave = true;
        type.MaxMinutes = 600;
        type.MaxCount = 10;
        type.TotalMinutes = 6000;
    }

    protected void CreateQuota(int employeeId, int absenceTypeId, double accrued, double used = 0,
        DateTime? from = null, DateTime? to = null)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                EntitlementFrom = from ?? new DateTime(2024, 1, 1),
                EntitlementTo = to ?? new DateTime(2024, 12, 31),
                DeductionFrom = from ?? new DateTime(2024, 1, 1),
                DeductionTo = to ?? new DateTime(2024, 12, 31),
                QuotaAccrued = accrued,
                QuotaUsed = used
            });
        });
    }

    protected EmployeeAbsenceQuota GetQuota(int employeeId, int absenceTypeId)
    {
        return UsingDbContext(context => context.Set<EmployeeAbsenceQuota>()
            .AsNoTracking()
            .Single(q => q.EmployeeId == employeeId && q.AbsenceTypeId == absenceTypeId));
    }

    /// <summary>
    /// Puts the employee on a salary profile (sub group) that offers <paramref name="profileTypeIds"/> and,
    /// optionally, a per-period allowance of <paramref name="allowanceDays"/> for <paramref name="allowanceTypeId"/>.
    /// </summary>
    protected void GiveSalaryProfile(int employeeId, IEnumerable<int> profileTypeIds, int? allowanceTypeId = null, int allowanceDays = 0)
    {
        UsingDbContext(context =>
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

            foreach (var typeId in profileTypeIds)
            {
                context.Set<EmployeeSubGroupAbsenceType>().Add(new EmployeeSubGroupAbsenceType
                {
                    TenantId = TenantId,
                    EmployeeSubGroupId = subGroup.Id,
                    AbsenceTypeId = typeId
                });
            }

            if (allowanceTypeId.HasValue)
            {
                context.Set<EmployeeSubGroupAbsence>().Add(new EmployeeSubGroupAbsence
                {
                    TenantId = TenantId,
                    EmployeeSubGroupId = subGroup.Id,
                    AbsenceTypeId = allowanceTypeId.Value,
                    StartDate = new DateTime(2000, 1, 1),
                    EndDate = new DateTime(2100, 12, 31),
                    TotalDays = allowanceDays
                });
            }

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                EmployeeSubGroupId = subGroup.Id,
                StartDate = new DateTime(2000, 1, 1),
                EndDate = new DateTime(2100, 12, 31)
            });
        });
    }

    protected void SetTimeSettings(Action<ManageTimeSettings> configure)
    {
        UsingDbContext(context =>
        {
            var settings = context.Set<ManageTimeSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                           ?? context.Set<ManageTimeSettings>().Add(new ManageTimeSettings { TenantId = TenantId }).Entity;
            configure(settings);
        });
    }

    protected void SetPayrollSettings(Action<ManagePayrollSettings> configure)
    {
        UsingDbContext(context =>
        {
            var settings = context.Set<ManagePayrollSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                           ?? context.Set<ManagePayrollSettings>().Add(new ManagePayrollSettings { TenantId = TenantId }).Entity;
            configure(settings);
        });
    }

    protected CreateOrEditEmployeeAbsenceDto Leave(int employeeId, int absenceTypeId, DateTime start, DateTime end)
    {
        return new CreateOrEditEmployeeAbsenceDto
        {
            EmployeeId = employeeId,
            AbsenceTypeId = absenceTypeId,
            StartDate = start,
            EndDate = end
        };
    }

    /// <summary>Every leave row of the employee, oldest first, as stored.</summary>
    protected List<EmployeeAbsence> GetLeaves(int employeeId)
    {
        return UsingDbContext(context => context.Set<EmployeeAbsence>()
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId)
            .OrderBy(a => a.StartDate)
            .ThenBy(a => a.Id)
            .ToList());
    }

    protected async Task<EmployeeAbsence> CreateAndGetAsync(CreateOrEditEmployeeAbsenceDto input)
    {
        await Service.CreateOrEdit(input);
        return GetLeaves(input.EmployeeId).Single(a => a.AbsenceTypeId == input.AbsenceTypeId && a.StartDate == input.StartDate.Date);
    }

    /// <summary>The form's values for a saved leave, as <c>GetEmployeeAbsenceForEdit</c> hands them back.</summary>
    protected async Task<CreateOrEditEmployeeAbsenceDto> EditOf(int leaveId)
    {
        var output = await Service.GetEmployeeAbsenceForEdit(new Abp.Application.Services.Dto.EntityDto(leaveId));
        return output.EmployeeAbsence;
    }
}
