using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.TravelManagement;
using HCMv3.TravelManagement.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyTravelAppService"/>
[AbpAuthorize(AppPermissions.Ess_MyTravel)]
public class MyTravelAppService : MyTransactionAppServiceBase<Travel>, IMyTravelAppService
{
    private readonly IRepository<TravelType> _travelTypeRepository;
    private readonly IRepository<TravelDetail> _travelDetailRepository;
    private readonly IRepository<TravelDetailCost> _travelDetailCostRepository;
    private readonly IRepository<TravelDestination> _destinationRepository;
    private readonly IRepository<ExpenseType> _expenseTypeRepository;

    public MyTravelAppService(
        IRepository<Travel> repository,
        IRepository<WorkflowRequest> requestRepository,
        IRepository<TravelType> travelTypeRepository,
        IRepository<TravelDetail> travelDetailRepository,
        IRepository<TravelDetailCost> travelDetailCostRepository,
        IRepository<TravelDestination> destinationRepository,
        IRepository<ExpenseType> expenseTypeRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
        _travelTypeRepository = travelTypeRepository;
        _travelDetailRepository = travelDetailRepository;
        _travelDetailCostRepository = travelDetailCostRepository;
        _destinationRepository = destinationRepository;
        _expenseTypeRepository = expenseTypeRepository;
    }

    protected override string RequestType => TravelWorkflowTransactionHandler.TravelRequestType;

    protected override string RequestNumberPrefix => "TRV";

    [HttpPost]
    public async Task<PagedResultDto<MyTravelDto>> GetMyTravels(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var today = DateTime.Today;

            // "Not history" means the trip has not happened yet or is still being decided. A trip that ended
            // last year is archive; a request still in an approver's inbox never is, whatever its dates say.
            var query = MyRowsQuery(employeeId)
                .WhereIf(!input.IncludeHistory,
                    t => t.EndDate >= today
                         || t.WorkflowStatus == RequestStatus.Draft
                         || t.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            var travelIds = items.Select(i => i.Id).ToList();

            var typeIds = items.Select(i => i.TravelTypeId).Distinct().ToList();
            var types = await _travelTypeRepository.GetAll()
                .Where(t => typeIds.Contains(t.Id))
                .ToListAsync();

            // Legs and costs are fetched through their parent ids -- the only route to them. That is the
            // same reason they need no WorkflowStatus of their own: hide the header and they are gone.
            var legs = await _travelDetailRepository.GetAll()
                .Where(d => travelIds.Contains(d.TravelId))
                .OrderBy(d => d.DepartureDate)
                .ToListAsync();

            var legIds = legs.Select(l => l.Id).ToList();

            var costs = await _travelDetailCostRepository.GetAll()
                .Where(c => legIds.Contains(c.TravelDetailId))
                .ToListAsync();

            var destinationIds = legs.Select(l => l.DepartureId)
                .Concat(legs.Select(l => l.ArrivalId))
                .Distinct()
                .ToList();

            var destinations = await _destinationRepository.GetAll()
                .Where(d => destinationIds.Contains(d.Id))
                .ToListAsync();

            var expenseTypeIds = costs.Select(c => c.ExpenseTypeId).Distinct().ToList();
            var expenseTypes = await _expenseTypeRepository.GetAll()
                .Where(e => expenseTypeIds.Contains(e.Id))
                .ToListAsync();

            var dtos = items.Select(travel =>
            {
                var travelLegs = legs.Where(l => l.TravelId == travel.Id).ToList();

                return new MyTravelDto
                {
                    Id = travel.Id,
                    Code = travel.Code,
                    TravelTypeId = travel.TravelTypeId,
                    TravelTypeNameEn = types.FirstOrDefault(t => t.Id == travel.TravelTypeId)?.NameEn,
                    TravelTypeNameAr = types.FirstOrDefault(t => t.Id == travel.TravelTypeId)?.NameAr,
                    StartDate = travel.StartDate,
                    EndDate = travel.EndDate,
                    TotalTravelCost = travelLegs.Sum(l => l.TotalAmount),
                    TravelPurpose = travel.TravelPurpose,
                    Comments = travel.Comments,
                    WorkflowStatus = travel.WorkflowStatus,
                    WorkflowRequestId = travel.WorkflowRequestId,
                    CanCancel = CanCancel(travel),
                    Legs = travelLegs.Select(leg => new MyTravelLegDto
                    {
                        Id = leg.Id,
                        DepartureId = leg.DepartureId,
                        DepartureNameEn = destinations.FirstOrDefault(d => d.Id == leg.DepartureId)?.NameEn,
                        DepartureNameAr = destinations.FirstOrDefault(d => d.Id == leg.DepartureId)?.NameAr,
                        ArrivalId = leg.ArrivalId,
                        ArrivalNameEn = destinations.FirstOrDefault(d => d.Id == leg.ArrivalId)?.NameEn,
                        ArrivalNameAr = destinations.FirstOrDefault(d => d.Id == leg.ArrivalId)?.NameAr,
                        DepartureDate = leg.DepartureDate,
                        DepartureTime = leg.DepartureTime,
                        ArrivalDate = leg.ArrivalDate,
                        ArrivalTime = leg.ArrivalTime,
                        HotelId = leg.HotelId,
                        TravelAirlineId = leg.TravelAirlineId,
                        NightStay = leg.NightStay,
                        TotalNights = leg.TotalNights,
                        TotalAmount = leg.TotalAmount,
                        IsVisaRequired = leg.IsVisaRequired,
                        VisaComments = leg.VisaComments,
                        IsHotelRequired = leg.IsHotelRequired,
                        HotelComments = leg.HotelComments,
                        AnyOtherExpense = leg.AnyOtherExpense,
                        OtherComments = leg.OtherComments,
                        Comments = leg.Comments,
                        Costs = costs.Where(c => c.TravelDetailId == leg.Id).Select(c => new MyTravelCostDto
                        {
                            Id = c.Id,
                            ExpenseTypeId = c.ExpenseTypeId,
                            ExpenseTypeNameEn = expenseTypes.FirstOrDefault(e => e.Id == c.ExpenseTypeId)?.NameEn,
                            CurrencyId = c.CurrencyId,
                            Amount = c.Amount,
                            Comments = c.Comments
                        }).ToList()
                    }).ToList()
                };
            }).ToList();

            return new PagedResultDto<MyTravelDto>(totalCount, dtos);
        }
    }

