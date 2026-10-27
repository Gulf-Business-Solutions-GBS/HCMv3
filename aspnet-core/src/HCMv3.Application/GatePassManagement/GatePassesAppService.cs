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
using HCMv3.GatePassManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GatePassManagement;

/// <summary>
/// Application service for the GatePass header (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GatePassManagement.GatePass</c>). Legacy <c>PerformValidation</c> parity:
/// GatePassType and Movement required, IssueDate/ValidUpto required, ValidUpto &gt;= IssueDate. See
/// <see cref="GatePass"/> class remarks for the Location/RequestedBy/Status optionality and GatePassNumber
/// simplifications.
///
/// Legacy <c>[Aggregated]</c> child collections (<see cref="GatePassPerson"/>/<see cref="GatePassMaterial"/>/
/// <see cref="GatePassVehicle"/>) implied cascade delete in XPO; that cascade is reproduced explicitly in
/// <see cref="Delete"/> here since each child now lives behind its own app service/repository.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePasses)]
public class GatePassesAppService : HCMv3AppServiceBase, IGatePassesAppService
{
    private readonly IRepository<GatePass> _repository;
    private readonly IRepository<GatePassMaterial> _materialRepository;
    private readonly IRepository<GatePassPerson> _personRepository;
    private readonly IRepository<GatePassVehicle> _vehicleRepository;

    public GatePassesAppService(
        IRepository<GatePass> repository,
        IRepository<GatePassMaterial> materialRepository,
        IRepository<GatePassPerson> personRepository,
        IRepository<GatePassVehicle> vehicleRepository)
    {
        _repository = repository;
        _materialRepository = materialRepository;
        _personRepository = personRepository;
        _vehicleRepository = vehicleRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<GatePassDto>> GetAll(GetAllGatePassesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(g => g.GatePassType)
            .Include(g => g.Movement)
            .Include(g => g.Location)
            .Include(g => g.RequestedBy)
            .Include(g => g.Status)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), g => g.Comments.Contains(input.Filter))
            .WhereIf(input.GatePassTypeId.HasValue, g => g.GatePassTypeId == input.GatePassTypeId)
            .WhereIf(input.RequestedByEmployeeId.HasValue, g => g.RequestedByEmployeeId == input.RequestedByEmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<GatePassDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetGatePassForEditOutput> GetGatePassForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetGatePassForEditOutput
        {
            GatePass = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditGatePassDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePasses_Delete)]
    public async Task Delete(EntityDto input)
    {
        var materials = await _materialRepository.GetAll().Where(m => m.GatePassId == input.Id).ToListAsync();
        foreach (var material in materials)
        {
            await _materialRepository.DeleteAsync(material);
        }

        var persons = await _personRepository.GetAll().Where(p => p.GatePassId == input.Id).ToListAsync();
        foreach (var person in persons)
        {
            await _personRepository.DeleteAsync(person);
        }

        var vehicles = await _vehicleRepository.GetAll().Where(v => v.GatePassId == input.Id).ToListAsync();
        foreach (var vehicle in vehicles)
        {
            await _vehicleRepository.DeleteAsync(vehicle);
        }

        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePasses_Create)]
    protected virtual async Task CreateAsync(CreateOrEditGatePassDto input)
    {
        var entity = MapToEntity(input, new GatePass());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateDates(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePasses_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditGatePassDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateDates(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>Legacy parity: ValidUpto must be &gt;= IssueDate.</summary>
    private void ValidateDates(GatePass entity)
    {
        if (entity.ValidUpto < entity.IssueDate)
        {
            throw new UserFriendlyException(L("GatePassValidUptoBeforeIssueDate"));
        }
    }

    private static GatePass MapToEntity(CreateOrEditGatePassDto input, GatePass entity)
    {
        entity.GatePassNumber = input.GatePassNumber;
        entity.GatePassTypeId = input.GatePassTypeId;
        entity.GatePassMovementId = input.GatePassMovementId;
        entity.LocationId = input.LocationId;
        entity.IssueDate = input.IssueDate;
        entity.ValidUpto = input.ValidUpto;
        entity.StartTime = input.StartTime;
        entity.EndTime = input.EndTime;
        entity.RequestedByEmployeeId = input.RequestedByEmployeeId;
        entity.GatePassStatusId = input.GatePassStatusId;
        entity.Comments = input.Comments;

        return entity;
    }

    private static GatePassDto ToDto(GatePass entity)
    {
        return new GatePassDto
        {
            Id = entity.Id,
            GatePassNumber = entity.GatePassNumber,
            GatePassTypeId = entity.GatePassTypeId,
            GatePassTypeNameEn = entity.GatePassType?.NameEn,
            GatePassMovementId = entity.GatePassMovementId,
            MovementNameEn = entity.Movement?.NameEn,
            LocationId = entity.LocationId,
            LocationNameEn = entity.Location?.NameEn,
            IssueDate = entity.IssueDate,
            ValidUpto = entity.ValidUpto,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            RequestedByEmployeeId = entity.RequestedByEmployeeId,
            RequestedByFullName = entity.RequestedBy?.FirstNameEn,
            GatePassStatusId = entity.GatePassStatusId,
            StatusNameEn = entity.Status?.NameEn,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditGatePassDto ToCreateOrEditDto(GatePass entity)
    {
        return new CreateOrEditGatePassDto
        {
            Id = entity.Id,
            GatePassNumber = entity.GatePassNumber,
            GatePassTypeId = entity.GatePassTypeId,
            GatePassMovementId = entity.GatePassMovementId,
            LocationId = entity.LocationId,
            IssueDate = entity.IssueDate,
            ValidUpto = entity.ValidUpto,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            RequestedByEmployeeId = entity.RequestedByEmployeeId,
            GatePassStatusId = entity.GatePassStatusId,
            Comments = entity.Comments
        };
    }
}
