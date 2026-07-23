using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.TimeManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// Application service for <see cref="TimeTransfer"/> (ported from legacy XAF TimeTransfer business object,
/// Wave 4). Ports the legacy <c>PerformValidation</c> (Employee/TransferType/TransferDate required -- the
/// commented-out duplicate check was never enabled in the legacy source and is NOT ported) and the
/// <c>Math.Round(TransferCount, 3)</c> rounding done in <c>OnSaving</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeTransfers)]
public class TimeTransfersAppService : HCMv3AppServiceBase, ITimeTransfersAppService
{
    private readonly IRepository<TimeTransfer> _repository;

    public TimeTransfersAppService(IRepository<TimeTransfer> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TimeTransferDto>> GetAll(GetAllTimeTransfersInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(t => t.Employee)
            .Include(t => t.TransferType)
            .WhereIf(input.EmployeeId.HasValue, t => t.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TimeTransferDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetTimeTransferForEditOutput> GetTimeTransferForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTimeTransferForEditOutput
        {
            TimeTransfer = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTimeTransferDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeTransfers_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTimeTransferDto input)
    {
        var entity = MapToEntity(input, new TimeTransfer());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeTransfers_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTimeTransferDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeTransfers_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static TimeTransfer MapToEntity(CreateOrEditTimeTransferDto input, TimeTransfer entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.TransferDate = input.TransferDate.Date;
        entity.TransferTypeId = input.TransferTypeId;
        // Legacy OnSaving: this.TransferCount = Math.Round(this.TransferCount, 3);
        entity.TransferCount = Math.Round(input.TransferCount, 3);
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: Employee/TransferType/TransferDate required. The
    /// commented-out duplicate (Employee, TransferType, TransferDate) check was never enabled in the legacy
    /// source and is intentionally NOT ported.
    /// </summary>
    private void ValidateAsync(TimeTransfer entity)
    {
        if (entity.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeCannotBeBlank"));
        }

        if (entity.TransferTypeId <= 0)
        {
            throw new UserFriendlyException(L("TransferTypeCannotBeBlank"));
        }

        if (entity.TransferDate == default)
        {
            throw new UserFriendlyException(L("TransferDateCannotBeBlank"));
        }
    }

    private static TimeTransferDto ToDto(TimeTransfer entity)
    {
        return new TimeTransferDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            TransferDate = entity.TransferDate,
            TransferTypeId = entity.TransferTypeId,
            TransferTypeName = entity.TransferType?.NameEn,
            TransferCount = entity.TransferCount,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditTimeTransferDto ToCreateOrEditDto(TimeTransfer entity)
    {
        return new CreateOrEditTimeTransferDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            TransferDate = entity.TransferDate,
            TransferTypeId = entity.TransferTypeId,
            TransferCount = entity.TransferCount,
            Comments = entity.Comments
        };
    }
}
