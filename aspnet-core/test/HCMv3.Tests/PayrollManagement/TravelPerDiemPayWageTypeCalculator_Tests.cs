using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo.Services;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.TravelManagement;
using HCMv3.Workflow;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// <b>Travel Per Diem Pay</b> -- legacy <c>GetTravelPerDiemPay</c>
/// (<c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 117-127 and 4136-4228; simulation copy
/// lines 4336-4346 and 7947-8028).
///
/// <para>Fixture: employee 18 in group 3 / sub-group 4 for the whole of 2024; a per-diem rate of 25.500 from
/// destination 10 to destination 20 for that group; the travel type is linked to earning wage type 500. One
/// leg of 4 nights therefore pays 4 x 25.500 = 102.000 at 100 %.</para>
///
/// <para>The tests named <c>Legacy_...</c> pin behaviour that looks odd but is what legacy does
/// (see the payroll legacy-parity rule): do not "fix" them without a business decision.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class TravelPerDiemPayWageTypeCalculator_Tests
{
    private const int EmployeeId = 18;
    private const int OtherEmployeeId = 19;
    private const int RegisterId = 700;
    private const int TravelWageTypeId = 500;
    private const int TravelTypeId = 1;
    private const int Kuwait = 10;
    private const int Dubai = 20;
    private const int GroupId = 3;
    private const int SubGroupId = 4;
    private const double Rate = 25.5;

    private static readonly DateTime JuneEnd = new DateTime(2024, 6, 30);

    #region << Fakes >>

    private static IRepository<TEntity> Repo<TEntity>(List<TEntity> items) where TEntity : class, IEntity<int>
    {
        var repository = Substitute.For<IRepository<TEntity>>();
        repository.GetAll().Returns(_ => items.AsQueryable());
        repository.GetAllIncluding(Arg.Any<Expression<Func<TEntity, object>>[]>())
            .Returns(_ => items.AsQueryable());
        return repository;
    }

    private sealed class FakeRegisterStore : IPayrollRegisterStore
    {
        private int _nextId = 1;

        public List<PayrollRegisterLine> Lines { get; } = new List<PayrollRegisterLine>();

        public List<PayrollRegisterTarget> SavedTo { get; } = new List<PayrollRegisterTarget>();

        public Task<List<PayrollRegisterLine>> GetLinesAsync(PayrollRegisterRef register)
            => Task.FromResult(Lines.ToList());

        public Task<PayrollRegisterLine> GetLineAsync(PayrollRegisterRef register, string wageCode)
            => Task.FromResult(Lines.FirstOrDefault(l => l.WageCode == wageCode));

        public Task<PayrollRegisterLine> GetLineByWageTypeIdAsync(PayrollRegisterRef register, int wageTypeId)
            => Task.FromResult(Lines.FirstOrDefault(l => l.WageTypeId == wageTypeId));

        public Task<PayrollRegisterLine> SaveLineAsync(
            PayrollRegisterRef register, PayrollRegisterLine line, bool deleteWhenAmountIsZero = true)
        {
            SavedTo.Add(register.Target);

            var existing = Lines.FirstOrDefault(l => l.WageTypeId == line.WageTypeId);
            if (deleteWhenAmountIsZero && line.Amount == 0)
            {
                if (existing != null)
                {
                    Lines.Remove(existing);
                }

                return Task.FromResult<PayrollRegisterLine>(null);
            }

            if (existing == null)
            {
                existing = new PayrollRegisterLine { Id = _nextId++, WageTypeId = line.WageTypeId, WageCode = line.WageCode };
                Lines.Add(existing);
            }

            existing.Rate = line.Rate;
            existing.Number = line.Number;
            existing.Amount = line.Amount;
            line.Id = existing.Id;
            return Task.FromResult(line);
        }

        public Task DeleteLineAsync(PayrollRegisterRef register, int wageTypeId)
        {
            Lines.RemoveAll(l => l.WageTypeId == wageTypeId);
            return Task.CompletedTask;
        }

        public Task ClearLinesAsync(PayrollRegisterRef register)
        {
            Lines.Clear();
            return Task.CompletedTask;
        }

        public Task SaveDayDetailsAsync(PayrollRegisterRef register, int lineId, IEnumerable<SalaryRegisterDayCount> days)
            => Task.CompletedTask;
    }

    /// <summary>
    /// Records every 2005 posting (including zeros, which the real service ignores) and uses the REAL
    /// month-splitting helper, so off-cycle windows span months as they do in the engine.
    /// </summary>
    private sealed class RecordingAdvancePaymentService : IPayrollAdvancePaymentService
    {
        public List<(DateTime PaymentDate, double Amount)> Postings { get; } = new List<(DateTime, double)>();

        public Task AdvancePaymentDedAsync(int employeeId, DateTime paymentDate, DateTime offCyclePaymentDate,
            double amount, PayrollRunType payrollRunType, string offCycleReason, string advancePayComment,
            PayrollEngineSettings settings)
        {
            Postings.Add((paymentDate, amount));
            return Task.CompletedTask;
        }

        public List<DateTime> GetRecoveryPeriods(DateTime payStartDate, DateTime payEndDate)
            => EmployeeBasicPayCalculationService.GetRecoveryPeriods(payStartDate, payEndDate);

        public Task<int> DeleteAdvancePaymentDedAsync(int employeeId, DateTime offCyclePaymentDate) => Task.FromResult(0);

        public Task<int> ZeroFutureAdvanceRecoveryAsync(int employeeId, DateTime payDate, bool hasAdvancePaidLeaveOnPayDate)
            => Task.FromResult(0);

        public Task<int> ZeroAdvanceRecoveryInWindowAsync(int employeeId, DateTime payStartDate, DateTime offCycleEndDate)
            => Task.FromResult(0);

        public Task<int> ZeroLastMonthRecoveryAsync(int employeeId, DateTime payDate) => Task.FromResult(0);
    }

    private sealed class Harness
    {
        public FakeRegisterStore Store { get; } = new FakeRegisterStore();

        public RecordingAdvancePaymentService AdvancePayment { get; } = new RecordingAdvancePaymentService();

        public List<TravelType> TravelTypes { get; } = new List<TravelType> { TravelTypeLinked(TravelTypeId) };

        public List<Travel> Travels { get; } = new List<Travel>();

        public List<TravelDetail> Legs { get; } = new List<TravelDetail>();

        public List<TravelPerDiem> Rates { get; } = new List<TravelPerDiem>
        {
            new TravelPerDiem
            {
                Id = 1, FromDestinationId = Kuwait, ToDestinationId = Dubai,
                EmployeeGroupId = GroupId, EmployeeSubGroupId = SubGroupId, Amount = Rate
            }
        };

        public IRepository<Travel> TravelRepository { get; }

        public IRepository<TravelDetail> TravelDetailRepository { get; }

        public TravelPerDiemPayWageTypeCalculator Calculator { get; }

        public Harness(IPayrollAdvancePaymentService advancePayment = null)
        {
            TravelRepository = Repo(Travels);
            TravelDetailRepository = Repo(Legs);

            Calculator = new TravelPerDiemPayWageTypeCalculator(
                Store,
                advancePayment ?? AdvancePayment,
                Repo(TravelTypes),
                TravelRepository,
                TravelDetailRepository,
                Repo(Rates));
        }

        /// <summary>Adds a trip with one leg departing on the trip's start date.</summary>
        public Travel AddTrip(
            DateTime startDate,
            int nights,
            double percentage = 100,
            int employeeId = EmployeeId,
            int travelTypeId = TravelTypeId,
            RequestStatus? status = null)
        {
            var travel = new Travel
            {
                Id = Travels.Count + 1,
                EmployeeId = employeeId,
                TravelTypeId = travelTypeId,
                StartDate = startDate,
                EndDate = startDate.AddDays(nights),
                WorkflowStatus = status
            };
            Travels.Add(travel);
            AddLeg(travel, startDate, nights, percentage);
            return travel;
        }

        public TravelDetail AddLeg(Travel travel, DateTime departureDate, int nights, double percentage = 100,
            int departureId = Kuwait, int arrivalId = Dubai)
        {
            var leg = new TravelDetail
            {
                Id = Legs.Count + 1,
                TravelId = travel.Id,
                DepartureId = departureId,
                ArrivalId = arrivalId,
                DepartureDate = departureDate,
                ArrivalDate = departureDate.AddDays(nights),
                TotalNights = nights,
                PerDiemPayPercentage = percentage
            };
            Legs.Add(leg);
            return leg;
        }

        public double? PostedAmount => Store.Lines.FirstOrDefault(l => l.WageTypeId == TravelWageTypeId)?.Amount;
    }

    private static TravelType TravelTypeLinked(int id) => new TravelType
    {
        Id = id, Code = id.ToString(), NameEn = "Business Trip " + id, NameAr = "Business Trip " + id,
        WageTypeId = TravelWageTypeId
    };

    private static WageType TravelWageType(bool includeInAdvancePay = false, bool dontImpactNetPay = false) => new WageType
    {
        Id = TravelWageTypeId,
        Code = "1030",
        NameEn = "Travel Per Diem",
        BillingType = BillingType.Earning,
        IncludeInAdvancePay = includeInAdvancePay,
        DontImpactNetPay = dontImpactNetPay
    };

    private static List<EmployeeOrgAssignment> WholeYearAssignment() => new List<EmployeeOrgAssignment>
    {
        new EmployeeOrgAssignment
        {
            Id = 1, EmployeeId = EmployeeId,
            StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(9999, 12, 31),
            EmployeeGroupId = GroupId, EmployeeSubGroupId = SubGroupId
        }
    };

    private static PayrollCalculationContext RegularContext(DateTime? payDate = null, DateTime? inPeriod = null)
    {
        var pay = payDate ?? JuneEnd;
        return new PayrollCalculationContext
        {
            EmployeeId = EmployeeId,
            PayDate = pay,
            PayStartDate = new DateTime(pay.Year, pay.Month, 1),
            PayInDate = inPeriod ?? pay,
            OffCycleEndDate = inPeriod ?? pay,
            PayrollRunType = PayrollRunType.Regular,
            WageType = TravelWageType(),
            OrgAssignments = WholeYearAssignment(),
            Register = new PayrollRegisterRef
            {
                RegisterId = RegisterId,
                Target = PayrollRegisterTarget.Live,
                ForPeriod = pay,
                InPeriod = inPeriod ?? pay,
                PayrollRunType = PayrollRunType.Regular
            }
        };
    }

    private static PayrollCalculationContext OffCycleContext(
        string reason,
        DateTime offCycleEndDate,
        bool includeInAdvancePay = true,
        bool dontImpactNetPay = false,
        DateTime? payDate = null,
        DateTime? inPeriod = null)
    {
        var pay = payDate ?? JuneEnd;
        return new PayrollCalculationContext
        {
            EmployeeId = EmployeeId,
            PayDate = pay,
            PayStartDate = new DateTime(pay.Year, pay.Month, 1),
            PayInDate = inPeriod ?? pay,
            OffCycleEndDate = offCycleEndDate,
            OffCyclePaymentDate = new DateTime(2024, 6, 20),
            PayrollRunType = PayrollRunType.OffCycle,
            OffCycleReason = reason,
            WageType = TravelWageType(includeInAdvancePay, dontImpactNetPay),
            OrgAssignments = WholeYearAssignment(),
            Register = new PayrollRegisterRef
            {
                RegisterId = RegisterId,
                Target = PayrollRegisterTarget.Live,
                ForPeriod = pay,
                InPeriod = inPeriod ?? pay,
                PayrollRunType = PayrollRunType.OffCycle
            }
        };
    }

    #endregion

    #region << Dispatch >>

    [Fact]
    public void Should_Claim_A_Wage_Type_Linked_To_A_Travel_Type()
    {
        var harness = new Harness();

        harness.Calculator.ClaimsWageType(TravelWageTypeId).ShouldBeTrue();
        harness.Calculator.ClaimsWageType(TravelWageTypeId + 1).ShouldBeFalse();
    }

    [Fact]
    public void Should_Declare_No_Wage_Code()
    {
        new Harness().Calculator.WageCode.ShouldBeNull();
    }

    [Fact]
    public async Task Resolver_Should_Dispatch_A_Travel_Linked_Wage_Type_To_This_Rule()
    {
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);
        var resolver = BuildResolver(harness.Calculator);
        var context = RegularContext();

        resolver.HasCalculatorFor(context.WageType).ShouldBeTrue();
        (await resolver.TryCalculateAsync(context)).ShouldBeTrue();

        harness.PostedAmount.ShouldBe(102);
    }

    [Fact]
    public async Task Resolver_Should_Not_Pay_When_All_Earnings_Are_On_Hold()
    {
        // Legacy line 120: the generic IsOnHold test, applied here by the dispatcher.
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);
        var context = RegularContext();
        context.OnHold.AllEarnings = true;

        (await BuildResolver(harness.Calculator).TryCalculateAsync(context)).ShouldBeFalse();

        harness.PostedAmount.ShouldBeNull();
    }

    private static WageTypeCalculatorResolver BuildResolver(IWageTypeCalculator calculator)
    {
        return new WageTypeCalculatorResolver(
            new[] { calculator },
            Repo(new List<WageTypeException>()),
            Repo(new List<SalaryRegisterDetail>()),
            Repo(new List<EmployeeSeparation>()),
            Repo(new List<EmployeeRejoin>()));
    }

    #endregion

    #region << Amount, percentage, rate >>

    [Fact]
    public async Task Should_Pay_Nights_Times_Rate()
    {
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);

        await harness.Calculator.CalculateAsync(RegularContext());

        harness.PostedAmount.ShouldBe(102);
    }

    [Fact]
    public async Task Should_Pay_Only_The_Per_Diem_Pay_Percentage()
    {
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4, percentage: 50);

        await harness.Calculator.CalculateAsync(RegularContext());

        harness.PostedAmount.ShouldBe(51);
    }

    [Fact]
    public async Task Should_Post_Nothing_When_The_Percentage_Is_Zero()
    {
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4, percentage: 0);

        await harness.Calculator.CalculateAsync(RegularContext());

        harness.PostedAmount.ShouldBeNull();
    }

    [Fact]
    public async Task Legacy_Should_Round_Each_Leg_To_3_Decimals_Before_The_Percentage_And_Not_After()
    {
        // 1 x 1.23456 = 1.23456 -> PerDiemAmount 1.235 (TravelDetail.cs line 141); x 50 % = 0.6175, which
        // legacy does NOT round again (EmployeeCalculatedPayService.cs line 4187).
        var harness = new Harness();
        harness.Rates[0].Amount = 1.23456;
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 1, percentage: 50);

        await harness.Calculator.CalculateAsync(RegularContext());

        harness.PostedAmount.Value.ShouldBe(0.6175, 0.0000001);
    }

    [Fact]
    public async Task Should_Sum_Every_Leg_Of_Every_Trip_Starting_In_The_Month()
    {
        var harness = new Harness();
        var first = harness.AddTrip(new DateTime(2024, 6, 3), nights: 2);              // 51.000
        harness.AddLeg(first, new DateTime(2024, 6, 5), nights: 1, percentage: 50);   // 12.750
        harness.AddTrip(new DateTime(2024, 6, 20), nights: 3);                         // 76.500

        await harness.Calculator.CalculateAsync(RegularContext());

        harness.PostedAmount.ShouldBe(140.25);
    }

    [Fact]
    public async Task Should_Use_The_Rate_Of_The_Group_The_Employee_Is_In_On_The_Departure_Date()
    {
        // Legacy TravelDetail.PerDiemRate: the org assignment covering DepartureDate, not the pay date.
        var harness = new Harness();
        harness.Rates.Add(new TravelPerDiem
        {
            Id = 2, FromDestinationId = Kuwait, ToDestinationId = Dubai,
            EmployeeGroupId = GroupId, EmployeeSubGroupId = SubGroupId + 1, Amount = 40
        });
        var trip = harness.AddTrip(new DateTime(2024, 6, 10), nights: 1);              // old sub-group: 25.500
        harness.AddLeg(trip, new DateTime(2024, 6, 20), nights: 1);                    // new sub-group: 40.000

        var context = RegularContext();
        context.OrgAssignments = new List<EmployeeOrgAssignment>
        {
            new EmployeeOrgAssignment
            {
                Id = 1, EmployeeId = EmployeeId, StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 6, 14),
                EmployeeGroupId = GroupId, EmployeeSubGroupId = SubGroupId
            },
            new EmployeeOrgAssignment
            {
                Id = 2, EmployeeId = EmployeeId, StartDate = new DateTime(2024, 6, 15), EndDate = new DateTime(9999, 12, 31),
                EmployeeGroupId = GroupId, EmployeeSubGroupId = SubGroupId + 1
            }
        };

        await harness.Calculator.CalculateAsync(context);

        harness.PostedAmount.ShouldBe(65.5);
    }

    [Fact]
    public async Task Should_Pay_Nothing_For_A_Leg_Whose_Destinations_Have_No_Rate()
    {
        var harness = new Harness();
        var trip = harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);              // 102.000
        harness.AddLeg(trip, new DateTime(2024, 6, 14), nights: 4, departureId: Dubai, arrivalId: Kuwait); // no rate

        await harness.Calculator.CalculateAsync(RegularContext());

        harness.PostedAmount.ShouldBe(102);
    }

    [Fact]
    public async Task Should_Pay_Nothing_When_No_Org_Assignment_Covers_The_Departure_Date()
    {
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);
        var context = RegularContext();
        context.OrgAssignments[0].StartDate = new DateTime(2024, 7, 1);

        await harness.Calculator.CalculateAsync(context);

        harness.PostedAmount.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Ignore_An_Org_Assignment_Without_A_Sub_Group()
    {
        // Legacy requires both EmployeeGroup and EmployeeSubGroup on the assignment (TravelDetail.cs line 117-118).
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);
        var context = RegularContext();
        context.OrgAssignments[0].EmployeeSubGroupId = null;

        await harness.Calculator.CalculateAsync(context);

        harness.PostedAmount.ShouldBeNull();
    }

    #endregion

    #region << Which trips >>

    [Fact]
    public async Task Should_Not_Pay_Another_Employees_Trip()
    {
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4, employeeId: OtherEmployeeId);

        await harness.Calculator.CalculateAsync(RegularContext());

        harness.PostedAmount.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_Pay_A_Trip_Of_A_Travel_Type_Not_Linked_To_This_Wage_Type()
    {
        var harness = new Harness();
        harness.TravelTypes.Add(new TravelType { Id = 2, Code = "2", NameEn = "Training", NameAr = "Training", WageTypeId = null });
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4, travelTypeId: 2);

        await harness.Calculator.CalculateAsync(RegularContext());

        harness.PostedAmount.ShouldBeNull();
    }

    [Theory]
    [InlineData(RequestStatus.Open)]
    [InlineData(RequestStatus.Rejected)]
    public async Task Should_Not_Pay_A_Trip_That_Is_Not_Approved(RequestStatus status)
    {
        // Legacy had no Travel row until the TravelDeskRequest was approved; here the pending request IS the
        // trip, so the ApprovedOnly rule stands in for that.
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4, status: status);

        await harness.Calculator.CalculateAsync(RegularContext());

        harness.PostedAmount.ShouldBeNull();
    }

    [Theory]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Closed)]
    public async Task Should_Pay_An_Approved_Or_Closed_Trip(RequestStatus status)
    {
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4, status: status);

        await harness.Calculator.CalculateAsync(RegularContext());

        harness.PostedAmount.ShouldBe(102);
    }

    [Fact]
    public async Task Should_Pay_A_Trip_Keyed_In_Directly_Without_Workflow()
    {
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4, status: null);

        await harness.Calculator.CalculateAsync(RegularContext());

        harness.PostedAmount.ShouldBe(102);
    }

    [Fact]
    public async Task Should_Not_Pay_A_Trip_That_Started_Before_The_Month_Even_If_Its_Nights_Fall_In_It()
    {
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 5, 31), nights: 5);

        await harness.Calculator.CalculateAsync(RegularContext());

        harness.PostedAmount.ShouldBeNull();
    }

    [Fact]
    public async Task Legacy_Should_Pay_A_Trip_In_Full_In_The_Month_It_Starts_Even_When_It_Runs_Into_The_Next()
    {
        // Partial periods are not apportioned: legacy selects on Travel.StartDate only (line 4175-4176).
        var harness = new Harness();
        var trip = harness.AddTrip(new DateTime(2024, 6, 30), nights: 2);              // 51.000
        harness.AddLeg(trip, new DateTime(2024, 7, 2), nights: 3);                     // 76.500, departs in July

        await harness.Calculator.CalculateAsync(RegularContext());
        harness.PostedAmount.ShouldBe(127.5);

        var july = new Harness();
        var sameTrip = july.AddTrip(new DateTime(2024, 6, 30), nights: 2);
        july.AddLeg(sameTrip, new DateTime(2024, 7, 2), nights: 3);

        await july.Calculator.CalculateAsync(RegularContext(new DateTime(2024, 7, 31)));
        july.PostedAmount.ShouldBeNull();
    }

    [Fact]
    public async Task Legacy_Should_Pay_Only_The_First_Travel_Type_Linked_To_The_Wage_Type()
    {
        // Legacy line 118: TravelTypeList.Where(r => r.WageType.Oid == currWageType.Oid).FirstOrDefault().
        var harness = new Harness();
        harness.TravelTypes.Add(TravelTypeLinked(2));
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4, travelTypeId: TravelTypeId);   // 102.000, paid
        harness.AddTrip(new DateTime(2024, 6, 20), nights: 2, travelTypeId: 2);              // 51.000, never paid

        await harness.Calculator.CalculateAsync(RegularContext());

        harness.PostedAmount.ShouldBe(102);
    }

    #endregion

    #region << Register line >>

    [Fact]
    public async Task Legacy_Should_Leave_An_Existing_Line_For_The_Wage_Type_Untouched()
    {
        // Legacy lines 4203-4221: the line is written only when none exists yet -- no overwrite, no add.
        var harness = new Harness();
        harness.Store.Lines.Add(new PayrollRegisterLine { Id = 99, WageTypeId = TravelWageTypeId, WageCode = "1030", Amount = 7 });
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);

        await harness.Calculator.CalculateAsync(RegularContext());

        harness.PostedAmount.ShouldBe(7);
        harness.Store.SavedTo.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Write_No_Line_When_There_Are_No_Trips()
    {
        var harness = new Harness();

        await harness.Calculator.CalculateAsync(RegularContext());

        harness.Store.Lines.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Write_Nothing_To_The_Trips()
    {
        // Legacy sets no "per diem paid" flag on the trip.
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);

        await harness.Calculator.CalculateAsync(RegularContext());

        await harness.TravelRepository.DidNotReceiveWithAnyArgs().UpdateAsync(default(Travel));
        await harness.TravelRepository.DidNotReceiveWithAnyArgs().InsertAsync(default(Travel));
        await harness.TravelDetailRepository.DidNotReceiveWithAnyArgs().UpdateAsync(default(TravelDetail));
    }

    [Fact]
    public async Task Legacy_Should_Pay_The_Same_Trip_Again_On_A_Re_Run()
    {
        // No paid flag, so a recalculation of the same month pays the same trips again -- the engine clears
        // the register first, so the result is the same amount, not a doubled one.
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);

        await harness.Calculator.CalculateAsync(RegularContext());
        await harness.Store.ClearLinesAsync(RegularContext().Register);
        await harness.Calculator.CalculateAsync(RegularContext());

        harness.PostedAmount.ShouldBe(102);
    }

    [Fact]
    public async Task Regular_Run_Should_Not_Post_Advance_Payment_Deduction()
    {
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);

        await harness.Calculator.CalculateAsync(RegularContext());

        harness.AdvancePayment.Postings.ShouldBeEmpty();
    }

    #endregion

    #region << Off-cycle >>

    [Fact]
    public async Task Advance_Leave_Off_Cycle_Should_Pay_Nothing_When_The_Wage_Type_Is_Not_Included_In_Advance_Pay()
    {
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);

        await harness.Calculator.CalculateAsync(
            OffCycleContext(PayrollOffCycleReasons.AdvanceLeave, new DateTime(2024, 6, 30), includeInAdvancePay: false));

        harness.PostedAmount.ShouldBeNull();
        harness.AdvancePayment.Postings.ShouldBeEmpty();
    }

    [Fact]
    public async Task Advance_Leave_Off_Cycle_Should_Pay_Every_Month_Up_To_The_Off_Cycle_End_And_Post_Each_To_2005()
    {
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);   // 102.000 in June
        harness.AddTrip(new DateTime(2024, 8, 5), nights: 2);    //  51.000 in August
        harness.AddTrip(new DateTime(2024, 9, 1), nights: 2);    // after the window, not paid

        await harness.Calculator.CalculateAsync(
            OffCycleContext(PayrollOffCycleReasons.AdvanceLeave, new DateTime(2024, 8, 15)));

        harness.PostedAmount.ShouldBe(153);

        // Legacy lines 4189-4194: once per month, positive, against the month's first day.
        harness.AdvancePayment.Postings.ShouldBe(new List<(DateTime, double)>
        {
            (new DateTime(2024, 6, 1), 102),
            (new DateTime(2024, 7, 1), 0),
            (new DateTime(2024, 8, 1), 51)
        });
    }

    [Fact]
    public async Task End_Of_Service_Off_Cycle_Should_Pay_Whatever_The_Advance_Pay_Flag()
    {
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);

        await harness.Calculator.CalculateAsync(
            OffCycleContext(PayrollOffCycleReasons.EndOfService, new DateTime(2024, 6, 30), includeInAdvancePay: false));

        harness.PostedAmount.ShouldBe(102);
    }

    [Fact]
    public async Task Leave_Encashment_Off_Cycle_Should_Pay_Nothing()
    {
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);

        await harness.Calculator.CalculateAsync(
            OffCycleContext(PayrollOffCycleReasons.LeaveEncashment, new DateTime(2024, 6, 30)));

        harness.PostedAmount.ShouldBeNull();
    }

    [Fact]
    public async Task Off_Cycle_Should_Not_Post_2005_When_The_Wage_Type_Does_Not_Impact_Net_Pay()
    {
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);

        await harness.Calculator.CalculateAsync(
            OffCycleContext(PayrollOffCycleReasons.AdvanceLeave, new DateTime(2024, 6, 30), dontImpactNetPay: true));

        harness.PostedAmount.ShouldBe(102);
        harness.AdvancePayment.Postings.ShouldBeEmpty();
    }

    #endregion

    #region << Retro >>

    [Fact]
    public async Task Retro_Period_Of_A_Regular_Run_Should_Pay_The_Trips_Of_The_Retro_Month()
    {
        // Retro: PayDate is the retro month's end (May), the register is paid in June.
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 5, 12), nights: 2);   // 51.000, May
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);   // June -- not this period

        await harness.Calculator.CalculateAsync(RegularContext(new DateTime(2024, 5, 31), inPeriod: JuneEnd));

        harness.PostedAmount.ShouldBe(51);
    }

    [Fact]
    public async Task Retro_Period_Of_An_Off_Cycle_Run_Should_Not_Extend_To_The_Off_Cycle_End()
    {
        // Legacy line 4151: the window runs to offCycleEndDate only when InPeriod == ForPeriod.
        var harness = new Harness();
        harness.AddTrip(new DateTime(2024, 5, 12), nights: 2);   // 51.000, May
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);   // June -- outside a May retro period

        await harness.Calculator.CalculateAsync(OffCycleContext(
            PayrollOffCycleReasons.AdvanceLeave,
            new DateTime(2024, 7, 15),
            payDate: new DateTime(2024, 5, 31),
            inPeriod: JuneEnd));

        harness.PostedAmount.ShouldBe(51);
        harness.AdvancePayment.Postings.ShouldBe(new List<(DateTime, double)> { (new DateTime(2024, 5, 1), 51) });
    }

    #endregion

    #region << Simulation >>

    [Fact]
    public async Task Simulation_Should_Pay_The_Same_Into_The_Simulation_Register_And_Touch_No_Live_Data()
    {
        // The real advance-payment service: in a simulation it must write no EmployeePayment (2005) row
        // (legacy SimulationPayrollHelper.AdvancePaymentDed is commented out).
        var employeePayments = Substitute.For<IRepository<EmployeePayment>>();
        employeePayments.GetAll().Returns(_ => new List<EmployeePayment>().AsQueryable());
        var advancePayment = new PayrollAdvancePaymentService(
            employeePayments,
            Repo(new List<WageType>()),
            Repo(new List<Employee>()),
            Substitute.For<ICurrencyService>());

        var harness = new Harness(advancePayment);
        harness.AddTrip(new DateTime(2024, 6, 10), nights: 4);
        harness.AddTrip(new DateTime(2024, 7, 3), nights: 2);

        var context = OffCycleContext(PayrollOffCycleReasons.AdvanceLeave, new DateTime(2024, 7, 31));
        context.Register.Target = PayrollRegisterTarget.Simulation;

        using (PayrollSimulationScope.Begin())
        {
            await harness.Calculator.CalculateAsync(context);
        }

        harness.PostedAmount.ShouldBe(153);
        harness.Store.SavedTo.ShouldAllBe(t => t == PayrollRegisterTarget.Simulation);

        await employeePayments.DidNotReceiveWithAnyArgs().InsertAsync(default(EmployeePayment));
        await employeePayments.DidNotReceiveWithAnyArgs().UpdateAsync(default(EmployeePayment));
        await harness.TravelRepository.DidNotReceiveWithAnyArgs().UpdateAsync(default(Travel));
        await harness.TravelDetailRepository.DidNotReceiveWithAnyArgs().UpdateAsync(default(TravelDetail));
        harness.Travels.Count.ShouldBe(2);
        harness.Legs.Count.ShouldBe(2);
    }

    #endregion
}
