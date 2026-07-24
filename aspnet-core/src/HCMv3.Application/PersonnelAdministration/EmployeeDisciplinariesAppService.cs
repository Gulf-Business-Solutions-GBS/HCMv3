using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeDisciplinary"/> (ported from legacy XAF EmployeeDisciplinary
/// business object, Wave 4 deferred bucket). Ports the essential active validation only: Employee/
/// DisciplinaryType/DisciplinaryDate required. The legacy <c>ReportedBy</c>-required-and-auto-populated-
/// from-current-user rule, and the <c>LastDisciplinaryDate</c>/<c>CurrentDeductionCount</c>/
/// <c>ViolationDeductionType</c> read-time computed properties, are NOT ported -- see class remarks on
/// <see cref="EmployeeDisciplinary"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDisciplinaries)]
public class EmployeeDisciplinariesAppService : HCMv3AppServiceBase, IEmployeeDisciplinariesAppService
{
    private readonly IRepository<EmployeeDisciplinary> _repository;

    public EmployeeDisciplinariesAppService(IRepository<EmployeeDisciplinary> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDisciplinaryDto>> GetAll(GetAllEmployeeDisciplinariesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.WorkLocation)
            .Include(e => e.DisciplinaryType)
            .Include(e => e.ReportedBy)
            .Include(e => e.DisciplinaryDeductionType)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeDisciplinaryDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeDisciplinaryForEditOutput> GetEmployeeDisciplinaryForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeDisciplinaryForEditOutput
        {
            EmployeeDisciplinary = new CreateOrEditEmployeeDisciplinaryDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                OrganizationUnitId = entity.OrganizationUnitId,
                WorkLocationId = entity.WorkLocationId,
                DisciplinaryDate = entity.DisciplinaryDate,
                DisciplinaryTime = entity.DisciplinaryTime,
                DisciplinaryTypeId = entity.DisciplinaryTypeId,
                ReportedById = entity.ReportedById,
                WLSendToEmp = entity.WLSendToEmp,
                WLRecvFromEmp = entity.WLRecvFromEmp,
                Comments = entity.Comments,
                Severity = entity.Severity,
                DisciplinaryDeductionTypeId = entity.DisciplinaryDeductionTypeId
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeDisciplinaryDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDisciplinaries_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeDisciplinaryDto input)
    {
        var entity = MapToEntity(input, new EmployeeDisciplinary());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDisciplinaries_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeDisciplinaryDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDisciplinaries_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeDisciplinary MapToEntity(CreateOrEditEmployeeDisciplinaryDto input, EmployeeDisciplinary entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.OrganizationUnitId = input.OrganizationUnitId;
        entity.WorkLocationId = input.WorkLocationId;
        entity.DisciplinaryDate = input.DisciplinaryDate.Date;
        entity.DisciplinaryTime = input.DisciplinaryTime;
        entity.DisciplinaryTypeId = input.DisciplinaryTypeId;
        entity.ReportedById = input.ReportedById;
        entity.WLSendToEmp = input.WLSendToEmp;
        entity.WLRecvFromEmp = input.WLRecvFromEmp;
        entity.Comments = input.Comments;
        entity.Severity = input.Severity;
        entity.DisciplinaryDeductionTypeId = input.DisciplinaryDeductionTypeId;
        return entity;
    }

    private static EmployeeDisciplinaryDto ToDto(EmployeeDisciplinary entity)
    {
        return new EmployeeDisciplinaryDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            OrganizationUnitId = entity.OrganizationUnitId,
            WorkLocationId = entity.WorkLocationId,
            WorkLocationNameEn = entity.WorkLocation?.NameEn,
            DisciplinaryDate = entity.DisciplinaryDate,
            DisciplinaryTime = entity.DisciplinaryTime,
            DisciplinaryTypeId = entity.DisciplinaryTypeId,
            DisciplinaryTypeNameEn = entity.DisciplinaryType?.NameEn,
            ReportedById = entity.ReportedById,
            ReportedByFullNameEn = entity.ReportedBy?.FullNameEn,
            WLSendToEmp = entity.WLSendToEmp,
            WLRecvFromEmp = entity.WLRecvFromEmp,
            Comments = entity.Comments,
            Severity = entity.Severity,
            DisciplinaryDeductionTypeId = entity.DisciplinaryDeductionTypeId,
            DisciplinaryDeductionTypeNameEn = entity.DisciplinaryDeductionType?.NameEn
        };
    }
}
