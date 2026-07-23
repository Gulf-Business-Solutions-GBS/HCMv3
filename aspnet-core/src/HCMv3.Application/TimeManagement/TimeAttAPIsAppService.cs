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
using HCMv3.TimeManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// Application service for <see cref="TimeAttAPI"/> (ported from legacy XAF TimeAttAPI business object,
/// Wave 4). Ports the legacy <c>PerformValidation</c> required-field checks. See the entity class remarks
/// for the known plain-text-password concern (not fixed in this port).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeAttAPIs)]
public class TimeAttAPIsAppService : HCMv3AppServiceBase, ITimeAttAPIsAppService
{
    private readonly IRepository<TimeAttAPI> _repository;

    public TimeAttAPIsAppService(IRepository<TimeAttAPI> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TimeAttAPIDto>> GetAll(GetAllTimeAttAPIsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TimeAttAPIDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetTimeAttAPIForEditOutput> GetTimeAttAPIForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTimeAttAPIForEditOutput
        {
            TimeAttAPI = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTimeAttAPIDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeAttAPIs_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTimeAttAPIDto input)
    {
        var entity = MapToEntity(input, new TimeAttAPI());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeAttAPIs_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTimeAttAPIDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeAttAPIs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static TimeAttAPI MapToEntity(CreateOrEditTimeAttAPIDto input, TimeAttAPI entity)
    {
        entity.TokenUrl = input.TokenUrl;
        entity.Username = input.Username;
        entity.Password = input.Password;
        entity.TransactionsUrl = input.TransactionsUrl;
        entity.NoOfRecords = input.NoOfRecords;
        entity.DevicesUrl = input.DevicesUrl;
        entity.PunchInCode = input.PunchInCode;
        entity.PunchOutCode = input.PunchOutCode;
        entity.BreakOutCode = input.BreakOutCode;
        entity.BreakInCode = input.BreakInCode;
        entity.Email = input.Email;
        entity.Company = input.Company;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: TokenUrl/Username/Password/TransactionsUrl/DevicesUrl
    /// required, NoOfRecords must not be zero.
    /// </summary>
    private void ValidateAsync(TimeAttAPI entity)
    {
        if (entity.TokenUrl.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("TokenUrlCannotBeBlank"));
        }

        if (entity.Username.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("UsernameCannotBeBlank"));
        }

        if (entity.Password.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("PasswordCannotBeBlank"));
        }

        if (entity.TransactionsUrl.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("TransactionsUrlCannotBeBlank"));
        }

        if (entity.NoOfRecords == 0)
        {
            throw new UserFriendlyException(L("NoOfRecordsCannotBeZero"));
        }

        if (entity.DevicesUrl.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("DevicesUrlCannotBeBlank"));
        }
    }

    private static TimeAttAPIDto ToDto(TimeAttAPI entity)
    {
        return new TimeAttAPIDto
        {
            Id = entity.Id,
            TokenUrl = entity.TokenUrl,
            Username = entity.Username,
            TransactionsUrl = entity.TransactionsUrl,
            NoOfRecords = entity.NoOfRecords,
            DevicesUrl = entity.DevicesUrl,
            PunchInCode = entity.PunchInCode,
            PunchOutCode = entity.PunchOutCode,
            BreakOutCode = entity.BreakOutCode,
            BreakInCode = entity.BreakInCode,
            Email = entity.Email,
            Company = entity.Company
        };
    }

    private static CreateOrEditTimeAttAPIDto ToCreateOrEditDto(TimeAttAPI entity)
    {
        return new CreateOrEditTimeAttAPIDto
        {
            Id = entity.Id,
            TokenUrl = entity.TokenUrl,
            Username = entity.Username,
            Password = entity.Password,
            TransactionsUrl = entity.TransactionsUrl,
            NoOfRecords = entity.NoOfRecords,
            DevicesUrl = entity.DevicesUrl,
            PunchInCode = entity.PunchInCode,
            PunchOutCode = entity.PunchOutCode,
            BreakOutCode = entity.BreakOutCode,
            BreakInCode = entity.BreakInCode,
            Email = entity.Email,
            Company = entity.Company
        };
    }
}
