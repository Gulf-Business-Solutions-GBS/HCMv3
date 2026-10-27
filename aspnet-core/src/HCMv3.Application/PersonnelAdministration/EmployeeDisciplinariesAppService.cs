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
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeDisciplinary"/> (ported from legacy XAF EmployeeDisciplinary
/// business object, Wave 4 deferred bucket).
///
/// <para>
/// Creates go through <see cref="IAdminTransactionSubmitter"/>: a disciplinary record is raised BY somebody
/// about SOMEBODY ELSE -- legacy pre-filled the reporter with the logged-in employee while the subject was
/// picked separately -- so this is an admin submission, never self-service. While the transaction type is
/// dormant the record is simply filed, exactly as before; once an admin routes it, the record stays
/// invisible until an approver agrees to it, and approval is what stamps the penalty
/// (see <see cref="DisciplinaryWorkflowTransactionHandler"/>).
/// </para>
///
/// <para>
/// The legacy <c>CurrentDeductionCount</c>/<c>ViolationDeductionType</c>/<c>DisciplinaryHistory</c>
/// computed properties ARE ported, but as read-only projections through
/// <see cref="IDisciplinaryEscalationService"/> rather than as stored columns.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDisciplinaries)]
public class EmployeeDisciplinariesAppService : HCMv3AppServiceBase, IEmployeeDisciplinariesAppService
{
    private readonly IRepository<EmployeeDisciplinary> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;
    private readonly IDisciplinaryEscalationService _escalationService;
    private readonly IEmployeeSessionService _employeeSessionService;

    public EmployeeDisciplinariesAppService(
        IRepository<EmployeeDisciplinary> repository,
        IAdminTransactionSubmitter transactionSubmitter,
        IDisciplinaryEscalationService escalationService,
        IEmployeeSessionService employeeSessionService)
    {
        _repository = repository;
        _transactionSubmitter = transactionSubmitter;
        _escalationService = escalationService;
        _employeeSessionService = employeeSessionService;
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

    /// <summary>
    /// What this offence would cost. Read-only, computed on every call -- the same ladder the workflow
    /// handler reads on approval, resolved through the same domain service so the number shown here and the
    /// number finally written cannot disagree.
    /// </summary>
    [HttpPost]
    public async Task<DisciplinaryEscalationDto> GetDisciplinaryEscalation(GetDisciplinaryEscalationInput input)
    {
        var escalation = await _escalationService.ResolveAsync(
            input.EmployeeId,
            input.DisciplinaryTypeId,
            input.DisciplinaryDate,
            input.ExcludeDisciplinaryId);

        return ToDto(escalation);
    }

    public async Task<GetEmployeeDisciplinaryForEditOutput> GetEmployeeDisciplinaryForEdit(EntityDto input)
    {
        // The record being edited may itself still be awaiting approval, and an editor has to be able to
        // open the thing they just raised.
        EmployeeDisciplinary entity;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            entity = await _repository.GetAsync(input.Id);
        }

        var escalation = await _escalationService.ResolveAsync(
            entity.EmployeeId,
            entity.DisciplinaryTypeId,
            entity.DisciplinaryDate,
            entity.Id);

        return new GetEmployeeDisciplinaryForEditOutput
        {
            Escalation = ToDto(escalation),
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

        // Legacy's AfterConstruction stamped the reporter with the logged-in employee. Kept, because it is
        // also the evidence that this is not self-service: the reporter and the subject are different
        // people by construction. Left null when the acting user has no employee link -- which is normal
        // for an HR admin account, and the routed path then asks for a reporter explicitly.
        entity.ReportedById ??= await _employeeSessionService.GetCurrentEmployeeIdAsync();

        // Routed for approval when the catalog says so, and simply filed when it does not -- the
        // pre-workflow behaviour. The sub-type is the offence type, so a safety violation can take a
        // different chain from a late arrival.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            DisciplinaryWorkflowTransactionHandler.DisciplinaryRequestType,
            "DSC",
            entity.DisciplinaryTypeId);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDisciplinaries_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeDisciplinaryDto input)
    {
        // An in-flight record is hidden from ordinary queries, so editing one has to opt out of the filter
        // the same way opening it does.
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var entity = await _repository.GetAsync(input.Id.Value);
            MapToEntity(input, entity);

            await _repository.UpdateAsync(entity);
        }
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
            DisciplinaryDeductionTypeNameEn = entity.DisciplinaryDeductionType?.NameEn,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId
        };
    }

    private static DisciplinaryEscalationDto ToDto(Services.DisciplinaryEscalation escalation)
    {
        return new DisciplinaryEscalationDto
        {
            EmployeeId = escalation.EmployeeId,
            DisciplinaryTypeId = escalation.DisciplinaryTypeId,
            DisciplinaryTypeNameEn = escalation.DisciplinaryTypeNameEn,
            DisciplinaryTypeNameAr = escalation.DisciplinaryTypeNameAr,
            RepeatDurationInDays = escalation.RepeatDurationInDays,
            WindowStartDate = escalation.WindowStartDate,
            WindowEndDate = escalation.WindowEndDate,
            PriorDisciplinaryCount = escalation.PriorDisciplinaryCount,
            OffenceNumber = escalation.OffenceNumber,
            DisciplinaryDeductionTypeId = escalation.DisciplinaryDeductionTypeId,
            DisciplinaryDeductionTypeNameEn = escalation.DisciplinaryDeductionTypeNameEn,
            DisciplinaryDeductionTypeNameAr = escalation.DisciplinaryDeductionTypeNameAr,
            History = escalation.History.Select(h => new DisciplinaryHistoryItemDto
            {
                Id = h.Id,
                DisciplinaryDate = h.DisciplinaryDate,
                Severity = h.Severity,
                DisciplinaryDeductionTypeId = h.DisciplinaryDeductionTypeId,
                DisciplinaryDeductionTypeNameEn = h.DisciplinaryDeductionTypeNameEn,
                IsWithinRepeatWindow = h.IsWithinRepeatWindow,
                WorkflowStatus = h.WorkflowStatus
            }).ToList()
        };
    }
}
