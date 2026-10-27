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
using HCMv3.PersonnelAdministration.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeRejoin"/>. Saving or deleting a rejoin runs legacy's checks and side
/// effects through <see cref="IEmployeeRejoinService"/>: reactivating the employee and zeroing the balances not
/// carried forward, or creating the new employee and opening the balances carried forward. All of it happens in
/// the method's unit of work, so it is saved together with the rejoin or not at all.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRejoins)]
public class EmployeeRejoinsAppService : HCMv3AppServiceBase, IEmployeeRejoinsAppService
{
    private readonly IRepository<EmployeeRejoin> _repository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IEmployeeRejoinService _rejoinService;

    public EmployeeRejoinsAppService(
        IRepository<EmployeeRejoin> repository,
        IRepository<Employee> employeeRepository,
        IEmployeeRejoinService rejoinService)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _rejoinService = rejoinService;
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

        var dtos = items.Select(ToDto).ToList();
        await FillNewEmployeesAsync(items, dtos);

        return new PagedResultDto<EmployeeRejoinDto>(totalCount, dtos);
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
                CarryForwardLoanBalance = entity.CarryForwardLoanBalance,
                CreateNewEmployeeCode = entity.CreateNewEmployeeCode
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

        var current = EmployeeRejoinValues.From(entity);
        await _rejoinService.ValidateSaveAsync(current, null, null);

        await _repository.InsertAsync(entity);
        await _rejoinService.ApplyAsync(current, null);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRejoins_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeRejoinDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        var previous = EmployeeRejoinValues.From(entity);

        MapToEntity(input, entity);
        var current = EmployeeRejoinValues.From(entity);

        await _rejoinService.ValidateSaveAsync(current, previous, entity.Id);

        await _repository.UpdateAsync(entity);
        await _rejoinService.ApplyAsync(current, previous);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRejoins_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        var previous = EmployeeRejoinValues.From(entity);

        await _rejoinService.ValidateDeleteAsync(previous, entity.Id);
        await _rejoinService.UndoAsync(previous);

        await _repository.DeleteAsync(entity);
    }

    /// <summary>
    /// The employee a new-code rejoin created: the one carrying the old code as its previous code and joining on
    /// the rejoin date (a person can rejoin with a new code more than once).
    /// </summary>
    private async Task FillNewEmployeesAsync(List<EmployeeRejoin> items, List<EmployeeRejoinDto> dtos)
    {
        var oldCodes = items
            .Where(r => r.CreateNewEmployeeCode && r.Employee?.Code != null)
            .Select(r => r.Employee.Code)
            .Distinct()
            .ToList();
        if (oldCodes.Count == 0)
        {
            return;
        }

        var candidates = await _employeeRepository.GetAll()
            .Where(e => oldCodes.Contains(e.PreviousEmpCode))
            .Select(e => new { e.Id, e.Code, e.PreviousEmpCode, e.JoiningDate })
            .ToListAsync();

        for (var i = 0; i < items.Count; i++)
        {
            var rejoin = items[i];
            if (!rejoin.CreateNewEmployeeCode || rejoin.Employee?.Code == null)
            {
                continue;
            }

            var match = candidates
                .Where(e => e.PreviousEmpCode == rejoin.Employee.Code && e.JoiningDate.Date == rejoin.RejoinDate.Date)
                .OrderBy(e => e.Id)
                .FirstOrDefault();
            if (match != null)
            {
                dtos[i].NewEmployeeId = match.Id;
                dtos[i].NewEmployeeCode = match.Code;
            }
        }
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
        entity.CreateNewEmployeeCode = input.CreateNewEmployeeCode;
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
            CarryForwardLoanBalance = entity.CarryForwardLoanBalance,
            CreateNewEmployeeCode = entity.CreateNewEmployeeCode
        };
    }
}
