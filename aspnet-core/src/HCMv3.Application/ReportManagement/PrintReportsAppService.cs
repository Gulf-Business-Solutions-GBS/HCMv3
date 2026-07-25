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
/// Application service for the printable report catalog (ported from legacy XAF PrintReport business
/// object). See <see cref="PrintReport"/> class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ReportManagement_PrintReports)]
public class PrintReportsAppService : HCMv3AppServiceBase, IPrintReportsAppService
{
    private readonly IRepository<PrintReport> _repository;

    public PrintReportsAppService(IRepository<PrintReport> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PrintReportDto>> GetAll(GetAllPrintReportsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), p =>
                p.Code.Contains(input.Filter) ||
                p.NameEn.Contains(input.Filter) ||
                p.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PrintReportDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPrintReportForEditOutput> GetPrintReportForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetPrintReportForEditOutput
        {
            PrintReport = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditPrintReportDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_PrintReports_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_PrintReports_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPrintReportDto input)
    {
        var entity = MapToEntity(input, new PrintReport());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_PrintReports_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPrintReportDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static PrintReport MapToEntity(CreateOrEditPrintReportDto input, PrintReport entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.ReportName = input.ReportName;
        entity.IsActive = input.IsActive;
        entity.ReportCategory = input.ReportCategory;
        entity.CustomPercentage = input.CustomPercentage;
        entity.ReportCreatedOn = input.ReportCreatedOn;

        return entity;
    }

    /// <summary>Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.</summary>
    private async Task AssignCodeIfEmptyAsync(PrintReport entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(p => p.Code != null && p.Code != "")
            .Select(p => p.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.</summary>
    private async Task ValidateAsync(PrintReport entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(p => p.Id != entity.Id && p.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(p => p.Id != entity.Id && (p.NameEn == entity.NameEn || p.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static PrintReportDto ToDto(PrintReport entity)
    {
        return new PrintReportDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            ReportName = entity.ReportName,
            IsActive = entity.IsActive,
            ReportCategory = entity.ReportCategory,
            CustomPercentage = entity.CustomPercentage,
            ReportCreatedOn = entity.ReportCreatedOn
        };
    }

    private static CreateOrEditPrintReportDto ToCreateOrEditDto(PrintReport entity)
    {
        return new CreateOrEditPrintReportDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            ReportName = entity.ReportName,
            IsActive = entity.IsActive,
            ReportCategory = entity.ReportCategory,
            CustomPercentage = entity.CustomPercentage,
            ReportCreatedOn = entity.ReportCreatedOn
        };
    }
}
