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
using HCMv3.Workflow.Services;
using HCMv3.PersonnelAdministration.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Back-office maintenance of <see cref="EmployeeHRService"/> (ported from legacy XAF
/// <c>HRServiceRequest</c>).
///
/// <para>
/// Creation here is a plain insert, NOT a workflow submission, and that is the deliberate difference from
/// <c>EmployeeITServicesAppService</c>. An HR service is self-service: the employee raises it in
/// <c>MyHRServiceAppService</c>, which is where the request is created and submitted. This screen exists so
/// HR can correct or record one after the fact, and routing a correction for approval by the same person who
/// just made it would be theatre.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeHRServices)]
public class EmployeeHRServicesAppService : HCMv3AppServiceBase, IEmployeeHRServicesAppService
{
    private readonly IRepository<EmployeeHRService> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;

    public EmployeeHRServicesAppService(IRepository<EmployeeHRService> repository,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _transactionSubmitter = transactionSubmitter;
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeHRServiceDto>> GetAll(GetAllEmployeeHRServicesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.HRServiceType)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value)
            .WhereIf(input.HRServiceTypeId.HasValue, e => e.HRServiceTypeId == input.HRServiceTypeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeHRServiceDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeHRServiceForEditOutput> GetEmployeeHRServiceForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeHRServiceForEditOutput
        {
            EmployeeHRService = new CreateOrEditEmployeeHRServiceDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                HRServiceTypeId = entity.HRServiceTypeId,
                ServiceDate = entity.ServiceDate,
                Amount = entity.Amount,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeHRServiceDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeHRServices_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeHRServiceDto input)
    {
        var entity = MapToEntity(input, new EmployeeHRService());
        entity.TenantId = AbpSession.TenantId ?? 0;

        // Routed for approval when the catalog says so, and simply filed when it does not -- the same
        // question the self-service form asks, so the answer no longer depends on which screen was used.
        // recordDirectly is refused unless the type permits a bypass AND the user holds the permission.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            HRServiceWorkflowTransactionHandler.HRServiceRequestType,
            "HRS",
            entity.HRServiceTypeId,
            input.RecordDirectly);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeHRServices_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeHRServiceDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeHRServices_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeHRService MapToEntity(CreateOrEditEmployeeHRServiceDto input, EmployeeHRService entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.HRServiceTypeId = input.HRServiceTypeId;
        entity.ServiceDate = input.ServiceDate.Date;
        entity.Amount = input.Amount;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeHRServiceDto ToDto(EmployeeHRService entity)
    {
        return new EmployeeHRServiceDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            HRServiceTypeId = entity.HRServiceTypeId,
            HRServiceTypeNameEn = entity.HRServiceType?.NameEn,
            HRServiceTypeNameAr = entity.HRServiceType?.NameAr,
            ServiceDate = entity.ServiceDate,
            Amount = entity.Amount,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId
        };
    }
}
