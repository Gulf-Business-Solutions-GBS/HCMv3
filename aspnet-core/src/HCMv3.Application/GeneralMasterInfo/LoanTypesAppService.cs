using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PayrollManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the LoanType master (ported from legacy XAF LoanType).
/// Mirrors <c>CitiesAppService</c>. The aggregated <c>LoanTypeDetail</c> collection is ported separately as
/// its own entity (<see cref="LoanTypeDetail"/>). Wave P9 restored the legacy <c>WageType</c> FK, which the
/// Payroll Engine dispatches wage code 2001 "Loan Recovery Deduction" through.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_LoanTypes)]
public class LoanTypesAppService : HCMv3AppServiceBase, ILoanTypesAppService
{
    /// <summary>Net Pay: never a deduction a loan recovers through (legacy <c>r.Code != "9000"</c>).</summary>
    private const string NetPayWageTypeCode = "9000";

    private readonly IRepository<LoanType> _repository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<OvertimeType> _overtimeTypeRepository;
    private readonly IRepository<DisciplinaryType> _disciplinaryTypeRepository;

    public LoanTypesAppService(
        IRepository<LoanType> repository,
        IRepository<WageType> wageTypeRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<OvertimeType> overtimeTypeRepository,
        IRepository<DisciplinaryType> disciplinaryTypeRepository)
    {
        _repository = repository;
        _wageTypeRepository = wageTypeRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _overtimeTypeRepository = overtimeTypeRepository;
        _disciplinaryTypeRepository = disciplinaryTypeRepository;
    }

    /// <summary>
    /// Legacy <c>LoanType.LoanDedWageType</c>: active deduction wage types that are not part of basic, calculated
    /// or arrear pay, not Net Pay, and not already used by a leave, overtime or violation type.
    /// </summary>
    public async Task<ListResultDto<LoanDeductionWageTypeDto>> GetDeductionWageTypes(NullableIdDto input)
    {
        var leaveWageTypeIds = _absenceTypeRepository.GetAll()
            .Where(t => t.WageTypeId != null)
            .Select(t => t.WageTypeId.Value);
        var overtimeWageTypeIds = _overtimeTypeRepository.GetAll()
            .Where(t => t.WageTypeId != null)
            .Select(t => t.WageTypeId.Value);
        var violationWageTypeIds = _disciplinaryTypeRepository.GetAll()
            .Where(t => t.WageTypeId != null)
            .Select(t => t.WageTypeId.Value);

        var items = await _wageTypeRepository.GetAll()
            .Where(w => w.Id == input.Id
                        || (w.Status
                            && w.BillingType == BillingType.Deduction
                            && !w.IncludeInBasicPay
                            && !w.IncludeInCalculatedPay
                            && !w.IncludeInArrearPay
                            && w.Code != NetPayWageTypeCode
                            && !leaveWageTypeIds.Contains(w.Id)
                            && !overtimeWageTypeIds.Contains(w.Id)
                            && !violationWageTypeIds.Contains(w.Id)))
            .OrderBy(w => w.Code)
            .Select(w => new LoanDeductionWageTypeDto
            {
                Id = w.Id,
                Code = w.Code,
                NameEn = w.NameEn,
                NameAr = w.NameAr
            })
            .ToListAsync();

        return new ListResultDto<LoanDeductionWageTypeDto>(items);
    }

    [HttpPost]
    public async Task<PagedResultDto<LoanTypeDto>> GetAll(GetAllLoanTypesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(t => t.WageType)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t =>
                t.Code.Contains(input.Filter) ||
                t.NameEn.Contains(input.Filter) ||
                t.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<LoanTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetLoanTypeForEditOutput> GetLoanTypeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetLoanTypeForEditOutput
        {
            LoanType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditLoanTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_LoanTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditLoanTypeDto input)
    {
        var entity = new LoanType
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            LoanDeductionType = input.LoanDeductionType,
            RecoveryOption = input.RecoveryOption,
            EligiblePeriod = input.EligiblePeriod,
            EligibleCountPerYear = input.EligibleCountPerYear,
            MaxInstallments = input.MaxInstallments,
            EligibleAmount = input.EligibleAmount,
            EligibleAmountNumber = input.EligibleAmountNumber,
            WageTypeId = input.WageTypeId,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_LoanTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditLoanTypeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.LoanDeductionType = input.LoanDeductionType;
        entity.RecoveryOption = input.RecoveryOption;
        entity.EligiblePeriod = input.EligiblePeriod;
        entity.EligibleCountPerYear = input.EligibleCountPerYear;
        entity.MaxInstallments = input.MaxInstallments;
        entity.EligibleAmount = input.EligibleAmount;
        entity.EligibleAmountNumber = input.EligibleAmountNumber;
        entity.WageTypeId = input.WageTypeId;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_LoanTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task AssignCodeIfEmptyAsync(LoanType entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(t => t.Code != null && t.Code != "")
            .Select(t => t.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    private async Task ValidateUniquenessAsync(LoanType entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(t => t.Id != entity.Id && t.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(t => t.Id != entity.Id &&
                           (t.NameEn == entity.NameEn || t.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static LoanTypeDto ToDto(LoanType entity)
    {
        return new LoanTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            LoanDeductionType = entity.LoanDeductionType,
            RecoveryOption = entity.RecoveryOption,
            EligiblePeriod = entity.EligiblePeriod,
            EligibleCountPerYear = entity.EligibleCountPerYear,
            MaxInstallments = entity.MaxInstallments,
            EligibleAmount = entity.EligibleAmount,
            EligibleAmountNumber = entity.EligibleAmountNumber,
            WageTypeId = entity.WageTypeId,
            WageTypeCode = entity.WageType != null ? entity.WageType.Code : null,
            WageTypeNameEn = entity.WageType != null ? entity.WageType.NameEn : null
        };
    }

    private static CreateOrEditLoanTypeDto ToCreateOrEditDto(LoanType entity)
    {
        return new CreateOrEditLoanTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            LoanDeductionType = entity.LoanDeductionType,
            RecoveryOption = entity.RecoveryOption,
            EligiblePeriod = entity.EligiblePeriod,
            EligibleCountPerYear = entity.EligibleCountPerYear,
            MaxInstallments = entity.MaxInstallments,
            EligibleAmount = entity.EligibleAmount,
            EligibleAmountNumber = entity.EligibleAmountNumber,
            WageTypeId = entity.WageTypeId
        };
    }
}
