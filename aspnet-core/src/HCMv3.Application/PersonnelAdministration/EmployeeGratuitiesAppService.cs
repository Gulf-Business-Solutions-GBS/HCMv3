using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeGratuity"/> (ported from legacy XAF EmployeeGratuity business
/// object, legacy display name "Indemnity"). Every save and delete moves <c>QuotaUsed</c> on the
/// <see cref="EmployeeGratuityQuota"/> covering the paid date, through <see cref="IEmployeeGratuityQuotaService"/>
/// (legacy <c>UpdateQuotaBalance</c>/<c>OnDeleting</c>).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeGratuities)]
public class EmployeeGratuitiesAppService : HCMv3AppServiceBase, IEmployeeGratuitiesAppService
{
    private readonly IRepository<EmployeeGratuity> _repository;
    private readonly IEmployeeGratuityQuotaService _quotaService;

    public EmployeeGratuitiesAppService(
        IRepository<EmployeeGratuity> repository,
        IEmployeeGratuityQuotaService quotaService)
    {
        _repository = repository;
        _quotaService = quotaService;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeGratuityDto>> GetAll(GetAllEmployeeGratuitiesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(g => g.Employee)
            .WhereIf(input.EmployeeId.HasValue, g => g.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeGratuityDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeGratuityForEditOutput> GetEmployeeGratuityForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeGratuityForEditOutput
        {
            EmployeeGratuity = new CreateOrEditEmployeeGratuityDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                GratuityPaidDate = entity.GratuityPaidDate,
                GratuityPaidDays = entity.GratuityPaidDays,
                ResetGratuityCalc = entity.ResetGratuityCalc,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeGratuityDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeGratuities_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeGratuityDto input)
    {
        var entity = MapToEntity(input, new EmployeeGratuity());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _quotaService.ApplyAsync(entity, previous: null);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeGratuities_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeGratuityDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        var previous = EmployeeGratuitySnapshot.Of(entity);
        MapToEntity(input, entity);

        await _quotaService.ApplyAsync(entity, previous);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeGratuities_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        await _quotaService.ReverseAsync(entity);
        await _repository.DeleteAsync(entity);
    }

    private static EmployeeGratuity MapToEntity(CreateOrEditEmployeeGratuityDto input, EmployeeGratuity entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.GratuityPaidDate = input.GratuityPaidDate.Date;
        entity.GratuityPaidDays = System.Math.Round(input.GratuityPaidDays, 3);
        entity.ResetGratuityCalc = input.ResetGratuityCalc;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeGratuityDto ToDto(EmployeeGratuity entity)
    {
        return new EmployeeGratuityDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            GratuityPaidDate = entity.GratuityPaidDate,
            GratuityPaidDays = entity.GratuityPaidDays,
            ResetGratuityCalc = entity.ResetGratuityCalc,
            Comments = entity.Comments
        };
    }
}
