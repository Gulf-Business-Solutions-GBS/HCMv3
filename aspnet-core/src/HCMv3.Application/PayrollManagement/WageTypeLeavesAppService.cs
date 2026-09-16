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
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the per-WageType applicable AbsenceType link (ported from legacy XAF
/// WageTypeLeave business object), listed separately and linked back via
/// <see cref="WageTypeLeave.WageTypeId"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeLeaves)]
public class WageTypeLeavesAppService : HCMv3AppServiceBase, IWageTypeLeavesAppService
{
    private readonly IRepository<WageTypeLeave> _wageTypeLeaveRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;

    public WageTypeLeavesAppService(
        IRepository<WageTypeLeave> wageTypeLeaveRepository,
        IRepository<AbsenceType> absenceTypeRepository)
    {
        _wageTypeLeaveRepository = wageTypeLeaveRepository;
        _absenceTypeRepository = absenceTypeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WageTypeLeaveDto>> GetAll(GetAllWageTypeLeavesInput input)
    {
        input.Normalize();

        var query = _wageTypeLeaveRepository.GetAll()
            .Include(l => l.WageType)
            .Include(l => l.AbsenceType)
            .WhereIf(input.WageTypeId.HasValue, l => l.WageTypeId == input.WageTypeId)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), l =>
                l.AbsenceType.Code.Contains(input.Filter) ||
                l.AbsenceType.NameEn.Contains(input.Filter) ||
                l.AbsenceType.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WageTypeLeaveDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetWageTypeLeaveForEditOutput> GetWageTypeLeaveForEdit(EntityDto input)
    {
        var entity = await _wageTypeLeaveRepository.GetAsync(input.Id);

        return new GetWageTypeLeaveForEditOutput
        {
            WageTypeLeave = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditWageTypeLeaveDto input)
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

    /// <summary>
    /// Legacy "Add All Leave Types": links every absence type the wage type is not linked to yet.
    /// Returns how many links were added.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeLeaves_Create)]
    public async Task<int> AddAllLeaveTypes(EntityDto input)
    {
        var linked = await _wageTypeLeaveRepository.GetAll()
            .Where(l => l.WageTypeId == input.Id)
            .Select(l => l.AbsenceTypeId)
            .ToListAsync();

        var missing = await _absenceTypeRepository.GetAll()
            .Where(a => !linked.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync();

        foreach (var absenceTypeId in missing)
        {
            await _wageTypeLeaveRepository.InsertAsync(new WageTypeLeave
            {
                TenantId = AbpSession.TenantId ?? 0,
                WageTypeId = input.Id,
                AbsenceTypeId = absenceTypeId
            });
        }

        return missing.Count;
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeLeaves_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _wageTypeLeaveRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeLeaves_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWageTypeLeaveDto input)
    {
        var entity = MapToEntity(input, new WageTypeLeave());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _wageTypeLeaveRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeLeaves_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWageTypeLeaveDto input)
    {
        var entity = await _wageTypeLeaveRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _wageTypeLeaveRepository.UpdateAsync(entity);
    }

    private static WageTypeLeave MapToEntity(CreateOrEditWageTypeLeaveDto input, WageTypeLeave entity)
    {
        entity.WageTypeId = input.WageTypeId;
        entity.AbsenceTypeId = input.AbsenceTypeId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate AbsenceTypeId per WageType.
    /// </summary>
    private async Task ValidateAsync(WageTypeLeave entity)
    {
        var duplicate = await _wageTypeLeaveRepository.GetAll()
            .AnyAsync(l => l.Id != entity.Id &&
                           l.WageTypeId == entity.WageTypeId &&
                           l.AbsenceTypeId == entity.AbsenceTypeId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("AbsenceTypeAlreadyAssigned"));
        }
    }

    private static WageTypeLeaveDto ToDto(WageTypeLeave entity)
    {
        return new WageTypeLeaveDto
        {
            Id = entity.Id,
            WageTypeId = entity.WageTypeId,
            WageTypeName = entity.WageType?.NameEn,
            AbsenceTypeId = entity.AbsenceTypeId,
            AbsenceTypeName = entity.AbsenceType?.NameEn
        };
    }

    private static CreateOrEditWageTypeLeaveDto ToCreateOrEditDto(WageTypeLeave entity)
    {
        return new CreateOrEditWageTypeLeaveDto
        {
            Id = entity.Id,
            WageTypeId = entity.WageTypeId,
            AbsenceTypeId = entity.AbsenceTypeId
        };
    }
}
