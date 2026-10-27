using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeRemuneration"/> (ported from legacy XAF EmployeeRemuneration
/// business object, Wave 4 deferred bucket). Ports the essential active validation only: Employee/
/// PaymentDate required and PaymentNumber != 0.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRemunerations)]
public class EmployeeRemunerationsAppService : HCMv3AppServiceBase, IEmployeeRemunerationsAppService
{
    private readonly IRepository<EmployeeRemuneration> _repository;

    public EmployeeRemunerationsAppService(IRepository<EmployeeRemuneration> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeRemunerationDto>> GetAll(GetAllEmployeeRemunerationsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.WageType)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeRemunerationDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeRemunerationForEditOutput> GetEmployeeRemunerationForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeRemunerationForEditOutput
        {
            EmployeeRemuneration = new CreateOrEditEmployeeRemunerationDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                WageTypeId = entity.WageTypeId,
                PaymentDate = entity.PaymentDate,
                PaymentNumber = entity.PaymentNumber,
                PaymentUnit = entity.PaymentUnit,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeRemunerationDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRemunerations_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeRemunerationDto input)
    {
        var entity = MapToEntity(input, new EmployeeRemuneration());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateEntity(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRemunerations_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeRemunerationDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        ValidateEntity(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRemunerations_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private void ValidateEntity(EmployeeRemuneration entity)
    {
        if (entity.PaymentNumber == 0)
        {
            throw new UserFriendlyException(L("AmountCannotBeZero"));
        }
    }

    private static EmployeeRemuneration MapToEntity(CreateOrEditEmployeeRemunerationDto input, EmployeeRemuneration entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.WageTypeId = input.WageTypeId;
        entity.PaymentDate = input.PaymentDate.Date;
        entity.PaymentNumber = input.PaymentNumber;
        entity.PaymentUnit = input.PaymentUnit;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeRemunerationDto ToDto(EmployeeRemuneration entity)
    {
        return new EmployeeRemunerationDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            WageTypeId = entity.WageTypeId,
            WageTypeNameEn = entity.WageType?.NameEn,
            PaymentDate = entity.PaymentDate,
            PaymentNumber = entity.PaymentNumber,
            PaymentUnit = entity.PaymentUnit,
            Comments = entity.Comments
        };
    }
}