    public async Task<List<TravelTypeLookupDto>> GetTravelTypes()
    {
        var types = await _travelTypeRepository.GetAll()
            .OrderBy(t => t.NameEn)
            .ToListAsync();

        return types.Select(t => new TravelTypeLookupDto
        {
            Id = t.Id,
            Code = t.Code,
            NameEn = t.NameEn,
            NameAr = t.NameAr
        }).ToList();
    }

    public Task<MyRequestSubmissionDto> RequestTravel(RequestTravelInput input)
    {
        if (input.Legs == null || input.Legs.Count == 0)
        {
            // The handler refuses this too, on the row, so it holds for the travel desk as well. This is the
            // early, friendlier failure before a row is created and rolled back.
            throw new UserFriendlyException(L("ATravelRequestMustHaveAtLeastOneLeg"));
        }

        if (input.EndDate.Date < input.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeBeforeStartDate"));
        }

        var tenantId = AbpSession.TenantId ?? 0;

        return CreateAndSubmitAsync(
            employeeId =>
            {
                // The legs travel with the header, so EF inserts the whole graph in one go and the children
                // exist by the time the handler validates the submission. Building a header now and adding
                // legs afterwards would give the handler a trip with nowhere to go and it would refuse it.
                var legs = BuildLegs(input, tenantId);

                return new Travel
                {
                    TenantId = tenantId,
                    EmployeeId = employeeId,
                    TravelTypeId = input.TravelTypeId,
                    StartDate = input.StartDate.Date,
                    EndDate = input.EndDate.Date,
                    TravelPurpose = input.TravelPurpose,
                    Comments = input.Comments,

                    // Code is left null on purpose. It is the travel desk's own reference and legacy never
                    // assigned one at request time either; while the trip is in flight the workflow request
                    // number is what identifies it.
                    Code = null,

                    // Stored but never recomputed by a cascade (see the entity remarks), so it is set once
                    // here from the lines it is the total of.
                    TotalTravelCost = legs.Sum(l => l.TotalAmount),
                    TravelDetails = legs
                };
            },
            // Routed by travel type, so an overseas trip can need a longer chain than a local one; null
            // would fall through to the type's default chain.
            input.TravelTypeId);
    }

    public async Task CancelMyTravel(EntityDto input)
    {
        // Ownership, status and the header row itself are all handled by the base -- and it throws before
        // deleting anything if the request is not the caller's to withdraw, which is why the children are
        // cleaned up after it rather than before.
        await CancelAsync(input.Id);

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var legIds = await _travelDetailRepository.GetAll()
                .Where(d => d.TravelId == input.Id)
                .Select(d => d.Id)
                .ToListAsync();

            // Withdrawn means withdrawn. Soft-deleting the header while leaving its legs behind would leave
            // rows that nothing can reach and nothing will ever clean up.
            await _travelDetailCostRepository.DeleteAsync(c => legIds.Contains(c.TravelDetailId));
            await _travelDetailRepository.DeleteAsync(d => d.TravelId == input.Id);
        }
    }

    private static List<TravelDetail> BuildLegs(RequestTravelInput input, int tenantId)
    {
        return input.Legs.Select(leg =>
        {
            var costs = (leg.Costs ?? new List<RequestTravelCostInput>()).Select(cost => new TravelDetailCost
            {
                TenantId = tenantId,
                ExpenseTypeId = cost.ExpenseTypeId,

                // Copied, unlike legacy, which dropped it -- see TravelDetailCost.CurrencyId.
                CurrencyId = cost.CurrencyId,
                Amount = cost.Amount,
                Comments = cost.Comments
            }).ToList();

            return new TravelDetail
            {
                TenantId = tenantId,
                DepartureId = leg.DepartureId,
                ArrivalId = leg.ArrivalId,
                DepartureDate = leg.DepartureDate,
                DepartureTime = leg.DepartureTime,
                ArrivalDate = leg.ArrivalDate,
                ArrivalTime = leg.ArrivalTime,
                HotelId = leg.HotelId,

                // Copied, unlike legacy, which dropped it -- see RequestTravelLegInput.TravelAirlineId.
                TravelAirlineId = leg.TravelAirlineId,
                NightStay = leg.NightStay,
                TotalNights = leg.TotalNights,
                IsVisaRequired = leg.IsVisaRequired,
                VisaComments = leg.VisaComments,
                IsHotelRequired = leg.IsHotelRequired,
                HotelComments = leg.HotelComments,
                AnyOtherExpense = leg.AnyOtherExpense,
                OtherComments = leg.OtherComments,
                Comments = leg.Comments,

                // The rollup is stored but never recomputed by a cascade (see the entity remarks), so it is
                // set once here from the lines it is the total of.
                TotalAmount = costs.Sum(c => c.Amount),
                TravelDetailCosts = costs
            };
        }).ToList();
    }
}
