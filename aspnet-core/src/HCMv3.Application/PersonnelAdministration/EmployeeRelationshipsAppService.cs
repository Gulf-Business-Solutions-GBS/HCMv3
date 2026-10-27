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
/// Application service for <see cref="EmployeeRelationship"/> (ported from legacy XAF
/// EmployeeRelationship business object). Legacy <c>PerformValidation</c> parity: blank checks,
/// EndDate &gt;= StartDate, and an overlap/duplicate check per employee + relationship type.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRelationships)]
public class EmployeeRelationshipsAppService : HCMv3AppServiceBase, IEmployeeRelationshipsAppService
{
    private readonly IRepository<EmployeeRelationship> _repository;

    public EmployeeRelationshipsAppService(IRepository<EmployeeRelationship> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeRelationshipDto>> GetAll(GetAllEmployeeRelationshipsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(r => r.Employee)
            .Include(r => r.RelatedEmployee)
            .Include(r => r.RelationshipType)
            .WhereIf(input.EmployeeId.HasValue, r => r.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeRelationshipDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeRelationshipForEditOutput> GetEmployeeRelationshipForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeRelationshipForEditOutput
        {
            EmployeeRelationship = new CreateOrEditEmployeeRelationshipDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                RelationshipTypeId = entity.RelationshipTypeId,
                RelatedEmployeeId = entity.RelatedEmployeeId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeRelationshipDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRelationships_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeRelationshipDto input)
    {
        var entity = new EmployeeRelationship
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = input.EmployeeId,
            RelationshipTypeId = input.RelationshipTypeId,
            RelatedEmployeeId = input.RelatedEmployeeId,
            StartDate = input.StartDate.Date,
            EndDate = input.EndDate.Date
        };

        await ValidateAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRelationships_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeRelationshipDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.EmployeeId = input.EmployeeId;
        entity.RelationshipTypeId = input.RelationshipTypeId;
        entity.RelatedEmployeeId = input.RelatedEmployeeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRelationships_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateAsync(EmployeeRelationship entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlapping = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id
                           && r.EmployeeId == entity.EmployeeId
                           && r.RelationshipTypeId == entity.RelationshipTypeId
                           && r.StartDate <= entity.EndDate
                           && r.EndDate >= entity.StartDate);
        if (overlapping)
        {
            throw new UserFriendlyException(L("OverlapConflictFound"));
        }
    }

    private static EmployeeRelationshipDto ToDto(EmployeeRelationship entity)
    {
        return new EmployeeRelationshipDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            RelationshipTypeId = entity.RelationshipTypeId,
            RelationshipTypeName = entity.RelationshipType?.NameEn,
            RelatedEmployeeId = entity.RelatedEmployeeId,
            RelatedEmployeeCode = entity.RelatedEmployee?.Code,
            RelatedEmployeeFullNameEn = entity.RelatedEmployee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
