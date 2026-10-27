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
/// Application service for the per-day breakdown of a <see cref="SalaryRegisterDetail"/> (ported from
/// legacy XAF SalaryRegisterDaysDetail business object), listed separately and linked back via
/// <see cref="SalaryRegisterDaysDetail.SalaryRegisterDetailId"/>.
///
/// LOCK CASCADE PARITY: create/update/delete are blocked when the ultimate parent (the
/// <c>SalaryRegisterDetail</c>'s own parent <see cref="SalaryRegister"/>) is
/// <see cref="SalaryRegister.Locked"/> — reached two levels up (legacy <c>OnSaving</c>/<c>OnDeleting</c>
/// parity).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisterDaysDetails)]
public class SalaryRegisterDaysDetailsAppService : HCMv3AppServiceBase, ISalaryRegisterDaysDetailsAppService
{
    private readonly IRepository<SalaryRegisterDaysDetail> _salaryRegisterDaysDetailRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;

    public SalaryRegisterDaysDetailsAppService(
        IRepository<SalaryRegisterDaysDetail> salaryRegisterDaysDetailRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<SalaryRegister> salaryRegisterRepository)
    {
        _salaryRegisterDaysDetailRepository = salaryRegisterDaysDetailRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SalaryRegisterDaysDetailDto>> GetAll(GetAllSalaryRegisterDaysDetailsInput input)
    {
        input.Normalize();

        var query = _salaryRegisterDaysDetailRepository.GetAll()
            .WhereIf(input.SalaryRegisterDetailId.HasValue, d => d.SalaryRegisterDetailId == input.SalaryRegisterDetailId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SalaryRegisterDaysDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetSalaryRegisterDaysDetailForEditOutput> GetSalaryRegisterDaysDetailForEdit(EntityDto input)
    {
        var entity = await _salaryRegisterDaysDetailRepository.GetAsync(input.Id);

        return new GetSalaryRegisterDaysDetailForEditOutput
        {
            SalaryRegisterDaysDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSalaryRegisterDaysDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisterDaysDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _salaryRegisterDaysDetailRepository.GetAsync(input.Id);
        await EnsureUltimateParentNotLockedAsync(entity.SalaryRegisterDetailId);

        await _salaryRegisterDaysDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisterDaysDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSalaryRegisterDaysDetailDto input)
    {
        await EnsureUltimateParentNotLockedAsync(input.SalaryRegisterDetailId);

        var entity = MapToEntity(input, new SalaryRegisterDaysDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _salaryRegisterDaysDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisterDaysDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSalaryRegisterDaysDetailDto input)
    {
        await EnsureUltimateParentNotLockedAsync(input.SalaryRegisterDetailId);

        var entity = await _salaryRegisterDaysDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _salaryRegisterDaysDetailRepository.UpdateAsync(entity);
    }

    private async Task EnsureUltimateParentNotLockedAsync(int salaryRegisterDetailId)
    {
        var parentDetail = await _salaryRegisterDetailRepository.GetAsync(salaryRegisterDetailId);
        var parentRegister = await _salaryRegisterRepository.GetAsync(parentDetail.SalaryRegisterId);
        if (parentRegister.Locked)
        {
            throw new UserFriendlyException(L("LockedSalaryRegisterCannotBeUpdated"));
        }
    }

    private static SalaryRegisterDaysDetail MapToEntity(CreateOrEditSalaryRegisterDaysDetailDto input, SalaryRegisterDaysDetail entity)
    {
        entity.SalaryRegisterDetailId = input.SalaryRegisterDetailId;
        entity.Period = input.Period;
        entity.Rate = input.Rate;
        entity.Number = input.Number;
        entity.Amount = input.Amount;

        return entity;
    }

    private static SalaryRegisterDaysDetailDto ToDto(SalaryRegisterDaysDetail entity)
    {
        return new SalaryRegisterDaysDetailDto
        {
            Id = entity.Id,
            SalaryRegisterDetailId = entity.SalaryRegisterDetailId,
            Period = entity.Period,
            Rate = entity.Rate,
            Number = entity.Number,
            Amount = entity.Amount
        };
    }

    private static CreateOrEditSalaryRegisterDaysDetailDto ToCreateOrEditDto(SalaryRegisterDaysDetail entity)
    {
        return new CreateOrEditSalaryRegisterDaysDetailDto
        {
            Id = entity.Id,
            SalaryRegisterDetailId = entity.SalaryRegisterDetailId,
            Period = entity.Period,
            Rate = entity.Rate,
            Number = entity.Number,
            Amount = entity.Amount
        };
    }
}
