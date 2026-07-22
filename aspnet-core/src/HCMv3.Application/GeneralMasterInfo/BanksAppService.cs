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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the Bank master (ported from legacy XAF Bank business object).
/// Entity/DTO mapping is done manually (not via Mapperly) since the list DTO also needs the FK's
/// display name (BankFileName) resolved. Mirrors <c>CitiesAppService</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Banks)]
public class BanksAppService : HCMv3AppServiceBase, IBanksAppService
{
    private readonly IRepository<Bank> _bankRepository;

    public BanksAppService(IRepository<Bank> bankRepository)
    {
        _bankRepository = bankRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<BankDto>> GetAll(GetAllBanksInput input)
    {
        input.Normalize();

        var query = _bankRepository.GetAll()
            .Include(b => b.BankFile)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), b =>
                b.Code.Contains(input.Filter) ||
                b.NameEn.Contains(input.Filter) ||
                b.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<BankDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetBankForEditOutput> GetBankForEdit(EntityDto input)
    {
        var bank = await _bankRepository.GetAsync(input.Id);

        return new GetBankForEditOutput
        {
            Bank = ToCreateOrEditDto(bank)
        };
    }

    public async Task CreateOrEdit(CreateOrEditBankDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Banks_Create)]
    protected virtual async Task CreateAsync(CreateOrEditBankDto input)
    {
        var bank = new Bank
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            ExternalCode = input.ExternalCode,
            SWIFTCode = input.SWIFTCode,
            CompanyCode = input.CompanyCode,
            BankFileId = input.BankFileId,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(bank);
        await ValidateUniquenessAsync(bank);

        await _bankRepository.InsertAsync(bank);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Banks_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditBankDto input)
    {
        var bank = await _bankRepository.GetAsync(input.Id.Value);

        bank.Code = input.Code;
        bank.NameEn = input.NameEn;
        bank.NameAr = input.NameAr;
        bank.ExternalCode = input.ExternalCode;
        bank.SWIFTCode = input.SWIFTCode;
        bank.CompanyCode = input.CompanyCode;
        bank.BankFileId = input.BankFileId;

        await AssignCodeIfEmptyAsync(bank);
        await ValidateUniquenessAsync(bank);

        await _bankRepository.UpdateAsync(bank);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Banks_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _bankRepository.DeleteAsync(input.Id);
    }

    private async Task AssignCodeIfEmptyAsync(Bank bank)
    {
        if (!bank.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _bankRepository.GetAll()
            .Where(b => b.Code != null && b.Code != "")
            .Select(b => b.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        bank.Code = (maxCode + 1).ToString();
    }

    private async Task ValidateUniquenessAsync(Bank bank)
    {
        var duplicateCode = await _bankRepository.GetAll()
            .AnyAsync(b => b.Id != bank.Id && b.Code == bank.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _bankRepository.GetAll()
            .AnyAsync(b => b.Id != bank.Id &&
                           (b.NameEn == bank.NameEn || b.NameAr == bank.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static BankDto ToDto(Bank bank)
    {
        return new BankDto
        {
            Id = bank.Id,
            Code = bank.Code,
            NameEn = bank.NameEn,
            NameAr = bank.NameAr,
            ExternalCode = bank.ExternalCode,
            SWIFTCode = bank.SWIFTCode,
            CompanyCode = bank.CompanyCode,
            BankFileId = bank.BankFileId,
            BankFileName = bank.BankFile?.NameEn
        };
    }

    private static CreateOrEditBankDto ToCreateOrEditDto(Bank bank)
    {
        return new CreateOrEditBankDto
        {
            Id = bank.Id,
            Code = bank.Code,
            NameEn = bank.NameEn,
            NameAr = bank.NameAr,
            ExternalCode = bank.ExternalCode,
            SWIFTCode = bank.SWIFTCode,
            CompanyCode = bank.CompanyCode,
            BankFileId = bank.BankFileId
        };
    }
}
