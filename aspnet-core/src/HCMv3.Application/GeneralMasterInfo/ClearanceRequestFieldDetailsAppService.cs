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
/// Application service for the ClearanceRequestFieldDetail detail record (ported from legacy XAF
/// ClearanceRequestFieldDetail). Pure detail record (no Code/NameEn/NameAr): plain CRUD without
/// auto-code or duplicate validation. The legacy <c>WorkflowRole</c> FK is not ported.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFieldDetails)]
public class ClearanceRequestFieldDetailsAppService : HCMv3AppServiceBase, IClearanceRequestFieldDetailsAppService
{
    private readonly IRepository<ClearanceRequestFieldDetail> _repository;

    public ClearanceRequestFieldDetailsAppService(IRepository<ClearanceRequestFieldDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ClearanceRequestFieldDetailDto>> GetAll(GetAllClearanceRequestFieldDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.ClearanceRequestField);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ClearanceRequestFieldDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetClearanceRequestFieldDetailForEditOutput> GetClearanceRequestFieldDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetClearanceRequestFieldDetailForEditOutput
        {
            ClearanceRequestFieldDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditClearanceRequestFieldDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFieldDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditClearanceRequestFieldDetailDto input)
    {
        var entity = new ClearanceRequestFieldDetail
        {
            ClearanceRequestFieldId = input.ClearanceRequestFieldId,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFieldDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditClearanceRequestFieldDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.ClearanceRequestFieldId = input.ClearanceRequestFieldId;

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFieldDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static ClearanceRequestFieldDetailDto ToDto(ClearanceRequestFieldDetail entity)
    {
        return new ClearanceRequestFieldDetailDto
        {
            Id = entity.Id,
            ClearanceRequestFieldId = entity.ClearanceRequestFieldId,
            ClearanceRequestFieldName = entity.ClearanceRequestField?.NameEn
        };
    }

    private static CreateOrEditClearanceRequestFieldDetailDto ToCreateOrEditDto(ClearanceRequestFieldDetail entity)
    {
        return new CreateOrEditClearanceRequestFieldDetailDto
        {
            Id = entity.Id,
            ClearanceRequestFieldId = entity.ClearanceRequestFieldId
        };
    }
}
