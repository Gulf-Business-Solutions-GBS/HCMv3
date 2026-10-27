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
    private readonly IRepository<PAPeerNominee> _peerNomineeRepository;
    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _sysSettingsAccessor;
    private readonly Review.IPAFormTickPolicy _tickPolicy;
    private readonly HCMv3.PersonnelAdministration.Services.IEmployeeScopeProvider _employeeScopeProvider;

    public PAFormsAppService(
        IRepository<PAForm> repository,
        IRepository<PAPeerNominee> peerNomineeRepository,
        HCMv3.SystemSettings.Services.ISysSettingsAccessor sysSettingsAccessor,
        Review.IPAFormTickPolicy tickPolicy,
        HCMv3.PersonnelAdministration.Services.IEmployeeScopeProvider employeeScopeProvider)
    {
        _repository = repository;
        _peerNomineeRepository = peerNomineeRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _tickPolicy = tickPolicy;
        _employeeScopeProvider = employeeScopeProvider;
    }

    /// <summary>
    /// The twelve boxes as the signed-in employee may use them -- the same answer the save is checked
    /// against, so the dialog cannot offer a box the server will then refuse.
    /// </summary>
    public async Task<PAFormTickRulesDto> GetPAFormTickRules(NullableIdDto input)
    {
        var stored = input?.Id.HasValue == true
            ? Review.PAFormTickSubject.From(await _repository.GetAsync(input.Id.Value))
            : null;

        var actingEmployeeId = await _employeeScopeProvider.GetCurrentEmployeeIdAsync();
        var rules = await _tickPolicy.EvaluateAsync(stored, actingEmployeeId, Clock.Now);
        var settings = await _sysSettingsAccessor.GetAppraisalSettingsAsync();

        return new PAFormTickRulesDto
        {
            Unrestricted = rules.Unrestricted,
            Phase = rules.Phase?.ToString(),
            Boxes = rules.Boxes
                .Select(b => new PAFormBoxRuleDto { Box = b.Box.ToString(), Visible = b.Visible, Enabled = b.Enabled })
                .ToList(),
            ShowPeerSubmitted = rules.ShowPeerSubmitted,
            CanTickPeerSubmitted = rules.CanTickPeerSubmitted,
            HiddenListBoxes = Review.PAFormTickRules
                .HiddenForCycle(settings?.AppraisalCycle ?? HCMv3.SystemSettings.AppraisalCycle.Quarterly)
                .Select(b => b.ToString())
                .ToList()
        };
    }

    /// <summary>
    /// Legacy <c>PAForm.UpdatePeerReviewStatus</c>: the peer's own nomination for this form's employee and
    /// period is marked submitted. Legacy offered the box to anyone nominated in the period, for any form,
    /// and then quietly saved nothing when the nomination was for somebody else; the box is offered the same
    /// way and the same nothing happens.
    /// </summary>
    public async Task SubmitPeerReview(EntityDto input)
    {
        var form = await _repository.GetAsync(input.Id);
        var actingEmployeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        var rules = await _tickPolicy.EvaluateAsync(Review.PAFormTickSubject.From(form), actingEmployeeId, Clock.Now);
        if (!rules.CanTickPeerSubmitted)
        {
            throw new UserFriendlyException(L("GoalFormBoxCannotBeChanged"));
        }

        var nomination = await _peerNomineeRepository.GetAll()
            .FirstOrDefaultAsync(n => n.PAPeriodId == form.PAPeriodId && n.EmployeeId == form.EmployeeId && n.PeerId == actingEmployeeId);

        if (nomination == null)
        {
            return;
        }

        nomination.Submitted = true;
        await _peerNomineeRepository.UpdateAsync(nomination);
    }

    /// <summary>
    /// Legacy <c>SysService.GetAppraisalSettings</c> as the PA form screens read it: the review stages the
    /// cycle shows, the peer-review switch and limits, and the marks mode.
    /// </summary>
    public async Task<AppraisalFormSettingsDto> GetAppraisalFormSettings()
    {
        var settings = await _sysSettingsAccessor.GetAppraisalSettingsAsync();

        return new AppraisalFormSettingsDto
        {
            AppraisalCycle = settings?.AppraisalCycle ?? HCMv3.SystemSettings.AppraisalCycle.Quarterly,
            EnablePAPeerReview = settings?.EnablePAPeerReview ?? false,
            SetPeerMinLimit = settings?.SetPeerMinLimit ?? 0,
            SetPeerMaxLimit = settings?.SetPeerMaxLimit ?? 0,
            EnableMarksInPAReview = settings?.EnableMarksInPAReview ?? false,
            MarksOn = settings?.MarksOn ?? default
        };
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

        // A new form has no open stage yet, so for an employee every box must start unticked.
        await _tickPolicy.EnsureChangesAllowedAsync(
            null,
            Review.PAFormBoxValues.From(entity),
            await _employeeScopeProvider.GetCurrentEmployeeIdAsync(),
            Clock.Now);

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPAFormDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        // Judged against the form as it was stored -- that is the state the dialog offered the boxes from.
        var stored = Review.PAFormTickSubject.From(entity);

        entity = MapToEntity(input, entity);

        await _tickPolicy.EnsureChangesAllowedAsync(
            stored,
            Review.PAFormBoxValues.From(entity),
            await _employeeScopeProvider.GetCurrentEmployeeIdAsync(),
            Clock.Now);

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

        // Legacy PAForm.PerformValidation: with peer review on, the employee must have between
        // ManageAppraisalSettings.SetPeerMinLimit and SetPeerMaxLimit peer nominees for the period.
        var appraisalSettings = await _sysSettingsAccessor.GetAppraisalSettingsAsync();
        if (appraisalSettings != null && appraisalSettings.EnablePAPeerReview)
        {
            var nominees = await _peerNomineeRepository.GetAll()
                .CountAsync(n => n.PAPeriodId == entity.PAPeriodId && n.EmployeeId == entity.EmployeeId);
            if (nominees < appraisalSettings.SetPeerMinLimit || nominees > appraisalSettings.SetPeerMaxLimit)
            {
                throw new UserFriendlyException(L("PeerNomineeCountOutOfRange",
                    appraisalSettings.SetPeerMinLimit, appraisalSettings.SetPeerMaxLimit));
            }
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
