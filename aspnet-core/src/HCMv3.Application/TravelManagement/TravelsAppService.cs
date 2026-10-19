using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Recruitment;
using HCMv3.TravelManagement.Dto;
using HCMv3.TravelManagement.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TravelManagement;

/// <summary>
/// Application service for <see cref="Travel"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TravelManagement.Travel</c>). Legacy <c>PerformValidation</c> parity
/// ported here: auto Code, duplicate Code check, EndDate must not be earlier than StartDate, and the trip
/// belongs to an employee or a job applicant -- exactly one (see <see cref="TravelParty"/>). TotalTravelCost is kept by
/// <see cref="ITravelCostRollupService"/> on create and on every update (legacy <c>Travel.UpdateTotalAmount</c>).
///
/// <para>
/// APPROVAL LIVES IN THE ENGINE. A trip booked here is raised through
/// <see cref="IAdminTransactionSubmitter"/>, exactly as a self-service request is raised through
/// <c>MyTravelAppService</c> -- a travel desk books on somebody's behalf, and that booking is the same
/// decision, so it takes the same route and is judged by the same
/// <see cref="TravelWorkflowTransactionHandler"/>. Nothing here writes approval state, and with workflow
/// switched off for the type the submitter simply inserts, which is the pre-workflow behaviour.
/// </para>
/// <para>
/// AN APPLICANT TRIP IS RECORDED, NOT ROUTED. Routing is decided by the employee a request is about -- their
/// manager, their organisation unit, the routing rules matched against them -- and an applicant has none of
/// those. Legacy's Travel screen was direct entry for everybody, so recording an applicant's trip straight
/// away is legacy behaviour; it is the employee path that HCMv3 added approval to.
/// </para>
/// <para>
/// The <c>ApprovedOnly</c> filter is disabled throughout, for the same reason as the manpower plan register:
/// this screen OWNS the transaction, so a trip that vanished from it the moment it was submitted would
/// leave the person who booked it with no way to see what became of it.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TravelManagement_Travels)]
public class TravelsAppService : HCMv3AppServiceBase, ITravelsAppService
{
    private readonly IRepository<Travel> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;
    private readonly ITravelCostRollupService _costRollupService;
    private readonly IRepository<Applicant> _applicantRepository;

    public TravelsAppService(
        IRepository<Travel> repository,
        IAdminTransactionSubmitter transactionSubmitter,
        ITravelCostRollupService costRollupService,
        IRepository<Applicant> applicantRepository)
    {
        _repository = repository;
        _transactionSubmitter = transactionSubmitter;
        _costRollupService = costRollupService;
        _applicantRepository = applicantRepository;
    }

    /// <summary>
    /// The applicants a trip or a claim can be booked for. Served here, under the travel permission, so the
    /// travel desk does not also need the Recruitment screens just to fill in one drop-down.
    /// </summary>
    public async Task<ListResultDto<TravelApplicantLookupDto>> GetApplicantsForLookup()
    {
        var applicants = await _applicantRepository.GetAll()
            .OrderBy(a => a.Code)
            .Select(a => new TravelApplicantLookupDto
            {
                Id = a.Id,
                Code = a.Code,
                FullNameEn = (a.FirstNameEn ?? "") + " " + (a.LastNameEn ?? ""),
                FullNameAr = (a.FirstNameAr ?? "") + " " + (a.LastNameAr ?? "")
            })
            .ToListAsync();

        return new ListResultDto<TravelApplicantLookupDto>(applicants);
    }

    [HttpPost]
    public async Task<PagedResultDto<TravelDto>> GetAll(GetAllTravelsInput input)
    {
        input.Normalize();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var query = _repository.GetAll()
                .Include(t => t.Employee)
                .Include(t => t.Applicant)
                .Include(t => t.TravelType)
                .WhereIf(input.EmployeeId.HasValue, t => t.EmployeeId == input.EmployeeId)
                .WhereIf(input.ApplicantId.HasValue, t => t.ApplicantId == input.ApplicantId)
                .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t => t.Code.Contains(input.Filter));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(input.Sorting)
                .PageBy(input)
                .ToListAsync();

