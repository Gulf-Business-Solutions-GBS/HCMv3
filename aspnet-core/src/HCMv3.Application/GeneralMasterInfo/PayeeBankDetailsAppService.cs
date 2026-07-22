using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the PayeeBankDetail detail record (ported from legacy XAF PayeeBankDetail).
/// Pure detail record (no Code/NameEn/NameAr): plain CRUD without auto-code or duplicate validation.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_PayeeBankDetails)]
public class PayeeBankDetailsAppService : HCMv3AppServiceBase, IPayeeBankDetailsAppService
{
    private readonly IRepository<PayeeBankDetail> _payeeBankDetailRepository;

    public PayeeBankDetailsAppService(IRepository<PayeeBankDetail> payeeBankDetailRepository)
    {
        _payeeBankDetailRepository = payeeBankDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PayeeBankDetailDto>> GetAll(GetAllPayeeBankDetailsInput input)
    {
        input.Normalize();

        var query = _payeeBankDetailRepository.GetAll()
            .Include(p => p.Bank)
            .Include(p => p.PayeeBank)
            .Include(p => p.BankFile);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PayeeBankDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetPayeeBankDetailForEditOutput> GetPayeeBankDetailForEdit(EntityDto input)
    {
        var entity = await _payeeBankDetailRepository.GetAsync(input.Id);

        return new GetPayeeBankDetailForEditOutput
        {
            PayeeBankDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditPayeeBankDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_PayeeBankDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPayeeBankDetailDto input)
    {
        var entity = new PayeeBankDetail
        {
            BankId = input.BankId,
            PayeeBankId = input.PayeeBankId,
            BankCode = input.BankCode,
            BankFileId = input.BankFileId,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _payeeBankDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_PayeeBankDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPayeeBankDetailDto input)
    {
        var entity = await _payeeBankDetailRepository.GetAsync(input.Id.Value);

        entity.BankId = input.BankId;
        entity.PayeeBankId = input.PayeeBankId;
        entity.BankCode = input.BankCode;
        entity.BankFileId = input.BankFileId;

        await _payeeBankDetailRepository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_PayeeBankDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _payeeBankDetailRepository.DeleteAsync(input.Id);
    }

    private static PayeeBankDetailDto ToDto(PayeeBankDetail entity)
    {
        return new PayeeBankDetailDto
        {
            Id = entity.Id,
            BankId = entity.BankId,
            BankName = entity.Bank?.NameEn,
            PayeeBankId = entity.PayeeBankId,
            PayeeBankName = entity.PayeeBank?.NameEn,
            BankCode = entity.BankCode,
            BankFileId = entity.BankFileId,
            BankFileName = entity.BankFile?.NameEn
        };
    }

    private static CreateOrEditPayeeBankDetailDto ToCreateOrEditDto(PayeeBankDetail entity)
    {
        return new CreateOrEditPayeeBankDetailDto
        {
            Id = entity.Id,
            BankId = entity.BankId,
            PayeeBankId = entity.PayeeBankId,
            BankCode = entity.BankCode,
            BankFileId = entity.BankFileId
        };
    }
}
