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
/// Application service for <see cref="ManageSecuritySettings"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageSecuritySettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws -- ported to <see cref="DeleteSecuritySettings"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSettings_SecuritySettings)]
public class ManageSecuritySettingsAppService : HCMv3AppServiceBase, IManageSecuritySettingsAppService
{
    private readonly IRepository<ManageSecuritySettings> _repository;

    public ManageSecuritySettingsAppService(IRepository<ManageSecuritySettings> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<ManageSecuritySettingsListDto>> GetSecuritySettingsList(PagedAndSortedResultRequestDto input)
    {
        var query = _repository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ManageSecuritySettingsListDto>(
            totalCount,
            items.Select(ToListDto).ToList());
    }

    public async Task<ManageSecuritySettingsEditDto> GetSecuritySettingsForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return ToEditDto(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_SecuritySettings_Create, AppPermissions.Pages_SystemSettings_SecuritySettings_Edit)]
    public async Task CreateOrEditSecuritySettings(ManageSecuritySettingsEditDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_SecuritySettings_Delete)]
    public Task DeleteSecuritySettings(EntityDto input)
    {
        throw new UserFriendlyException(L("CannotDeleteSystemSettingsRecord"));
    }

    private async Task CreateAsync(ManageSecuritySettingsEditDto input)
    {
        var entity = MapToEntity(input, new ManageSecuritySettings());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    private async Task UpdateAsync(ManageSecuritySettingsEditDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static ManageSecuritySettings MapToEntity(ManageSecuritySettingsEditDto input, ManageSecuritySettings entity)
    {
        entity.EnableRowLevelSecurity = input.EnableRowLevelSecurity;
        entity.EnableAuditLogInTimeEngine = input.EnableAuditLogInTimeEngine;
        entity.EnableAuditLogInPayrollEngine = input.EnableAuditLogInPayrollEngine;
        entity.EnableAuditLogInProjectCostEngine = input.EnableAuditLogInProjectCostEngine;
        entity.EnableAuditLogInCostEngine = input.EnableAuditLogInCostEngine;
        entity.EnableAuditLogInGridReport = input.EnableAuditLogInGridReport;
        entity.EnableAuditLogInPrintReport = input.EnableAuditLogInPrintReport;
        entity.EnableReportExportPwd = input.EnableReportExportPwd;

        return entity;
    }

    private static ManageSecuritySettingsListDto ToListDto(ManageSecuritySettings entity)
    {
        return new ManageSecuritySettingsListDto
        {
            Id = entity.Id,
            EnableRowLevelSecurity = entity.EnableRowLevelSecurity,
            EnableAuditLogInTimeEngine = entity.EnableAuditLogInTimeEngine,
            EnableAuditLogInPayrollEngine = entity.EnableAuditLogInPayrollEngine,
            EnableAuditLogInProjectCostEngine = entity.EnableAuditLogInProjectCostEngine,
            EnableAuditLogInCostEngine = entity.EnableAuditLogInCostEngine,
            EnableAuditLogInGridReport = entity.EnableAuditLogInGridReport,
            EnableAuditLogInPrintReport = entity.EnableAuditLogInPrintReport,
            EnableReportExportPwd = entity.EnableReportExportPwd
        };
    }

    private static ManageSecuritySettingsEditDto ToEditDto(ManageSecuritySettings entity)
    {
        return new ManageSecuritySettingsEditDto
        {
            Id = entity.Id,
            EnableRowLevelSecurity = entity.EnableRowLevelSecurity,
            EnableAuditLogInTimeEngine = entity.EnableAuditLogInTimeEngine,
            EnableAuditLogInPayrollEngine = entity.EnableAuditLogInPayrollEngine,
            EnableAuditLogInProjectCostEngine = entity.EnableAuditLogInProjectCostEngine,
            EnableAuditLogInCostEngine = entity.EnableAuditLogInCostEngine,
            EnableAuditLogInGridReport = entity.EnableAuditLogInGridReport,
            EnableAuditLogInPrintReport = entity.EnableAuditLogInPrintReport,
            EnableReportExportPwd = entity.EnableReportExportPwd
        };
    }
}
