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
/// Application service for <see cref="EmployeePayrollStatus"/> (ported from legacy XAF
/// <c>EmpPayrollStatus</c>/<c>EmployeePayrollStatus</c> business object -- see <see cref="EmployeePayrollStatus"/>
/// class remarks for the legacy file-name/class-name mismatch note). The legacy one-row-per-Employee rule
/// (<c>this.Oid == -1</c> check, i.e. only on insert) is ported as create-only in <see cref="ValidateAsync"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeePayrollStatuses)]
public class EmployeePayrollStatusesAppService : HCMv3AppServiceBase, IEmployeePayrollStatusesAppService
{
    private readonly IRepository<EmployeePayrollStatus> _repository;

    public EmployeePayrollStatusesAppService(IRepository<EmployeePayrollStatus> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeePayrollStatusDto>> GetAll(GetAllEmployeePayrollStatusesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(s => s.Employee)
            .WhereIf(input.EmployeeId.HasValue, s => s.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeePayrollStatusDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeePayrollStatusForEditOutput> GetEmployeePayrollStatusForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeePayrollStatusForEditOutput
        {
            EmployeePayrollStatus = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeePayrollStatusDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeePayrollStatuses_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeePayrollStatuses_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeePayrollStatusDto input)
    {
        var entity = MapToEntity(input, new EmployeePayrollStatus());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity, isCreate: true);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeePayrollStatuses_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeePayrollStatusDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity, isCreate: false);

        await _repository.UpdateAsync(entity);
    }

    private static EmployeePayrollStatus MapToEntity(CreateOrEditEmployeePayrollStatusDto input, EmployeePayrollStatus entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.EarliestRetroAccountingDate = input.EarliestRetroAccountingDate;
        entity.AccountedTo = input.AccountedTo;
        entity.RunPayrollUpto = input.RunPayrollUpto;
        entity.EarliestMasterDataChange = input.EarliestMasterDataChange;
        entity.DoNoAccountAfter = input.DoNoAccountAfter;
        entity.PersonLocked = input.PersonLocked;
        entity.PayrollCorrection = input.PayrollCorrection;
        entity.EarliestRecalculationDate = input.EarliestRecalculationDate;
        entity.PersonalCalendarFrom = input.PersonalCalendarFrom;
        entity.NextTimeEvaluationFrom = input.NextTimeEvaluationFrom;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: exactly one <see cref="EmployeePayrollStatus"/> row is
    /// allowed per Employee, enforced ONLY on create (legacy checks <c>this.Oid == -1</c>, i.e. new/unsaved
    /// records only, so an existing row can still be edited without tripping the uniqueness check).
    /// </summary>
    private async Task ValidateAsync(EmployeePayrollStatus entity, bool isCreate)
    {
        if (!isCreate)
        {
            return;
        }

        var alreadyExists = await _repository.GetAll()
            .AnyAsync(s => s.EmployeeId == entity.EmployeeId);
        if (alreadyExists)
        {
            throw new UserFriendlyException(L("EmployeeCanHaveOnlyOnePayrollStatus"));
        }
    }

    private static EmployeePayrollStatusDto ToDto(EmployeePayrollStatus entity)
    {
        return new EmployeePayrollStatusDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeName = entity.Employee?.FullNameEn,
            EarliestRetroAccountingDate = entity.EarliestRetroAccountingDate,
            AccountedTo = entity.AccountedTo,
            RunPayrollUpto = entity.RunPayrollUpto,
            EarliestMasterDataChange = entity.EarliestMasterDataChange,
            DoNoAccountAfter = entity.DoNoAccountAfter,
            PersonLocked = entity.PersonLocked,
            PayrollCorrection = entity.PayrollCorrection,
            EarliestRecalculationDate = entity.EarliestRecalculationDate,
            PersonalCalendarFrom = entity.PersonalCalendarFrom,
            NextTimeEvaluationFrom = entity.NextTimeEvaluationFrom
        };
    }

    private static CreateOrEditEmployeePayrollStatusDto ToCreateOrEditDto(EmployeePayrollStatus entity)
    {
        return new CreateOrEditEmployeePayrollStatusDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EarliestRetroAccountingDate = entity.EarliestRetroAccountingDate,
            AccountedTo = entity.AccountedTo,
            RunPayrollUpto = entity.RunPayrollUpto,
            EarliestMasterDataChange = entity.EarliestMasterDataChange,
            DoNoAccountAfter = entity.DoNoAccountAfter,
            PersonLocked = entity.PersonLocked,
            PayrollCorrection = entity.PayrollCorrection,
            EarliestRecalculationDate = entity.EarliestRecalculationDate,
            PersonalCalendarFrom = entity.PersonalCalendarFrom,
            NextTimeEvaluationFrom = entity.NextTimeEvaluationFrom
        };
    }
}
