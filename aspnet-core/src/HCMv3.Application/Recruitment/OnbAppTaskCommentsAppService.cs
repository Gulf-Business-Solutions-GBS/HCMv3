using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Recruitment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Recruitment;

/// <summary>
/// Application service for <see cref="OnbAppTaskComments"/>, an aggregated child of
/// <see cref="OnbApplicantTasks"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.OnbAppTaskComments</c>). Legacy <c>PerformValidation</c> was
/// commented out, so this is plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_OnbAppTaskComments)]
public class OnbAppTaskCommentsAppService : HCMv3AppServiceBase, IOnbAppTaskCommentsAppService
{
    private readonly IRepository<OnbAppTaskComments> _onbAppTaskCommentsRepository;

    public OnbAppTaskCommentsAppService(IRepository<OnbAppTaskComments> onbAppTaskCommentsRepository)
    {
        _onbAppTaskCommentsRepository = onbAppTaskCommentsRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<OnbAppTaskCommentsDto>> GetAll(GetAllOnbAppTaskCommentsInput input)
    {
        input.Normalize();

        var query = _onbAppTaskCommentsRepository.GetAll()
            .WhereIf(input.OnbApplicantTasksId.HasValue, c => c.OnbApplicantTasksId == input.OnbApplicantTasksId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<OnbAppTaskCommentsDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetOnbAppTaskCommentsForEditOutput> GetOnbAppTaskCommentsForEdit(EntityDto input)
    {
        var entity = await _onbAppTaskCommentsRepository.GetAsync(input.Id);

        return new GetOnbAppTaskCommentsForEditOutput
        {
            OnbAppTaskComments = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditOnbAppTaskCommentsDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbAppTaskComments_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _onbAppTaskCommentsRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbAppTaskComments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditOnbAppTaskCommentsDto input)
    {
        var entity = MapToEntity(input, new OnbAppTaskComments());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _onbAppTaskCommentsRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbAppTaskComments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditOnbAppTaskCommentsDto input)
    {
        var entity = await _onbAppTaskCommentsRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _onbAppTaskCommentsRepository.UpdateAsync(entity);
    }

    private static OnbAppTaskComments MapToEntity(CreateOrEditOnbAppTaskCommentsDto input, OnbAppTaskComments entity)
    {
        entity.OnbApplicantTasksId = input.OnbApplicantTasksId;
        entity.Comments = input.Comments;
        entity.IsByAdmin = input.IsByAdmin;

        return entity;
    }

    private static OnbAppTaskCommentsDto ToDto(OnbAppTaskComments entity)
    {
        return new OnbAppTaskCommentsDto
        {
            Id = entity.Id,
            OnbApplicantTasksId = entity.OnbApplicantTasksId,
            Comments = entity.Comments,
            IsByAdmin = entity.IsByAdmin
        };
    }

    private static CreateOrEditOnbAppTaskCommentsDto ToCreateOrEditDto(OnbAppTaskComments entity)
    {
        return new CreateOrEditOnbAppTaskCommentsDto
        {
            Id = entity.Id,
            OnbApplicantTasksId = entity.OnbApplicantTasksId,
            Comments = entity.Comments,
            IsByAdmin = entity.IsByAdmin
        };
    }
}
