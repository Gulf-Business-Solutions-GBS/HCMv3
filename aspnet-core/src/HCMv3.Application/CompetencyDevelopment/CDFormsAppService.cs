using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.CompetencyDevelopment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.CompetencyDevelopment;

/// <summary>
/// Application service for an employee's competency-development review form (ported from legacy XAF CDForm
/// business object). See <see cref="CDForm"/> class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDForms)]
public class CDFormsAppService : HCMv3AppServiceBase, ICDFormsAppService
{
    private readonly IRepository<CDForm> _repository;

    public CDFormsAppService(IRepository<CDForm> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CDFormDto>> GetAll(GetAllCDFormsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(f => f.CDPeriod)
            .Include(f => f.Employee)
            .WhereIf(input.CDPeriodId.HasValue, f => f.CDPeriodId == input.CDPeriodId)
            .WhereIf(input.EmployeeId.HasValue, f => f.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CDFormDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetCDFormForEditOutput> GetCDFormForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetCDFormForEditOutput
        {
            CDForm = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCDFormDto input)
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

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDForms_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDForms_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCDFormDto input)
    {
        var entity = MapToEntity(input, new CDForm());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDForms_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCDFormDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static CDForm MapToEntity(CreateOrEditCDFormDto input, CDForm entity)
    {
        entity.CDPeriodId = input.CDPeriodId;
        entity.EmployeeId = input.EmployeeId;
        entity.OverallScore = input.OverallScore;
        entity.ModeratedScore = input.ModeratedScore;
        entity.EmployeeSignedOff = input.EmployeeSignedOff;
        entity.ManagerSignedOff = input.ManagerSignedOff;

        return entity;
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: one CDForm per Period+Employee combination.</summary>
    private async Task ValidateAsync(CDForm entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(f => f.Id != entity.Id &&
                           f.CDPeriodId == entity.CDPeriodId &&
                           f.EmployeeId == entity.EmployeeId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("CDFormAlreadyExistsForPeriodAndEmployee"));
        }
    }

    private static CDFormDto ToDto(CDForm entity)
    {
        return new CDFormDto
        {
            Id = entity.Id,
            CDPeriodId = entity.CDPeriodId,
            CDPeriodName = entity.CDPeriod?.Name,
            EmployeeId = entity.EmployeeId,
            EmployeeName = entity.Employee != null ? entity.Employee.FirstNameEn + " " + entity.Employee.LastNameEn : null,
            OverallScore = entity.OverallScore,
            ModeratedScore = entity.ModeratedScore,
            EmployeeSignedOff = entity.EmployeeSignedOff,
            ManagerSignedOff = entity.ManagerSignedOff
        };
    }

    private static CreateOrEditCDFormDto ToCreateOrEditDto(CDForm entity)
    {
        return new CreateOrEditCDFormDto
        {
            Id = entity.Id,
            CDPeriodId = entity.CDPeriodId,
            EmployeeId = entity.EmployeeId,
            OverallScore = entity.OverallScore,
            ModeratedScore = entity.ModeratedScore,
            EmployeeSignedOff = entity.EmployeeSignedOff,
            ManagerSignedOff = entity.ManagerSignedOff
        };
    }
}
