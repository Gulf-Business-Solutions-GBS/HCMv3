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
/// Application service for the report-builder report type lookup (ported from legacy XAF ReportType
/// business object). See <see cref="ReportType"/> class remarks for why it is not a
/// <c>SimpleLookupEntity</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportTypes)]
public class ReportTypesAppService : HCMv3AppServiceBase, IReportTypesAppService
{
    private readonly IRepository<ReportType> _repository;

    public ReportTypesAppService(IRepository<ReportType> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ReportTypeDto>> GetAll(GetAllReportTypesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), r =>
                r.Code.Contains(input.Filter) ||
                r.NameEn.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ReportTypeDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetReportTypeForEditOutput> GetReportTypeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetReportTypeForEditOutput
        {
            ReportType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditReportTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditReportTypeDto input)
    {
        var entity = MapToEntity(input, new ReportType());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditReportTypeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static ReportType MapToEntity(CreateOrEditReportTypeDto input, ReportType entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;

        return entity;
    }

    /// <summary>Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.</summary>
    private async Task AssignCodeIfEmptyAsync(ReportType entity)
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

    /// <summary>Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn.</summary>
    private async Task ValidateAsync(ReportType entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && r.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && r.NameEn == entity.NameEn);
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static ReportTypeDto ToDto(ReportType entity)
    {
        return new ReportTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn
        };
    }

    private static CreateOrEditReportTypeDto ToCreateOrEditDto(ReportType entity)
    {
        return new CreateOrEditReportTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn
        };
    }
}
