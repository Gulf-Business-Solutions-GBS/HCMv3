using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GatePassManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GatePassManagement;

/// <summary>
/// Application service for <see cref="GatePassPerson"/>, the aggregated child of <see cref="GatePass"/>
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GatePassManagement.GatePassPerson</c>). Legacy
/// <c>PerformValidation</c> parity: PersonType/Employee/Vendor/PersonFullName required, and no duplicate
/// PersonType+Employee combination within the same gate pass.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassPersons)]
public class GatePassPersonsAppService : HCMv3AppServiceBase, IGatePassPersonsAppService
{
    private readonly IRepository<GatePassPerson> _repository;

    public GatePassPersonsAppService(IRepository<GatePassPerson> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<GatePassPersonDto>> GetAll(GetAllGatePassPersonsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(p => p.PersonType)
            .Include(p => p.Employee)
            .Include(p => p.Vendor)
            .WhereIf(input.GatePassId.HasValue, p => p.GatePassId == input.GatePassId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<GatePassPersonDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetGatePassPersonForEditOutput> GetGatePassPersonForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetGatePassPersonForEditOutput
        {
            GatePassPerson = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditGatePassPersonDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassPersons_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassPersons_Create)]
    protected virtual async Task CreateAsync(CreateOrEditGatePassPersonDto input)
    {
        var entity = MapToEntity(input, new GatePassPerson());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateNoDuplicatePersonAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassPersons_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditGatePassPersonDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateNoDuplicatePersonAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy parity: within the same gate pass, the same PersonType+Employee combination cannot repeat.
    /// </summary>
    private async Task ValidateNoDuplicatePersonAsync(GatePassPerson entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(p => p.Id != entity.Id &&
                           p.GatePassId == entity.GatePassId &&
                           p.PersonTypeId == entity.PersonTypeId &&
                           p.EmployeeId == entity.EmployeeId);

        if (duplicate)
        {
            throw new UserFriendlyException(L("GatePassPersonTypeAlreadyExists"));
        }
    }

    private static GatePassPerson MapToEntity(CreateOrEditGatePassPersonDto input, GatePassPerson entity)
    {
        entity.GatePassId = input.GatePassId;
        entity.PersonTypeId = input.PersonTypeId;
        entity.EmployeeId = input.EmployeeId;
        entity.PersonFullName = input.PersonFullName;
        entity.PersonCivildID = input.PersonCivildID;
        entity.PersonPassportNumber = input.PersonPassportNumber;
        entity.GatePassVendorId = input.GatePassVendorId;

        return entity;
    }

    private static GatePassPersonDto ToDto(GatePassPerson entity)
    {
        return new GatePassPersonDto
        {
            Id = entity.Id,
            GatePassId = entity.GatePassId,
            PersonTypeId = entity.PersonTypeId,
            PersonTypeNameEn = entity.PersonType?.NameEn,
            EmployeeId = entity.EmployeeId,
            EmployeeFullName = entity.Employee?.FirstNameEn,
            PersonFullName = entity.PersonFullName,
            PersonCivildID = entity.PersonCivildID,
            PersonPassportNumber = entity.PersonPassportNumber,
            GatePassVendorId = entity.GatePassVendorId,
            VendorNameEn = entity.Vendor?.NameEn
        };
    }

    private static CreateOrEditGatePassPersonDto ToCreateOrEditDto(GatePassPerson entity)
    {
        return new CreateOrEditGatePassPersonDto
        {
            Id = entity.Id,
            GatePassId = entity.GatePassId,
            PersonTypeId = entity.PersonTypeId,
            EmployeeId = entity.EmployeeId,
            PersonFullName = entity.PersonFullName,
            PersonCivildID = entity.PersonCivildID,
            PersonPassportNumber = entity.PersonPassportNumber,
            GatePassVendorId = entity.GatePassVendorId
        };
    }
}
