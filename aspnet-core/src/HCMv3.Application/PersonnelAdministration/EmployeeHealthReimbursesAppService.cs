using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeHealthReimburse"/> (ported from legacy XAF
/// EmployeeHealthReimburse business object, Wave 4 deferred bucket).
///
/// <para>
/// Creates go through <see cref="IAdminTransactionSubmitter"/>, because HR raising a claim on an employee's
/// behalf is an ordinary thing to do -- somebody brings a receipt to the HR desk -- and it must not be the
/// back door that skips approval. While the transaction type is dormant the claim is simply filed, exactly as
/// before; once an administrator routes the type, an HR-entered claim stays invisible until an approver
/// agrees to it, on the same terms as a self-service one.
/// </para>
///
/// <para>
/// <b>The quota enforcement is now ported</b> (it was previously recorded here as not ported). It runs on
/// every write, on both paths, via <see cref="IHealthReimburseQuotaService"/> -- which is the point of
/// putting it in a service rather than in the workflow handler: with the type dormant nothing reaches a
/// handler, and legacy checked the balance in <c>OnSaving</c>, i.e. on every save whoever made it.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeHealthReimburses)]
public class EmployeeHealthReimbursesAppService : HCMv3AppServiceBase, IEmployeeHealthReimbursesAppService
{
    private readonly IRepository<EmployeeHealthReimburse> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;
    private readonly IHealthReimburseQuotaService _quotaService;

    public EmployeeHealthReimbursesAppService(
        IRepository<EmployeeHealthReimburse> repository,
        IAdminTransactionSubmitter transactionSubmitter,
        IHealthReimburseQuotaService quotaService)
    {
        _repository = repository;
        _transactionSubmitter = transactionSubmitter;
        _quotaService = quotaService;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeHealthReimburseDto>> GetAll(GetAllEmployeeHealthReimbursesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeHealthReimburseDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeHealthReimburseForEditOutput> GetEmployeeHealthReimburseForEdit(EntityDto input)
    {
        // An in-flight claim is hidden from ordinary queries, so opening one has to opt out of the filter.
        using var _ = CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly);

        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeHealthReimburseForEditOutput
        {
            EmployeeHealthReimburse = new CreateOrEditEmployeeHealthReimburseDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                PaymentDate = entity.PaymentDate,
                Amount = entity.Amount,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeHealthReimburseDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeHealthReimburses_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeHealthReimburseDto input)
    {
        var entity = MapToEntity(input, new EmployeeHealthReimburse());
        entity.TenantId = AbpSession.TenantId ?? 0;

        // Checked before the row exists, so HR is told what is wrong rather than having the insert rolled
        // back underneath them. The handler applies the identical rule to anything that reaches the engine.
        await _quotaService.EnsureClaimIsAllowedAsync(entity.EmployeeId, entity.PaymentDate, entity.Amount);

        // Routed for approval when the catalog says so, and simply filed when it does not -- the
        // pre-workflow behaviour. No sub-type: legacy routed every medical claim the same way.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            HealthReimburseWorkflowTransactionHandler.HealthReimburseRequestType,
            "HRB");
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeHealthReimburses_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeHealthReimburseDto input)
    {
        // An in-flight claim is hidden from ordinary queries, so editing one has to opt out of the filter
        // the same way opening it does.
        using var _ = CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly);

        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        // The claim being edited is excluded from its own balance check -- otherwise raising an existing
        // claim from 100 to 150 would be compared against a balance that still counts the original 100.
        await _quotaService.EnsureClaimIsAllowedAsync(
            entity.EmployeeId, entity.PaymentDate, entity.Amount, excludeClaimId: entity.Id);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeHealthReimburses_Delete)]
    public async Task Delete(EntityDto input)
    {
        using var _ = CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly);

        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeHealthReimburse MapToEntity(CreateOrEditEmployeeHealthReimburseDto input, EmployeeHealthReimburse entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.PaymentDate = input.PaymentDate.Date;
        entity.Amount = input.Amount;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeHealthReimburseDto ToDto(EmployeeHealthReimburse entity)
    {
        return new EmployeeHealthReimburseDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            PaymentDate = entity.PaymentDate,
            Amount = entity.Amount,
            Comments = entity.Comments
        };
    }
}
