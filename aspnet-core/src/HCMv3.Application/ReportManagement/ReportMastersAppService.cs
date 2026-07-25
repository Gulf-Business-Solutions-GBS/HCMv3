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
/// Application service for drill-down report definitions (ported from legacy XAF ReportMaster business
/// object). See <see cref="ReportMaster"/> class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportMasters)]
public class ReportMastersAppService : HCMv3AppServiceBase, IReportMastersAppService
{
    private readonly IRepository<ReportMaster> _repository;

    public ReportMastersAppService(IRepository<ReportMaster> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ReportMasterDto>> GetAll(GetAllReportMastersInput input)
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

        return new PagedResultDto<ReportMasterDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetReportMasterForEditOutput> GetReportMasterForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetReportMasterForEditOutput
        {
            ReportMaster = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditReportMasterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportMasters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportMasters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditReportMasterDto input)
    {
        var entity = MapToEntity(input, new ReportMaster());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportMasters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditReportMasterDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static ReportMaster MapToEntity(CreateOrEditReportMasterDto input, ReportMaster entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;

        return entity;
    }

    /// <summary>Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.</summary>
    private async Task AssignCodeIfEmptyAsync(ReportMaster entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(r => r.Code != null && r.Code != "")
            .Select(r => r.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.</summary>
    private async Task ValidateAsync(ReportMaster entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && r.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && (r.NameEn == entity.NameEn || r.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static ReportMasterDto ToDto(ReportMaster entity)
    {
        return new ReportMasterDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr
        };
    }

    private static CreateOrEditReportMasterDto ToCreateOrEditDto(ReportMaster entity)
    {
        return new CreateOrEditReportMasterDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr
        };
    }
}
