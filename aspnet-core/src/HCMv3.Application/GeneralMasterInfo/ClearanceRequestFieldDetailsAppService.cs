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
/// Application service for the ClearanceRequestFieldDetail record (ported from legacy XAF
/// ClearanceRequestFieldDetail): which department owns a given clearance checklist line. No
/// Code/NameEn/NameAr, so plain CRUD without auto-code or duplicate validation.
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
            .Include(d => d.ClearanceRequestField)
            .Include(d => d.WorkflowRole)
            .WhereIf(input.ClearanceRequestFieldId.HasValue, d => d.ClearanceRequestFieldId == input.ClearanceRequestFieldId);

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
            WorkflowRoleId = input.WorkflowRoleId,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFieldDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditClearanceRequestFieldDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.ClearanceRequestFieldId = input.ClearanceRequestFieldId;
        entity.WorkflowRoleId = input.WorkflowRoleId;

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
            ClearanceRequestFieldName = entity.ClearanceRequestField?.NameEn,
            WorkflowRoleId = entity.WorkflowRoleId,
            WorkflowRoleName = entity.WorkflowRole?.NameEn
        };
    }

    private static CreateOrEditClearanceRequestFieldDetailDto ToCreateOrEditDto(ClearanceRequestFieldDetail entity)
    {
        return new CreateOrEditClearanceRequestFieldDetailDto
        {
            Id = entity.Id,
            ClearanceRequestFieldId = entity.ClearanceRequestFieldId,
            WorkflowRoleId = entity.WorkflowRoleId
        };
    }
}
