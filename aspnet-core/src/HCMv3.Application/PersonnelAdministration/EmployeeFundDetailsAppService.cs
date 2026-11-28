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
/// Application service for <see cref="EmployeeFundDetail"/> -- the "Fund Detail" tab of legacy (LHRMS) Fund.
/// Legacy has no validation on the row itself (its <c>PerformValidation</c> is empty).
///
/// Every save and delete re-totals the parent <see cref="EmployeeFund"/> (legacy
/// <c>EmployeeFundDetail.UpdateDueBalance</c>, run on saving and on deleting): Amount, Paid Amount and Balance
/// Amount by <see cref="EmployeeFundTotals"/>. When a row is moved to another fund, both funds are re-totalled.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFundDetails)]
public class EmployeeFundDetailsAppService : HCMv3AppServiceBase, IEmployeeFundDetailsAppService
{
    private readonly IRepository<EmployeeFundDetail> _repository;
    private readonly IRepository<EmployeeFund> _fundRepository;

    public EmployeeFundDetailsAppService(
        IRepository<EmployeeFundDetail> repository,
        IRepository<EmployeeFund> fundRepository)
    {
        _repository = repository;
        _fundRepository = fundRepository;
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
        await RetotalFundAsync(entity.EmployeeFundId);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFundDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeFundDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        var previousFundId = entity.EmployeeFundId;
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
        await RetotalFundAsync(entity.EmployeeFundId);

        if (previousFundId != entity.EmployeeFundId)
        {
            await RetotalFundAsync(previousFundId);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFundDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        await _repository.DeleteAsync(entity);
        await RetotalFundAsync(entity.EmployeeFundId);
    }

    /// <summary>Legacy <c>UpdateDueBalance</c>: the fund's totals from the rows it has once this change is stored.</summary>
    private async Task RetotalFundAsync(int employeeFundId)
    {
        await CurrentUnitOfWork.SaveChangesAsync();

        var fund = await _fundRepository.FirstOrDefaultAsync(employeeFundId);
        if (fund == null)
        {
            return;
        }

        var details = await _repository.GetAll()
            .Where(d => d.EmployeeFundId == employeeFundId)
            .ToListAsync();

        EmployeeFundTotals.Apply(fund, details);
        await _fundRepository.UpdateAsync(fund);
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
