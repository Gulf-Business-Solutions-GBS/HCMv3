using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PerformanceManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PerformanceManagement;

[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PATimelineExceptions)]
public class PATimelineExceptionsAppService : HCMv3AppServiceBase, IPATimelineExceptionsAppService
{
    private readonly IRepository<PATimelineException> _repository;

    public PATimelineExceptionsAppService(IRepository<PATimelineException> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PATimelineExceptionDto>> GetAll(GetAllPATimelineExceptionsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.PAPeriod)
            .Include(e => e.Employee)
            .WhereIf(input.PAPeriodId.HasValue, e => e.PAPeriodId == input.PAPeriodId)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PATimelineExceptionDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPATimelineExceptionForEditOutput> GetPATimelineExceptionForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPATimelineExceptionForEditOutput { PATimelineException = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPATimelineExceptionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PATimelineExceptions_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PATimelineExceptions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPATimelineExceptionDto input)
    {
        var entity = new PATimelineException
        {
            TenantId = AbpSession.TenantId ?? 0,
            PAPeriodId = input.PAPeriodId,
            EmployeeId = input.EmployeeId,
            ActivityType = input.ActivityType,
            ExtensionDate = input.ExtensionDate.Date
        };

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PATimelineExceptions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPATimelineExceptionDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.PAPeriodId = input.PAPeriodId;
        entity.EmployeeId = input.EmployeeId;
        entity.ActivityType = input.ActivityType;
        entity.ExtensionDate = input.ExtensionDate.Date;

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: no duplicate Period+Employee+ActivityType.</summary>
    private async Task ValidateAsync(PATimelineException entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id
                           && e.PAPeriodId == entity.PAPeriodId
                           && e.EmployeeId == entity.EmployeeId
                           && e.ActivityType == entity.ActivityType);
        if (duplicate)
        {
            throw new UserFriendlyException(L("PATimelineExceptionAlreadyExists"));
        }
    }

    private static PATimelineExceptionDto ToDto(PATimelineException entity)
    {
        return new PATimelineExceptionDto
        {
            Id = entity.Id,
            PAPeriodId = entity.PAPeriodId,
            PAPeriodName = entity.PAPeriod?.Name,
            EmployeeId = entity.EmployeeId,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            ActivityType = entity.ActivityType,
            ExtensionDate = entity.ExtensionDate
        };
    }

    private static CreateOrEditPATimelineExceptionDto ToCreateOrEditDto(PATimelineException entity)
    {
        return new CreateOrEditPATimelineExceptionDto
        {
            Id = entity.Id,
            PAPeriodId = entity.PAPeriodId,
            EmployeeId = entity.EmployeeId,
            ActivityType = entity.ActivityType,
            ExtensionDate = entity.ExtensionDate
        };
    }
}
