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
/// Application service for the drill-down levels of a <see cref="ReportMaster"/> (ported from legacy XAF
/// ReportDetail business object). The legacy's own <c>PerformValidation</c> body was entirely commented out,
/// so no extra business validation is ported here beyond the required FK.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportDetails)]
public class ReportDetailsAppService : HCMv3AppServiceBase, IReportDetailsAppService
{
    private readonly IRepository<ReportDetail> _repository;

    public ReportDetailsAppService(IRepository<ReportDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ReportDetailDto>> GetAll(GetAllReportDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.ReportMaster)
            .Include(d => d.ReportSetting)
            .WhereIf(input.ReportMasterId.HasValue, d => d.ReportMasterId == input.ReportMasterId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ReportDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetReportDetailForEditOutput> GetReportDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetReportDetailForEditOutput
        {
            ReportDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditReportDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditReportDetailDto input)
    {
        var entity = MapToEntity(input, new ReportDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditReportDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static ReportDetail MapToEntity(CreateOrEditReportDetailDto input, ReportDetail entity)
    {
        entity.ReportMasterId = input.ReportMasterId;
        entity.ReportSettingId = input.ReportSettingId;
        entity.DrillDownLevel = input.DrillDownLevel;

        return entity;
    }

    private static ReportDetailDto ToDto(ReportDetail entity)
    {
        return new ReportDetailDto
        {
            Id = entity.Id,
            ReportMasterId = entity.ReportMasterId,
            ReportMasterName = entity.ReportMaster?.NameEn,
            ReportSettingId = entity.ReportSettingId,
            ReportSettingName = entity.ReportSetting?.NameEn,
            DrillDownLevel = entity.DrillDownLevel
        };
    }

    private static CreateOrEditReportDetailDto ToCreateOrEditDto(ReportDetail entity)
    {
        return new CreateOrEditReportDetailDto
        {
            Id = entity.Id,
            ReportMasterId = entity.ReportMasterId,
            ReportSettingId = entity.ReportSettingId,
            DrillDownLevel = entity.DrillDownLevel
        };
    }
}
