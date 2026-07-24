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
/// Application service for <see cref="EmployeeAccountAssignment"/> (ported from legacy XAF
/// EmployeeAccountAssignment business object, Wave 4 deferred bucket). The legacy source has no
/// <c>PerformValidation</c> method at all (only sys-field bookkeeping in <c>OnSaving</c>), so no additional
/// business validation is ported here (matches legacy active behavior).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAccountAssignments)]
public class EmployeeAccountAssignmentsAppService : HCMv3AppServiceBase, IEmployeeAccountAssignmentsAppService
{
    private readonly IRepository<EmployeeAccountAssignment> _repository;

    public EmployeeAccountAssignmentsAppService(IRepository<EmployeeAccountAssignment> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAccountAssignmentDto>> GetAll(GetAllEmployeeAccountAssignmentsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.CostElement1)
            .Include(e => e.CostElement2)
            .Include(e => e.CostElement3)
            .Include(e => e.CostElement4)
            .Include(e => e.CostElement5)
            .Include(e => e.CostElement6)
            .Include(e => e.CostElement7)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeAccountAssignmentDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeAccountAssignmentForEditOutput> GetEmployeeAccountAssignmentForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeAccountAssignmentForEditOutput
        {
            EmployeeAccountAssignment = new CreateOrEditEmployeeAccountAssignmentDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                CostElement1Id = entity.CostElement1Id,
                CostElement2Id = entity.CostElement2Id,
                CostElement3Id = entity.CostElement3Id,
                CostElement4Id = entity.CostElement4Id,
                CostElement5Id = entity.CostElement5Id,
                CostElement6Id = entity.CostElement6Id,
                CostElement7Id = entity.CostElement7Id,
                CostPercentage = entity.CostPercentage
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeAccountAssignmentDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAccountAssignments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeAccountAssignmentDto input)
    {
        var entity = MapToEntity(input, new EmployeeAccountAssignment());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAccountAssignments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeAccountAssignmentDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAccountAssignments_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeAccountAssignment MapToEntity(CreateOrEditEmployeeAccountAssignmentDto input, EmployeeAccountAssignment entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.CostElement1Id = input.CostElement1Id;
        entity.CostElement2Id = input.CostElement2Id;
        entity.CostElement3Id = input.CostElement3Id;
        entity.CostElement4Id = input.CostElement4Id;
        entity.CostElement5Id = input.CostElement5Id;
        entity.CostElement6Id = input.CostElement6Id;
        entity.CostElement7Id = input.CostElement7Id;
        entity.CostPercentage = input.CostPercentage;
        return entity;
    }

    private static EmployeeAccountAssignmentDto ToDto(EmployeeAccountAssignment entity)
    {
        return new EmployeeAccountAssignmentDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            CostElement1Id = entity.CostElement1Id,
            CostElement1NameEn = entity.CostElement1?.NameEn,
            CostElement2Id = entity.CostElement2Id,
            CostElement2NameEn = entity.CostElement2?.NameEn,
            CostElement3Id = entity.CostElement3Id,
            CostElement3NameEn = entity.CostElement3?.NameEn,
            CostElement4Id = entity.CostElement4Id,
            CostElement4NameEn = entity.CostElement4?.NameEn,
            CostElement5Id = entity.CostElement5Id,
            CostElement5NameEn = entity.CostElement5?.NameEn,
            CostElement6Id = entity.CostElement6Id,
            CostElement6NameEn = entity.CostElement6?.NameEn,
            CostElement7Id = entity.CostElement7Id,
            CostElement7NameEn = entity.CostElement7?.NameEn,
            CostPercentage = entity.CostPercentage
        };
    }
}
