using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.ReportManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ReportManagement;

/// <summary>
/// Application service for ad-hoc/dynamic report definitions (ported from legacy XAF ReportInfo business
/// object). See <see cref="ReportInfo"/> class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportInfos)]
public class ReportInfosAppService : HCMv3AppServiceBase, IReportInfosAppService
{
    private readonly IRepository<ReportInfo> _repository;

    public ReportInfosAppService(IRepository<ReportInfo> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ReportInfoDto>> GetAll(GetAllReportInfosInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(r => r.ReportType)
            .Include(r => r.DomainType)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), r => r.ReportName.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ReportInfoDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetReportInfoForEditOutput> GetReportInfoForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetReportInfoForEditOutput
        {
            ReportInfo = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditReportInfoDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportInfos_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportInfos_Create)]
    protected virtual async Task CreateAsync(CreateOrEditReportInfoDto input)
    {
        var entity = MapToEntity(input, new ReportInfo());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportInfos_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditReportInfoDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static ReportInfo MapToEntity(CreateOrEditReportInfoDto input, ReportInfo entity)
    {
        entity.ReportName = input.ReportName;
        entity.ReportTypeId = input.ReportTypeId;
        entity.DomainTypeId = input.DomainTypeId;
        entity.Description = input.Description;
        entity.ReportQuery = input.ReportQuery;
        entity.IsActive = input.IsActive;

        return entity;
    }

    private static ReportInfoDto ToDto(ReportInfo entity)
    {
        return new ReportInfoDto
        {
            Id = entity.Id,
            ReportName = entity.ReportName,
            ReportTypeId = entity.ReportTypeId,
            ReportTypeName = entity.ReportType?.NameEn,
            DomainTypeId = entity.DomainTypeId,
            DomainTypeName = entity.DomainType?.NameEn,
            Description = entity.Description,
            ReportQuery = entity.ReportQuery,
            IsActive = entity.IsActive
        };
    }

    private static CreateOrEditReportInfoDto ToCreateOrEditDto(ReportInfo entity)
    {
        return new CreateOrEditReportInfoDto
        {
            Id = entity.Id,
            ReportName = entity.ReportName,
            ReportTypeId = entity.ReportTypeId,
            DomainTypeId = entity.DomainTypeId,
            Description = entity.Description,
            ReportQuery = entity.ReportQuery,
            IsActive = entity.IsActive
        };
    }
}
