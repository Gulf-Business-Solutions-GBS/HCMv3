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
/// Application service for <see cref="LoanInstallment"/> (ported from legacy XAF LoanInstallment business
/// object, Wave 2 deferred bucket). The legacy source has no active validation (its
/// <c>PerformValidation</c> call is commented out / dead code), so none is ported here either.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_LoanInstallments)]
public class LoanInstallmentsAppService : HCMv3AppServiceBase, ILoanInstallmentsAppService
{
    private readonly IRepository<LoanInstallment> _repository;

    public LoanInstallmentsAppService(IRepository<LoanInstallment> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<LoanInstallmentDto>> GetAll(GetAllLoanInstallmentsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(input.EmployeeLoanId.HasValue, i => i.EmployeeLoanId == input.EmployeeLoanId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<LoanInstallmentDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetLoanInstallmentForEditOutput> GetLoanInstallmentForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetLoanInstallmentForEditOutput
        {
            LoanInstallment = new CreateOrEditLoanInstallmentDto
            {
                Id = entity.Id,
                EmployeeLoanId = entity.EmployeeLoanId,
                InstallmentDate = entity.InstallmentDate,
                InstallmentAmount = entity.InstallmentAmount,
                InstallmentStatus = entity.InstallmentStatus
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditLoanInstallmentDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_LoanInstallments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditLoanInstallmentDto input)
    {
        var entity = MapToEntity(input, new LoanInstallment());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_LoanInstallments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditLoanInstallmentDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_LoanInstallments_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static LoanInstallment MapToEntity(CreateOrEditLoanInstallmentDto input, LoanInstallment entity)
    {
        entity.EmployeeLoanId = input.EmployeeLoanId;
        entity.InstallmentDate = input.InstallmentDate.Date;
        entity.InstallmentAmount = input.InstallmentAmount;
        entity.InstallmentStatus = input.InstallmentStatus;
        return entity;
    }

    private static LoanInstallmentDto ToDto(LoanInstallment entity)
    {
        return new LoanInstallmentDto
        {
            Id = entity.Id,
            EmployeeLoanId = entity.EmployeeLoanId,
            InstallmentDate = entity.InstallmentDate,
            InstallmentAmount = entity.InstallmentAmount,
            InstallmentStatus = entity.InstallmentStatus
        };
    }
}
