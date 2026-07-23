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
/// Application service for <see cref="EmployeeRejoin"/> -- DELIBERATELY MINIMAL "record-keeping only"
/// port of the legacy XAF EmployeeRejoin business object (Wave B). This is a simple audit/record-keeping
/// CRUD service; NONE of the legacy carry-forward orchestration (leave/ticket/indemnity/loan balance
/// adjustments, new-employee-code creation, ValidatePreviousEntry chain-blocking) is implemented -- see
/// the entity class remarks for the full list of dropped behaviors. Only required-field validation
/// (already enforced by DTO Data Annotations) applies.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRejoins)]
public class EmployeeRejoinsAppService : HCMv3AppServiceBase, IEmployeeRejoinsAppService
{
    private readonly IRepository<EmployeeRejoin> _repository;

    public EmployeeRejoinsAppService(IRepository<EmployeeRejoin> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeRejoinDto>> GetAll(GetAllEmployeeRejoinsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(r => r.Employee)
            .WhereIf(input.EmployeeId.HasValue, r => r.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeRejoinDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeRejoinForEditOutput> GetEmployeeRejoinForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeRejoinForEditOutput
        {
            EmployeeRejoin = new CreateOrEditEmployeeRejoinDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                RejoinDate = entity.RejoinDate,
                Comments = entity.Comments,
                CarryForwardLeaveBalance = entity.CarryForwardLeaveBalance,
                CarryForwardTicketBalance = entity.CarryForwardTicketBalance,
                CarryForwardIndemnityBalance = entity.CarryForwardIndemnityBalance,
                CarryForwardLoanBalance = entity.CarryForwardLoanBalance
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeRejoinDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRejoins_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeRejoinDto input)
    {
        var entity = MapToEntity(input, new EmployeeRejoin());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRejoins_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeRejoinDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRejoins_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeRejoin MapToEntity(CreateOrEditEmployeeRejoinDto input, EmployeeRejoin entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.RejoinDate = input.RejoinDate.Date;
        entity.Comments = input.Comments;
        entity.CarryForwardLeaveBalance = input.CarryForwardLeaveBalance;
        entity.CarryForwardTicketBalance = input.CarryForwardTicketBalance;
        entity.CarryForwardIndemnityBalance = input.CarryForwardIndemnityBalance;
        entity.CarryForwardLoanBalance = input.CarryForwardLoanBalance;
        return entity;
    }

    private static EmployeeRejoinDto ToDto(EmployeeRejoin entity)
    {
        return new EmployeeRejoinDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            RejoinDate = entity.RejoinDate,
            Comments = entity.Comments,
            CarryForwardLeaveBalance = entity.CarryForwardLeaveBalance,
            CarryForwardTicketBalance = entity.CarryForwardTicketBalance,
            CarryForwardIndemnityBalance = entity.CarryForwardIndemnityBalance,
            CarryForwardLoanBalance = entity.CarryForwardLoanBalance
        };
    }
}
