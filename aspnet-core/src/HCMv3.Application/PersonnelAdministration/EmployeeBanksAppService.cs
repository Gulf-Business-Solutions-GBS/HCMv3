using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeBank"/> (ported from legacy XAF EmployeeBank business
/// object, Wave 4 deferred bucket). Ports the essential active validation only: Employee/StartDate
/// required, Bank required when PaymentType == Bank, and no duplicate StartDate per employee. The legacy
/// auto-EndDate cascade is NOT ported -- see class remarks on <see cref="EmployeeBank"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBanks)]
public class EmployeeBanksAppService : HCMv3AppServiceBase, IEmployeeBanksAppService
{
    private readonly IRepository<EmployeeBank> _repository;

    public EmployeeBanksAppService(IRepository<EmployeeBank> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeBankDto>> GetAll(GetAllEmployeeBanksInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.Bank)
            .Include(e => e.BankCountry)
            .Include(e => e.PaymentCurrency)
            .Include(e => e.PayerBank)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeBankDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeBankForEditOutput> GetEmployeeBankForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeBankForEditOutput
        {
            EmployeeBank = new CreateOrEditEmployeeBankDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                BankId = entity.BankId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                BankCountryId = entity.BankCountryId,
                PaymentType = entity.PaymentType,
                AccountNum = entity.AccountNum,
                PaymentCurrencyId = entity.PaymentCurrencyId,
                IBAN = entity.IBAN,
                BankBranch = entity.BankBranch,
                PayerBankId = entity.PayerBankId
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeBankDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBanks_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeBankDto input)
    {
        var entity = MapToEntity(input, new EmployeeBank());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateEntityAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBanks_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeBankDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateEntityAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBanks_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateEntityAsync(EmployeeBank entity)
    {
        if (entity.PaymentType == PaymentType.Bank && entity.BankId == null)
        {
            throw new UserFriendlyException(L("EmployeeBankBankRequiredForBankPaymentType"));
        }

        var hasDuplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.EmployeeId == entity.EmployeeId && e.StartDate == entity.StartDate);
        if (hasDuplicate)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    private static EmployeeBank MapToEntity(CreateOrEditEmployeeBankDto input, EmployeeBank entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.BankId = input.BankId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.BankCountryId = input.BankCountryId;
        entity.PaymentType = input.PaymentType;
        entity.AccountNum = input.AccountNum;
        entity.PaymentCurrencyId = input.PaymentCurrencyId;
        entity.IBAN = input.IBAN;
        entity.BankBranch = input.BankBranch;
        entity.PayerBankId = input.PayerBankId;
        return entity;
    }

    private static EmployeeBankDto ToDto(EmployeeBank entity)
    {
        return new EmployeeBankDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            BankId = entity.BankId,
            BankNameEn = entity.Bank?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            BankCountryId = entity.BankCountryId,
            BankCountryNameEn = entity.BankCountry?.NameEn,
            PaymentType = entity.PaymentType,
            AccountNum = entity.AccountNum,
            PaymentCurrencyId = entity.PaymentCurrencyId,
            PaymentCurrencyNameEn = entity.PaymentCurrency?.NameEn,
            IBAN = entity.IBAN,
            BankBranch = entity.BankBranch,
            PayerBankId = entity.PayerBankId,
            PayerBankNameEn = entity.PayerBank?.NameEn
        };
    }
}
