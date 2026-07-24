using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.TravelManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TravelManagement;

/// <summary>
/// Application service for <see cref="TravelFeedback"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TravelManagement.TravelFeedback</c>). See <see cref="TravelFeedback"/>
/// class remarks for the Applicant-drop and DocAttachment-drop simplifications. Legacy filtered the
/// selectable Travel list by whichever of Employee/Applicant was set on the header; since Applicant is
/// dropped, callers filter by <see cref="TravelFeedback.EmployeeId"/> only (via <c>ITravelsAppService.GetAll</c>
/// with <c>EmployeeId</c>, not duplicated here).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelFeedbacks)]
public class TravelFeedbacksAppService : HCMv3AppServiceBase, ITravelFeedbacksAppService
{
    private readonly IRepository<TravelFeedback> _repository;

    public TravelFeedbacksAppService(IRepository<TravelFeedback> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TravelFeedbackDto>> GetAll(GetAllTravelFeedbacksInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(f => f.Employee)
            .Include(f => f.Travel)
            .WhereIf(input.EmployeeId.HasValue, f => f.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TravelFeedbackDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetTravelFeedbackForEditOutput> GetTravelFeedbackForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTravelFeedbackForEditOutput
        {
            TravelFeedback = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTravelFeedbackDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelFeedbacks_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTravelFeedbackDto input)
    {
        var entity = MapToEntity(input, new TravelFeedback());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelFeedbacks_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTravelFeedbackDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelFeedbacks_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static TravelFeedback MapToEntity(CreateOrEditTravelFeedbackDto input, TravelFeedback entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.TravelId = input.TravelId;
        entity.ClaimDate = input.ClaimDate;
        entity.ClaimAmount = input.ClaimAmount;
        entity.Comments = input.Comments;

        return entity;
    }

    private static TravelFeedbackDto ToDto(TravelFeedback entity)
    {
        return new TravelFeedbackDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee != null
                ? $"{entity.Employee.FirstNameEn} {entity.Employee.LastNameEn}"
                : null,
            TravelId = entity.TravelId,
            TravelCode = entity.Travel?.Code,
            ClaimDate = entity.ClaimDate,
            ClaimAmount = entity.ClaimAmount,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditTravelFeedbackDto ToCreateOrEditDto(TravelFeedback entity)
    {
        return new CreateOrEditTravelFeedbackDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            TravelId = entity.TravelId,
            ClaimDate = entity.ClaimDate,
            ClaimAmount = entity.ClaimAmount,
            Comments = entity.Comments
        };
    }
}
