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
using HCMv3.ReportManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ReportManagement;

/// <summary>
/// Application service for saved report grid layouts (ported from legacy XAF ReportSetting business
/// object). See <see cref="ReportSetting"/> class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportSettings)]
public class ReportSettingsAppService : HCMv3AppServiceBase, IReportSettingsAppService
{
    private readonly IRepository<ReportSetting> _repository;

    public ReportSettingsAppService(IRepository<ReportSetting> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ReportSettingDto>> GetAll(GetAllReportSettingsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), r =>
                r.Code.Contains(input.Filter) ||
                r.NameEn.Contains(input.Filter) ||
                r.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ReportSettingDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetReportSettingForEditOutput> GetReportSettingForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetReportSettingForEditOutput
        {
            ReportSetting = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditReportSettingDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportSettings_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportSettings_Create)]
    protected virtual async Task CreateAsync(CreateOrEditReportSettingDto input)
    {
        var entity = MapToEntity(input, new ReportSetting());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportSettings_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditReportSettingDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static ReportSetting MapToEntity(CreateOrEditReportSettingDto input, ReportSetting entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.ReportId = input.ReportId;
        entity.Level = input.Level;

        return entity;
    }

    private static ReportSettingDto ToDto(ReportSetting entity)
    {
        return new ReportSettingDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            ReportId = entity.ReportId,
            Level = entity.Level
        };
    }

    private static CreateOrEditReportSettingDto ToCreateOrEditDto(ReportSetting entity)
    {
        return new CreateOrEditReportSettingDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            ReportId = entity.ReportId,
            Level = entity.Level
        };
    }
}
