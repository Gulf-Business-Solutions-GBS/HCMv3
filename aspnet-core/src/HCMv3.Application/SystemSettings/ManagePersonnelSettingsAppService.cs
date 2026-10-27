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
using HCMv3.SystemSettings.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSettings;

/// <summary>
/// Application service for <see cref="ManagePersonnelSettings"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManagePersonnelSettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws ("You can't delete record. Please contact administrator.") -- ported to
/// <see cref="DeletePersonnelSettings"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSettings_PersonnelSettings)]
public class ManagePersonnelSettingsAppService : HCMv3AppServiceBase, IManagePersonnelSettingsAppService
{
    private readonly IRepository<ManagePersonnelSettings> _repository;

    public ManagePersonnelSettingsAppService(IRepository<ManagePersonnelSettings> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<PersonnelSettingsListDto>> GetPersonnelSettingsList(PagedAndSortedResultRequestDto input)
    {
        var query = _repository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PersonnelSettingsListDto>(
            totalCount,
            items.Select(ToListDto).ToList());
    }

    public async Task<PersonnelSettingsEditDto> GetPersonnelSettingsForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return ToEditDto(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_PersonnelSettings_Create, AppPermissions.Pages_SystemSettings_PersonnelSettings_Edit)]
    public async Task CreateOrEditPersonnelSettings(PersonnelSettingsEditDto input)
    {
        if (input.Id == 0)
        {
            await CreateAsync(input);
        }
        else
        {
            await UpdateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_PersonnelSettings_Delete)]
    public Task DeletePersonnelSettings(EntityDto input)
    {
        throw new UserFriendlyException(L("CannotDeleteSystemSettingsRecord"));
    }

    private async Task CreateAsync(PersonnelSettingsEditDto input)
    {
        var entity = MapToEntity(input, new ManagePersonnelSettings());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    private async Task UpdateAsync(PersonnelSettingsEditDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static ManagePersonnelSettings MapToEntity(PersonnelSettingsEditDto input, ManagePersonnelSettings entity)
    {
        entity.EnableSponsorGovtJobTitleQuota = input.EnableSponsorGovtJobTitleQuota;
        entity.MaxPhotoSize = input.MaxPhotoSize;
        entity.ProbationDaysCalculation = input.ProbationDaysCalculation;
        entity.AutoWageAssignOnHire = input.AutoWageAssignOnHire;

        return entity;
    }

    private static PersonnelSettingsListDto ToListDto(ManagePersonnelSettings entity)
    {
        return new PersonnelSettingsListDto
        {
            Id = entity.Id,
            EnableSponsorGovtJobTitleQuota = entity.EnableSponsorGovtJobTitleQuota,
            MaxPhotoSize = entity.MaxPhotoSize,
            ProbationDaysCalculation = entity.ProbationDaysCalculation,
            AutoWageAssignOnHire = entity.AutoWageAssignOnHire
        };
    }

    private static PersonnelSettingsEditDto ToEditDto(ManagePersonnelSettings entity)
    {
        return new PersonnelSettingsEditDto
        {
            Id = entity.Id,
            EnableSponsorGovtJobTitleQuota = entity.EnableSponsorGovtJobTitleQuota,
            MaxPhotoSize = entity.MaxPhotoSize,
            ProbationDaysCalculation = entity.ProbationDaysCalculation,
            AutoWageAssignOnHire = entity.AutoWageAssignOnHire
        };
    }
}
