using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.ReportManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ReportManagement;

/// <summary>
/// Application service for the grid column configuration of a <see cref="ReportSetting"/> (ported from
/// legacy XAF ReportSettingDetail business object). See <see cref="ReportSettingDetail"/> class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportSettingDetails)]
public class ReportSettingDetailsAppService : HCMv3AppServiceBase, IReportSettingDetailsAppService
{
    private readonly IRepository<ReportSettingDetail> _repository;

    public ReportSettingDetailsAppService(IRepository<ReportSettingDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ReportSettingDetailDto>> GetAll(GetAllReportSettingDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(input.ReportSettingId.HasValue, d => d.ReportSettingId == input.ReportSettingId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ReportSettingDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetReportSettingDetailForEditOutput> GetReportSettingDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetReportSettingDetailForEditOutput
        {
            ReportSettingDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditReportSettingDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportSettingDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportSettingDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditReportSettingDetailDto input)
    {
        var entity = MapToEntity(input, new ReportSettingDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportSettingDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditReportSettingDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static ReportSettingDetail MapToEntity(CreateOrEditReportSettingDetailDto input, ReportSettingDetail entity)
    {
        entity.ReportSettingId = input.ReportSettingId;
        entity.FieldName = input.FieldName;
        entity.CaptionEn = input.CaptionEn;
        entity.CaptionAr = input.CaptionAr;
        entity.Order = input.Order;
        entity.ColumnDataType = input.ColumnDataType;
        entity.DisplayFormat = input.DisplayFormat;
        entity.Width = input.Width;
        entity.VisibleInEn = input.VisibleInEn;
        entity.VisibleInAr = input.VisibleInAr;
        entity.DataHAlignment = input.DataHAlignment;

        return entity;
    }

    private static ReportSettingDetailDto ToDto(ReportSettingDetail entity)
    {
        return new ReportSettingDetailDto
        {
            Id = entity.Id,
            ReportSettingId = entity.ReportSettingId,
            FieldName = entity.FieldName,
            CaptionEn = entity.CaptionEn,
            CaptionAr = entity.CaptionAr,
            Order = entity.Order,
            ColumnDataType = entity.ColumnDataType,
            DisplayFormat = entity.DisplayFormat,
            Width = entity.Width,
            VisibleInEn = entity.VisibleInEn,
            VisibleInAr = entity.VisibleInAr,
            DataHAlignment = entity.DataHAlignment
        };
    }

    private static CreateOrEditReportSettingDetailDto ToCreateOrEditDto(ReportSettingDetail entity)
    {
        return new CreateOrEditReportSettingDetailDto
        {
            Id = entity.Id,
            ReportSettingId = entity.ReportSettingId,
            FieldName = entity.FieldName,
            CaptionEn = entity.CaptionEn,
            CaptionAr = entity.CaptionAr,
            Order = entity.Order,
            ColumnDataType = entity.ColumnDataType,
            DisplayFormat = entity.DisplayFormat,
            Width = entity.Width,
            VisibleInEn = entity.VisibleInEn,
            VisibleInAr = entity.VisibleInAr,
            DataHAlignment = entity.DataHAlignment
        };
    }
}
