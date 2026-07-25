using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.ReportManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ReportManagement;

/// <summary>
/// Application service for the allowed-reports child list of a <see cref="PrintReportPermission"/> (ported
/// from legacy XAF PrintReportPermissionDetail business object). See
/// <see cref="PrintReportPermissionDetail"/> class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ReportManagement_PrintReportPermissionDetails)]
public class PrintReportPermissionDetailsAppService : HCMv3AppServiceBase, IPrintReportPermissionDetailsAppService
{
    private readonly IRepository<PrintReportPermissionDetail> _repository;

    public PrintReportPermissionDetailsAppService(IRepository<PrintReportPermissionDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PrintReportPermissionDetailDto>> GetAll(GetAllPrintReportPermissionDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.PrintReport)
            .WhereIf(input.PrintReportPermissionId.HasValue, d => d.PrintReportPermissionId == input.PrintReportPermissionId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PrintReportPermissionDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPrintReportPermissionDetailForEditOutput> GetPrintReportPermissionDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetPrintReportPermissionDetailForEditOutput
        {
            PrintReportPermissionDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditPrintReportPermissionDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_PrintReportPermissionDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_PrintReportPermissionDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPrintReportPermissionDetailDto input)
    {
        var entity = MapToEntity(input, new PrintReportPermissionDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_PrintReportPermissionDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPrintReportPermissionDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static PrintReportPermissionDetail MapToEntity(CreateOrEditPrintReportPermissionDetailDto input, PrintReportPermissionDetail entity)
    {
        entity.PrintReportPermissionId = input.PrintReportPermissionId;
        entity.PrintReportId = input.PrintReportId;

        return entity;
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: no duplicate PrintReport per permission.</summary>
    private async Task ValidateAsync(PrintReportPermissionDetail entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(d => d.Id != entity.Id &&
                           d.PrintReportPermissionId == entity.PrintReportPermissionId &&
                           d.PrintReportId == entity.PrintReportId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("PrintReportAlreadyAssigned"));
        }
    }

    private static PrintReportPermissionDetailDto ToDto(PrintReportPermissionDetail entity)
    {
        return new PrintReportPermissionDetailDto
        {
            Id = entity.Id,
            PrintReportPermissionId = entity.PrintReportPermissionId,
            PrintReportId = entity.PrintReportId,
            PrintReportName = entity.PrintReport?.NameEn
        };
    }

    private static CreateOrEditPrintReportPermissionDetailDto ToCreateOrEditDto(PrintReportPermissionDetail entity)
    {
        return new CreateOrEditPrintReportPermissionDetailDto
        {
            Id = entity.Id,
            PrintReportPermissionId = entity.PrintReportPermissionId,
            PrintReportId = entity.PrintReportId
        };
    }
}
