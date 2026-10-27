using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the LoanTypeDetail detail record (ported from legacy XAF LoanTypeDetail).
/// Pure detail record (no Code/NameEn/NameAr). Ports legacy <c>PerformValidation</c>: a wage type is required,
/// a loan type may list a wage type only once, and the percent must be above 0 and at most 100.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_LoanTypeDetails)]
public class LoanTypeDetailsAppService : HCMv3AppServiceBase, ILoanTypeDetailsAppService
{
    private readonly IRepository<LoanTypeDetail> _repository;

    public LoanTypeDetailsAppService(IRepository<LoanTypeDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<LoanTypeDetailDto>> GetAll(GetAllLoanTypeDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.LoanType)
            .Include(d => d.WageType);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<LoanTypeDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetLoanTypeDetailForEditOutput> GetLoanTypeDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetLoanTypeDetailForEditOutput
        {
            LoanTypeDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditLoanTypeDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_LoanTypeDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditLoanTypeDetailDto input)
    {
        var entity = new LoanTypeDetail
        {
            LoanTypeId = input.LoanTypeId,
            WageTypeId = input.WageTypeId,
            Percent = input.Percent,
            TenantId = AbpSession.TenantId ?? 0
        };

        await ValidateAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_LoanTypeDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditLoanTypeDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.LoanTypeId = input.LoanTypeId;
        entity.WageTypeId = input.WageTypeId;
        entity.Percent = input.Percent;

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_LoanTypeDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>Legacy <c>LoanTypeDetail.PerformValidation</c>, in legacy's order.</summary>
    private async Task ValidateAsync(LoanTypeDetail entity)
    {
        if (!entity.WageTypeId.HasValue)
        {
            throw new UserFriendlyException(L("LoanTypeDetailWageTypeRequired"));
        }

        var duplicate = await _repository.GetAll()
            .AnyAsync(d => d.Id != entity.Id
                           && d.LoanTypeId == entity.LoanTypeId
                           && d.WageTypeId == entity.WageTypeId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("LoanTypeDetailWageTypeAlreadyListed"));
        }

        if (entity.Percent <= 0 || entity.Percent > 100)
        {
            throw new UserFriendlyException(L("LoanTypeDetailPercentOutOfRange"));
        }
    }

    private static LoanTypeDetailDto ToDto(LoanTypeDetail entity)
    {
        return new LoanTypeDetailDto
        {
            Id = entity.Id,
            LoanTypeId = entity.LoanTypeId,
            LoanTypeName = entity.LoanType?.NameEn,
            WageTypeId = entity.WageTypeId,
            WageTypeName = entity.WageType == null ? null : entity.WageType.Code + " - " + entity.WageType.NameEn,
            Percent = entity.Percent
        };
    }

    private static CreateOrEditLoanTypeDetailDto ToCreateOrEditDto(LoanTypeDetail entity)
    {
        return new CreateOrEditLoanTypeDetailDto
        {
            Id = entity.Id,
            LoanTypeId = entity.LoanTypeId,
            WageTypeId = entity.WageTypeId,
            Percent = entity.Percent
        };
    }
}
