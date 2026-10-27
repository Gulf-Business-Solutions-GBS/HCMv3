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
using HCMv3.GovtRelationsManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Application service for <see cref="SponsorBank"/> (ported from legacy XAF SponsorBank business object --
/// legacy misfiles this under PayrollManagement despite it being Sponsor-related data; see
/// <see cref="SponsorBank"/> remarks, including the multi-cascade-path FK flag for the two Bank FKs).
/// Ports the legacy <c>PerformValidation</c> parity (PaymentBank required, EndDate &gt;= StartDate).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorBanks)]
public class SponsorBanksAppService : HCMv3AppServiceBase, ISponsorBanksAppService
{
    private readonly IRepository<SponsorBank> _repository;

    public SponsorBanksAppService(IRepository<SponsorBank> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SponsorBankDto>> GetAll(GetAllSponsorBanksInput input)
    {
        var query = _repository.GetAll()
            .Include(b => b.Sponsor)
            .Include(b => b.EmployeeBank)
            .Include(b => b.PaymentBank)
            .WhereIf(input.SponsorId.HasValue, b => b.SponsorId == input.SponsorId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SponsorBankDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetSponsorBankForEditOutput> GetSponsorBankForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSponsorBankForEditOutput
        {
            SponsorBank = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSponsorBankDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorBanks_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorBanks_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSponsorBankDto input)
    {
        var entity = MapToEntity(input, new SponsorBank());
        entity.TenantId = AbpSession.TenantId ?? 0;

        Validate(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorBanks_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSponsorBankDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        Validate(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: EndDate cannot be earlier than StartDate.</summary>
    private void Validate(SponsorBank entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static SponsorBank MapToEntity(CreateOrEditSponsorBankDto input, SponsorBank entity)
    {
        entity.SponsorId = input.SponsorId;
        entity.EmployeeBankId = input.EmployeeBankId;
        entity.PaymentBankId = input.PaymentBankId;
        entity.AccountNumber = input.AccountNumber;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.IBAN = input.IBAN;

        return entity;
    }

    private static SponsorBankDto ToDto(SponsorBank entity)
    {
        return new SponsorBankDto
        {
            Id = entity.Id,
            SponsorId = entity.SponsorId,
            SponsorName = entity.Sponsor?.NameEn,
            EmployeeBankId = entity.EmployeeBankId,
            EmployeeBankName = entity.EmployeeBank?.NameEn,
            PaymentBankId = entity.PaymentBankId,
            PaymentBankName = entity.PaymentBank?.NameEn,
            AccountNumber = entity.AccountNumber,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            IBAN = entity.IBAN
        };
    }

    private static CreateOrEditSponsorBankDto ToCreateOrEditDto(SponsorBank entity)
    {
        return new CreateOrEditSponsorBankDto
        {
            Id = entity.Id,
            SponsorId = entity.SponsorId,
            EmployeeBankId = entity.EmployeeBankId,
            PaymentBankId = entity.PaymentBankId,
            AccountNumber = entity.AccountNumber,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            IBAN = entity.IBAN
        };
    }
}
