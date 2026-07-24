using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PerformanceManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Application service for the older appraisal model's aggregate root, <see cref="PAForm"/>. See
/// <see cref="PAForm"/> remarks for the deliberately dropped auto-cascade-to-next-stage orchestration and
/// AppraisalSettings-driven behaviors.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms)]
public class PAFormsAppService : HCMv3AppServiceBase, IPAFormsAppService
{
    private readonly IRepository<PAForm> _repository;

    public PAFormsAppService(IRepository<PAForm> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PAFormDto>> GetAll(GetAllPAFormsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.PAPeriod)
            .Include(e => e.Employee)
            .WhereIf(input.PAPeriodId.HasValue, e => e.PAPeriodId == input.PAPeriodId)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PAFormDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPAFormForEditOutput> GetPAFormForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPAFormForEditOutput { PAForm = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPAFormDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPAFormDto input)
    {
        var entity = MapToEntity(input, new PAForm());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPAFormDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static PAForm MapToEntity(CreateOrEditPAFormDto input, PAForm entity)
    {
        entity.PAPeriodId = input.PAPeriodId;
        entity.EmployeeId = input.EmployeeId;
        entity.ModeratedScore = input.ModeratedScore;
        entity.PlanSubmitByAppraiser = input.PlanSubmitByAppraiser;
        entity.PlanSubmitByAppraisee = input.PlanSubmitByAppraisee;
        entity.Quarter1SubmitByAppraiser = input.Quarter1SubmitByAppraiser;
        entity.Quarter1SubmitByAppraisee = input.Quarter1SubmitByAppraisee;
        entity.MRSubmitByAppraiser = input.MRSubmitByAppraiser;
        entity.MRSubmitByAppraisee = input.MRSubmitByAppraisee;
        entity.Quarter3SubmitByAppraiser = input.Quarter3SubmitByAppraiser;
        entity.Quarter3SubmitByAppraisee = input.Quarter3SubmitByAppraisee;
        entity.ERSubmitByAppraiser = input.ERSubmitByAppraiser;
        entity.ERSubmitByAppraisee = input.ERSubmitByAppraisee;
        entity.AppraiserSignedOff = input.AppraiserSignedOff;
        entity.AppraiseeSignedOff = input.AppraiseeSignedOff;
        entity.AppraiseeStrength = input.AppraiseeStrength;
        entity.AreasOfImprovement = input.AreasOfImprovement;
        entity.ReviewofPreviousGoals = input.ReviewofPreviousGoals;
        entity.NewlySetGoals = input.NewlySetGoals;
        entity.OverallComments = input.OverallComments;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: no duplicate Period+Employee.</summary>
    private async Task ValidateAsync(PAForm entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.PAPeriodId == entity.PAPeriodId && e.EmployeeId == entity.EmployeeId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("PAFormAlreadyExistsForPeriodAndEmployee"));
        }
    }

    private static PAFormDto ToDto(PAForm entity)
    {
        return new PAFormDto
        {
            Id = entity.Id,
            PAPeriodId = entity.PAPeriodId,
            PAPeriodName = entity.PAPeriod?.Name,
            EmployeeId = entity.EmployeeId,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            OverallScore = entity.OverallScore,
            ModeratedScore = entity.ModeratedScore,
            PlanSubmitByAppraiser = entity.PlanSubmitByAppraiser,
            PlanSubmitByAppraisee = entity.PlanSubmitByAppraisee,
            Quarter1SubmitByAppraiser = entity.Quarter1SubmitByAppraiser,
            Quarter1SubmitByAppraisee = entity.Quarter1SubmitByAppraisee,
            MRSubmitByAppraiser = entity.MRSubmitByAppraiser,
            MRSubmitByAppraisee = entity.MRSubmitByAppraisee,
            Quarter3SubmitByAppraiser = entity.Quarter3SubmitByAppraiser,
            Quarter3SubmitByAppraisee = entity.Quarter3SubmitByAppraisee,
            ERSubmitByAppraiser = entity.ERSubmitByAppraiser,
            ERSubmitByAppraisee = entity.ERSubmitByAppraisee,
            AppraiserSignedOff = entity.AppraiserSignedOff,
            AppraiseeSignedOff = entity.AppraiseeSignedOff,
            AppraiseeStrength = entity.AppraiseeStrength,
            AreasOfImprovement = entity.AreasOfImprovement,
            ReviewofPreviousGoals = entity.ReviewofPreviousGoals,
            NewlySetGoals = entity.NewlySetGoals,
            OverallComments = entity.OverallComments,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditPAFormDto ToCreateOrEditDto(PAForm entity)
    {
        return new CreateOrEditPAFormDto
        {
            Id = entity.Id,
            PAPeriodId = entity.PAPeriodId,
            EmployeeId = entity.EmployeeId,
            ModeratedScore = entity.ModeratedScore,
            PlanSubmitByAppraiser = entity.PlanSubmitByAppraiser,
            PlanSubmitByAppraisee = entity.PlanSubmitByAppraisee,
            Quarter1SubmitByAppraiser = entity.Quarter1SubmitByAppraiser,
            Quarter1SubmitByAppraisee = entity.Quarter1SubmitByAppraisee,
            MRSubmitByAppraiser = entity.MRSubmitByAppraiser,
            MRSubmitByAppraisee = entity.MRSubmitByAppraisee,
            Quarter3SubmitByAppraiser = entity.Quarter3SubmitByAppraiser,
            Quarter3SubmitByAppraisee = entity.Quarter3SubmitByAppraisee,
            ERSubmitByAppraiser = entity.ERSubmitByAppraiser,
            ERSubmitByAppraisee = entity.ERSubmitByAppraisee,
            AppraiserSignedOff = entity.AppraiserSignedOff,
            AppraiseeSignedOff = entity.AppraiseeSignedOff,
            AppraiseeStrength = entity.AppraiseeStrength,
            AreasOfImprovement = entity.AreasOfImprovement,
            ReviewofPreviousGoals = entity.ReviewofPreviousGoals,
            NewlySetGoals = entity.NewlySetGoals,
            OverallComments = entity.OverallComments,
            Comments = entity.Comments
        };
    }
}
