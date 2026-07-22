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
/// Application service for the DisciplinaryTypeDetail detail record (ported from legacy XAF
/// DisciplinaryTypeDetail). Pure detail record (no Code/NameEn/NameAr): plain CRUD without
/// auto-code or duplicate validation.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryTypeDetails)]
public class DisciplinaryTypeDetailsAppService : HCMv3AppServiceBase, IDisciplinaryTypeDetailsAppService
{
    private readonly IRepository<DisciplinaryTypeDetail> _repository;

    public DisciplinaryTypeDetailsAppService(IRepository<DisciplinaryTypeDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<DisciplinaryTypeDetailDto>> GetAll(GetAllDisciplinaryTypeDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.DisciplinaryType)
            .Include(d => d.DisciplinaryDeductionType);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<DisciplinaryTypeDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetDisciplinaryTypeDetailForEditOutput> GetDisciplinaryTypeDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetDisciplinaryTypeDetailForEditOutput
        {
            DisciplinaryTypeDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditDisciplinaryTypeDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryTypeDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditDisciplinaryTypeDetailDto input)
    {
        var entity = new DisciplinaryTypeDetail
        {
            DisciplinaryTypeId = input.DisciplinaryTypeId,
            DisciplinaryCount = input.DisciplinaryCount,
            DisciplinaryDeductionTypeId = input.DisciplinaryDeductionTypeId,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryTypeDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditDisciplinaryTypeDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.DisciplinaryTypeId = input.DisciplinaryTypeId;
        entity.DisciplinaryCount = input.DisciplinaryCount;
        entity.DisciplinaryDeductionTypeId = input.DisciplinaryDeductionTypeId;

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryTypeDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static DisciplinaryTypeDetailDto ToDto(DisciplinaryTypeDetail entity)
    {
        return new DisciplinaryTypeDetailDto
        {
            Id = entity.Id,
            DisciplinaryTypeId = entity.DisciplinaryTypeId,
            DisciplinaryTypeName = entity.DisciplinaryType?.NameEn,
            DisciplinaryCount = entity.DisciplinaryCount,
            DisciplinaryDeductionTypeId = entity.DisciplinaryDeductionTypeId,
            DisciplinaryDeductionTypeName = entity.DisciplinaryDeductionType?.NameEn
        };
    }

    private static CreateOrEditDisciplinaryTypeDetailDto ToCreateOrEditDto(DisciplinaryTypeDetail entity)
    {
        return new CreateOrEditDisciplinaryTypeDetailDto
        {
            Id = entity.Id,
            DisciplinaryTypeId = entity.DisciplinaryTypeId,
            DisciplinaryCount = entity.DisciplinaryCount,
            DisciplinaryDeductionTypeId = entity.DisciplinaryDeductionTypeId
        };
    }
}
