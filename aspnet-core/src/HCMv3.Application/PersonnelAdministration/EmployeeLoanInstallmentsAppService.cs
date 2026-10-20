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
/// The instalment schedule of an employee loan (<see cref="EmployeeLoanInstallment"/>), edited from inside the
/// loan as legacy did. A row is part of the loan, so the loan's own permissions govern it: seeing the loan lets you
/// see its schedule, and changing the schedule is changing the loan.
///
/// <para>
/// Everything that happens around a save -- the rules, the end-date chaining, the loan's recovery end date and the
/// payroll retro date -- is <see cref="IEmployeeLoanInstallmentService"/>'s, shared with loan approval and rejoin.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoans)]
public class EmployeeLoanInstallmentsAppService : HCMv3AppServiceBase, IEmployeeLoanInstallmentsAppService
{
    private readonly IRepository<EmployeeLoanInstallment> _repository;
    private readonly IEmployeeLoanInstallmentService _installmentService;

    public EmployeeLoanInstallmentsAppService(
        IRepository<EmployeeLoanInstallment> repository,
        IEmployeeLoanInstallmentService installmentService)
    {
        _repository = repository;
        _installmentService = installmentService;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeLoanInstallmentDto>> GetAll(GetAllEmployeeLoanInstallmentsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(input.EmployeeLoanId.HasValue, r => r.EmployeeLoanId == input.EmployeeLoanId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeLoanInstallmentDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeLoanInstallmentForEditOutput> GetEmployeeLoanInstallmentForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeLoanInstallmentForEditOutput
        {
            EmployeeLoanInstallment = new CreateOrEditEmployeeLoanInstallmentDto
            {
                Id = entity.Id,
                EmployeeLoanId = entity.EmployeeLoanId,
                StartDate = entity.StartDate,
                Amount = entity.Amount,
                Remark = entity.Remark
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeLoanInstallmentDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoans_Edit)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeLoanInstallmentDto input)
    {
        var entity = MapToEntity(input, new EmployeeLoanInstallment());
        entity.EmployeeLoanId = input.EmployeeLoanId;
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _installmentService.CreateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoans_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeLoanInstallmentDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        var previousStartDate = entity.StartDate;
        var previousAmount = entity.Amount;

        // The loan a row belongs to is fixed: the screen edits a row from inside its loan.
        MapToEntity(input, entity);

        await _installmentService.UpdateAsync(entity, previousStartDate, previousAmount);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoans_Edit)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.FirstOrDefaultAsync(r => r.Id == input.Id);
        if (entity == null)
        {
            return;
        }

        await _installmentService.DeleteAsync(entity);
    }

    private static EmployeeLoanInstallment MapToEntity(CreateOrEditEmployeeLoanInstallmentDto input, EmployeeLoanInstallment entity)
    {
        entity.StartDate = input.StartDate.Date;
        entity.Amount = input.Amount;
        entity.Remark = input.Remark;
        return entity;
    }

    private static EmployeeLoanInstallmentDto ToDto(EmployeeLoanInstallment entity)
    {
        return new EmployeeLoanInstallmentDto
        {
            Id = entity.Id,
            EmployeeLoanId = entity.EmployeeLoanId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Amount = entity.Amount,
            Remark = entity.Remark
        };
    }
}
