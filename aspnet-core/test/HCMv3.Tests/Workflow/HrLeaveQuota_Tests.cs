using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// HR's own leave screen and the quota. Legacy charged the quota inside <c>EmployeeAbsence.OnSaving</c> and
/// refunded it in <c>OnDeleting</c>, so a leave keyed in by HR counted exactly like one that came through
/// approval. This port had left that out: HR could add, change and delete leave all day without the balance
/// moving.
/// </summary>
// ReSharper disable once InconsistentNaming
public class HrLeaveQuota_Tests : EssLeaveWorkflowTestBase
{
    private readonly IEmployeeAbsencesAppService _absences;

    public HrLeaveQuota_Tests()
    {
        _absences = Resolve<IEmployeeAbsencesAppService>();
    }

    private static DateTime Soon => DateTime.Today.AddDays(30);

    private Task AddLeaveAsync(int employeeId, int typeId, DateTime start, int days) =>
        _absences.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
        {
            EmployeeId = employeeId,
            AbsenceTypeId = typeId,
            StartDate = start,
            EndDate = start.AddDays(days - 1)
        });

    private int LeaveIdOf(int employeeId) =>
        UsingDbContext(context => context.Set<EmployeeAbsence>()
            .IgnoreQueryFilters()
            .Where(a => a.EmployeeId == employeeId && !a.IsDeleted)
            .OrderByDescending(a => a.Id)
            .Select(a => a.Id)
            .First());

    [Fact]
    public async Task Leave_Entered_By_HR_Should_Come_Out_Of_The_Quota()
    {
        var employeeId = CreateEmployee("HRQ100");
        var typeId = CreateAbsenceType();
        CreateQuota(employeeId, typeId, accrued: 30);

        await AddLeaveAsync(employeeId, typeId, Soon, days: 4);

        QuotaRow(employeeId, typeId).QuotaUsed.ShouldBe(4);
    }

    [Fact]
    public async Task Changing_A_Leave_Should_Move_The_Quota_With_It()
    {
        var employeeId = CreateEmployee("HRQ110");
        var typeId = CreateAbsenceType();
        CreateQuota(employeeId, typeId, accrued: 30);

        await AddLeaveAsync(employeeId, typeId, Soon, days: 4);
        var leaveId = LeaveIdOf(employeeId);

        await _absences.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
        {
            Id = leaveId,
            EmployeeId = employeeId,
            AbsenceTypeId = typeId,
            StartDate = Soon,
            EndDate = Soon.AddDays(1)
        });

        // Four released, two charged -- not a delta, because a change of type moves days between two rows.
        QuotaRow(employeeId, typeId).QuotaUsed.ShouldBe(2);
    }

    [Fact]
    public async Task Moving_A_Leave_To_Another_Type_Should_Move_The_Days_Between_Quotas()
    {
        var employeeId = CreateEmployee("HRQ120");
        var annualId = CreateAbsenceType(nameEn: "Annual");
        var sickId = CreateAbsenceType(nameEn: "Sick");
        CreateQuota(employeeId, annualId, accrued: 30);
        CreateQuota(employeeId, sickId, accrued: 30);

        await AddLeaveAsync(employeeId, annualId, Soon, days: 3);
        var leaveId = LeaveIdOf(employeeId);

        await _absences.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
        {
            Id = leaveId,
            EmployeeId = employeeId,
            AbsenceTypeId = sickId,
            StartDate = Soon,
            EndDate = Soon.AddDays(2)
        });

        QuotaRow(employeeId, annualId).QuotaUsed.ShouldBe(0);
        QuotaRow(employeeId, sickId).QuotaUsed.ShouldBe(3);
    }

    [Fact]
    public async Task Deleting_A_Leave_Should_Give_The_Days_Back()
    {
        var employeeId = CreateEmployee("HRQ130");
        var typeId = CreateAbsenceType();
        CreateQuota(employeeId, typeId, accrued: 30);

        await AddLeaveAsync(employeeId, typeId, Soon, days: 4);
        var leaveId = LeaveIdOf(employeeId);

        await _absences.Delete(new EntityDto(leaveId));

        QuotaRow(employeeId, typeId).QuotaUsed.ShouldBe(0);
    }

    [Fact]
    public async Task A_Leave_Still_Awaiting_Approval_Should_Not_Be_Refunded_When_HR_Deletes_It()
    {
        // It never took the quota in the first place -- a pending request consumes BOOKABLE, not the balance.
        var meId = CreateCurrentUserEmployee("HRQ140");
        var approverId = EnableLeaveWorkflowWithApprover("HRQ140APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var applied = await _myLeaveAppService.ApplyForLeave(new HCMv3.Ess.Dto.ApplyForLeaveInput
        {
            AbsenceTypeId = typeId,
            StartDate = Soon,
            EndDate = Soon.AddDays(3)
        });

        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(0);

        await _absences.Delete(new EntityDto(applied.EmployeeAbsenceId));

        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(0);
        approverId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Leave_Entered_By_HR_Across_A_Weekend_Should_Charge_Only_The_Working_Days()
    {
        // Legacy UpdateQuotaInformation: calendar days less the days the type excludes.
        var employeeId = CreateEmployee("HRQ150");
        var typeId = CreateAbsenceType();
        CreateQuota(employeeId, typeId, accrued: 30);
        SetupFridaySaturdayWeekend(employeeId);

        var sunday = NextSunday(Soon);
        await AddLeaveAsync(employeeId, typeId, sunday, days: 8);

        var leave = UsingDbContext(context => context.Set<EmployeeAbsence>()
            .IgnoreQueryFilters()
            .Single(a => a.Id == LeaveIdOf(employeeId)));

        leave.CalendarDays.ShouldBe(8);
        leave.TotalLeaveDays.ShouldBe(6);
        QuotaRow(employeeId, typeId).QuotaUsed.ShouldBe(6);
    }
}
