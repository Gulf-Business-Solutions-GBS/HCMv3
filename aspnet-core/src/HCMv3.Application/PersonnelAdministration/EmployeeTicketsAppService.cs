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
/// Application service for <see cref="EmployeeTicket"/> (ported from legacy XAF EmployeeTicket business
/// object, Wave 3 deferred bucket). The legacy quota-balance cascade / auto-calculated Amount / dependant
/// age-limit and benefit-exception checks are NOT ported here -- see <see cref="EmployeeTicket"/> remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTickets)]
public class EmployeeTicketsAppService : HCMv3AppServiceBase, IEmployeeTicketsAppService
{
    private readonly IRepository<EmployeeTicket> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;

    public EmployeeTicketsAppService(IRepository<EmployeeTicket> repository,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _transactionSubmitter = transactionSubmitter;
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeTicketDto>> GetAll(GetAllEmployeeTicketsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(t => t.Employee)
            .Include(t => t.TicketType)
            .WhereIf(input.EmployeeId.HasValue, t => t.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeTicketDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeTicketForEditOutput> GetEmployeeTicketForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeTicketForEditOutput
        {
            EmployeeTicket = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeTicketDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTickets_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTickets_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeTicketDto input)
    {
        var entity = MapToEntity(input, new EmployeeTicket());
        entity.TenantId = AbpSession.TenantId ?? 0;

        // Routed for approval when the catalog says so, and simply filed when it does not -- the same
        // question the self-service form asks, so the answer no longer depends on which screen was used.
        // recordDirectly is refused unless the type permits a bypass AND the user holds the permission.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            TicketWorkflowTransactionHandler.TicketRequestType,
            "TKT",
            entity.TicketTypeId,
            input.RecordDirectly);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTickets_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeTicketDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static EmployeeTicket MapToEntity(CreateOrEditEmployeeTicketDto input, EmployeeTicket entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.TicketTypeId = input.TicketTypeId;
        entity.TicketDate = input.TicketDate.Date;
        entity.PaymentDate = input.PaymentDate.Date;
        entity.Compensate = input.Compensate;
        entity.TicketPaymentType = input.TicketPaymentType;
        entity.Amount = input.Amount;
        entity.AdvancedTicketPaid = input.AdvancedTicketPaid;
        entity.Comments = input.Comments;
        entity.OverrideAmount = input.OverrideAmount;
        return entity;
    }

    private static EmployeeTicketDto ToDto(EmployeeTicket entity)
    {
        return new EmployeeTicketDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            TicketTypeId = entity.TicketTypeId,
            TicketTypeName = entity.TicketType?.NameEn,
            TicketDate = entity.TicketDate,
            PaymentDate = entity.PaymentDate,
            Compensate = entity.Compensate,
            TicketPaymentType = entity.TicketPaymentType,
            Amount = entity.Amount,
            AdvancedTicketPaid = entity.AdvancedTicketPaid,
            Comments = entity.Comments,
            OverrideAmount = entity.OverrideAmount
        };
    }

    private static CreateOrEditEmployeeTicketDto ToCreateOrEditDto(EmployeeTicket entity)
    {
        return new CreateOrEditEmployeeTicketDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            TicketTypeId = entity.TicketTypeId,
            TicketDate = entity.TicketDate,
            PaymentDate = entity.PaymentDate,
            Compensate = entity.Compensate,
            TicketPaymentType = entity.TicketPaymentType,
            Amount = entity.Amount,
            AdvancedTicketPaid = entity.AdvancedTicketPaid,
            Comments = entity.Comments,
            OverrideAmount = entity.OverrideAmount
        };
    }
}
