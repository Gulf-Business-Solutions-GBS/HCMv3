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
/// ported here: auto Code, duplicate Code check, EndDate must not be earlier than StartDate. See
/// <see cref="Travel"/> class remarks for the Applicant-drop and TotalTravelCost-rollup simplifications.
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

    public TravelsAppService(
        IRepository<Travel> repository,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _repository = repository;
        _transactionSubmitter = transactionSubmitter;
    }

    [HttpPost]
    public async Task<PagedResultDto<TravelDto>> GetAll(GetAllTravelsInput input)
    {
        input.Normalize();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var query = _repository.GetAll()
                .Include(t => t.Employee)
                .Include(t => t.TravelType)
                .WhereIf(input.EmployeeId.HasValue, t => t.EmployeeId == input.EmployeeId)
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
                Travel = ToCreateOrEditDto(entity)
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
        entity.TotalTravelCost = entity.TravelDetails?.Sum(d => d.TotalAmount) ?? 0;

        await ValidateAsync(entity);
        await AssignCodeIfEmptyAsync(entity);

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
            entity = MapToEntity(input, entity);

            await ValidateAsync(entity);
            await AssignCodeIfEmptyAsync(entity);

            await _repository.UpdateAsync(entity);
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
    /// Legacy <c>PerformValidation</c> parity: EndDate must not be earlier than StartDate.
    /// </summary>
    private Task ValidateAsync(Travel entity)
    {
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
        entity.EmployeeId = input.EmployeeId;
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
                TotalAmount = costs.Sum(c => c.Amount),
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
            TravelTypeId = entity.TravelTypeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            TravelPurpose = entity.TravelPurpose,
            Comments = entity.Comments
        };
    }
}
