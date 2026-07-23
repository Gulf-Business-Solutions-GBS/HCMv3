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
/// Application service for <see cref="TempEmployeeBenefit"/> (ported from legacy XAF
/// TempEmployeeBenefit business object). Enforces the legacy strict 1:1 Employee->any-TempBenefitType rule
/// (legacy "Employee is already assigned to this Grade" -- checked by EmployeeId only, not by the
/// (Employee, TempBenefitType) combination).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_TempEmployeeBenefits)]
public class TempEmployeeBenefitsAppService : HCMv3AppServiceBase, ITempEmployeeBenefitsAppService
{
    private readonly IRepository<TempEmployeeBenefit> _repository;

    public TempEmployeeBenefitsAppService(IRepository<TempEmployeeBenefit> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TempEmployeeBenefitDto>> GetAll(GetAllTempEmployeeBenefitsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(b => b.Employee)
            .Include(b => b.TempBenefitType)
            .WhereIf(input.EmployeeId.HasValue, b => b.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TempEmployeeBenefitDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetTempEmployeeBenefitForEditOutput> GetTempEmployeeBenefitForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTempEmployeeBenefitForEditOutput
        {
            TempEmployeeBenefit = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTempEmployeeBenefitDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TempEmployeeBenefits_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TempEmployeeBenefits_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTempEmployeeBenefitDto input)
    {
        var entity = MapToEntity(input, new TempEmployeeBenefit());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TempEmployeeBenefits_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTempEmployeeBenefitDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static TempEmployeeBenefit MapToEntity(CreateOrEditTempEmployeeBenefitDto input, TempEmployeeBenefit entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.TempBenefitTypeId = input.TempBenefitTypeId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: an Employee may only have ONE TempEmployeeBenefit row total
    /// (checked by EmployeeId only, matching the legacy's strict rule regardless of Grade).
    /// </summary>
    private async Task ValidateAsync(TempEmployeeBenefit entity)
    {
        var alreadyAssigned = await _repository.GetAll()
            .AnyAsync(b => b.Id != entity.Id && b.EmployeeId == entity.EmployeeId);
        if (alreadyAssigned)
        {
            throw new UserFriendlyException(L("EmployeeAlreadyAssignedToGrade"));
        }
    }

    private static TempEmployeeBenefitDto ToDto(TempEmployeeBenefit entity)
    {
        return new TempEmployeeBenefitDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeName = entity.Employee?.FullNameEn,
            TempBenefitTypeId = entity.TempBenefitTypeId,
            TempBenefitTypeGrade = entity.TempBenefitType?.Grade
        };
    }

    private static CreateOrEditTempEmployeeBenefitDto ToCreateOrEditDto(TempEmployeeBenefit entity)
    {
        return new CreateOrEditTempEmployeeBenefitDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            TempBenefitTypeId = entity.TempBenefitTypeId
        };
    }
}
