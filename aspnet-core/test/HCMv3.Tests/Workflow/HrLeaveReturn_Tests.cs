using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.SystemSettings;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// HR recording a return directly on the leave -- legacy <c>EmployeeAbsence.SaveDiffInformation</c>, which ran
/// whenever a return date was typed onto the row.
///
/// <para>
/// It shares the routine with an approved ESS return (<c>ILeaveReturnProcessor</c>), which is the point: legacy
/// had one routine, so the two paths could not drift. These tests exercise it through HR's screen; the ESS side
/// has its own tests in <see cref="ReturnFromLeave_Tests"/>.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class HrLeaveReturn_Tests : EssLeaveWorkflowTestBase
{
    private readonly IEmployeeAbsencesAppService _absences;

    public HrLeaveReturn_Tests()
    {
        _absences = Resolve<IEmployeeAbsencesAppService>();
    }

    private static DateTime Soon => DateTime.Today.AddDays(30);

    private async Task<int> AddLeaveAsync(int employeeId, int typeId, DateTime start, int days)
    {
        await _absences.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
        {
            EmployeeId = employeeId,
            AbsenceTypeId = typeId,
            StartDate = start,
            EndDate = start.AddDays(days - 1)
        });

        return UsingDbContext(context => context.Set<EmployeeAbsence>()
            .IgnoreQueryFilters()
            .Where(a => a.EmployeeId == employeeId && !a.IsDeleted)
            .OrderByDescending(a => a.Id)
            .Select(a => a.Id)
            .First());
    }

    private Task RecordReturnAsync(
        int leaveId, int employeeId, int typeId, DateTime start, DateTime end, DateTime returnDate,
        DateTime? paidStart = null, DateTime? paidEnd = null, DateTime? unpaidStart = null, DateTime? unpaidEnd = null) =>
        _absences.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
        {
            Id = leaveId,
            EmployeeId = employeeId,
            AbsenceTypeId = typeId,
            StartDate = start,
            EndDate = end,
            ReturnDate = returnDate,
            PaidStartDate = paidStart,
            PaidEndDate = paidEnd,
            UnpaidStartDate = unpaidStart,
            UnpaidEndDate = unpaidEnd
        });

    [Fact]
    public async Task An_On_Time_Return_Should_Just_Stamp_The_Leave()
    {
        var employeeId = CreateEmployee("HRR100");
        var typeId = CreateAbsenceType();
        CreateQuota(employeeId, typeId, accrued: 30);

        var leaveId = await AddLeaveAsync(employeeId, typeId, Soon, days: 5);

        await RecordReturnAsync(leaveId, employeeId, typeId, Soon, Soon.AddDays(4), Soon.AddDays(5));

        var leave = LeaveRow(leaveId);
        leave.IsReturned.ShouldBeTrue();
        leave.EndDate.ShouldBe(Soon.AddDays(4));
        leave.TotalLeaveDays.ShouldBe(5);
        QuotaRow(employeeId, typeId).QuotaUsed.ShouldBe(5);
    }

    [Fact]
    public async Task An_Early_Return_Should_Shorten_The_Leave_And_Give_The_Days_Back()
    {
        var employeeId = CreateEmployee("HRR110");
        var typeId = CreateAbsenceType();
        CreateQuota(employeeId, typeId, accrued: 30);

        var leaveId = await AddLeaveAsync(employeeId, typeId, Soon, days: 5);

        // Back on the fourth day: the leave now ends the day before.
        await RecordReturnAsync(leaveId, employeeId, typeId, Soon, Soon.AddDays(4), Soon.AddDays(3));

        var leave = LeaveRow(leaveId);
        leave.EndDate.ShouldBe(Soon.AddDays(2));
        leave.InitialEndDate.ShouldBe(Soon.AddDays(4));
        leave.TotalLeaveDays.ShouldBe(3);
        leave.IsReturned.ShouldBeTrue();

        QuotaRow(employeeId, typeId).QuotaUsed.ShouldBe(3);
    }

    [Fact]
    public async Task A_Late_Return_Should_Book_The_Extra_Days_As_Leave()
    {
        var employeeId = CreateEmployee("HRR120");
        var typeId = CreateAbsenceType();
        CreateQuota(employeeId, typeId, accrued: 30);

        var leaveId = await AddLeaveAsync(employeeId, typeId, Soon, days: 5);

        // Back three days late, all taken as paid leave of the same type.
        await RecordReturnAsync(
            leaveId, employeeId, typeId, Soon, Soon.AddDays(4), Soon.AddDays(8),
            paidStart: Soon.AddDays(5), paidEnd: Soon.AddDays(7));

        var rows = LeaveRowsOf(employeeId);
        rows.Count.ShouldBe(2);

        var generated = rows.Single(r => r.Id != leaveId);
        generated.StartDate.ShouldBe(Soon.AddDays(5));
        generated.EndDate.ShouldBe(Soon.AddDays(7));
        generated.TotalLeaveDays.ShouldBe(3);
        generated.ReturnDate.ShouldBe(Soon.AddDays(8));
        generated.IsReturned.ShouldBeTrue();

        // The original keeps its dates and is only marked returned, as legacy left it.
        var original = rows.Single(r => r.Id == leaveId);
        original.EndDate.ShouldBe(Soon.AddDays(4));
        original.IsReturned.ShouldBeTrue();

        QuotaRow(employeeId, typeId).QuotaUsed.ShouldBe(8);
    }

    [Fact]
    public async Task A_Late_Return_Beyond_The_Balance_Should_Book_The_Rest_Unpaid()
    {
        var employeeId = CreateEmployee("HRR130");
        var typeId = CreateAbsenceType();
        var unpaidTypeId = EnsureUnpaidLeaveType();
        CreateQuota(employeeId, typeId, accrued: 30);

        var leaveId = await AddLeaveAsync(employeeId, typeId, Soon, days: 5);

        await RecordReturnAsync(
            leaveId, employeeId, typeId, Soon, Soon.AddDays(4), Soon.AddDays(8),
            paidStart: Soon.AddDays(5), paidEnd: Soon.AddDays(5),
            unpaidStart: Soon.AddDays(6), unpaidEnd: Soon.AddDays(7));

        var rows = LeaveRowsOf(employeeId);
        rows.Count.ShouldBe(3);

        var unpaid = rows.Single(r => r.AbsenceTypeId == unpaidTypeId);
        unpaid.StartDate.ShouldBe(Soon.AddDays(6));
        unpaid.EndDate.ShouldBe(Soon.AddDays(7));
        unpaid.IsReturned.ShouldBeTrue();

        // Five for the leave, one paid late day; the unpaid days do not touch the annual quota.
        QuotaRow(employeeId, typeId).QuotaUsed.ShouldBe(6);
    }

    [Fact]
    public async Task Re_Saving_A_Returned_Leave_Should_Not_Book_The_Late_Days_Twice()
    {
        var employeeId = CreateEmployee("HRR140");
        var typeId = CreateAbsenceType();
        CreateQuota(employeeId, typeId, accrued: 30);

        var leaveId = await AddLeaveAsync(employeeId, typeId, Soon, days: 5);

        await RecordReturnAsync(
            leaveId, employeeId, typeId, Soon, Soon.AddDays(4), Soon.AddDays(8),
            paidStart: Soon.AddDays(5), paidEnd: Soon.AddDays(7));

        // HR opens the leave again and saves it unchanged.
        await RecordReturnAsync(
            leaveId, employeeId, typeId, Soon, Soon.AddDays(4), Soon.AddDays(8),
            paidStart: Soon.AddDays(5), paidEnd: Soon.AddDays(7));

        LeaveRowsOf(employeeId).Count.ShouldBe(2);
        QuotaRow(employeeId, typeId).QuotaUsed.ShouldBe(8);
    }

    [Fact]
    public async Task Back_To_Back_Leave_Should_Be_Marked_Returned_Too()
    {
        // Legacy UpdateReturnInfoInConsecutiveLeave: the employee did not come back between two stretches of
        // leave, so the earlier one must not hold payroll as "never returned".
        var employeeId = CreateEmployee("HRR150");
        var typeId = CreateAbsenceType();
        CreateQuota(employeeId, typeId, accrued: 30);
        EnableAutoReturnInConsecutiveLeave();

        var firstId = await AddLeaveAsync(employeeId, typeId, Soon, days: 3);
        var secondId = await AddLeaveAsync(employeeId, typeId, Soon.AddDays(3), days: 3);

        LeaveRow(firstId).IsReturned.ShouldBeFalse();

        await RecordReturnAsync(secondId, employeeId, typeId, Soon.AddDays(3), Soon.AddDays(5), Soon.AddDays(6));

        LeaveRow(secondId).IsReturned.ShouldBeTrue();
        LeaveRow(firstId).IsReturned.ShouldBeTrue();

        // Only the leave they actually came back from carries the rejoining date.
        LeaveRow(firstId).ReturnDate.ShouldBe(default(DateTime));
    }

    [Fact]
    public async Task Back_To_Back_Leave_Should_Be_Left_Alone_When_The_Setting_Is_Off()
    {
        var employeeId = CreateEmployee("HRR160");
        var typeId = CreateAbsenceType();
        CreateQuota(employeeId, typeId, accrued: 30);

        var firstId = await AddLeaveAsync(employeeId, typeId, Soon, days: 3);
        var secondId = await AddLeaveAsync(employeeId, typeId, Soon.AddDays(3), days: 3);

        await RecordReturnAsync(secondId, employeeId, typeId, Soon.AddDays(3), Soon.AddDays(5), Soon.AddDays(6));

        LeaveRow(firstId).IsReturned.ShouldBeFalse();
    }

    private void EnableAutoReturnInConsecutiveLeave() =>
        UsingDbContext(context =>
        {
            var settings = context.Set<ManageTimeSettings>().FirstOrDefault();
            if (settings == null)
            {
                context.Set<ManageTimeSettings>().Add(new ManageTimeSettings
                {
                    TenantId = TenantId,
                    EnableAutoReturnInConsecutiveLeave = true,
                    EarliestEvaluationDate = new DateTime(2020, 1, 1)
                });
            }
            else
            {
                settings.EnableAutoReturnInConsecutiveLeave = true;
                settings.EarliestEvaluationDate = new DateTime(2020, 1, 1);
            }
        });
}
