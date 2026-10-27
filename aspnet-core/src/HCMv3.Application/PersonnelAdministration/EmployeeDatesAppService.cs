using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeDate"/> (ported from legacy XAF EmployeeDate business
/// object). Plain CRUD; the legacy source has no StartDate/EndDate chaining for this entity.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDates)]
public class EmployeeDatesAppService : HCMv3AppServiceBase, IEmployeeDatesAppService
{
    private readonly IRepository<EmployeeDate> _employeeDateRepository;

    public EmployeeDatesAppService(IRepository<EmployeeDate> employeeDateRepository)
    {
        _employeeDateRepository = employeeDateRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDateDto>> GetAll(GetAllEmployeeDatesInput input)
    {
        input.Normalize();

        var query = _employeeDateRepository.GetAll()
            .Include(d => d.Employee)
            .Include(d => d.DateType)
            .WhereIf(input.EmployeeId.HasValue, d => d.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeDateDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeDateForEditOutput> GetEmployeeDateForEdit(EntityDto input)
    {
        var entity = await _employeeDateRepository.GetAsync(input.Id);

        return new GetEmployeeDateForEditOutput
        {
            EmployeeDate = new CreateOrEditEmployeeDateDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                DateTypeId = entity.DateTypeId,
                DateData = entity.DateData,
                DayData = entity.DayData
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeDateDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDates_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeDateDto input)
    {
        var entity = MapToEntity(input, new EmployeeDate());
        entity.TenantId = AbpSession.TenantId ?? 0;
        await _employeeDateRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDates_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeDateDto input)
    {
        var entity = await _employeeDateRepository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);
        await _employeeDateRepository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDates_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeDateRepository.DeleteAsync(input.Id);
    }

    private static EmployeeDate MapToEntity(CreateOrEditEmployeeDateDto input, EmployeeDate entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.DateTypeId = input.DateTypeId;
        entity.DateData = input.DateData;
        entity.DayData = input.DayData;
        return entity;
    }

    private static EmployeeDateDto ToDto(EmployeeDate entity)
    {
        return new EmployeeDateDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            DateTypeId = entity.DateTypeId,
            DateTypeName = entity.DateType?.NameEn,
            DateData = entity.DateData,
            DayData = entity.DayData
        };
    }
}
