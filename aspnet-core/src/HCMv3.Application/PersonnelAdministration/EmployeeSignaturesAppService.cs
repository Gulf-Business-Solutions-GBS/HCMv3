using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeSignature"/> (ported from legacy XAF
/// EmployeeSignature business object). Plain CRUD.
///
/// <para>
/// The signature image travels in the DTOs as bytes, exactly as before; it is kept on disk under the
/// tenant's folder (<see cref="IEntityImageStore"/>), never in the database.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSignatures)]
public class EmployeeSignaturesAppService : HCMv3AppServiceBase, IEmployeeSignaturesAppService
{
    private readonly IRepository<EmployeeSignature> _repository;
    private readonly IEntityImageStore _imageStore;

    public EmployeeSignaturesAppService(IRepository<EmployeeSignature> repository, IEntityImageStore imageStore)
    {
        _repository = repository;
        _imageStore = imageStore;
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

        var dtos = new List<EmployeeSignatureDto>(items.Count);
        foreach (var item in items)
        {
            dtos.Add(ToDto(item, await ReadSignatureAsync(item)));
        }

        return new PagedResultDto<EmployeeSignatureDto>(totalCount, dtos);
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
                Signature = await ReadSignatureAsync(entity)
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
            EmployeeId = input.EmployeeId
        };

        // The file name carries the row id, so the row is inserted first.
        await _repository.InsertAndGetIdAsync(entity);
        entity.SignaturePath = await _imageStore.SaveIfChangedAsync(
            EntityImageKinds.EmployeeSignature, entity.TenantId, entity.Id, null, input.Signature);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSignatures_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSignatureDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.EmployeeId = input.EmployeeId;
        entity.SignaturePath = await _imageStore.SaveIfChangedAsync(
            EntityImageKinds.EmployeeSignature, entity.TenantId, entity.Id, entity.SignaturePath, input.Signature);
        entity.LegacySignature = null;

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSignatures_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private Task<byte[]> ReadSignatureAsync(EmployeeSignature entity) =>
        _imageStore.ReadWithLegacyAsync(entity.TenantId, entity.SignaturePath, entity.LegacySignature);

    private static EmployeeSignatureDto ToDto(EmployeeSignature entity, byte[] signature)
    {
        return new EmployeeSignatureDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            Signature = signature
        };
    }
}
