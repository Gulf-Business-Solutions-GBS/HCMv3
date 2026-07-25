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
/// Application service for role-based access to <see cref="PrintReport"/>s (ported from legacy XAF
/// PrintReportPermission business object). See <see cref="PrintReportPermission"/> class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ReportManagement_PrintReportPermissions)]
public class PrintReportPermissionsAppService : HCMv3AppServiceBase, IPrintReportPermissionsAppService
{
    private readonly IRepository<PrintReportPermission> _repository;

    public PrintReportPermissionsAppService(IRepository<PrintReportPermission> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PrintReportPermissionDto>> GetAll(GetAllPrintReportPermissionsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll().Include(p => p.Role);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PrintReportPermissionDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPrintReportPermissionForEditOutput> GetPrintReportPermissionForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetPrintReportPermissionForEditOutput
        {
            PrintReportPermission = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditPrintReportPermissionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_PrintReportPermissions_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_PrintReportPermissions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPrintReportPermissionDto input)
    {
        var entity = MapToEntity(input, new PrintReportPermission());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_PrintReportPermissions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPrintReportPermissionDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static PrintReportPermission MapToEntity(CreateOrEditPrintReportPermissionDto input, PrintReportPermission entity)
    {
        entity.Code = input.Code;
        entity.RoleId = input.RoleId;

        return entity;
    }

    /// <summary>Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.</summary>
    private async Task AssignCodeIfEmptyAsync(PrintReportPermission entity)
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

    /// <summary>Legacy <c>PerformValidation</c> parity: unique Code, and only one permission record per Role.</summary>
    private async Task ValidateAsync(PrintReportPermission entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(p => p.Id != entity.Id && p.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateRole = await _repository.GetAll()
            .AnyAsync(p => p.Id != entity.Id && p.RoleId == entity.RoleId);
        if (duplicateRole)
        {
            throw new UserFriendlyException(L("RoleAlreadyAssigned"));
        }
    }

    private static PrintReportPermissionDto ToDto(PrintReportPermission entity)
    {
        return new PrintReportPermissionDto
        {
            Id = entity.Id,
            Code = entity.Code,
            RoleId = entity.RoleId,
            RoleName = entity.Role?.DisplayName
        };
    }

    private static CreateOrEditPrintReportPermissionDto ToCreateOrEditDto(PrintReportPermission entity)
    {
        return new CreateOrEditPrintReportPermissionDto
        {
            Id = entity.Id,
            Code = entity.Code,
            RoleId = entity.RoleId
        };
    }
}
