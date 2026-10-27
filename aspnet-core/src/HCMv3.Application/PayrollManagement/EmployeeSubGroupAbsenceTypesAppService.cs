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
/// Application service for the per-EmployeeSubGroup applicable AbsenceType link (ported from legacy XAF
/// EmployeeSubGroupAbsenceType business object), listed separately and linked back via
/// <see cref="EmployeeSubGroupAbsenceType.EmployeeSubGroupId"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsenceTypes)]
public class EmployeeSubGroupAbsenceTypesAppService : HCMv3AppServiceBase, IEmployeeSubGroupAbsenceTypesAppService
{
    private readonly IRepository<EmployeeSubGroupAbsenceType> _employeeSubGroupAbsenceTypeRepository;

    public EmployeeSubGroupAbsenceTypesAppService(IRepository<EmployeeSubGroupAbsenceType> employeeSubGroupAbsenceTypeRepository)
    {
        _employeeSubGroupAbsenceTypeRepository = employeeSubGroupAbsenceTypeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSubGroupAbsenceTypeDto>> GetAll(GetAllEmployeeSubGroupAbsenceTypesInput input)
    {
        input.Normalize();

        var query = _employeeSubGroupAbsenceTypeRepository.GetAll()
            .Include(t => t.EmployeeSubGroup)
            .Include(t => t.AbsenceType)
            .WhereIf(input.EmployeeSubGroupId.HasValue, t => t.EmployeeSubGroupId == input.EmployeeSubGroupId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSubGroupAbsenceTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSubGroupAbsenceTypeForEditOutput> GetEmployeeSubGroupAbsenceTypeForEdit(EntityDto input)
    {
        var entity = await _employeeSubGroupAbsenceTypeRepository.GetAsync(input.Id);

        return new GetEmployeeSubGroupAbsenceTypeForEditOutput
        {
            EmployeeSubGroupAbsenceType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSubGroupAbsenceTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsenceTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeSubGroupAbsenceTypeRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsenceTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSubGroupAbsenceTypeDto input)
    {
        var entity = MapToEntity(input, new EmployeeSubGroupAbsenceType());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _employeeSubGroupAbsenceTypeRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsenceTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSubGroupAbsenceTypeDto input)
    {
        var entity = await _employeeSubGroupAbsenceTypeRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _employeeSubGroupAbsenceTypeRepository.UpdateAsync(entity);
    }

    private static EmployeeSubGroupAbsenceType MapToEntity(CreateOrEditEmployeeSubGroupAbsenceTypeDto input, EmployeeSubGroupAbsenceType entity)
    {
        entity.EmployeeSubGroupId = input.EmployeeSubGroupId;
        entity.AbsenceTypeId = input.AbsenceTypeId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate AbsenceTypeId per EmployeeSubGroup (the legacy
    /// source ran this duplicate check twice with equivalent predicates -- only run once here).
    /// </summary>
    private async Task ValidateAsync(EmployeeSubGroupAbsenceType entity)
    {
        var duplicate = await _employeeSubGroupAbsenceTypeRepository.GetAll()
            .AnyAsync(t => t.Id != entity.Id &&
                           t.EmployeeSubGroupId == entity.EmployeeSubGroupId &&
                           t.AbsenceTypeId == entity.AbsenceTypeId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("AbsenceTypeAlreadyAssigned"));
        }
    }

    private static EmployeeSubGroupAbsenceTypeDto ToDto(EmployeeSubGroupAbsenceType entity)
    {
        return new EmployeeSubGroupAbsenceTypeDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            EmployeeSubGroupName = entity.EmployeeSubGroup?.NameEn,
            AbsenceTypeId = entity.AbsenceTypeId,
            AbsenceTypeName = entity.AbsenceType?.NameEn
        };
    }

    private static CreateOrEditEmployeeSubGroupAbsenceTypeDto ToCreateOrEditDto(EmployeeSubGroupAbsenceType entity)
    {
        return new CreateOrEditEmployeeSubGroupAbsenceTypeDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            AbsenceTypeId = entity.AbsenceTypeId
        };
    }
}
