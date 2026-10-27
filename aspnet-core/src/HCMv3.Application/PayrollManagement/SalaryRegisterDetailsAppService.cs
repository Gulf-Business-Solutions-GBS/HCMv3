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
/// Application service for the wage-type calculation line of a <see cref="SalaryRegister"/> (ported from
/// legacy XAF SalaryRegisterDetail business object), listed separately and linked back via
/// <see cref="SalaryRegisterDetail.SalaryRegisterId"/>.
///
/// LOCK CASCADE PARITY: create/update/delete are blocked when the parent <see cref="SalaryRegister"/> is
/// <see cref="SalaryRegister.Locked"/> (legacy <c>OnSaving</c>/<c>OnDeleting</c> parity).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisterDetails)]
public class SalaryRegisterDetailsAppService : HCMv3AppServiceBase, ISalaryRegisterDetailsAppService
{
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;

    public SalaryRegisterDetailsAppService(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<SalaryRegister> salaryRegisterRepository)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SalaryRegisterDetailDto>> GetAll(GetAllSalaryRegisterDetailsInput input)
    {
        input.Normalize();

        var query = _salaryRegisterDetailRepository.GetAll()
            .Include(d => d.WageType)
            .WhereIf(input.SalaryRegisterId.HasValue, d => d.SalaryRegisterId == input.SalaryRegisterId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SalaryRegisterDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetSalaryRegisterDetailForEditOutput> GetSalaryRegisterDetailForEdit(EntityDto input)
    {
        var entity = await _salaryRegisterDetailRepository.GetAsync(input.Id);

        return new GetSalaryRegisterDetailForEditOutput
        {
            SalaryRegisterDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSalaryRegisterDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisterDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _salaryRegisterDetailRepository.GetAsync(input.Id);
        await EnsureParentNotLockedAsync(entity.SalaryRegisterId);

        await _salaryRegisterDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisterDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSalaryRegisterDetailDto input)
    {
        await EnsureParentNotLockedAsync(input.SalaryRegisterId);

        var entity = MapToEntity(input, new SalaryRegisterDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _salaryRegisterDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisterDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSalaryRegisterDetailDto input)
    {
        await EnsureParentNotLockedAsync(input.SalaryRegisterId);

        var entity = await _salaryRegisterDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _salaryRegisterDetailRepository.UpdateAsync(entity);
    }

    private async Task EnsureParentNotLockedAsync(int salaryRegisterId)
    {
        var parent = await _salaryRegisterRepository.GetAsync(salaryRegisterId);
        if (parent.Locked)
        {
            throw new UserFriendlyException(L("LockedSalaryRegisterCannotBeUpdated"));
        }
    }

    private static SalaryRegisterDetail MapToEntity(CreateOrEditSalaryRegisterDetailDto input, SalaryRegisterDetail entity)
    {
        entity.SalaryRegisterId = input.SalaryRegisterId;
        entity.WageTypeId = input.WageTypeId;
        entity.Rate = input.Rate;
        entity.Number = input.Number;
        entity.Amount = input.Amount;

        return entity;
    }

    private static SalaryRegisterDetailDto ToDto(SalaryRegisterDetail entity)
    {
        return new SalaryRegisterDetailDto
        {
            Id = entity.Id,
            SalaryRegisterId = entity.SalaryRegisterId,
            WageTypeId = entity.WageTypeId,
            WageTypeName = entity.WageType?.NameEn,
            Rate = entity.Rate,
            Number = entity.Number,
            Amount = entity.Amount
        };
    }

    private static CreateOrEditSalaryRegisterDetailDto ToCreateOrEditDto(SalaryRegisterDetail entity)
    {
        return new CreateOrEditSalaryRegisterDetailDto
        {
            Id = entity.Id,
            SalaryRegisterId = entity.SalaryRegisterId,
            WageTypeId = entity.WageTypeId,
            Rate = entity.Rate,
            Number = entity.Number,
            Amount = entity.Amount
        };
    }
}
