using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeFund"/> -- legacy (LHRMS) Personnel Administration "Fund".
///
/// Amount, Paid Amount and Balance Amount are read-only, as in legacy: they are worked out from the fund's
/// detail rows (<see cref="EmployeeFundTotals"/>) whenever a detail row is saved or deleted
/// (<see cref="EmployeeFundDetailsAppService"/>) and by the payroll run (wage types 23 / 25). Saving the fund
/// itself never takes them from the client. Deleting a fund deletes its detail and exemption rows with it
/// (legacy aggregated collections).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFunds)]
public class EmployeeFundsAppService : HCMv3AppServiceBase, IEmployeeFundsAppService
{
    private readonly IRepository<EmployeeFund> _repository;
    private readonly IRepository<EmployeeFundDetail> _detailRepository;
    private readonly IRepository<EmployeeFundExemption> _exemptionRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public EmployeeFundsAppService(
        IRepository<EmployeeFund> repository,
        IRepository<EmployeeFundDetail> detailRepository,
        IRepository<EmployeeFundExemption> exemptionRepository,
        IRepository<Employee> employeeRepository)
    {
        _repository = repository;
        _detailRepository = detailRepository;
        _exemptionRepository = exemptionRepository;
        _employeeRepository = employeeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeFundDto>> GetAll(GetAllEmployeeFundsInput input)
    {
        input.Normalize();

        var filter = input.Filter?.Trim();

        var query = _repository.GetAll()
            .Include(f => f.Employee)
            .WhereIf(input.EmployeeId.HasValue, f => f.EmployeeId == input.EmployeeId.Value)
            .WhereIf(!filter.IsNullOrWhiteSpace(), f =>
                f.Employee.Code.Contains(filter) ||
                f.Employee.FullNameEn.Contains(filter) ||
                f.Employee.FullNameAr.Contains(filter) ||
                f.Comments.Contains(filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeFundDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeFundForEditOutput> GetEmployeeFundForEdit(EntityDto input)
    {
        var entity = await _repository.GetAll()
            .Include(f => f.Employee)
            .FirstOrDefaultAsync(f => f.Id == input.Id)
            ?? throw new EntityNotFoundException(typeof(EmployeeFund), input.Id);

        return new GetEmployeeFundForEditOutput
        {
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            Amount = entity.Amount,
            PaidAmount = entity.PaidAmount,
            BalanceAmount = entity.BalanceAmount,
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

    public async Task<int> CreateOrEdit(CreateOrEditEmployeeFundDto input)
    {
        if (input.Id.HasValue)
        {
            return await UpdateAsync(input);
        }

        return await CreateAsync(input);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFunds_Create)]
    protected virtual async Task<int> CreateAsync(CreateOrEditEmployeeFundDto input)
    {
        var entity = MapToEntity(input, new EmployeeFund());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateEntity(entity);
        return await _repository.InsertAndGetIdAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFunds_Edit)]
    protected virtual async Task<int> UpdateAsync(CreateOrEditEmployeeFundDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        ValidateEntity(entity);
        await _repository.UpdateAsync(entity);
        return entity.Id;
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFunds_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _detailRepository.DeleteAsync(d => d.EmployeeFundId == input.Id);
        await _exemptionRepository.DeleteAsync(e => e.EmployeeFundId == input.Id);
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Every employee of the tenant, as a small projection. Ordered by code with shorter codes first, so numeric
    /// codes come in number order (2 before 10).
    /// </summary>
    [HttpPost]
    public async Task<ListResultDto<SimpleLookupDto>> GetEmployeesForLookup(GetEmployeeFundEmployeesInput input)
    {
        var filter = input.Filter?.Trim();

        var items = await _employeeRepository.GetAll()
            .WhereIf(!filter.IsNullOrWhiteSpace(), e =>
                e.Code.Contains(filter) ||
                e.FullNameEn.Contains(filter) ||
                e.FullNameAr.Contains(filter))
            .OrderBy(e => e.Code.Length)
            .ThenBy(e => e.Code)
            .Select(e => new SimpleLookupDto
            {
                Id = e.Id,
                Code = e.Code,
                NameEn = e.FullNameEn,
                NameAr = e.FullNameAr
            })
            .ToListAsync();

        return new ListResultDto<SimpleLookupDto>(items);
    }

    /// <summary>Legacy <c>EmployeeFund.PerformValidation</c>.</summary>
    private void ValidateEntity(EmployeeFund entity)
    {
        if (entity.EmployeeId == 0)
        {
            throw new UserFriendlyException(L("EmployeeIsRequired"));
        }

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
