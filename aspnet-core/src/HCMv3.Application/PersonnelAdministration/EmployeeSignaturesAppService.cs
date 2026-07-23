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
/// Application service for <see cref="EmployeeSignature"/> (ported from legacy XAF
/// EmployeeSignature business object). Plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSignatures)]
public class EmployeeSignaturesAppService : HCMv3AppServiceBase, IEmployeeSignaturesAppService
{
    private readonly IRepository<EmployeeSignature> _repository;

    public EmployeeSignaturesAppService(IRepository<EmployeeSignature> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSignatureDto>> GetAll(GetAllEmployeeSignaturesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(s => s.Employee)
            .WhereIf(input.EmployeeId.HasValue, s => s.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSignatureDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSignatureForEditOutput> GetEmployeeSignatureForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeSignatureForEditOutput
        {
            EmployeeSignature = new CreateOrEditEmployeeSignatureDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                Signature = entity.Signature
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSignatureDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSignatures_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSignatureDto input)
    {
        var entity = new EmployeeSignature
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = input.EmployeeId,
            Signature = input.Signature
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSignatures_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSignatureDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.EmployeeId = input.EmployeeId;
        entity.Signature = input.Signature;

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSignatures_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeSignatureDto ToDto(EmployeeSignature entity)
    {
        return new EmployeeSignatureDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            Signature = entity.Signature
        };
    }
}
