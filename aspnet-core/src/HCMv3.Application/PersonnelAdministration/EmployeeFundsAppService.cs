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
/// Application service for <see cref="EmployeeFund"/> (ported from legacy XAF EmployeeFund business object,
/// Wave 2 deferred bucket). Ports the essential active validation only (EndDate not earlier than StartDate).
/// The legacy cascade that recomputes Amount/PaidAmount/BalanceAmount from detail rows is NOT ported -- see
/// class remarks on <see cref="EmployeeFund"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFunds)]
public class EmployeeFundsAppService : HCMv3AppServiceBase, IEmployeeFundsAppService
{
    private readonly IRepository<EmployeeFund> _repository;

    public EmployeeFundsAppService(IRepository<EmployeeFund> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeFundDto>> GetAll(GetAllEmployeeFundsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(f => f.Employee)
            .WhereIf(input.EmployeeId.HasValue, f => f.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeFundDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeFundForEditOutput> GetEmployeeFundForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeFundForEditOutput
        {
            EmployeeFund = new CreateOrEditEmployeeFundDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeFundDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFunds_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeFundDto input)
    {
        var entity = MapToEntity(input, new EmployeeFund());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateEntity(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFunds_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeFundDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        ValidateEntity(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFunds_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private void ValidateEntity(EmployeeFund entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static EmployeeFund MapToEntity(CreateOrEditEmployeeFundDto input, EmployeeFund entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeFundDto ToDto(EmployeeFund entity)
    {
        return new EmployeeFundDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Amount = entity.Amount,
            PaidAmount = entity.PaidAmount,
            BalanceAmount = entity.BalanceAmount,
            Comments = entity.Comments
        };
    }
}
