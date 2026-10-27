using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Probation measured in WORKING days: a day the employee was not available does not count, so unpaid leave
/// pushes the end of probation out (legacy <c>Employee.GetProbationEndDate</c>). The original port only had the
/// calendar-days mode, because the working-days one needs the time engine's data.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ProbationEndDate_Tests : EssLeaveWorkflowTestBase
{
    private readonly IEmployeeAbsencesAppService _absences;
    private readonly IProbationEndDateService _probation;

    public ProbationEndDate_Tests()
    {
        _absences = Resolve<IEmployeeAbsencesAppService>();
        _probation = Resolve<IProbationEndDateService>();
    }

    private void SetProbationMode(ProbationDaysCalculation mode) =>
        UsingDbContext(context =>
        {
            var settings = context.Set<ManagePersonnelSettings>().FirstOrDefault();
            if (settings == null)
            {
                context.Set<ManagePersonnelSettings>().Add(new ManagePersonnelSettings
                {
                    TenantId = TenantId,
                    ProbationDaysCalculation = mode
                });
            }
            else
            {
                settings.ProbationDaysCalculation = mode;
            }
        });

    private int GivenAnEmployeeOnProbation(string code, DateTime joining, int probationDays)
    {
        var employeeId = CreateEmployee(code);

        UsingDbContext(context =>
        {
            var employee = context.Set<Employee>().Single(e => e.Id == employeeId);
            employee.JoiningDate = joining;
            employee.ProbationDays = probationDays;
            employee.ProbationEndDate = joining.AddDays(probationDays);
        });

        return employeeId;
    }

    private DateTime ProbationEndOf(int employeeId) =>
        UsingDbContext(context => context.Set<Employee>().Single(e => e.Id == employeeId).ProbationEndDate);

    [Fact]
    public async Task Calendar_Days_Mode_Should_Simply_Add_The_Probation_Days()
    {
        var joining = DateTime.Today.AddDays(-10);
        var employeeId = GivenAnEmployeeOnProbation("PRB100", joining, probationDays: 90);
        SetProbationMode(ProbationDaysCalculation.CalendarDays);

        (await _probation.CalculateAsync(employeeId)).ShouldBe(joining.AddDays(90));
    }

    [Fact]
    public async Task Working_Days_Mode_Should_Skip_Week_Offs()
    {
        // Sunday start, 10 working days, Friday and Saturday off: the 10th working day is two weeks later.
        var joining = NextSunday(DateTime.Today.AddDays(-40));
        var employeeId = GivenAnEmployeeOnProbation("PRB110", joining, probationDays: 10);
        SetupFridaySaturdayWeekend(employeeId);
        SetProbationMode(ProbationDaysCalculation.WorkingDays);

        // Sun-Thu is five working days a week. The tenth falls on the second Thursday (joining + 11), and
        // legacy's loop leaves the end date on the day AFTER the last day it counted -- so joining + 12.
        (await _probation.CalculateAsync(employeeId)).ShouldBe(joining.AddDays(12));
    }

    [Fact]
    public async Task Unpaid_Leave_During_Probation_Should_Push_The_End_Date_Out()
    {
        var joining = DateTime.Today.AddDays(-20);
        var employeeId = GivenAnEmployeeOnProbation("PRB120", joining, probationDays: 30);
        SetProbationMode(ProbationDaysCalculation.WorkingDays);

        var unpaidTypeId = EnsureUnpaidLeaveType();
        CreateQuota(employeeId, unpaidTypeId, accrued: 30);

        var before = await _probation.CalculateAsync(employeeId);

        // Three days of unpaid leave inside the probation period.
        await _absences.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
        {
            EmployeeId = employeeId,
            AbsenceTypeId = unpaidTypeId,
            StartDate = joining.AddDays(5),
            EndDate = joining.AddDays(7)
        });

        (await _probation.CalculateAsync(employeeId)).ShouldBe(before.AddDays(3));

        // And the employee's stored date was updated by the save, not just the calculation.
        ProbationEndOf(employeeId).ShouldBe(before.AddDays(3));
    }

    [Fact]
    public async Task Paid_Leave_Should_Not_Move_Probation()
    {
        var joining = DateTime.Today.AddDays(-20);
        var employeeId = GivenAnEmployeeOnProbation("PRB130", joining, probationDays: 30);
        SetProbationMode(ProbationDaysCalculation.WorkingDays);

        var paidTypeId = CreateAbsenceType(t => { t.IsPaid = true; t.AllowBackDatedDays = 3650; }, nameEn: "Annual Paid");
        CreateQuota(employeeId, paidTypeId, accrued: 30);

        var before = await _probation.CalculateAsync(employeeId);

        await _absences.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
        {
            EmployeeId = employeeId,
            AbsenceTypeId = paidTypeId,
            StartDate = joining.AddDays(5),
            EndDate = joining.AddDays(7)
        });

        (await _probation.CalculateAsync(employeeId)).ShouldBe(before);
    }

    [Fact]
    public async Task Removing_The_Unpaid_Leave_Should_Bring_Probation_Back()
    {
        var joining = DateTime.Today.AddDays(-20);
        var employeeId = GivenAnEmployeeOnProbation("PRB140", joining, probationDays: 30);
        SetProbationMode(ProbationDaysCalculation.WorkingDays);

        var unpaidTypeId = EnsureUnpaidLeaveType();
        CreateQuota(employeeId, unpaidTypeId, accrued: 30);

        var before = await _probation.CalculateAsync(employeeId);

        await _absences.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
        {
            EmployeeId = employeeId,
            AbsenceTypeId = unpaidTypeId,
            StartDate = joining.AddDays(5),
            EndDate = joining.AddDays(7)
        });

        var leaveId = UsingDbContext(context => context.Set<EmployeeAbsence>()
            .Where(a => a.EmployeeId == employeeId && !a.IsDeleted)
            .Select(a => a.Id)
            .Single());

        await _absences.Delete(new Abp.Application.Services.Dto.EntityDto(leaveId));

        ProbationEndOf(employeeId).ShouldBe(before);
    }

    [Fact]
    public async Task Unpaid_Leave_Should_Not_Move_Probation_In_Calendar_Days_Mode()
    {
        var joining = DateTime.Today.AddDays(-20);
        var employeeId = GivenAnEmployeeOnProbation("PRB150", joining, probationDays: 30);
        SetProbationMode(ProbationDaysCalculation.CalendarDays);

        var unpaidTypeId = EnsureUnpaidLeaveType();
        CreateQuota(employeeId, unpaidTypeId, accrued: 30);

        await _absences.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
        {
            EmployeeId = employeeId,
            AbsenceTypeId = unpaidTypeId,
            StartDate = joining.AddDays(5),
            EndDate = joining.AddDays(7)
        });

        ProbationEndOf(employeeId).ShouldBe(joining.AddDays(30));
    }
}
