using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PerformanceManagement.Dto;
using HCMv3.PerformanceManagement.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Application service for the newer appraisal model's aggregate root, <see cref="PAEmployee"/>. See
/// <see cref="PAEmployee"/> remarks for how approval routing works now.
///
/// <para>
/// An appraisal is opened FOR an employee -- by HR for a cycle, or by a manager for their team -- so the
/// subject comes from the form and submission belongs here rather than in an ESS service. That is also what
/// legacy did: its ESS entry point took the appraisee's code as a parameter separate from the signed-in
/// user, which is what let a manager or a bulk org-unit launch open appraisals for other people. The
/// employee's own contribution is their self-rating, and that is recorded on
/// <see cref="PAEmployeeReviewer"/>/<see cref="PAEmployeeRating"/>, not on this row.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAEmployees)]
public class PAEmployeesAppService : HCMv3AppServiceBase, IPAEmployeesAppService
{
    private readonly IRepository<PAEmployee> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;

    public PAEmployeesAppService(
        IRepository<PAEmployee> repository,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _repository = repository;
        _transactionSubmitter = transactionSubmitter;
    }

    [HttpPost]
    public async Task<PagedResultDto<PAEmployeeDto>> GetAll(GetAllPAEmployeesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.PACycle)
            .Include(e => e.FormType)
            .WhereIf(input.PACycleId.HasValue, e => e.PACycleId == input.PACycleId)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PAEmployeeDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPAEmployeeForEditOutput> GetPAEmployeeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPAEmployeeForEditOutput { PAEmployee = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPAEmployeeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAEmployees_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAEmployees_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPAEmployeeDto input)
    {
        var entity = MapToEntity(input, new PAEmployee());
        entity.TenantId = AbpSession.TenantId ?? 0;

        // Routed for approval when the catalog says so, and simply recorded when it does not -- the
        // pre-workflow behaviour, which is what makes leaving appraisals unrouted a real option. The form
        // type is the sub-type, because that is the axis a 360 review routes differently from a probation
        // form on; the cycle is only a period.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            AppraisalWorkflowTransactionHandler.AppraisalRequestType,
            "APR",
            entity.FormTypeId);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAEmployees_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPAEmployeeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static PAEmployee MapToEntity(CreateOrEditPAEmployeeDto input, PAEmployee entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.PACycleId = input.PACycleId;
        entity.FormTypeId = input.FormTypeId;

        // RequestComments and WorkflowComments are deliberately NOT assigned, and not left off by accident:
        // they are legacy columns kept only so migrated appraisals keep their old approval text. Not writing
        // them here is also what stops an edit from wiping that text, since the edit DTO no longer carries
        // it. New comments belong to the WorkflowComment thread on the governing request.
        entity.ProposedIncrement = input.ProposedIncrement;
        entity.ProposedJobTitle = input.ProposedJobTitle;
        entity.AreaOfImprovement = input.AreaOfImprovement;

        return entity;
    }

    private static PAEmployeeDto ToDto(PAEmployee entity)
    {
        return new PAEmployeeDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            PACycleId = entity.PACycleId,
            PACycleName = entity.PACycle?.NameEn,
            FormTypeId = entity.FormTypeId,
            FormTypeName = entity.FormType?.NameEn,
            OverallAverageRatingId = entity.OverallAverageRatingId,
            OverallAverageScore = entity.OverallAverageScore,
            PeerAverageRatingId = entity.PeerAverageRatingId,
            PeerAverageScore = entity.PeerAverageScore,
            EmployeeAverageRatingId = entity.EmployeeAverageRatingId,
            EmployeeAverageScore = entity.EmployeeAverageScore,
            RequestComments = entity.RequestComments,
            WorkflowComments = entity.WorkflowComments,
            ProposedIncrement = entity.ProposedIncrement,
            ProposedJobTitle = entity.ProposedJobTitle,
            AreaOfImprovement = entity.AreaOfImprovement
        };
    }

    private static CreateOrEditPAEmployeeDto ToCreateOrEditDto(PAEmployee entity)
    {
        return new CreateOrEditPAEmployeeDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            PACycleId = entity.PACycleId,
            FormTypeId = entity.FormTypeId,
            ProposedIncrement = entity.ProposedIncrement,
            ProposedJobTitle = entity.ProposedJobTitle,
            AreaOfImprovement = entity.AreaOfImprovement
        };
    }
}
