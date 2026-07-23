using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the FI account mapping row of a <see cref="WageAccGroup"/> (ported from legacy
/// XAF WageAccGroupDetail business object), listed separately and linked back via
/// <see cref="WageAccGroupDetail.WageAccGroupId"/>. See <see cref="WageAccGroupDetail"/> class remarks for
/// the dropped CostElement-slot CostElementType cross-check.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageAccGroupDetails)]
public class WageAccGroupDetailsAppService : HCMv3AppServiceBase, IWageAccGroupDetailsAppService
{
    private readonly IRepository<WageAccGroupDetail> _wageAccGroupDetailRepository;

    public WageAccGroupDetailsAppService(IRepository<WageAccGroupDetail> wageAccGroupDetailRepository)
    {
        _wageAccGroupDetailRepository = wageAccGroupDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WageAccGroupDetailDto>> GetAll(GetAllWageAccGroupDetailsInput input)
    {
        input.Normalize();

        var query = _wageAccGroupDetailRepository.GetAll()
            .Include(d => d.WageAccGroup)
            .Include(d => d.WageType)
            .Include(d => d.AccountInfo)
            .Include(d => d.OffsetAccountInfo)
            .WhereIf(input.WageAccGroupId.HasValue, d => d.WageAccGroupId == input.WageAccGroupId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WageAccGroupDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetWageAccGroupDetailForEditOutput> GetWageAccGroupDetailForEdit(EntityDto input)
    {
        var entity = await _wageAccGroupDetailRepository.GetAsync(input.Id);

        return new GetWageAccGroupDetailForEditOutput
        {
            WageAccGroupDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditWageAccGroupDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageAccGroupDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _wageAccGroupDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageAccGroupDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWageAccGroupDetailDto input)
    {
        var entity = MapToEntity(input, new WageAccGroupDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _wageAccGroupDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageAccGroupDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWageAccGroupDetailDto input)
    {
        var entity = await _wageAccGroupDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _wageAccGroupDetailRepository.UpdateAsync(entity);
    }

    private static WageAccGroupDetail MapToEntity(CreateOrEditWageAccGroupDetailDto input, WageAccGroupDetail entity)
    {
        entity.WageAccGroupId = input.WageAccGroupId;
        entity.WageTypeId = input.WageTypeId;
        entity.AccountInfoId = input.AccountInfoId;
        entity.OffsetAccountInfoType = input.OffsetAccountInfoType;
        entity.OffsetAccountInfoId = input.OffsetAccountInfoId;
        entity.CostElement1Id = input.CostElement1Id;
        entity.CostElement2Id = input.CostElement2Id;
        entity.CostElement3Id = input.CostElement3Id;
        entity.CostElement4Id = input.CostElement4Id;
        entity.CostElement5Id = input.CostElement5Id;
        entity.CostElement6Id = input.CostElement6Id;
        entity.CostElement7Id = input.CostElement7Id;
        entity.CostPercentage = input.CostPercentage;
        entity.MergeOffsetAccount = input.MergeOffsetAccount;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidations</c> parity, SIMPLIFIED: the legacy CostElement-slot CostElementType
    /// cross-check is not ported (see <see cref="WageAccGroupDetail"/> class remarks) — only basic
    /// required-field and duplicate-combination (WageAccGroupId, WageTypeId, AccountInfoId, all 7
    /// CostElements) validation is enforced here.
    /// </summary>
    private async Task ValidateAsync(WageAccGroupDetail entity)
    {
        var duplicate = await _wageAccGroupDetailRepository.GetAll()
            .AnyAsync(d => d.Id != entity.Id &&
                           d.WageAccGroupId == entity.WageAccGroupId &&
                           d.WageTypeId == entity.WageTypeId &&
                           d.AccountInfoId == entity.AccountInfoId &&
                           d.CostElement1Id == entity.CostElement1Id &&
                           d.CostElement2Id == entity.CostElement2Id &&
                           d.CostElement3Id == entity.CostElement3Id &&
                           d.CostElement4Id == entity.CostElement4Id &&
                           d.CostElement5Id == entity.CostElement5Id &&
                           d.CostElement6Id == entity.CostElement6Id &&
                           d.CostElement7Id == entity.CostElement7Id);
        if (duplicate)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    private static WageAccGroupDetailDto ToDto(WageAccGroupDetail entity)
    {
        return new WageAccGroupDetailDto
        {
            Id = entity.Id,
            WageAccGroupId = entity.WageAccGroupId,
            WageAccGroupName = entity.WageAccGroup?.NameEn,
            WageTypeId = entity.WageTypeId,
            WageTypeName = entity.WageType?.NameEn,
            AccountInfoId = entity.AccountInfoId,
            AccountInfoName = entity.AccountInfo?.NameEn,
            OffsetAccountInfoType = entity.OffsetAccountInfoType,
            OffsetAccountInfoId = entity.OffsetAccountInfoId,
            OffsetAccountInfoName = entity.OffsetAccountInfo?.NameEn,
            CostElement1Id = entity.CostElement1Id,
            CostElement2Id = entity.CostElement2Id,
            CostElement3Id = entity.CostElement3Id,
            CostElement4Id = entity.CostElement4Id,
            CostElement5Id = entity.CostElement5Id,
            CostElement6Id = entity.CostElement6Id,
            CostElement7Id = entity.CostElement7Id,
            CostPercentage = entity.CostPercentage,
            MergeOffsetAccount = entity.MergeOffsetAccount
        };
    }

    private static CreateOrEditWageAccGroupDetailDto ToCreateOrEditDto(WageAccGroupDetail entity)
    {
        return new CreateOrEditWageAccGroupDetailDto
        {
            Id = entity.Id,
            WageAccGroupId = entity.WageAccGroupId,
            WageTypeId = entity.WageTypeId,
            AccountInfoId = entity.AccountInfoId,
            OffsetAccountInfoType = entity.OffsetAccountInfoType,
            OffsetAccountInfoId = entity.OffsetAccountInfoId,
            CostElement1Id = entity.CostElement1Id,
            CostElement2Id = entity.CostElement2Id,
            CostElement3Id = entity.CostElement3Id,
            CostElement4Id = entity.CostElement4Id,
            CostElement5Id = entity.CostElement5Id,
            CostElement6Id = entity.CostElement6Id,
            CostElement7Id = entity.CostElement7Id,
            CostPercentage = entity.CostPercentage,
            MergeOffsetAccount = entity.MergeOffsetAccount
        };
    }
}
