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
/// Application service for the LoanTypeDetail detail record (ported from legacy XAF LoanTypeDetail).
/// Pure detail record (no Code/NameEn/NameAr): plain CRUD without auto-code or duplicate validation.
/// The legacy <c>WageType</c> FK is not ported (Payroll module out of scope).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_LoanTypeDetails)]
public class LoanTypeDetailsAppService : HCMv3AppServiceBase, ILoanTypeDetailsAppService
{
    private readonly IRepository<LoanTypeDetail> _repository;

    public LoanTypeDetailsAppService(IRepository<LoanTypeDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<LoanTypeDetailDto>> GetAll(GetAllLoanTypeDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.LoanType);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<LoanTypeDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetLoanTypeDetailForEditOutput> GetLoanTypeDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetLoanTypeDetailForEditOutput
        {
            LoanTypeDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditLoanTypeDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_LoanTypeDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditLoanTypeDetailDto input)
    {
        var entity = new LoanTypeDetail
        {
            LoanTypeId = input.LoanTypeId,
            Percent = input.Percent,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_LoanTypeDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditLoanTypeDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.LoanTypeId = input.LoanTypeId;
        entity.Percent = input.Percent;

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_LoanTypeDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static LoanTypeDetailDto ToDto(LoanTypeDetail entity)
    {
        return new LoanTypeDetailDto
        {
            Id = entity.Id,
            LoanTypeId = entity.LoanTypeId,
            LoanTypeName = entity.LoanType?.NameEn,
            Percent = entity.Percent
        };
    }

    private static CreateOrEditLoanTypeDetailDto ToCreateOrEditDto(LoanTypeDetail entity)
    {
        return new CreateOrEditLoanTypeDetailDto
        {
            Id = entity.Id,
            LoanTypeId = entity.LoanTypeId,
            Percent = entity.Percent
        };
    }
}
