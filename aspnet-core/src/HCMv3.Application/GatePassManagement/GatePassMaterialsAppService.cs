using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.GatePassManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GatePassManagement;

/// <summary>
/// Application service for <see cref="GatePassMaterial"/>, the aggregated child of <see cref="GatePass"/>
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GatePassManagement.GatePassMaterial</c>). Legacy
/// <c>PerformValidation</c> parity: ItemCode/ItemName/ExpectedReturnDate required, Quantity must not be zero.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassMaterials)]
public class GatePassMaterialsAppService : HCMv3AppServiceBase, IGatePassMaterialsAppService
{
    private readonly IRepository<GatePassMaterial> _repository;

    public GatePassMaterialsAppService(IRepository<GatePassMaterial> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<GatePassMaterialDto>> GetAll(GetAllGatePassMaterialsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(input.GatePassId.HasValue, m => m.GatePassId == input.GatePassId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<GatePassMaterialDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetGatePassMaterialForEditOutput> GetGatePassMaterialForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetGatePassMaterialForEditOutput
        {
            GatePassMaterial = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditGatePassMaterialDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassMaterials_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassMaterials_Create)]
    protected virtual async Task CreateAsync(CreateOrEditGatePassMaterialDto input)
    {
        var entity = MapToEntity(input, new GatePassMaterial());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassMaterials_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditGatePassMaterialDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static GatePassMaterial MapToEntity(CreateOrEditGatePassMaterialDto input, GatePassMaterial entity)
    {
        entity.GatePassId = input.GatePassId;
        entity.ItemCode = input.ItemCode;
        entity.ItemName = input.ItemName;
        entity.Quantity = input.Quantity;
        entity.ExpectedReturnDate = input.ExpectedReturnDate;
        entity.ReturnedQuantity = input.ReturnedQuantity;

        return entity;
    }

    private static GatePassMaterialDto ToDto(GatePassMaterial entity)
    {
        return new GatePassMaterialDto
        {
            Id = entity.Id,
            GatePassId = entity.GatePassId,
            ItemCode = entity.ItemCode,
            ItemName = entity.ItemName,
            Quantity = entity.Quantity,
            ExpectedReturnDate = entity.ExpectedReturnDate,
            ReturnedQuantity = entity.ReturnedQuantity
        };
    }

    private static CreateOrEditGatePassMaterialDto ToCreateOrEditDto(GatePassMaterial entity)
    {
        return new CreateOrEditGatePassMaterialDto
        {
            Id = entity.Id,
            GatePassId = entity.GatePassId,
            ItemCode = entity.ItemCode,
            ItemName = entity.ItemName,
            Quantity = entity.Quantity,
            ExpectedReturnDate = entity.ExpectedReturnDate,
            ReturnedQuantity = entity.ReturnedQuantity
        };
    }
}
