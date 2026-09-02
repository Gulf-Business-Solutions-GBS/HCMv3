using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.TravelManagement;
using HCMv3.TravelManagement.Dto;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Self-service travel requests -- the front half of travel, and until this wave the missing half.
///
/// <para>
/// The test to read first is
/// <see cref="An_Expense_Claim_Should_Be_Raisable_Against_A_Trip_The_Employee_Requested_And_Had_Approved"/>.
/// It is the whole point of the exercise. <c>TravelFeedback</c>, the travel EXPENSE CLAIM, has been routable
/// for some time and points at a <see cref="Travel"/> row -- but nothing in the product could create that
/// row except an administrator typing it in, so an employee could claim expenses for a trip that nobody was
/// able to request. That test walks the full path end to end: request the trip, approve it, claim against
/// it. If it passes, the orphan is closed.
/// </para>
///
/// <para>
/// Its close companions are the visibility pair. <see cref="Travel"/> carries the workflow status; its legs
/// and its cost lines deliberately do not, because they are not requests -- they hang off one and are only
/// ever reached through it. So the assertion that matters is not just that a pending trip is hidden, but
/// that its ITINERARY is hidden with it: a leaked leg is a hotel somebody books for a trip that was never
/// agreed.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyTravelAppService_Tests : AppTestBase
{
    private readonly IMyTravelAppService _travel;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public MyTravelAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _travel = Resolve<IMyTravelAppService>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- fixtures

    private int CreateEmployee(string code, long? userId = null)
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

        return employee.Id;
    }

    private int CreateCurrentUserEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    private int CreateTravelType()
    {
        TravelType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<TravelType>().Add(new TravelType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Business",
                NameAr = "عمل"
            }).Entity;
        });

        return type.Id;
    }

    private int CreateDestination(string name)
    {
        TravelDestination destination = null;
        UsingDbContext(context =>
        {
            destination = context.Set<TravelDestination>().Add(new TravelDestination
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = name,
                NameAr = name
            }).Entity;
        });

        return destination.Id;
    }

    private int CreateExpenseType()
    {
        ExpenseType expenseType = null;
        UsingDbContext(context =>
        {
            expenseType = context.Set<ExpenseType>().Add(new ExpenseType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Airfare",
                NameAr = "تذاكر"
            }).Entity;
        });

        return expenseType.Id;
    }

    private int CreateAirline()
    {
        TravelAirline airline = null;
        UsingDbContext(context =>
        {
            airline = context.Set<TravelAirline>().Add(new TravelAirline
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Kuwait Airways",
                NameAr = "الخطوط الكويتية"
            }).Entity;
        });

        return airline.Id;
    }

    private int CreateCurrency()
    {
        Currency currency = null;
        UsingDbContext(context =>
        {
            currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "US Dollar",
                NameAr = "دولار"
            }).Entity;
        });

        return currency.Id;
    }

    /// <summary>
    /// Turns workflow on for the named transaction types, all pointing at one real one-step chain. Returns
    /// the approver's employee id.
    /// </summary>
    private int EnableWorkflow(string approverCode, params string[] typeCodes)
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
                Code = "TRV-STD-" + Guid.NewGuid().ToString("N").Substring(0, 4),
                NameEn = "Standard",
                NameAr = "Standard",
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
                EndDate = DateTime.Today.AddYears(5)
            });

            foreach (var code in typeCodes)
            {
                var type = context.Set<WorkflowTransactionType>().First(t => t.Code == code);
                type.IsWorkflowEnabled = true;
                type.IsEssEnabled = true;
                type.IsActive = true;
                type.DefaultRequestMasterId = master.Id;
            }
        });

        return approverId;
    }

    private static readonly DateTime TripStart = new DateTime(2026, 5, 10);
    private static readonly DateTime TripEnd = new DateTime(2026, 5, 15);

    private RequestTravelInput TripFor(
        int travelTypeId,
        int departureId,
        int arrivalId,
        DateTime? start = null,
        DateTime? end = null,
        int? expenseTypeId = null,
        int? airlineId = null,
        int? currencyId = null,
        bool withLegs = true)
    {
        var input = new RequestTravelInput
        {
            TravelTypeId = travelTypeId,
            StartDate = start ?? TripStart,
            EndDate = end ?? TripEnd,
            TravelPurpose = "Supplier audit",
            Comments = "Please book early",
            Legs = withLegs ? new List<RequestTravelLegInput>() : null
        };

        if (!withLegs)
        {
            return input;
        }

        input.Legs.Add(new RequestTravelLegInput
        {
            DepartureId = departureId,
            ArrivalId = arrivalId,
            DepartureDate = input.StartDate,
            DepartureTime = input.StartDate.AddHours(8),
            ArrivalDate = input.StartDate,
            ArrivalTime = input.StartDate.AddHours(12),
            TravelAirlineId = airlineId,
            NightStay = true,
            TotalNights = 5,
            IsVisaRequired = true,
            VisaComments = "Business visa on arrival",
            IsHotelRequired = true,
            HotelComments = "Near the plant",
            AnyOtherExpense = true,
            OtherComments = "Airport transfers",
            Costs = expenseTypeId.HasValue
                ? new List<RequestTravelCostInput>
                {
                    new RequestTravelCostInput
                    {
                        ExpenseTypeId = expenseTypeId.Value,
                        CurrencyId = currencyId,
                        Amount = 450,
                        Comments = "Return airfare"
                    }
                }
                : null
        });

        return input;
    }

    private static GetMyRequestsInput ListInput() =>
        new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 };

    /// <summary>Plants a trip directly, bypassing every service, so any workflow status can be seeded.</summary>
    private int PlantTrip(int employeeId, int travelTypeId, DateTime start, DateTime end,
        int departureId, int arrivalId, RequestStatus? status)
    {
        Travel travel = null;
        UsingDbContext(context =>
        {
            travel = context.Set<Travel>().Add(new Travel
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                TravelTypeId = travelTypeId,
                StartDate = start,
                EndDate = end,
                WorkflowStatus = status,
                TravelDetails = new List<TravelDetail>
                {
                    new TravelDetail
                    {
                        TenantId = TenantId,
                        DepartureId = departureId,
                        ArrivalId = arrivalId,
                        DepartureDate = start,
                        ArrivalDate = start
                    }
                }
            }).Entity;
        });

        return travel.Id;
    }

    /// <summary>
    /// Reads inside a unit of work, which is the only place an ABP data filter exists -- and therefore the
    /// only place "hidden" means anything.
    /// </summary>
    private async Task<TResult> InUnitOfWorkAsync<TResult>(Func<Task<TResult>> action)
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            var result = await action();
            await uow.CompleteAsync();

            return result;
        }
    }

    private Task<bool> TripIsVisibleAsync(int travelId)
    {
        return InUnitOfWorkAsync(() =>
            Resolve<IRepository<Travel>>().GetAll().AnyAsync(t => t.Id == travelId));
    }

    /// <summary>
    /// How many of the trip's legs are reachable, and how many cost lines under them -- through the header,
    /// which is the only route to either. That semi-join is not the test being clever: it is exactly what
    /// <c>TravelDetailsAppService</c> and <c>TravelDetailCostsAppService</c> do, and it is the whole
    /// mechanism by which a child hides with its parent when the child carries no status of its own.
    /// </summary>
    private Task<(int legs, int costs)> VisibleItineraryAsync(int travelId)
    {
        return InUnitOfWorkAsync(async () =>
        {
            var travels = Resolve<IRepository<Travel>>();
            var details = Resolve<IRepository<TravelDetail>>();
            var costs = Resolve<IRepository<TravelDetailCost>>();

            var visibleTravelIds = travels.GetAll().Select(t => t.Id);

            var visibleLegIds = await details.GetAll()
                .Where(d => d.TravelId == travelId && visibleTravelIds.Contains(d.TravelId))
                .Select(d => d.Id)
                .ToListAsync();

            var costCount = await costs.GetAll()
                .CountAsync(c => visibleLegIds.Contains(c.TravelDetailId));

            return (visibleLegIds.Count, costCount);
        });
    }

    private async Task ApproveTheOutstandingRequestAsync(string requestType, int approverId)
    {
        var requestId = UsingDbContext(c => c.Set<WorkflowRequest>()
            .Where(r => r.RequestType == requestType && r.RequestStatus == RequestStatus.Open)
            .Select(r => r.Id)
            .Single());

        await Resolve<IWorkflowApprovalService>().ApproveAsync(requestId, approverId, "Agreed");
    }

    // ---------------------------------------------------------------- a trip has to go somewhere

    [Fact]
    public async Task A_Trip_With_No_Legs_Should_Be_Refused()
    {
        // Nowhere to go, nothing to book, nothing to cost -- there is no decision for an approver to make.
        // Refused by the DTO's [Required]/[MinLength(1)] annotations before the service is even entered,
        // which is the earliest and cheapest place to say no. The service's own check remains as the
        // backstop for any caller that bypasses validation, and the handler's check
        // (A_Trip_With_No_Legs_Should_Be_Refused_At_The_Travel_Desk_Too) is the one that holds on the row.
        CreateCurrentUserEmployee("TRV100");
        var typeId = CreateTravelType();

        await Assert.ThrowsAsync<Abp.Runtime.Validation.AbpValidationException>(() =>
            _travel.RequestTravel(TripFor(typeId, 0, 0, withLegs: false)));
    }

    [Fact]
    public async Task A_Trip_With_No_Legs_Should_Be_Refused_At_The_Travel_Desk_Too()
    {
        // The same rule, enforced on the ROW by the handler rather than on the form -- which is what makes
        // it hold for a booking the travel desk raises on somebody's behalf, not only for a self-service
        // request. The refusal is the point: legacy's TravelDeskRequest.OnSaving swallowed every exception,
        // so a request could be marked approved with nothing written and nothing reported. Here the throw
        // reaches the caller.
        //
        // NOTE ON WHAT THIS DOES NOT ASSERT: create and submit share one unit of work, so in production the
        // half-created trip is rolled back with the exception. That cannot be verified here --
        // HCMv3TestBaseModule sets Configuration.UnitOfWork.IsTransactional = false, so the harness has no
        // transaction to roll back. Asserting the row count would test the harness, not the product. What
        // is asserted instead is that nobody was ever asked to approve it.
        var employeeId = CreateEmployee("TRV110");
        var typeId = CreateTravelType();
        EnableWorkflow("TRV110APP", "Travel");

        await Should.ThrowAsync<UserFriendlyException>(() =>
            Resolve<ITravelsAppService>().CreateOrEdit(new CreateOrEditTravelDto
            {
                EmployeeId = employeeId,
                TravelTypeId = typeId,
                StartDate = TripStart,
                EndDate = TripEnd
            }));

        UsingDbContext(c => c.Set<WorkFlowApprover>().Count())
            .ShouldBe(0, "Nobody should be asked to review a trip that was never a valid request.");
    }

    // ---------------------------------------------------------------- one trip at a time

    [Fact]
    public async Task An_Overlapping_Trip_Should_Be_Refused()
    {
        // Legacy's own overlap predicate, moved from the moment of approval (where it silently skipped
        // creating the trip) to the moment of submission, where it can actually tell the employee why.
        var meId = CreateCurrentUserEmployee("TRV200");
        var typeId = CreateTravelType();
        var from = CreateDestination("Kuwait");
        var to = CreateDestination("Dubai");
        EnableWorkflow("TRV200APP", "Travel");

        PlantTrip(meId, typeId, TripStart, TripEnd, from, to, RequestStatus.Approved);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _travel.RequestTravel(TripFor(typeId, from, to,
                start: TripStart.AddDays(2), end: TripEnd.AddDays(2))));
    }

    [Fact]
    public async Task An_Overlapping_Trip_Should_Be_Refused_Even_While_The_First_Is_Still_Pending()
    {
        // THE one worth having. The overlap check runs with ApprovedOnly disabled precisely so that a trip
        // still sitting in an approver's inbox counts. With the filter on, the pending trip is invisible to
        // the check and the same fortnight can be requested twice over -- and since both would then be
        // approved, this is exactly the state the rule exists to prevent.
        var meId = CreateCurrentUserEmployee("TRV210");
        var typeId = CreateTravelType();
        var from = CreateDestination("Kuwait");
        var to = CreateDestination("Cairo");
        EnableWorkflow("TRV210APP", "Travel");

        var first = await _travel.RequestTravel(TripFor(typeId, from, to));
        first.SubmittedForApproval.ShouldBeTrue();

        UsingDbContext(c => c.Set<Travel>().IgnoreQueryFilters().Single(t => t.Id == first.TransactionId))
            .WorkflowStatus.ShouldBe(RequestStatus.Open,
                "The first trip must still be in flight for this test to be testing anything.");

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _travel.RequestTravel(TripFor(typeId, from, to,
                start: TripStart.AddDays(1), end: TripEnd.AddDays(1))));
    }

    // ---------------------------------------------------------------- hidden until approved, tree and all

    [Fact]
    public async Task A_Pending_Trip_Should_Be_Hidden_And_So_Should_Its_Legs_And_Its_Costs()
    {
        var meId = CreateCurrentUserEmployee("TRV300");
        var typeId = CreateTravelType();
        var from = CreateDestination("Kuwait");
        var to = CreateDestination("London");
        var expenseTypeId = CreateExpenseType();
        EnableWorkflow("TRV300APP", "Travel");

        var submission = await _travel.RequestTravel(
            TripFor(typeId, from, to, expenseTypeId: expenseTypeId));

        // The row exists -- this is a single-table design, not a shadow request table.
        UsingDbContext(c => c.Set<Travel>().IgnoreQueryFilters().Count(t => t.EmployeeId == meId))
            .ShouldBe(1);

        (await TripIsVisibleAsync(submission.TransactionId)).ShouldBeFalse();

        var itinerary = await VisibleItineraryAsync(submission.TransactionId);
        itinerary.legs.ShouldBe(0, "A leg of a trip nobody can see must not be visible either.");
        itinerary.costs.ShouldBe(0, "Nor a cost line two levels below it.");
    }

    [Fact]
    public async Task Approving_Should_Make_The_Whole_Tree_Visible()
    {
        // There is no copy step on approval and there is not meant to be one: the trip, its legs and its
        // costs were all written up front, and the status stamp is what brings the lot into view. That is
        // the entire replacement for legacy's field-by-field TravelDeskRequest -> Travel transcription.
        var meId = CreateCurrentUserEmployee("TRV310");
        var typeId = CreateTravelType();
        var from = CreateDestination("Kuwait");
        var to = CreateDestination("Rome");
        var expenseTypeId = CreateExpenseType();
        var approverId = EnableWorkflow("TRV310APP", "Travel");

        var submission = await _travel.RequestTravel(
            TripFor(typeId, from, to, expenseTypeId: expenseTypeId));

        await ApproveTheOutstandingRequestAsync("Travel", approverId);

        (await TripIsVisibleAsync(submission.TransactionId)).ShouldBeTrue();

        var itinerary = await VisibleItineraryAsync(submission.TransactionId);
        itinerary.legs.ShouldBe(1);
        itinerary.costs.ShouldBe(1);

        UsingDbContext(c => c.Set<Travel>().IgnoreQueryFilters().Single(t => t.Id == submission.TransactionId))
            .WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    // ---------------------------------------------------------------- the two legacy fields that got lost

    [Fact]
    public async Task The_Airline_And_The_Currency_Should_Survive_The_Request()
    {
        // Both were on the legacy request and both were simply never copied onto the rows it created --
        // plain bugs rather than design decisions. The employee named an airline the travel desk never
        // received, and every foreign-currency estimate was silently reread as company currency. Here there
        // is nothing to copy: the request IS the row, so the only way to lose them would be to not store
        // them at all.
        CreateCurrentUserEmployee("TRV400");
        var typeId = CreateTravelType();
        var from = CreateDestination("Kuwait");
        var to = CreateDestination("Paris");
        var expenseTypeId = CreateExpenseType();
        var airlineId = CreateAirline();
        var currencyId = CreateCurrency();

        var submission = await _travel.RequestTravel(TripFor(typeId, from, to,
            expenseTypeId: expenseTypeId, airlineId: airlineId, currencyId: currencyId));

        var leg = UsingDbContext(c => c.Set<TravelDetail>().IgnoreQueryFilters()
            .Single(d => d.TravelId == submission.TransactionId));

        leg.TravelAirlineId.ShouldBe(airlineId);
        leg.IsVisaRequired.ShouldBeTrue();
        leg.IsHotelRequired.ShouldBeTrue();
        leg.AnyOtherExpense.ShouldBeTrue();
        leg.VisaComments.ShouldBe("Business visa on arrival");

        UsingDbContext(c => c.Set<TravelDetailCost>().IgnoreQueryFilters()
            .Single(c2 => c2.TravelDetailId == leg.Id)).CurrencyId.ShouldBe(currencyId);

        UsingDbContext(c => c.Set<Travel>().IgnoreQueryFilters()
            .Single(t => t.Id == submission.TransactionId)).TravelPurpose.ShouldBe("Supplier audit");
    }

    // ---------------------------------------------------------------- the orphan, closed

    [Fact]
    public async Task An_Expense_Claim_Should_Be_Raisable_Against_A_Trip_The_Employee_Requested_And_Had_Approved()
    {
        // THE test. TravelFeedback has been able to route for approval since the engine shipped, and it
        // points at a Travel row -- but nothing could create that row except an administrator typing it in.
        // An employee could claim expenses for a trip that nobody in the product was able to request. This
        // walks the whole path in one go: ask for the trip, have it approved, then claim against it.
        var meId = CreateCurrentUserEmployee("TRV500");
        var typeId = CreateTravelType();
        var from = CreateDestination("Kuwait");
        var to = CreateDestination("Doha");
        var expenseTypeId = CreateExpenseType();
        var approverId = EnableWorkflow("TRV500APP", "Travel", "TravelFeedback");

        var trip = await _travel.RequestTravel(TripFor(typeId, from, to, expenseTypeId: expenseTypeId));
        await ApproveTheOutstandingRequestAsync("Travel", approverId);

        (await TripIsVisibleAsync(trip.TransactionId)).ShouldBeTrue(
            "The claim has nothing to point at unless the trip is real by now.");

        var claim = await Resolve<IMyTravelClaimAppService>().RequestTravelClaim(new RequestTravelClaimInput
        {
            TravelId = trip.TransactionId,
            ClaimDate = TripEnd.AddDays(1),
            ClaimAmount = 512.75
        });

        claim.SubmittedForApproval.ShouldBeTrue();

        await ApproveTheOutstandingRequestAsync("TravelFeedback", approverId);

        var saved = UsingDbContext(c => c.Set<TravelFeedback>().IgnoreQueryFilters()
            .Single(f => f.Id == claim.TransactionId));

        saved.TravelId.ShouldBe(trip.TransactionId);
        saved.EmployeeId.ShouldBe(meId);
        saved.WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    // ---------------------------------------------------------------- listing and withdrawing

    [Fact]
    public async Task My_Trips_Should_Come_Back_With_Their_Legs_And_Costs()
    {
        // An in-flight trip must be listed back to the employee who asked for it -- that is the main reason
        // they open the page -- and listed with its itinerary, because "Travel, 10-15 May" is not something
        // anybody can check.
        CreateCurrentUserEmployee("TRV600");
        var typeId = CreateTravelType();
        var from = CreateDestination("Kuwait");
        var to = CreateDestination("Beirut");
        var expenseTypeId = CreateExpenseType();
        EnableWorkflow("TRV600APP", "Travel");

        await _travel.RequestTravel(TripFor(typeId, from, to, expenseTypeId: expenseTypeId));

        var listed = await _travel.GetMyTravels(ListInput());

        var dto = listed.Items.Single();
        dto.WorkflowStatus.ShouldBe(RequestStatus.Open);
        dto.CanCancel.ShouldBeTrue();
        dto.TravelPurpose.ShouldBe("Supplier audit");
        dto.Legs.Single().ArrivalNameEn.ShouldBe("Beirut");
        dto.Legs.Single().Costs.Single().Amount.ShouldBe(450);
        dto.TotalTravelCost.ShouldBe(450);
    }

    [Fact]
    public async Task Withdrawing_A_Trip_Should_Take_Its_Legs_And_Costs_With_It()
    {
        // A soft-deleted header with live children underneath is rows nothing can reach and nothing will
        // ever tidy up.
        CreateCurrentUserEmployee("TRV610");
        var typeId = CreateTravelType();
        var from = CreateDestination("Kuwait");
        var to = CreateDestination("Amman");
        var expenseTypeId = CreateExpenseType();
        EnableWorkflow("TRV610APP", "Travel");

        var submission = await _travel.RequestTravel(
            TripFor(typeId, from, to, expenseTypeId: expenseTypeId));

        await _travel.CancelMyTravel(new Abp.Application.Services.Dto.EntityDto(submission.TransactionId));

        // IsDeleted is checked explicitly rather than relying on the soft-delete filter: UsingDbContext runs
        // outside a unit of work, where no ABP data filter applies, so a soft-deleted row is still returned.
        var legId = UsingDbContext(c => c.Set<TravelDetail>()
            .Where(d => d.TravelId == submission.TransactionId).Select(d => d.Id).Single());

        UsingDbContext(c => c.Set<Travel>().Count(t => t.Id == submission.TransactionId && !t.IsDeleted))
            .ShouldBe(0);
        UsingDbContext(c => c.Set<TravelDetail>().Count(d => d.TravelId == submission.TransactionId && !d.IsDeleted))
            .ShouldBe(0);
        UsingDbContext(c => c.Set<TravelDetailCost>().Count(c2 => c2.TravelDetailId == legId && !c2.IsDeleted))
            .ShouldBe(0);
    }

    [Fact]
    public async Task An_Unlinked_User_Should_Be_Refused()
    {
        // Admin accounts are deliberately never linked to an employee, so this is the normal state of the
        // signed-in user rather than an edge case. "Whose trip is this?" has no answer, and the screen must
        // say so rather than guess.
        await Should.ThrowAsync<UserFriendlyException>(() => _travel.GetMyTravels(ListInput()));

        var typeId = CreateTravelType();
        var from = CreateDestination("Kuwait");
        var to = CreateDestination("Manama");

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _travel.RequestTravel(TripFor(typeId, from, to)));
    }
}
