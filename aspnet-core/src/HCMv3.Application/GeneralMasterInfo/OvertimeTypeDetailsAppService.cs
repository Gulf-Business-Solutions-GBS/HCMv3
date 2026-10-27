using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the OvertimeTypeDetail detail record (ported from legacy XAF OvertimeTypeDetail).
/// Pure detail record (no Code/NameEn/NameAr): plain CRUD without auto-code or duplicate validation.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_OvertimeTypeDetails)]
public class OvertimeTypeDetailsAppService : HCMv3AppServiceBase, IOvertimeTypeDetailsAppService
{
    private readonly IRepository<OvertimeTypeDetail> _repository;

    public OvertimeTypeDetailsAppService(IRepository<OvertimeTypeDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<OvertimeTypeDetailDto>> GetAll(GetAllOvertimeTypeDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.OvertimeType);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<OvertimeTypeDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetOvertimeTypeDetailForEditOutput> GetOvertimeTypeDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetOvertimeTypeDetailForEditOutput
        {
            OvertimeTypeDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditOvertimeTypeDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_OvertimeTypeDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditOvertimeTypeDetailDto input)
    {
        var entity = new OvertimeTypeDetail
        {
            OvertimeTypeId = input.OvertimeTypeId,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            OvertimeRateType = input.OvertimeRateType,
            Rate = input.Rate,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_OvertimeTypeDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditOvertimeTypeDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.OvertimeTypeId = input.OvertimeTypeId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.OvertimeRateType = input.OvertimeRateType;
        entity.Rate = input.Rate;

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_OvertimeTypeDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static OvertimeTypeDetailDto ToDto(OvertimeTypeDetail entity)
    {
        return new OvertimeTypeDetailDto
        {
            Id = entity.Id,
            OvertimeTypeId = entity.OvertimeTypeId,
            OvertimeTypeName = entity.OvertimeType?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            OvertimeRateType = entity.OvertimeRateType,
            Rate = entity.Rate
        };
    }

    private static CreateOrEditOvertimeTypeDetailDto ToCreateOrEditDto(OvertimeTypeDetail entity)
    {
        return new CreateOrEditOvertimeTypeDetailDto
        {
            Id = entity.Id,
            OvertimeTypeId = entity.OvertimeTypeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            OvertimeRateType = entity.OvertimeRateType,
            Rate = entity.Rate
        };
    }
}
