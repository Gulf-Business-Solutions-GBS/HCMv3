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
using HCMv3.OrganizationManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Application service for the Organization Bank Info record (ported from legacy XAF
/// OrganizationBankInfo business object). No auto-code/duplicate-name validation applies (there's no
/// Code/NameEn/NameAr) — just the legacy required-field checks for OrganizationUnit/Bank/AccountNo.
/// Entity/DTO mapping is done manually (not via Mapperly) since the list DTO also needs the FKs'
/// display names resolved.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_OrganizationManagement_OrganizationBankInfos)]
public class OrganizationBankInfosAppService : HCMv3AppServiceBase, IOrganizationBankInfosAppService
{
    private readonly IRepository<OrganizationBankInfo> _organizationBankInfoRepository;

    public OrganizationBankInfosAppService(IRepository<OrganizationBankInfo> organizationBankInfoRepository)
    {
        _organizationBankInfoRepository = organizationBankInfoRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<OrganizationBankInfoDto>> GetAll(GetAllOrganizationBankInfosInput input)
    {
        input.Normalize();

        var query = _organizationBankInfoRepository.GetAll()
            .Include(i => i.OrganizationUnit)
            .Include(i => i.Bank)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), i =>
                i.AccountNo.Contains(input.Filter) ||
                (i.SC_Code != null && i.SC_Code.Contains(input.Filter)));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<OrganizationBankInfoDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetOrganizationBankInfoForEditOutput> GetOrganizationBankInfoForEdit(EntityDto input)
    {
        var organizationBankInfo = await _organizationBankInfoRepository.GetAsync(input.Id);

        return new GetOrganizationBankInfoForEditOutput
        {
            OrganizationBankInfo = ToCreateOrEditDto(organizationBankInfo)
        };
    }

    public async Task CreateOrEdit(CreateOrEditOrganizationBankInfoDto input)
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

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_OrganizationBankInfos_Create)]
    protected virtual async Task CreateAsync(CreateOrEditOrganizationBankInfoDto input)
    {
        var organizationBankInfo = new OrganizationBankInfo
        {
            OrganizationUnitId = input.OrganizationUnitId,
            BankId = input.BankId,
            AccountNo = input.AccountNo,
            SC_Code = input.SC_Code,
            TenantId = AbpSession.TenantId ?? 0
        };

        ValidateRequiredFields(organizationBankInfo);

        await _organizationBankInfoRepository.InsertAsync(organizationBankInfo);
    }

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_OrganizationBankInfos_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditOrganizationBankInfoDto input)
    {
        var organizationBankInfo = await _organizationBankInfoRepository.GetAsync(input.Id.Value);

        organizationBankInfo.OrganizationUnitId = input.OrganizationUnitId;
        organizationBankInfo.BankId = input.BankId;
        organizationBankInfo.AccountNo = input.AccountNo;
        organizationBankInfo.SC_Code = input.SC_Code;

        ValidateRequiredFields(organizationBankInfo);

        await _organizationBankInfoRepository.UpdateAsync(organizationBankInfo);
    }

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_OrganizationBankInfos_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _organizationBankInfoRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: OrganizationUnit, Bank, and AccountNo are all required.
    /// </summary>
    private void ValidateRequiredFields(OrganizationBankInfo organizationBankInfo)
    {
        if (organizationBankInfo.OrganizationUnitId == null)
        {
            throw new UserFriendlyException(L("OrganizationUnitCannotBeBlank"));
        }

        if (organizationBankInfo.BankId == null)
        {
            throw new UserFriendlyException(L("BankCannotBeBlank"));
        }

        if (organizationBankInfo.AccountNo.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("AccountNoCannotBeBlank"));
        }
    }

    private static OrganizationBankInfoDto ToDto(OrganizationBankInfo organizationBankInfo)
    {
        return new OrganizationBankInfoDto
        {
            Id = organizationBankInfo.Id,
            OrganizationUnitId = organizationBankInfo.OrganizationUnitId,
            OrganizationUnitName = organizationBankInfo.OrganizationUnit?.DisplayName,
            BankId = organizationBankInfo.BankId,
            BankName = organizationBankInfo.Bank?.NameEn,
            AccountNo = organizationBankInfo.AccountNo,
            SC_Code = organizationBankInfo.SC_Code
        };
    }

    private static CreateOrEditOrganizationBankInfoDto ToCreateOrEditDto(OrganizationBankInfo organizationBankInfo)
    {
        return new CreateOrEditOrganizationBankInfoDto
        {
            Id = organizationBankInfo.Id,
            OrganizationUnitId = organizationBankInfo.OrganizationUnitId,
            BankId = organizationBankInfo.BankId,
            AccountNo = organizationBankInfo.AccountNo,
            SC_Code = organizationBankInfo.SC_Code
        };
    }
}
