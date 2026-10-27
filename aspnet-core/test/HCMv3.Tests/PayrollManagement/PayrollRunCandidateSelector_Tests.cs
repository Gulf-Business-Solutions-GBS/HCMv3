using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using NSubstitute;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Who the Payroll Engine screen's employee picker lists -- legacy <c>uscPayrollEngine.BuildEmployeePopupList</c>
/// (<c>uscPayrollEngine.ascx.cs</c> lines 788-1020). The control record sits on September 2026, so an off-cycle
/// lands in October 2026.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollRunCandidateSelector_Tests
{
    private const string Area = "KWT";

    private static readonly DateTime September = new DateTime(2026, 9, 30);
    private static readonly DateTime October = new DateTime(2026, 10, 31);

    /// <summary>A day after October, so the CurrentDate pick-up rule never narrows the window unless a test says so.</summary>
    private static readonly DateTime AfterOctober = new DateTime(2026, 11, 15);

    private readonly List<Employee> _employees = new();
    private readonly List<SalaryRegister> _registers = new();
    private readonly List<PayrollReasonType> _reasonTypes = new();
    private readonly List<EmployeeAbsence> _absences = new();
    private readonly List<EmployeeAbsAdjustment> _adjustments = new();
    private readonly List<EmployeeFnFPayment> _fnfPayments = new();
    private readonly List<EmployeeSeparation> _separations = new();
    private readonly List<int> _employeeIdsInArea = new();

    private readonly AbsenceType _paidLeave = new() { Id = 1, Code = "1", IsPaid = true, QuotaType = QuotaType.AnnualLeave };
    private readonly AbsenceType _unpaidLeave = new() { Id = 2, Code = "9", IsPaid = false, QuotaType = QuotaType.None };

    public PayrollRunCandidateSelector_Tests()
    {
        _reasonTypes.Add(new PayrollReasonType { Id = 11, NameEn = PayrollOffCycleReasons.EndOfService, NameAr = "EOS" });
        _reasonTypes.Add(new PayrollReasonType { Id = 12, NameEn = PayrollOffCycleReasons.AdvanceLeave, NameAr = "AL" });
        _reasonTypes.Add(new PayrollReasonType { Id = 13, NameEn = PayrollOffCycleReasons.LeaveEncashment, NameAr = "LE" });
    }

    private PayrollRunCandidateSelector CreateSelector()
    {
        var resolver = Substitute.For<IEmployeePayrollAreaResolver>();
        resolver.GetEmployeeIdsInPayrollAreaAsync(Area, Arg.Any<DateTime>())
            .Returns(_ => Task.FromResult(_employeeIdsInArea.ToList()));
        resolver.GetEmployeeIdsInPayrollAreaAsync(Arg.Is<string>(s => s != Area), Arg.Any<DateTime>())
            .Returns(_ => Task.FromResult(new List<int>()));

        return new PayrollRunCandidateSelector(
            Repo(_employees),
            Repo(_registers),
            Repo(_reasonTypes),
            Repo(_absences),
            Repo(_adjustments),
            Repo(_fnfPayments),
            Repo(_separations),
            resolver);
    }

    private Employee AddEmployee(int id, EmployeeStatus status = EmployeeStatus.Active, DateTime? joiningDate = null, bool inArea = true)
    {
        var employee = new Employee
        {
            Id = id,
            Code = "E" + id,
            FullNameEn = "Employee " + id,
            FullNameAr = "Arabic " + id,
            Status = status,
            JoiningDate = joiningDate ?? new DateTime(2020, 1, 1)
        };
        _employees.Add(employee);
        if (inArea)
        {
            _employeeIdsInArea.Add(id);
        }

        return employee;
    }

    private void AddRegister(int employeeId, PayrollRunType runType, DateTime period, int? reasonTypeId = null)
    {
        _registers.Add(new SalaryRegister
        {
            Id = _registers.Count + 1,
            EmployeeId = employeeId,
            PayrollRunType = runType,
            ForPeriod = period,
            InPeriod = period,
            PayrollReasonTypeId = reasonTypeId
        });
    }

    private void AddAdvanceLeave(int employeeId, DateTime paymentDate, AbsenceType type = null, LeavePaymentType paymentType = LeavePaymentType.PayInAdvance)
    {
        type ??= _paidLeave;
        _absences.Add(new EmployeeAbsence
        {
            Id = _absences.Count + 1,
            EmployeeId = employeeId,
            AbsenceTypeId = type.Id,
            AbsenceType = type,
            LeavePaymentType = paymentType,
            PaymentDate = paymentDate
        });
    }

    private void AddEncashment(int employeeId, DateTime effectiveDate, AdjustmentPaymentType paymentType = AdjustmentPaymentType.PayInAdvance)
    {
        _adjustments.Add(new EmployeeAbsAdjustment
        {
            Id = _adjustments.Count + 1,
            EmployeeId = employeeId,
            AbsenceTypeId = _paidLeave.Id,
            AbsenceType = _paidLeave,
            AdjustmentType = AdjustmentType.Encashment,
            AdjustmentPaymentType = paymentType,
            EffectiveDate = effectiveDate
        });
    }

    private void AddFnF(int employeeId, DateTime compensationDate)
    {
        _fnfPayments.Add(new EmployeeFnFPayment { Id = _fnfPayments.Count + 1, EmployeeId = employeeId, CompensationDate = compensationDate });
    }

    private void AddSeparation(int employeeId, DateTime? revokeDate = null)
    {
        _separations.Add(new EmployeeSeparation { Id = _separations.Count + 1, EmployeeId = employeeId, RevokeDate = revokeDate });
    }

    private Task<List<PayrollRunCandidate>> Run(
        PayrollRunType runType,
        string reason = null,
        PickOffcyclePayment pick = PickOffcyclePayment.CurrentMonth,
        DateTime? today = null,
        string area = Area)
    {
        return CreateSelector().GetCandidatesAsync(area, runType, reason, September, pick, today ?? AfterOctober);
    }

    [Fact]
    public async Task Should_List_Only_Active_Joined_Employees_Of_The_Area_For_A_Regular_Run()
    {
        AddEmployee(1);
        AddEmployee(2, EmployeeStatus.Inactive);
        AddEmployee(3, joiningDate: new DateTime(2026, 10, 1));
        AddEmployee(4, inArea: false);

        var result = await Run(PayrollRunType.Regular);

        result.Select(c => c.EmployeeId).ShouldBe(new[] { 1 });
    }

    [Fact]
    public async Task Should_Mark_Employees_With_A_Regular_Register_For_The_Period_As_Processed()
    {
        AddEmployee(1);
        AddEmployee(2);
        AddEmployee(3);
        AddRegister(1, PayrollRunType.Regular, September);
        AddRegister(2, PayrollRunType.Regular, new DateTime(2026, 8, 31));
        AddRegister(3, PayrollRunType.OffCycle, September);

        var result = await Run(PayrollRunType.Regular);

        result.Single(c => c.EmployeeId == 1).IsProcessed.ShouldBeTrue();
        result.Single(c => c.EmployeeId == 2).IsProcessed.ShouldBeFalse();
        result.Single(c => c.EmployeeId == 3).IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_List_Paid_Pay_In_Advance_Leave_And_Encashments_Of_The_Next_Period_For_Advance_Leave()
    {
        AddEmployee(1);
        AddEmployee(2);
        AddEmployee(3);
        AddEmployee(4);
        AddEmployee(5);
        AddEmployee(6, EmployeeStatus.Inactive);
        AddAdvanceLeave(1, new DateTime(2026, 10, 15));
        AddEncashment(2, new DateTime(2026, 10, 20));
        AddAdvanceLeave(3, new DateTime(2026, 10, 15), _unpaidLeave);
        AddAdvanceLeave(4, new DateTime(2026, 10, 15), paymentType: LeavePaymentType.PayWithPayroll);
        AddAdvanceLeave(5, new DateTime(2026, 9, 15));
        AddAdvanceLeave(6, new DateTime(2026, 10, 15));

        var result = await Run(PayrollRunType.OffCycle, PayrollOffCycleReasons.AdvanceLeave);

        result.Select(c => c.EmployeeId).OrderBy(id => id).ShouldBe(new[] { 1, 2 });
    }

    [Fact]
    public async Task Should_List_Only_Advance_Paid_Encashments_For_Leave_Encashment()
    {
        AddEmployee(1);
        AddEmployee(2);
        AddEmployee(3);
        AddAdvanceLeave(1, new DateTime(2026, 10, 15));
        AddEncashment(2, new DateTime(2026, 10, 20));
        AddEncashment(3, new DateTime(2026, 10, 20), AdjustmentPaymentType.PayWithPayroll);

        var result = await Run(PayrollRunType.OffCycle, PayrollOffCycleReasons.LeaveEncashment);

        result.Select(c => c.EmployeeId).ShouldBe(new[] { 2 });
    }

    [Fact]
    public async Task Should_List_Inactive_Employees_With_FnF_And_Open_Separation_For_End_Of_Service()
    {
        AddEmployee(1, EmployeeStatus.Inactive);
        AddEmployee(2, EmployeeStatus.Inactive);
        AddEmployee(3, EmployeeStatus.Inactive);
        AddEmployee(4);
        AddFnF(1, new DateTime(2026, 10, 10));
        AddSeparation(1);
        AddFnF(2, new DateTime(2026, 10, 10));
        AddSeparation(2, revokeDate: new DateTime(2026, 10, 12));
        AddSeparation(3);
        AddFnF(4, new DateTime(2026, 10, 10));
        AddSeparation(4);

        var result = await Run(PayrollRunType.OffCycle, PayrollOffCycleReasons.EndOfService);

        result.Select(c => c.EmployeeId).ShouldBe(new[] { 1 });
    }

    [Fact]
    public async Task Should_Mark_Off_Cycle_Processed_Only_With_The_Same_Reason_In_The_Next_Period()
    {
        AddEmployee(1);
        AddEmployee(2);
        AddAdvanceLeave(1, new DateTime(2026, 10, 15));
        AddAdvanceLeave(2, new DateTime(2026, 10, 15));
        AddRegister(1, PayrollRunType.OffCycle, October, reasonTypeId: 12);
        AddRegister(2, PayrollRunType.OffCycle, October, reasonTypeId: 13);

        var result = await Run(PayrollRunType.OffCycle, PayrollOffCycleReasons.AdvanceLeave);

        result.Single(c => c.EmployeeId == 1).IsProcessed.ShouldBeTrue();
        result.Single(c => c.EmployeeId == 2).IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Mark_End_Of_Service_Processed_From_An_End_Of_Service_Register()
    {
        AddEmployee(1, EmployeeStatus.Inactive);
        AddFnF(1, new DateTime(2026, 10, 10));
        AddSeparation(1);
        AddRegister(1, PayrollRunType.OffCycle, October, reasonTypeId: 11);

        var result = await Run(PayrollRunType.OffCycle, PayrollOffCycleReasons.EndOfService);

        result.Single().IsProcessed.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Pick_Payments_Only_Up_To_Today_When_The_Setting_Says_Current_Date()
    {
        AddEmployee(1);
        AddEmployee(2);
        AddAdvanceLeave(1, new DateTime(2026, 10, 5));
        AddAdvanceLeave(2, new DateTime(2026, 10, 25));

        var result = await Run(
            PayrollRunType.OffCycle,
            PayrollOffCycleReasons.AdvanceLeave,
            PickOffcyclePayment.CurrentDate,
            today: new DateTime(2026, 10, 10));

        result.Select(c => c.EmployeeId).ShouldBe(new[] { 1 });
    }

    [Fact]
    public async Task Should_Pick_The_Whole_Next_Period_When_The_Setting_Says_Current_Month()
    {
        AddEmployee(1);
        AddEmployee(2);
        AddAdvanceLeave(1, new DateTime(2026, 10, 5));
        AddAdvanceLeave(2, new DateTime(2026, 10, 25));

        var result = await Run(
            PayrollRunType.OffCycle,
            PayrollOffCycleReasons.AdvanceLeave,
            PickOffcyclePayment.CurrentMonth,
            today: new DateTime(2026, 10, 10));

        result.Select(c => c.EmployeeId).OrderBy(id => id).ShouldBe(new[] { 1, 2 });
    }

    [Fact]
    public async Task Should_List_Nobody_For_A_Reason_The_Engine_Does_Not_Know()
    {
        AddEmployee(1);
        AddAdvanceLeave(1, new DateTime(2026, 10, 15));

        var result = await Run(PayrollRunType.OffCycle, "Bonus");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_List_Nobody_When_The_Area_Has_No_Employees()
    {
        AddEmployee(1);

        var result = await Run(PayrollRunType.Regular, area: "OTHER");

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Use_Today_Only_Inside_The_Period_For_Current_Date_Pick_Up()
    {
        var start = new DateTime(2026, 10, 1);

        PayrollRunCandidateSelector.PickUpToDate(PickOffcyclePayment.CurrentDate, start, October, new DateTime(2026, 10, 10, 14, 0, 0))
            .ShouldBe(new DateTime(2026, 10, 10));
        PayrollRunCandidateSelector.PickUpToDate(PickOffcyclePayment.CurrentDate, start, October, new DateTime(2026, 9, 20))
            .ShouldBe(October);
        PayrollRunCandidateSelector.PickUpToDate(PickOffcyclePayment.CurrentMonth, start, October, new DateTime(2026, 10, 10))
            .ShouldBe(October);
    }
}
