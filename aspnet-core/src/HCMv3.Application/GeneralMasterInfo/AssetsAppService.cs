using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the Asset master (ported from legacy XAF Asset business object).
/// The legacy <c>PerformValidation</c> logic (auto Code, duplicate Code/Name checks) lives here,
/// out of the persistence lifecycle. Entity/DTO mapping is done manually (not via Mapperly) since
/// the list DTO also needs the FK's display name (AssetTypeName) resolved -- same bespoke pattern
/// as <see cref="CitiesAppService"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Assets)]
public class AssetsAppService : HCMv3AppServiceBase, IAssetsAppService
{
    private readonly IRepository<Asset> _assetRepository;

    public AssetsAppService(IRepository<Asset> assetRepository)
    {
        _assetRepository = assetRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AssetDto>> GetAll(GetAllAssetsInput input)
    {
        input.Normalize();

        var query = _assetRepository.GetAll()
            .Include(a => a.AssetType)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), a =>
                a.Code.Contains(input.Filter) ||
                a.NameEn.Contains(input.Filter) ||
                a.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AssetDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetAssetForEditOutput> GetAssetForEdit(EntityDto input)
    {
        var asset = await _assetRepository.GetAsync(input.Id);

        return new GetAssetForEditOutput
        {
            Asset = ToCreateOrEditDto(asset)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAssetDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Assets_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAssetDto input)
    {
        var asset = new Asset
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            Brand = input.Brand,
            Model = input.Model,
            Color = input.Color,
            VIN_Number = input.VIN_Number,
            Serial = input.Serial,
            PlateNumber = input.PlateNumber,
            Barcode = input.Barcode,
            Year = input.Year,
            ExpiryDate = input.ExpiryDate,
            Value = input.Value,
            AssetTypeId = input.AssetTypeId,
            Status = input.Status,
            AssetAllocationStatus = input.AssetAllocationStatus,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(asset);
        await ValidateUniquenessAsync(asset);

        await _assetRepository.InsertAsync(asset);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Assets_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAssetDto input)
    {
        var asset = await _assetRepository.GetAsync(input.Id.Value);

        asset.Code = input.Code;
        asset.NameEn = input.NameEn;
        asset.NameAr = input.NameAr;
        asset.Brand = input.Brand;
        asset.Model = input.Model;
        asset.Color = input.Color;
        asset.VIN_Number = input.VIN_Number;
        asset.Serial = input.Serial;
        asset.PlateNumber = input.PlateNumber;
        asset.Barcode = input.Barcode;
        asset.Year = input.Year;
        asset.ExpiryDate = input.ExpiryDate;
        asset.Value = input.Value;
        asset.AssetTypeId = input.AssetTypeId;
        asset.Status = input.Status;
        asset.AssetAllocationStatus = input.AssetAllocationStatus;

        await AssignCodeIfEmptyAsync(asset);
        await ValidateUniquenessAsync(asset);

        await _assetRepository.UpdateAsync(asset);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Assets_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _assetRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Asset asset)
    {
        if (!asset.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _assetRepository.GetAll()
            .Where(a => a.Code != null && a.Code != "")
            .Select(a => a.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        asset.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(Asset asset)
    {
        var duplicateCode = await _assetRepository.GetAll()
            .AnyAsync(a => a.Id != asset.Id && a.Code == asset.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _assetRepository.GetAll()
            .AnyAsync(a => a.Id != asset.Id &&
                           (a.NameEn == asset.NameEn || a.NameAr == asset.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static AssetDto ToDto(Asset asset)
    {
        return new AssetDto
        {
            Id = asset.Id,
            Code = asset.Code,
            NameEn = asset.NameEn,
            NameAr = asset.NameAr,
            Brand = asset.Brand,
            Model = asset.Model,
            Color = asset.Color,
            VIN_Number = asset.VIN_Number,
            Serial = asset.Serial,
            PlateNumber = asset.PlateNumber,
            Barcode = asset.Barcode,
            Year = asset.Year,
            ExpiryDate = asset.ExpiryDate,
            Value = asset.Value,
            AssetTypeId = asset.AssetTypeId,
            AssetTypeName = asset.AssetType?.NameEn,
            Status = asset.Status,
            AssetAllocationStatus = asset.AssetAllocationStatus
        };
    }

    private static CreateOrEditAssetDto ToCreateOrEditDto(Asset asset)
    {
        return new CreateOrEditAssetDto
        {
            Id = asset.Id,
            Code = asset.Code,
            NameEn = asset.NameEn,
            NameAr = asset.NameAr,
            Brand = asset.Brand,
            Model = asset.Model,
            Color = asset.Color,
            VIN_Number = asset.VIN_Number,
            Serial = asset.Serial,
            PlateNumber = asset.PlateNumber,
            Barcode = asset.Barcode,
            Year = asset.Year,
            ExpiryDate = asset.ExpiryDate,
            Value = asset.Value,
            AssetTypeId = asset.AssetTypeId,
            Status = asset.Status,
            AssetAllocationStatus = asset.AssetAllocationStatus
        };
    }
}
