using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the per-AbsenceType incompatibility record (ported from legacy XAF
/// AbsenceTypeException business object), listed separately and linked back via
/// <see cref="AbsenceTypeException.AbsenceTypeId"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceTypeExceptions)]
public class AbsenceTypeExceptionsAppService : HCMv3AppServiceBase, IAbsenceTypeExceptionsAppService
{
    private readonly IRepository<AbsenceTypeException> _absenceTypeExceptionRepository;

    public AbsenceTypeExceptionsAppService(IRepository<AbsenceTypeException> absenceTypeExceptionRepository)
    {
        _absenceTypeExceptionRepository = absenceTypeExceptionRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AbsenceTypeExceptionDto>> GetAll(GetAllAbsenceTypeExceptionsInput input)
    {
        input.Normalize();

        var query = _absenceTypeExceptionRepository.GetAll()
            .Include(e => e.AbsenceType)
            .Include(e => e.ExceptAbsenceType)
            .WhereIf(input.AbsenceTypeId.HasValue, e => e.AbsenceTypeId == input.AbsenceTypeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AbsenceTypeExceptionDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetAbsenceTypeExceptionForEditOutput> GetAbsenceTypeExceptionForEdit(EntityDto input)
    {
        var entity = await _absenceTypeExceptionRepository.GetAsync(input.Id);

        return new GetAbsenceTypeExceptionForEditOutput
        {
            AbsenceTypeException = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAbsenceTypeExceptionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceTypeExceptions_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _absenceTypeExceptionRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceTypeExceptions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAbsenceTypeExceptionDto input)
    {
        var entity = MapToEntity(input, new AbsenceTypeException());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _absenceTypeExceptionRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceTypeExceptions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAbsenceTypeExceptionDto input)
    {
        var entity = await _absenceTypeExceptionRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _absenceTypeExceptionRepository.UpdateAsync(entity);
    }

    private static AbsenceTypeException MapToEntity(CreateOrEditAbsenceTypeExceptionDto input, AbsenceTypeException entity)
    {
        entity.AbsenceTypeId = input.AbsenceTypeId;
        entity.ExceptAbsenceTypeId = input.ExceptAbsenceTypeId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate (AbsenceTypeId, ExceptAbsenceTypeId) pair. Also
    /// disallows an AbsenceType excepting itself (not explicitly checked in the legacy source, but a sound
    /// business rule for this pairing).
    /// </summary>
    private async Task ValidateAsync(AbsenceTypeException entity)
    {
        if (entity.AbsenceTypeId == entity.ExceptAbsenceTypeId)
        {
            throw new UserFriendlyException(L("AbsenceTypeCannotExceptItself"));
        }

        var duplicate = await _absenceTypeExceptionRepository.GetAll()
            .AnyAsync(e => e.Id != entity.Id &&
                           e.AbsenceTypeId == entity.AbsenceTypeId &&
                           e.ExceptAbsenceTypeId == entity.ExceptAbsenceTypeId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("ExceptAbsenceTypeAlreadyExists"));
        }
    }

    private static AbsenceTypeExceptionDto ToDto(AbsenceTypeException entity)
    {
        return new AbsenceTypeExceptionDto
        {
            Id = entity.Id,
            AbsenceTypeId = entity.AbsenceTypeId,
            AbsenceTypeName = entity.AbsenceType?.NameEn,
            ExceptAbsenceTypeId = entity.ExceptAbsenceTypeId,
            ExceptAbsenceTypeName = entity.ExceptAbsenceType?.NameEn
        };
    }

    private static CreateOrEditAbsenceTypeExceptionDto ToCreateOrEditDto(AbsenceTypeException entity)
    {
        return new CreateOrEditAbsenceTypeExceptionDto
        {
            Id = entity.Id,
            AbsenceTypeId = entity.AbsenceTypeId,
            ExceptAbsenceTypeId = entity.ExceptAbsenceTypeId
        };
    }
}
