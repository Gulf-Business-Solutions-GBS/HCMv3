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
/// Application service for <see cref="EmployeeHealthReimburse"/> (ported from legacy XAF
/// EmployeeHealthReimburse business object, Wave 4 deferred bucket). Ports the essential active validation
/// only: required fields and Amount != 0. The legacy quota/carry-forward enforcement is NOT ported -- see
/// class remarks on <see cref="EmployeeHealthReimburse"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeHealthReimburses)]
public class EmployeeHealthReimbursesAppService : HCMv3AppServiceBase, IEmployeeHealthReimbursesAppService
{
    private readonly IRepository<EmployeeHealthReimburse> _repository;

    public EmployeeHealthReimbursesAppService(IRepository<EmployeeHealthReimburse> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeHealthReimburseDto>> GetAll(GetAllEmployeeHealthReimbursesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeHealthReimburseDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeHealthReimburseForEditOutput> GetEmployeeHealthReimburseForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeHealthReimburseForEditOutput
        {
            EmployeeHealthReimburse = new CreateOrEditEmployeeHealthReimburseDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                PaymentDate = entity.PaymentDate,
                Amount = entity.Amount,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeHealthReimburseDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeHealthReimburses_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeHealthReimburseDto input)
    {
        var entity = MapToEntity(input, new EmployeeHealthReimburse());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateEntity(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeHealthReimburses_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeHealthReimburseDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        ValidateEntity(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeHealthReimburses_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private void ValidateEntity(EmployeeHealthReimburse entity)
    {
        if (entity.Amount == 0)
        {
            throw new UserFriendlyException(L("AmountCannotBeZero"));
        }
    }

    private static EmployeeHealthReimburse MapToEntity(CreateOrEditEmployeeHealthReimburseDto input, EmployeeHealthReimburse entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.PaymentDate = input.PaymentDate.Date;
        entity.Amount = input.Amount;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeHealthReimburseDto ToDto(EmployeeHealthReimburse entity)
    {
        return new EmployeeHealthReimburseDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            PaymentDate = entity.PaymentDate,
            Amount = entity.Amount,
            Comments = entity.Comments
        };
    }
}
