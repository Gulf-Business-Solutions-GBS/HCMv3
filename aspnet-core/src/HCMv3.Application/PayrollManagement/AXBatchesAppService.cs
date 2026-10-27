using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the AX (accounting export) journal batch header (ported from legacy XAF AXBatch
/// business object). Trivial entity — no Code/auto-gen, only the three required strings are validated (via
/// DTO data annotations); no extra business-rule validation was in the legacy source beyond blank checks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_AXBatches)]
public class AXBatchesAppService : HCMv3AppServiceBase, IAXBatchesAppService
{
    private readonly IRepository<AXBatch> _axBatchRepository;

    public AXBatchesAppService(IRepository<AXBatch> axBatchRepository)
    {
        _axBatchRepository = axBatchRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AXBatchDto>> GetAll(GetAllAXBatchesInput input)
    {
        input.Normalize();

        var query = _axBatchRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), a =>
                a.JournalBatchNumber.Contains(input.Filter) ||
                a.Name.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AXBatchDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetAXBatchForEditOutput> GetAXBatchForEdit(EntityDto input)
    {
        var entity = await _axBatchRepository.GetAsync(input.Id);

        return new GetAXBatchForEditOutput
        {
            AXBatch = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAXBatchDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AXBatches_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _axBatchRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AXBatches_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAXBatchDto input)
    {
        var entity = MapToEntity(input, new AXBatch());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _axBatchRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AXBatches_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAXBatchDto input)
    {
        var entity = await _axBatchRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _axBatchRepository.UpdateAsync(entity);
    }

    private static AXBatch MapToEntity(CreateOrEditAXBatchDto input, AXBatch entity)
    {
        entity.JournalBatchNumber = input.JournalBatchNumber;
        entity.Name = input.Name;
        entity.Description = input.Description;

        return entity;
    }

    private static AXBatchDto ToDto(AXBatch entity)
    {
        return new AXBatchDto
        {
            Id = entity.Id,
            JournalBatchNumber = entity.JournalBatchNumber,
            Name = entity.Name,
            Description = entity.Description
        };
    }

    private static CreateOrEditAXBatchDto ToCreateOrEditDto(AXBatch entity)
    {
        return new CreateOrEditAXBatchDto
        {
            Id = entity.Id,
            JournalBatchNumber = entity.JournalBatchNumber,
            Name = entity.Name,
            Description = entity.Description
        };
    }
}
