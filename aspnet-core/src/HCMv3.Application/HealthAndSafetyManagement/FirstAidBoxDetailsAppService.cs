using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.HealthAndSafetyManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Application service for <see cref="FirstAidBoxDetail"/>, the aggregated child of
/// <see cref="FirstAidBox"/> (ported from legacy XAF FirstAidBoxDetail business object). Legacy
/// <c>PerformValidation</c> required ExpiryDate, Medicine, AllocationDate and AllocatedBy -- ported
/// as-is via DataAnnotations on <see cref="CreateOrEditFirstAidBoxDetailDto"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidBoxDetails)]
public class FirstAidBoxDetailsAppService : HCMv3AppServiceBase, IFirstAidBoxDetailsAppService
{
    private readonly IRepository<FirstAidBoxDetail> _firstAidBoxDetailRepository;

    public FirstAidBoxDetailsAppService(IRepository<FirstAidBoxDetail> firstAidBoxDetailRepository)
    {
        _firstAidBoxDetailRepository = firstAidBoxDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<FirstAidBoxDetailDto>> GetAll(GetAllFirstAidBoxDetailsInput input)
    {
        input.Normalize();

        var query = _firstAidBoxDetailRepository.GetAll()
            .Include(d => d.Medicine)
            .Include(d => d.AllocatedBy)
            .WhereIf(input.FirstAidBoxId.HasValue, d => d.FirstAidBoxId == input.FirstAidBoxId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<FirstAidBoxDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetFirstAidBoxDetailForEditOutput> GetFirstAidBoxDetailForEdit(EntityDto input)
    {
        var entity = await _firstAidBoxDetailRepository.GetAsync(input.Id);

        return new GetFirstAidBoxDetailForEditOutput
        {
            FirstAidBoxDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditFirstAidBoxDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidBoxDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _firstAidBoxDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidBoxDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditFirstAidBoxDetailDto input)
    {
        var entity = MapToEntity(input, new FirstAidBoxDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _firstAidBoxDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidBoxDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditFirstAidBoxDetailDto input)
    {
        var entity = await _firstAidBoxDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _firstAidBoxDetailRepository.UpdateAsync(entity);
    }

    private static FirstAidBoxDetail MapToEntity(CreateOrEditFirstAidBoxDetailDto input, FirstAidBoxDetail entity)
    {
        entity.FirstAidBoxId = input.FirstAidBoxId;
        entity.MedicineId = input.MedicineId;
        entity.ExpiryDate = input.ExpiryDate;
        entity.AllocationDate = input.AllocationDate;
        entity.AllocatedById = input.AllocatedById;
        entity.IsExpired = input.IsExpired;
        entity.IsReplaced = input.IsReplaced;
        entity.Comments = input.Comments;

        return entity;
    }

    private static FirstAidBoxDetailDto ToDto(FirstAidBoxDetail entity)
    {
        return new FirstAidBoxDetailDto
        {
            Id = entity.Id,
            FirstAidBoxId = entity.FirstAidBoxId,
            MedicineId = entity.MedicineId,
            MedicineNameEn = entity.Medicine?.NameEn,
            ExpiryDate = entity.ExpiryDate,
            AllocationDate = entity.AllocationDate,
            AllocatedById = entity.AllocatedById,
            AllocatedByCode = entity.AllocatedBy?.Code,
            IsExpired = entity.IsExpired,
            IsReplaced = entity.IsReplaced,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditFirstAidBoxDetailDto ToCreateOrEditDto(FirstAidBoxDetail entity)
    {
        return new CreateOrEditFirstAidBoxDetailDto
        {
            Id = entity.Id,
            FirstAidBoxId = entity.FirstAidBoxId,
            MedicineId = entity.MedicineId,
            ExpiryDate = entity.ExpiryDate,
            AllocationDate = entity.AllocationDate,
            AllocatedById = entity.AllocatedById,
            IsExpired = entity.IsExpired,
            IsReplaced = entity.IsReplaced,
            Comments = entity.Comments
        };
    }
}
