using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// The only entry point for <see cref="EmployeeITService"/> (ported from legacy XAF
/// <c>ITServiceRequest</c>).
///
/// <para>
/// Creation routes through <see cref="IAdminTransactionSubmitter"/>, following the
/// <c>EmployeeActionsAppService</c> precedent, because this type has no self-service screen: legacy's
/// <c>HCM10.ESS</c> contains no view, controller or model for an IT service, only a back-office one. So the
/// request has to be raised where the record is raised. With workflow switched off for the type the
/// submitter simply inserts the row, which is the pre-workflow behaviour.
/// </para>
///
/// <para>
/// Legacy's <c>OnSaving</c> is not reproduced. Its whole body sat inside <c>catch (Exception) { }</c>,
/// so its validation, its approver lookup and its duplicate-request-number check all failed silently and it
/// saved anyway. The required-field checks it intended live on the DTO and in
/// <see cref="ITServiceWorkflowTransactionHandler"/>.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeITServices)]
public class EmployeeITServicesAppService : HCMv3AppServiceBase, IEmployeeITServicesAppService
{
    private readonly IRepository<EmployeeITService> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;

    public EmployeeITServicesAppService(
        IRepository<EmployeeITService> repository,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _repository = repository;
        _transactionSubmitter = transactionSubmitter;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeITServiceDto>> GetAll(GetAllEmployeeITServicesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.ITServiceType)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value)
            .WhereIf(input.ITServiceTypeId.HasValue, e => e.ITServiceTypeId == input.ITServiceTypeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeITServiceDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeITServiceForEditOutput> GetEmployeeITServiceForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeITServiceForEditOutput
        {
            EmployeeITService = new CreateOrEditEmployeeITServiceDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                ITServiceTypeId = entity.ITServiceTypeId,
                ServiceDate = entity.ServiceDate,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeITServiceDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeITServices_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeITServiceDto input)
    {
        var entity = MapToEntity(input, new EmployeeITService());
        entity.TenantId = AbpSession.TenantId ?? 0;

        // Routed for approval when the catalog says so, and simply recorded when it does not. There is no
        // ESS service for this type, so submission belongs here or nowhere.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            ITServiceWorkflowTransactionHandler.ITServiceRequestType,
            "ITS",
            entity.ITServiceTypeId);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeITServices_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeITServiceDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeITServices_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeITService MapToEntity(CreateOrEditEmployeeITServiceDto input, EmployeeITService entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.ITServiceTypeId = input.ITServiceTypeId;
        entity.ServiceDate = input.ServiceDate.Date;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeITServiceDto ToDto(EmployeeITService entity)
    {
        return new EmployeeITServiceDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            ITServiceTypeId = entity.ITServiceTypeId,
            ITServiceTypeNameEn = entity.ITServiceType?.NameEn,
            ITServiceTypeNameAr = entity.ITServiceType?.NameAr,
            ServiceDate = entity.ServiceDate,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId
        };
    }
}