            return new PagedResultDto<TravelDto>(
                totalCount,
                items.Select(ToDto).ToList());
        }
    }

    public async Task<GetTravelForEditOutput> GetTravelForEdit(EntityDto input)
    {
        // Same reason as GetAll: an in-flight trip is still the owning screen's business.
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var entity = await _repository.GetAsync(input.Id);

            return new GetTravelForEditOutput
            {
                Travel = ToCreateOrEditDto(entity),
                TotalTravelCost = entity.TotalTravelCost
            };
        }
    }

    public async Task CreateOrEdit(CreateOrEditTravelDto input)
    {
        if (input.Id.HasValue)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_Travels_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTravelDto input)
    {
        var entity = MapToEntity(input, new Travel());
        entity.TenantId = AbpSession.TenantId ?? 0;

        // Legs travel with the header so they exist by the time the handler validates the submission -- EF
        // inserts the graph, and the submitter saves before it submits. Same shape as the manpower plan.
        entity.TravelDetails = BuildDetails(input, entity.TenantId);
        // Legacy Travel.UpdateTotalAmount (Travel.cs lines 194-211) and TravelDetailCost.UpdateTotalAmount
        // (TravelDetailCost.cs lines 117-118): leg = rounded sum of its costs, trip = rounded sum of its legs.
        await _costRollupService.ApplyToNewTravelAsync(entity);

        await ValidateAsync(entity);
        await AssignCodeIfEmptyAsync(entity);

        if (!entity.EmployeeId.HasValue)
        {
            // An applicant's trip: recorded, not routed -- see class remarks. Null status means "in effect",
            // the same state the submitter leaves a row in when workflow is off for the type.
            entity.WorkflowStatus = null;
            await _repository.InsertAsync(entity);
            return;
        }

        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            TravelWorkflowTransactionHandler.TravelRequestType,
            "TRV",
            entity.TravelTypeId);
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_Travels_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTravelDto input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var entity = await _repository.GetAsync(input.Id.Value);
            var raisedForApproval = entity.WorkflowRequestId.HasValue;

            entity = MapToEntity(input, entity);

            // The approval request was raised against the employee and is routed by them. Moving the trip to
            // an applicant afterwards would leave an approver deciding a request about somebody who is no
            // longer on it.
            if (raisedForApproval && !entity.EmployeeId.HasValue)
            {
                throw new UserFriendlyException(L("ATripSentForApprovalCannotBeMovedToAnApplicant"));
            }

            await ValidateAsync(entity);
            await AssignCodeIfEmptyAsync(entity);

            await _repository.UpdateAsync(entity);

            // Legacy Travel.OnSaving (Travel.cs lines 153-156): every save of the header re-sums its legs.
            await _costRollupService.RecalculateTravelTotalAsync(entity.Id);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_Travels_Delete)]
    public async Task Delete(EntityDto input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await _repository.DeleteAsync(input.Id);
        }
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: an employee or an applicant (exactly one -- see
    /// <see cref="TravelParty"/>), and EndDate must not be earlier than StartDate.
    /// </summary>
    private Task ValidateAsync(Travel entity)
    {
        if (!TravelParty.HasExactlyOne(entity.EmployeeId, entity.ApplicantId))
        {
            throw new UserFriendlyException(L("SelectEitherAnEmployeeOrAnApplicant"));
        }

        if (entity.EndDate.Date < entity.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Travel entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            var duplicateCode = await _repository.GetAll()
                .AnyAsync(t => t.Id != entity.Id && t.Code == entity.Code);
            if (duplicateCode)
            {
                throw new UserFriendlyException(L("DuplicateCodeFound"));
            }

            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(t => t.Code != null && t.Code != "")
            .Select(t => t.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    private static Travel MapToEntity(CreateOrEditTravelDto input, Travel entity)
    {
        entity.Code = input.Code;
        entity.EmployeeId = TravelParty.Normalize(input.EmployeeId);
        entity.ApplicantId = TravelParty.Normalize(input.ApplicantId);
        entity.TravelTypeId = input.TravelTypeId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.TravelPurpose = input.TravelPurpose;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// The legs and their cost lines, built on create only. TravelId and TravelDetailId are left unset on
    /// purpose -- EF fills them in from the graph, and a client-supplied parent id here would be a way to
    /// attach a leg to somebody else's trip.
    /// </summary>
    private static List<TravelDetail> BuildDetails(CreateOrEditTravelDto input, int tenantId)
    {
        if (input.TravelDetails == null)
        {
            return new List<TravelDetail>();
        }

        return input.TravelDetails.Select(leg =>
        {
            var costs = (leg.TravelDetailCosts ?? new List<CreateOrEditTravelDetailCostDto>())
                .Select(cost => new TravelDetailCost
                {
                    TenantId = tenantId,
                    ExpenseTypeId = cost.ExpenseTypeId,
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
                TravelAirlineId = leg.TravelAirlineId,
                NightStay = leg.NightStay,
                TotalNights = leg.TotalNights,
                PerDiemPayPercentage = leg.PerDiemPayPercentage,
                IsVisaRequired = leg.IsVisaRequired,
                VisaComments = leg.VisaComments,
                IsHotelRequired = leg.IsHotelRequired,
                HotelComments = leg.HotelComments,
                AnyOtherExpense = leg.AnyOtherExpense,
                OtherComments = leg.OtherComments,
                Comments = leg.Comments,
                // Set by ITravelCostRollupService.ApplyToNewTravelAsync once the graph is built.
                TravelDetailCosts = costs
            };
        }).ToList();
    }

    private static TravelDto ToDto(Travel entity)
    {
        return new TravelDto
        {
            Id = entity.Id,
            Code = entity.Code,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee != null
                ? $"{entity.Employee.FirstNameEn} {entity.Employee.LastNameEn}"
                : null,
            ApplicantId = entity.ApplicantId,
            ApplicantCode = entity.Applicant?.Code,
            ApplicantFullNameEn = entity.Applicant != null
                ? $"{entity.Applicant.FirstNameEn} {entity.Applicant.LastNameEn}"
                : null,
            TravelTypeId = entity.TravelTypeId,
            TravelTypeNameEn = entity.TravelType?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            TotalTravelCost = entity.TotalTravelCost,
            TravelPurpose = entity.TravelPurpose,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId
        };
    }

    private static CreateOrEditTravelDto ToCreateOrEditDto(Travel entity)
    {
        return new CreateOrEditTravelDto
        {
            Id = entity.Id,
            Code = entity.Code,
            EmployeeId = entity.EmployeeId,
            ApplicantId = entity.ApplicantId,
            TravelTypeId = entity.TravelTypeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            TravelPurpose = entity.TravelPurpose,
            Comments = entity.Comments
        };
    }
}
