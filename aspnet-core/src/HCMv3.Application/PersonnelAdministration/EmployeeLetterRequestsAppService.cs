using System;
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
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Admin service for <see cref="EmployeeLetterRequest"/> -- the HR counterpart to
/// <c>MyLetterRequestAppService</c>.
///
/// <para>
/// The validation restated here is the same five required fields the workflow handler enforces. It is
/// duplicated deliberately: a row HR creates while workflow is off for the type never reaches the handler at
/// all, so the handler alone would leave a hole exactly where there is no approver to catch it.
/// </para>
///
/// <para>
/// Unlike the ESS service, this one does NOT insist the document type is flagged as a letter. HR picking an
/// unflagged type is a deliberate act by somebody who can also change the flag; an employee doing it would
/// be bypassing the dropdown.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLetterRequests)]
public class EmployeeLetterRequestsAppService : HCMv3AppServiceBase, IEmployeeLetterRequestsAppService
{
    private readonly IRepository<EmployeeLetterRequest> _repository;

    public EmployeeLetterRequestsAppService(IRepository<EmployeeLetterRequest> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeLetterRequestDto>> GetAll(GetAllEmployeeLetterRequestsInput input)
    {
        input.Normalize();

        // Opting out of ApprovedOnly is what "include pending" means -- there is no separate request table
        // to look in, so an in-flight letter is simply a row the filter is hiding.
        using (input.IncludePending
                   ? CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly)
                   : NullDisposable.Instance)
        {
            var query = _repository.GetAll()
                .Include(e => e.Employee)
                .Include(e => e.DocumentType)
                .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value)
                .WhereIf(input.DocumentTypeId.HasValue, e => e.DocumentTypeId == input.DocumentTypeId.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(input.Sorting)
                .PageBy(input)
                .ToListAsync();

            return new PagedResultDto<EmployeeLetterRequestDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public async Task<GetEmployeeLetterRequestForEditOutput> GetEmployeeLetterRequestForEdit(EntityDto input)
    {
        // Pending rows are exactly the ones HR is asked to correct, so the filter is off for the lookup.
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var entity = await _repository.GetAsync(input.Id);

            return new GetEmployeeLetterRequestForEditOutput
            {
                EmployeeLetterRequest = new CreateOrEditEmployeeLetterRequestDto
                {
                    Id = entity.Id,
                    EmployeeId = entity.EmployeeId,
                    DocumentTypeId = entity.DocumentTypeId,
                    EffectiveDate = entity.EffectiveDate,
                    AddressToEn = entity.AddressToEn,
                    AddressToAr = entity.AddressToAr,
                    Comments = entity.Comments
                }
            };
        }
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeLetterRequestDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLetterRequests_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeLetterRequestDto input)
    {
        var entity = MapToEntity(input, new EmployeeLetterRequest());
        entity.TenantId = AbpSession.TenantId ?? 0;

        Validate(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLetterRequests_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeLetterRequestDto input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var entity = await _repository.GetAsync(input.Id.Value);
            MapToEntity(input, entity);

            Validate(entity);

            await _repository.UpdateAsync(entity);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLetterRequests_Delete)]
    public async Task Delete(EntityDto input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await _repository.DeleteAsync(input.Id);
        }
    }

    private void Validate(EmployeeLetterRequest entity)
    {
        if (entity.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeIsRequired"));
        }

        if (entity.EffectiveDate == default)
        {
            throw new UserFriendlyException(L("EffectiveDateIsRequired"));
        }

        if (entity.DocumentTypeId <= 0)
        {
            throw new UserFriendlyException(L("DocumentTypeIsRequired"));
        }

        if (entity.AddressToEn.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("AddressToEnIsRequired"));
        }

        if (entity.AddressToAr.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("AddressToArIsRequired"));
        }
    }

    private static EmployeeLetterRequest MapToEntity(
        CreateOrEditEmployeeLetterRequestDto input,
        EmployeeLetterRequest entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.DocumentTypeId = input.DocumentTypeId;
        entity.EffectiveDate = input.EffectiveDate.Date;
        entity.AddressToEn = input.AddressToEn?.Trim();
        entity.AddressToAr = input.AddressToAr?.Trim();
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeLetterRequestDto ToDto(EmployeeLetterRequest entity)
    {
        return new EmployeeLetterRequestDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            DocumentTypeId = entity.DocumentTypeId,
            DocumentTypeNameEn = entity.DocumentType?.NameEn,
            DocumentTypeNameAr = entity.DocumentType?.NameAr,
            EffectiveDate = entity.EffectiveDate,
            AddressToEn = entity.AddressToEn,
            AddressToAr = entity.AddressToAr,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanPrint = entity.WorkflowStatus == RequestStatus.Approved
        };
    }

    /// <summary>A no-op <see cref="IDisposable"/>, so the conditional filter opt-out above can stay a single
    /// <c>using</c> rather than two copies of the query body.</summary>
    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new NullDisposable();

        private NullDisposable()
        {
        }

        public void Dispose()
        {
        }
    }
}
