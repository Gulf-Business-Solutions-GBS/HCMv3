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
using HCMv3.GovtRelationsManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Application service for <see cref="SponsorLicenseInfo"/> (ported from legacy XAF SponsorLicenseInfo
/// business object, one of <see cref="Sponsor"/>'s aggregated child collections -- see <see cref="Sponsor"/>
/// remarks). Ports the legacy <c>PerformValidation</c> parity: unique TradeLicenseNum per Sponsor.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorLicenseInfos)]
public class SponsorLicenseInfosAppService : HCMv3AppServiceBase, ISponsorLicenseInfosAppService
{
    private readonly IRepository<SponsorLicenseInfo> _repository;

    public SponsorLicenseInfosAppService(IRepository<SponsorLicenseInfo> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SponsorLicenseInfoDto>> GetAll(GetAllSponsorLicenseInfosInput input)
    {
        var query = _repository.GetAll()
            .Include(l => l.Sponsor)
            .WhereIf(input.SponsorId.HasValue, l => l.SponsorId == input.SponsorId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SponsorLicenseInfoDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetSponsorLicenseInfoForEditOutput> GetSponsorLicenseInfoForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSponsorLicenseInfoForEditOutput
        {
            SponsorLicenseInfo = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSponsorLicenseInfoDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorLicenseInfos_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorLicenseInfos_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSponsorLicenseInfoDto input)
    {
        var entity = MapToEntity(input, new SponsorLicenseInfo());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorLicenseInfos_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSponsorLicenseInfoDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static SponsorLicenseInfo MapToEntity(CreateOrEditSponsorLicenseInfoDto input, SponsorLicenseInfo entity)
    {
        entity.SponsorId = input.SponsorId;
        entity.TradeLicenseNum = input.TradeLicenseNum;
        entity.LicenseIssueDate = input.LicenseIssueDate.Date;
        entity.LicenseExpiryDate = input.LicenseExpiryDate.Date;
        entity.CivilId = input.CivilId;
        entity.ActivityType = input.ActivityType;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: date range validity plus unique TradeLicenseNum per Sponsor.
    /// </summary>
    private async Task ValidateAsync(SponsorLicenseInfo entity)
    {
        if (entity.LicenseExpiryDate < entity.LicenseIssueDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var duplicate = await _repository.GetAll()
            .AnyAsync(l => l.Id != entity.Id &&
                           l.SponsorId == entity.SponsorId &&
                           l.TradeLicenseNum == entity.TradeLicenseNum);
        if (duplicate)
        {
            throw new UserFriendlyException(L("DuplicateTradeLicenseForSponsor"));
        }
    }

    private static SponsorLicenseInfoDto ToDto(SponsorLicenseInfo entity)
    {
        return new SponsorLicenseInfoDto
        {
            Id = entity.Id,
            SponsorId = entity.SponsorId,
            SponsorName = entity.Sponsor?.NameEn,
            TradeLicenseNum = entity.TradeLicenseNum,
            LicenseIssueDate = entity.LicenseIssueDate,
            LicenseExpiryDate = entity.LicenseExpiryDate,
            CivilId = entity.CivilId,
            ActivityType = entity.ActivityType
        };
    }

    private static CreateOrEditSponsorLicenseInfoDto ToCreateOrEditDto(SponsorLicenseInfo entity)
    {
        return new CreateOrEditSponsorLicenseInfoDto
        {
            Id = entity.Id,
            SponsorId = entity.SponsorId,
            TradeLicenseNum = entity.TradeLicenseNum,
            LicenseIssueDate = entity.LicenseIssueDate,
            LicenseExpiryDate = entity.LicenseExpiryDate,
            CivilId = entity.CivilId,
            ActivityType = entity.ActivityType
        };
    }
}
