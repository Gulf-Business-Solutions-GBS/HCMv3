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
/// Application service for <see cref="EmployeeFundDetail"/> (ported from legacy XAF EmployeeFundDetail
/// business object, Wave 2 deferred bucket). Records the fund transaction row itself; the legacy source has
/// no active validation (its <c>PerformValidation</c> body is empty) and the cascade that recomputes the
/// parent <see cref="EmployeeFund"/> totals is NOT ported -- see class remarks on
/// <see cref="EmployeeFundDetail"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFundDetails)]
public class EmployeeFundDetailsAppService : HCMv3AppServiceBase, IEmployeeFundDetailsAppService
{
    private readonly IRepository<EmployeeFundDetail> _repository;

    public EmployeeFundDetailsAppService(IRepository<EmployeeFundDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeFundDetailDto>> GetAll(GetAllEmployeeFundDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(input.EmployeeFundId.HasValue, d => d.EmployeeFundId == input.EmployeeFundId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeFundDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeFundDetailForEditOutput> GetEmployeeFundDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeFundDetailForEditOutput
        {
            EmployeeFundDetail = new CreateOrEditEmployeeFundDetailDto
            {
                Id = entity.Id,
                EmployeeFundId = entity.EmployeeFundId,
                TransactionDate = entity.TransactionDate,
                Amount = entity.Amount,
                FundPayType = entity.FundPayType,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeFundDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFundDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeFundDetailDto input)
    {
        var entity = MapToEntity(input, new EmployeeFundDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFundDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeFundDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFundDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeFundDetail MapToEntity(CreateOrEditEmployeeFundDetailDto input, EmployeeFundDetail entity)
    {
        entity.EmployeeFundId = input.EmployeeFundId;
        entity.TransactionDate = input.TransactionDate.Date;
        entity.Amount = input.Amount;
        entity.FundPayType = input.FundPayType;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeFundDetailDto ToDto(EmployeeFundDetail entity)
    {
        return new EmployeeFundDetailDto
        {
            Id = entity.Id,
            EmployeeFundId = entity.EmployeeFundId,
            TransactionDate = entity.TransactionDate,
            Amount = entity.Amount,
            FundPayType = entity.FundPayType,
            Comments = entity.Comments
        };
    }
}
