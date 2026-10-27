using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the calculated salary register header (ported from legacy XAF SalaryRegister
/// business object).
///
/// LOCK CASCADE PARITY: the legacy's session-cached <c>prevLocked</c>/<c>OnLoaded</c> guard is replicated
/// per-request here — on update, the persisted entity is loaded first and its previous <see cref="SalaryRegister.Locked"/>
/// value is compared to the incoming one: if BOTH are true, the update is rejected ("a Locked register
/// cannot be updated while remaining Locked"). On delete, the persisted <see cref="SalaryRegister.Locked"/>
/// value alone blocks the operation.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
public class SalaryRegistersAppService : HCMv3AppServiceBase, ISalaryRegistersAppService
{
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;

    public SalaryRegistersAppService(IRepository<SalaryRegister> salaryRegisterRepository)
    {
        _salaryRegisterRepository = salaryRegisterRepository;
    }

    /// <summary>
    /// Property-injected so callers that build this service by hand keep compiling; without it the list
    /// simply has no payroll area column.
    /// </summary>
    public IRepository<EmployeeOrgAssignment> EmployeeOrgAssignmentRepository { get; set; }

    [HttpPost]
    public async Task<PagedResultDto<SalaryRegisterDto>> GetAll(GetAllSalaryRegistersInput input)
    {
        input.Normalize();

        var today = Clock.Now.Date;
        var currentAssignments = EmployeeOrgAssignmentRepository?.GetAll()
            .Where(a => a.StartDate <= today && a.EndDate >= today);

        var query = _salaryRegisterRepository.GetAll()
            .Include(r => r.Employee)
            .Include(r => r.PayrollReasonType)
            .WhereIf(input.EmployeeId.HasValue, r => r.EmployeeId == input.EmployeeId)
            .WhereIf(input.PayrollAreaId.HasValue && currentAssignments != null,
                r => currentAssignments.Any(a => a.EmployeeId == r.EmployeeId && a.PayrollAreaId == input.PayrollAreaId));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var dtos = items.Select(ToDto).ToList();
        await FillCurrentPayrollAreasAsync(dtos, currentAssignments);

        return new PagedResultDto<SalaryRegisterDto>(totalCount, dtos);
    }

    private static async Task FillCurrentPayrollAreasAsync(
        List<SalaryRegisterDto> dtos, IQueryable<EmployeeOrgAssignment> currentAssignments)
    {
        if (currentAssignments == null || dtos.Count == 0)
        {
            return;
        }

        var employeeIds = dtos.Select(d => d.EmployeeId).Distinct().ToList();
        var areas = await currentAssignments
            .Where(a => employeeIds.Contains(a.EmployeeId) && a.PayrollAreaId != null)
            .Select(a => new { a.EmployeeId, a.PayrollAreaId, a.PayrollArea.Name })
            .ToListAsync();
        var byEmployee = areas.GroupBy(a => a.EmployeeId).ToDictionary(g => g.Key, g => g.First());

        foreach (var dto in dtos)
        {
            if (byEmployee.TryGetValue(dto.EmployeeId, out var area))
            {
                dto.PayrollAreaId = area.PayrollAreaId;
                dto.PayrollAreaName = area.Name;
            }
        }
    }

    public async Task<GetSalaryRegisterForEditOutput> GetSalaryRegisterForEdit(EntityDto input)
    {
        var entity = await _salaryRegisterRepository.GetAsync(input.Id);

        return new GetSalaryRegisterForEditOutput
        {
            SalaryRegister = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSalaryRegisterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _salaryRegisterRepository.GetAsync(input.Id);
        if (entity.Locked)
        {
            throw new UserFriendlyException(L("LockedSalaryRegisterCannotBeDeleted"));
        }

        await _salaryRegisterRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSalaryRegisterDto input)
    {
        var entity = MapToEntity(input, new SalaryRegister());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _salaryRegisterRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSalaryRegisterDto input)
    {
        var entity = await _salaryRegisterRepository.GetAsync(input.Id.Value);

        var wasLocked = entity.Locked;
        if (wasLocked && input.Locked)
        {
            throw new UserFriendlyException(L("LockedSalaryRegisterCannotBeUpdated"));
        }

        entity = MapToEntity(input, entity);

        await _salaryRegisterRepository.UpdateAsync(entity);
    }

    private static SalaryRegister MapToEntity(CreateOrEditSalaryRegisterDto input, SalaryRegister entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.ForPeriod = input.ForPeriod;
        entity.InPeriod = input.InPeriod;
        entity.PayrollRunType = input.PayrollRunType;
        entity.PayrollResultType = input.PayrollResultType;
        entity.PayrollReasonTypeId = input.PayrollReasonTypeId;
        entity.OffCyclePaymentDate = input.OffCyclePaymentDate;
        entity.Locked = input.Locked;

        return entity;
    }

    private static SalaryRegisterDto ToDto(SalaryRegister entity)
    {
        return new SalaryRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            ForPeriod = entity.ForPeriod,
            InPeriod = entity.InPeriod,
            PayrollRunType = entity.PayrollRunType,
            PayrollResultType = entity.PayrollResultType,
            PayrollReasonTypeId = entity.PayrollReasonTypeId,
            PayrollReasonTypeName = entity.PayrollReasonType?.NameEn,
            OffCyclePaymentDate = entity.OffCyclePaymentDate,
            Locked = entity.Locked
        };
    }

    private static CreateOrEditSalaryRegisterDto ToCreateOrEditDto(SalaryRegister entity)
    {
        return new CreateOrEditSalaryRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            ForPeriod = entity.ForPeriod,
            InPeriod = entity.InPeriod,
            PayrollRunType = entity.PayrollRunType,
            PayrollResultType = entity.PayrollResultType,
            PayrollReasonTypeId = entity.PayrollReasonTypeId,
            OffCyclePaymentDate = entity.OffCyclePaymentDate,
            Locked = entity.Locked
        };
    }
}
