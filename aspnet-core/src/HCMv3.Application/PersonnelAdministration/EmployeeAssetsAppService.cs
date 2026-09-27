using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HCMv3.Workflow.Services;
using HCMv3.PersonnelAdministration.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeAsset"/> (ported from legacy XAF EmployeeAsset business
/// object, Wave 4 deferred bucket). Ports the essential active validation only: required fields,
/// EndDate &gt;= StartDate, and no overlapping StartDate/EndDate window for the same Asset (legacy
/// parity -- note the legacy check is scoped by Asset only, not Employee+Asset, i.e. the same physical
/// asset can't be assigned to two overlapping periods regardless of employee).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAssets)]
public class EmployeeAssetsAppService : HCMv3AppServiceBase, IEmployeeAssetsAppService
{
    private readonly IRepository<EmployeeAsset> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;

    public EmployeeAssetsAppService(IRepository<EmployeeAsset> repository,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _transactionSubmitter = transactionSubmitter;
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAssetDto>> GetAll(GetAllEmployeeAssetsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.Asset)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeAssetDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeAssetForEditOutput> GetEmployeeAssetForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeAssetForEditOutput
        {
            EmployeeAsset = new CreateOrEditEmployeeAssetDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                AssetId = entity.AssetId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                AssetDeductionMode = entity.AssetDeductionMode,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeAssetDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAssets_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeAssetDto input)
    {
        var entity = MapToEntity(input, new EmployeeAsset());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateEntityAsync(entity);
        // Routed for approval when the catalog says so, and simply filed when it does not -- the same
        // question the self-service form asks, so the answer no longer depends on which screen was used.
        // recordDirectly is refused unless the type permits a bypass AND the user holds the permission.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            AssetWorkflowTransactionHandler.AssetRequestType,
            "AST",
            null,
            input.RecordDirectly);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAssets_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeAssetDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateEntityAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAssets_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateEntityAsync(EmployeeAsset entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var hasOverlap = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.AssetId == entity.AssetId
                && e.StartDate.Date <= entity.EndDate.Date && e.EndDate.Date >= entity.StartDate.Date);
        if (hasOverlap)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    private static EmployeeAsset MapToEntity(CreateOrEditEmployeeAssetDto input, EmployeeAsset entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.AssetId = input.AssetId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.AssetDeductionMode = input.AssetDeductionMode;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeAssetDto ToDto(EmployeeAsset entity)
    {
        return new EmployeeAssetDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            AssetId = entity.AssetId,
            AssetNameEn = entity.Asset?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            AssetDeductionMode = entity.AssetDeductionMode,
            Comments = entity.Comments
        };
    }
}
