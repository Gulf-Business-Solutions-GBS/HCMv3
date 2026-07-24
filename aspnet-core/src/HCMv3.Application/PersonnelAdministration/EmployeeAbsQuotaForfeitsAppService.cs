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
/// Application service for <see cref="EmployeeAbsQuotaForfeit"/> (ported from legacy XAF
/// EmployeeAbsQuotaForfeit business object, Wave 1 deferred bucket). Records the manual forfeiture
/// transaction itself; the legacy cascade that adds/subtracts <c>QuotaForfeited</c> on the linked
/// <see cref="EmployeeAbsenceQuota"/> record (with negative-balance validation) is NOT ported here -- see
/// class remarks on <see cref="EmployeeAbsQuotaForfeit"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsQuotaForfeits)]
public class EmployeeAbsQuotaForfeitsAppService : HCMv3AppServiceBase, IEmployeeAbsQuotaForfeitsAppService
{
    private readonly IRepository<EmployeeAbsQuotaForfeit> _repository;

    public EmployeeAbsQuotaForfeitsAppService(IRepository<EmployeeAbsQuotaForfeit> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAbsQuotaForfeitDto>> GetAll(GetAllEmployeeAbsQuotaForfeitsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(f => f.Employee)
            .Include(f => f.AbsenceType)
            .WhereIf(input.EmployeeId.HasValue, f => f.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeAbsQuotaForfeitDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeAbsQuotaForfeitForEditOutput> GetEmployeeAbsQuotaForfeitForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeAbsQuotaForfeitForEditOutput
        {
            EmployeeAbsQuotaForfeit = new CreateOrEditEmployeeAbsQuotaForfeitDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                AbsenceTypeId = entity.AbsenceTypeId,
                EffectiveDate = entity.EffectiveDate,
                QuotaForfeited = entity.QuotaForfeited,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeAbsQuotaForfeitDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsQuotaForfeits_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeAbsQuotaForfeitDto input)
    {
        var entity = MapToEntity(input, new EmployeeAbsQuotaForfeit());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsQuotaForfeits_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeAbsQuotaForfeitDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsQuotaForfeits_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeAbsQuotaForfeit MapToEntity(CreateOrEditEmployeeAbsQuotaForfeitDto input, EmployeeAbsQuotaForfeit entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.AbsenceTypeId = input.AbsenceTypeId;
        entity.EffectiveDate = input.EffectiveDate.Date;
        entity.QuotaForfeited = System.Math.Round(input.QuotaForfeited, 6);
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeAbsQuotaForfeitDto ToDto(EmployeeAbsQuotaForfeit entity)
    {
        return new EmployeeAbsQuotaForfeitDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            AbsenceTypeId = entity.AbsenceTypeId,
            AbsenceTypeName = entity.AbsenceType?.NameEn,
            EffectiveDate = entity.EffectiveDate,
            QuotaForfeited = entity.QuotaForfeited,
            Comments = entity.Comments
        };
    }
}
