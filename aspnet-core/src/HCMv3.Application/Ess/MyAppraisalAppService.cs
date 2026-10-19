using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Ess.Dto;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Review;
using HCMv3.PerformanceManagement.Workflow;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyAppraisalAppService"/>
///
/// <remarks>
/// Ported from legacy ESS <c>PerformanceManagementController</c> (Index, RequestForm, CopyKpi, UpdateRating,
/// UpdateMarks, UpdateComment, GetRatingBalanceLimit, SubmitReview, AddPeer) and the XAF
/// <c>PAEmployeeController</c> Save Review / Submit Review / Copy KPI actions. Legacy saved each rating,
/// mark and comment the moment it was changed; here the reviewer's lines are saved together by Save Review,
/// which is the Save / Submit / Cancel pattern every self-service screen uses.
///
/// <para>
/// Every read runs with <see cref="HCMv3DataFilters.ApprovedOnly"/> switched off. An appraisal is reviewed
/// while it is in flight -- exactly while that filter hides it -- so without this the screen would be empty
/// for the whole of the period in which it is needed.
/// </para>
/// </remarks>
[AbpAuthorize(AppPermissions.Ess_MyAppraisals)]
public class MyAppraisalAppService : HCMv3AppServiceBase, IMyAppraisalAppService
{
    private readonly IRepository<PAEmployee> _appraisalRepository;
    private readonly IRepository<PAEmployeeDetail> _detailRepository;
    private readonly IRepository<PAEmployeeReviewer> _reviewerRepository;
    private readonly IRepository<PAEmployeeRating> _ratingRepository;
    private readonly IRepository<PARating> _paRatingRepository;
    private readonly IRepository<KpiByJobTitle> _kpiByJobTitleRepository;
    private readonly IRepository<KpiByJobTitleDetail> _kpiByJobTitleDetailRepository;
    private readonly IRepository<KpiByOrgUnit> _kpiByOrgUnitRepository;
    private readonly IRepository<KpiByOrgUnitDetail> _kpiByOrgUnitDetailRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<EmployeeRelationship> _employeeRelationshipRepository;
    private readonly IRepository<AppraisalStepSetting> _stepSettingRepository;
    private readonly IRepository<AppraisalStepSettingRelationship> _stepSettingRelationshipRepository;
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly IRepository<WorkFlowApprover> _approverRepository;
    private readonly IEmployeeScopeProvider _employeeScopeProvider;
    private readonly IAppraisalStepSettingResolver _stepSettingResolver;
    private readonly IAppraisalReviewerManager _reviewerManager;
    private readonly IPAEmployeeScoreCalculator _scoreCalculator;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IWorkflowApprovalService _workflowApprovalService;

    public MyAppraisalAppService(
        IRepository<PAEmployee> appraisalRepository,
        IRepository<PAEmployeeDetail> detailRepository,
        IRepository<PAEmployeeReviewer> reviewerRepository,
        IRepository<PAEmployeeRating> ratingRepository,
        IRepository<PARating> paRatingRepository,
        IRepository<KpiByJobTitle> kpiByJobTitleRepository,
        IRepository<KpiByJobTitleDetail> kpiByJobTitleDetailRepository,
        IRepository<KpiByOrgUnit> kpiByOrgUnitRepository,
        IRepository<KpiByOrgUnitDetail> kpiByOrgUnitDetailRepository,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<EmployeeRelationship> employeeRelationshipRepository,
        IRepository<AppraisalStepSetting> stepSettingRepository,
        IRepository<AppraisalStepSettingRelationship> stepSettingRelationshipRepository,
        IRepository<WorkflowRequest> requestRepository,
        IRepository<WorkFlowApprover> approverRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IAppraisalStepSettingResolver stepSettingResolver,
        IAppraisalReviewerManager reviewerManager,
        IPAEmployeeScoreCalculator scoreCalculator,
        ISysSettingsAccessor sysSettingsAccessor,
        IWorkflowApprovalService workflowApprovalService)
    {
        _appraisalRepository = appraisalRepository;
        _detailRepository = detailRepository;
        _reviewerRepository = reviewerRepository;
        _ratingRepository = ratingRepository;
        _paRatingRepository = paRatingRepository;
        _kpiByJobTitleRepository = kpiByJobTitleRepository;
        _kpiByJobTitleDetailRepository = kpiByJobTitleDetailRepository;
        _kpiByOrgUnitRepository = kpiByOrgUnitRepository;
        _kpiByOrgUnitDetailRepository = kpiByOrgUnitDetailRepository;
        _employeeRepository = employeeRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _employeeRelationshipRepository = employeeRelationshipRepository;
        _stepSettingRepository = stepSettingRepository;
        _stepSettingRelationshipRepository = stepSettingRelationshipRepository;
        _requestRepository = requestRepository;
        _approverRepository = approverRepository;
        _employeeScopeProvider = employeeScopeProvider;
        _stepSettingResolver = stepSettingResolver;
        _reviewerManager = reviewerManager;
        _scoreCalculator = scoreCalculator;
        _sysSettingsAccessor = sysSettingsAccessor;
        _workflowApprovalService = workflowApprovalService;
    }

    private static bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

    // ----------------------------------------------------------------------------------------------------
    // Lists
    // ----------------------------------------------------------------------------------------------------

    [HttpPost]
    public async Task<PagedResultDto<MyAppraisalListDto>> GetMyAppraisals(GetMyAppraisalsInput input)
    {
        input.Normalize();

        var me = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var query = _appraisalRepository.GetAll()
                .Include(a => a.Employee)
                .Include(a => a.PACycle)
                .Include(a => a.FormType)
                .AsQueryable();

            switch (input.Scope)
            {
                case MyAppraisalListScope.Mine:
                    query = query.Where(a => a.EmployeeId == me);
                    break;

                case MyAppraisalListScope.ToReview:
                    // On the reviewer list, or an approver on the step the request is waiting at.
                    var reviewing = _reviewerRepository.GetAll()
                        .Where(r => r.EmployeeId == me)
                        .Select(r => r.PAEmployeeId);

                    var approving = _approverRepository.GetAll()
                        .Where(a => a.ApproverId == me
                                    && a.WorkflowRequest.RequestType == AppraisalWorkflowTransactionHandler.AppraisalRequestType
                                    && a.WorkflowRequest.RequestStatus == RequestStatus.Open
                                    && a.WorkflowStep == a.WorkflowRequest.WorkflowStep
                                    && a.WorkflowRequest.TargetEntityId != null)
                        .Select(a => a.WorkflowRequest.TargetEntityId.Value);

                    query = query.Where(a => reviewing.Contains(a.Id) || approving.Contains(a.Id));
                    break;

                case MyAppraisalListScope.Team:
                    // One level down, as legacy's manager list (EmployeeManager, current-dated).
                    query = await _employeeScopeProvider.ApplyScopeAsync(query, EmployeeScope.DirectReports);
                    break;
            }

            var totalCount = await query.CountAsync();
            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            var requestIds = items.Where(i => i.WorkflowRequestId.HasValue).Select(i => i.WorkflowRequestId!.Value).ToList();
            var numbers = await _requestRepository.GetAll()
                .Where(r => requestIds.Contains(r.Id))
                .Select(r => new { r.Id, r.RequestNumber })
                .ToListAsync();

            return new PagedResultDto<MyAppraisalListDto>(totalCount, items.Select(a => new MyAppraisalListDto
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                EmployeeCode = a.Employee?.Code,
                EmployeeFullNameEn = a.Employee?.FullNameEn,
                EmployeeFullNameAr = a.Employee?.FullNameAr,
                PACycleNameEn = a.PACycle?.NameEn,
                PACycleNameAr = a.PACycle?.NameAr,
                FormTypeNameEn = a.FormType?.NameEn,
                FormTypeNameAr = a.FormType?.NameAr,
                WorkflowRequestId = a.WorkflowRequestId,
                RequestNumber = numbers.FirstOrDefault(n => n.Id == a.WorkflowRequestId)?.RequestNumber,
                WorkflowStatus = a.WorkflowStatus,
                CreationTime = a.CreationTime
            }).ToList());
        }
    }

    // ----------------------------------------------------------------------------------------------------
    // The review form
    // ----------------------------------------------------------------------------------------------------

    public async Task<MyAppraisalReviewDto> GetAppraisalReview(EntityDto input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var state = await LoadAsync(input.Id);
            var access = state.Access;
            var appraisal = state.Appraisal;

            var result = new MyAppraisalReviewDto
            {
                Id = appraisal.Id,
                RequestNumber = state.Request?.RequestNumber,
                WorkflowRequestId = appraisal.WorkflowRequestId,
                WorkflowStatus = appraisal.WorkflowStatus,
                WorkflowStep = state.Request?.WorkflowStep ?? 0,
                EmployeeId = appraisal.EmployeeId,
                EmployeeCode = appraisal.Employee?.Code,
                EmployeeFullNameEn = appraisal.Employee?.FullNameEn,
                EmployeeFullNameAr = appraisal.Employee?.FullNameAr,
                PACycleNameEn = appraisal.PACycle?.NameEn,
                PACycleNameAr = appraisal.PACycle?.NameAr,
                FormTypeNameEn = appraisal.FormType?.NameEn,
                FormTypeNameAr = appraisal.FormType?.NameAr,
                Access = ToAccessDto(state),
                CustomTextField1 = state.OwnReviewer?.CustomTextField1,
                CustomTextField2 = state.OwnReviewer?.CustomTextField2,
                CustomTextField3 = state.OwnReviewer?.CustomTextField3
            };

            if (access.ShowAverages)
            {
                result.OverallAverageScore = appraisal.OverallAverageScore;
                result.PeerAverageScore = appraisal.PeerAverageScore;
                result.EmployeeAverageScore = appraisal.EmployeeAverageScore;
            }

            if (access.ShowKpiDetails)
            {
                result.Kpis = (await _detailRepository.GetAll()
                        .Include(d => d.KpiInfo).ThenInclude(k => k.KpiCategory)
                        .Where(d => d.PAEmployeeId == appraisal.Id)
                        .OrderBy(d => d.Id)
                        .ToListAsync())
                    .Select(d => new MyAppraisalKpiDto
                    {
                        Id = d.Id,
                        KpiNameEn = d.KpiInfo?.NameEn,
                        KpiNameAr = d.KpiInfo?.NameAr,
                        KpiCategoryNameEn = d.KpiInfo?.KpiCategory?.NameEn,
                        KpiCategoryNameAr = d.KpiInfo?.KpiCategory?.NameAr,
                        KpiDescription = d.KpiInfo?.Comments,
                        Weight = d.Weight,
                        Target = d.Target
                    })
                    .ToList();
            }

            if (access.ShowRatings)
            {
                result.RatingLines = await GetVisibleRatingLinesAsync(state);
            }

            if (access.ShowReviewers)
            {
                result.Reviewers = await GetReviewersAsync(state);
            }

            result.RatingOptions = await GetRatingOptionsAsync(state);

            return result;
        }
    }

    public async Task SaveReview(SaveMyAppraisalReviewInput input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var state = await LoadAsync(input.PAEmployeeId);

            if (!state.Access.CanSaveReview)
            {
                throw new UserFriendlyException(L("YouCannotChangeThisReview"));
            }

            await WriteOwnLinesAsync(state, input);
        }
    }

    public async Task<SubmitMyAppraisalReviewResultDto> SubmitReview(SaveMyAppraisalReviewInput input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var state = await LoadAsync(input.PAEmployeeId);

            // The submit rule, not the save rule: an approver who rates on an ordinary step may save their
            // lines but finishes by approving, not by submitting a review (legacy hid the button).
            if (!state.Access.CanSubmitReview)
            {
                throw new UserFriendlyException(L("YouCannotSubmitThisReview"));
            }

            // Saved first, so what is locked is what is on the screen.
            var ownLines = await WriteOwnLinesAsync(state, input);

            await EnsureReviewCompleteAsync(state, ownLines);

            state.OwnReviewer.ReviewSubmitted = true;
            await _reviewerRepository.UpdateAsync(state.OwnReviewer);
            await CurrentUnitOfWork.SaveChangesAsync();

            return new SubmitMyAppraisalReviewResultDto
            {
                StepApproved = await AutoApproveOnLastFillAsync(state)
            };
        }
    }

    // ----------------------------------------------------------------------------------------------------
    // Copy KPI
    // ----------------------------------------------------------------------------------------------------

    public async Task<MyAppraisalKpiSourcesDto> GetKpiSources(EntityDto input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var state = await LoadAsync(input.Id);
            var result = new MyAppraisalKpiSourcesDto();

            if (!state.Access.CanCopyKpi)
            {
                return result;
            }

            result.JobTitles = (await _kpiByJobTitleRepository.GetAll()
                    .Include(k => k.JobTitle)
                    .OrderBy(k => k.JobTitleId).ThenByDescending(k => k.StartDate)
                    .ToListAsync())
                .Select(k => new MyAppraisalKpiSourceDto
                {
                    Id = k.Id,
                    NameEn = k.JobTitle?.NameEn,
                    NameAr = k.JobTitle?.NameAr,
                    StartDate = k.StartDate,
                    EndDate = k.EndDate
                })
                .ToList();

            result.OrganizationUnits = (await _kpiByOrgUnitRepository.GetAll()
                    .Include(k => k.OrganizationUnit)
                    .OrderBy(k => k.OrganizationUnitId).ThenByDescending(k => k.StartDate)
                    .ToListAsync())
                .Select(k => new MyAppraisalKpiSourceDto
                {
                    Id = k.Id,
                    NameEn = k.OrganizationUnit?.DisplayName,
                    NameAr = k.OrganizationUnit?.DisplayName,
                    StartDate = k.StartDate,
                    EndDate = k.EndDate
                })
                .ToList();

            // Other appraisals in the same cycle that have something to copy.
            var appraisalsWithKpis = _detailRepository.GetAll().Select(d => d.PAEmployeeId);

            var people = await _appraisalRepository.GetAll()
                .Where(a => a.PACycleId == state.Appraisal.PACycleId
                            && a.Id != state.Appraisal.Id
                            && appraisalsWithKpis.Contains(a.Id))
                .Select(a => new { a.EmployeeId, a.Employee.Code, a.Employee.FullNameEn, a.Employee.FullNameAr })
                .ToListAsync();

            result.Employees = people
                .GroupBy(p => p.EmployeeId)
                .Select(g => g.First())
                .OrderBy(p => p.Code)
                .Select(p => new MyAppraisalPersonDto
                {
                    Id = p.EmployeeId,
                    Code = p.Code,
                    FullNameEn = p.FullNameEn,
                    FullNameAr = p.FullNameAr
                })
                .ToList();

            return result;
        }
    }

    public async Task CopyKpi(CopyMyAppraisalKpiInput input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var state = await LoadAsync(input.PAEmployeeId);
            var appraisal = state.Appraisal;

            if (!state.Access.CanCopyKpi)
            {
                throw new UserFriendlyException(L("YouCannotCopyKpisOnThisAppraisal"));
            }

            var source = await ReadKpiSourceAsync(input, appraisal);

            // Legacy DialogController_Accepting: with peer review on, the copy is refused when one more
            // reviewer would break the form type's limit -- the copy itself adds the approver as a reviewer.
            var appraisalSettings = await _sysSettingsAccessor.GetAppraisalSettingsAsync();
            if (appraisalSettings != null && appraisalSettings.EnablePAPeerReview)
            {
                var max = appraisal.FormType?.MaxReviewerLimit ?? 0;
                var count = await _reviewerRepository.GetAll().CountAsync(r => r.PAEmployeeId == appraisal.Id);

                if (max != 0 && count + 1 > max)
                {
                    throw new UserFriendlyException(L("MaxReviewerLimitReached", max));
                }
            }

            var existingKpiIds = await _detailRepository.GetAll()
                .Where(d => d.PAEmployeeId == appraisal.Id)
                .Select(d => d.KpiInfoId)
                .ToListAsync();

            var reviewerIds = await _reviewerRepository.GetAll()
                .Where(r => r.PAEmployeeId == appraisal.Id)
                .Select(r => r.Id)
                .ToListAsync();

            // Only KPIs the appraisal does not have yet: copying twice must not double a KPI's weight.
            foreach (var kpi in source.Where(k => !existingKpiIds.Contains(k.KpiInfoId)))
            {
                existingKpiIds.Add(kpi.KpiInfoId);

                var detailId = await _detailRepository.InsertAndGetIdAsync(new PAEmployeeDetail
                {
                    TenantId = appraisal.TenantId,
                    PAEmployeeId = appraisal.Id,
                    KpiInfoId = kpi.KpiInfoId,
                    Weight = kpi.Weight,
                    Target = kpi.Target
                });

                // Legacy FillReview from the KPI side: every reviewer already on the appraisal gets a line.
                foreach (var reviewerId in reviewerIds)
                {
                    await _ratingRepository.InsertAsync(new PAEmployeeRating
                    {
                        TenantId = appraisal.TenantId,
                        PAEmployeeId = appraisal.Id,
                        PAEmployeeReviewerId = reviewerId,
                        PAEmployeeDetailId = detailId
                    });
                }
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            // Legacy "Add loggedinEmp as Person in Rating": on a step where the approver rates, copying the
            // KPIs also puts the approver on the reviewer list, not as a peer.
            if (state.Setting != null && state.Setting.CanAddRatings && state.OwnReviewer == null)
            {
                await _reviewerManager.AddReviewerAsync(appraisal, state.Me, isPeer: false, peerAddedById: state.Me, state.Setting);
            }
        }
    }

    // ----------------------------------------------------------------------------------------------------
    // Add peer
    // ----------------------------------------------------------------------------------------------------

    [HttpPost]
    public async Task<List<MyAppraisalPersonDto>> GetPeerCandidates(GetMyAppraisalPeerCandidatesInput input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var state = await LoadAsync(input.PAEmployeeId);
            if (!state.Access.CanAddPeer)
            {
                return new List<MyAppraisalPersonDto>();
            }

            var excluded = await GetExcludedPeerIdsAsync(state);

            return await _employeeRepository.GetAll()
                .Where(e => e.Status == EmployeeStatus.Active && !excluded.Contains(e.Id))
                .WhereIf(!input.Filter.IsNullOrWhiteSpace(),
                    e => e.Code.Contains(input.Filter) || e.FullNameEn.Contains(input.Filter) || e.FullNameAr.Contains(input.Filter))
                .OrderBy(e => e.Code)
                .Take(50)
                .Select(e => new MyAppraisalPersonDto
                {
                    Id = e.Id,
                    Code = e.Code,
                    FullNameEn = e.FullNameEn,
                    FullNameAr = e.FullNameAr
                })
                .ToListAsync();
        }
    }

    public async Task AddPeer(AddMyAppraisalPeerInput input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var state = await LoadAsync(input.PAEmployeeId);
            if (!state.Access.CanAddPeer)
            {
                throw new UserFriendlyException(L("YouCannotAddPeersToThisAppraisal"));
            }

            // Legacy ESS AddPeer: the Appraisal Settings peer limit must be set, and the whole reviewer list
            // -- not only the peers -- is measured against it.
            var appraisalSettings = state.AppraisalSettings;
            if (appraisalSettings == null || appraisalSettings.SetPeerMaxLimit <= 0)
            {
                throw new UserFriendlyException(L("PeerReviewerLimitIsNotConfigured"));
            }

            var reviewerCount = await _reviewerRepository.GetAll().CountAsync(r => r.PAEmployeeId == state.Appraisal.Id);
            if (reviewerCount >= appraisalSettings.SetPeerMaxLimit)
            {
                throw new UserFriendlyException(L("PeerLimitReached", appraisalSettings.SetPeerMaxLimit));
            }

            var excluded = await GetExcludedPeerIdsAsync(state, includeExistingReviewers: false);
            var peerIsActive = await _employeeRepository.GetAll()
                .AnyAsync(e => e.Id == input.PeerEmployeeId && e.Status == EmployeeStatus.Active);

            if (!peerIsActive || excluded.Contains(input.PeerEmployeeId))
            {
                throw new UserFriendlyException(L("ThisEmployeeCannotBeAddedAsPeer"));
            }

            // Duplicate, form-type limit and the employee's own allowance are the reviewer manager's checks.
            await _reviewerManager.AddReviewerAsync(state.Appraisal, input.PeerEmployeeId, isPeer: true, peerAddedById: state.Me, state.Setting);
        }
    }

    // ----------------------------------------------------------------------------------------------------
    // Internals
    // ----------------------------------------------------------------------------------------------------

    /// <summary>Everything about one appraisal and the signed-in employee's place on it.</summary>
    private sealed class ReviewState
    {
        public int Me { get; init; }

        public PAEmployee Appraisal { get; init; }

        public WorkflowRequest Request { get; init; }

        public List<PAEmployeeReviewer> Reviewers { get; init; }

        public PAEmployeeReviewer OwnReviewer { get; init; }

        public bool IsApprover { get; init; }

        public AppraisalStepSetting Setting { get; init; }

        public ManageAppraisalSettings AppraisalSettings { get; init; }

        public AppraisalReviewAccess Access { get; init; }
    }

    /// <summary>
    /// Loads the appraisal and refuses anybody it is not about: the appraisee, a reviewer, anyone who has
    /// held an approver row on its request, or the appraisee's manager. An appraisal id is a small integer;
    /// without this anyone with the permission could read every score in the tenant.
    /// </summary>
    private async Task<ReviewState> LoadAsync(int paEmployeeId)
    {
        var me = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        var appraisal = await _appraisalRepository.GetAll()
            .Include(a => a.Employee)
            .Include(a => a.PACycle)
            .Include(a => a.FormType)
            .FirstOrDefaultAsync(a => a.Id == paEmployeeId);

        if (appraisal == null)
        {
            throw new UserFriendlyException(L("AppraisalNotFound"));
        }

        var request = appraisal.WorkflowRequestId.HasValue
            ? await _requestRepository.FirstOrDefaultAsync(appraisal.WorkflowRequestId.Value)
            : null;

        var reviewers = await _reviewerRepository.GetAll()
            .Where(r => r.PAEmployeeId == appraisal.Id)
            .OrderBy(r => r.Id)
            .ToListAsync();

        var ownReviewer = reviewers.FirstOrDefault(r => r.EmployeeId == me);

        var myApproverSteps = request == null
            ? new List<int>()
            : await _approverRepository.GetAll()
                .Where(a => a.WorkflowRequestId == request.Id && a.ApproverId == me)
                .Select(a => a.WorkflowStep)
                .ToListAsync();

        var isApprover = request != null && myApproverSteps.Contains(request.WorkflowStep);

        var allowed = appraisal.EmployeeId == me
                      || ownReviewer != null
                      || myApproverSteps.Count > 0
                      || await _employeeScopeProvider.IsInScopeAsync(appraisal.EmployeeId, EmployeeScope.DirectReports);

        if (!allowed)
        {
            throw new AbpAuthorizationException(L("YouCannotViewThisAppraisal"));
        }

        var setting = await _stepSettingResolver.GetCurrentAsync(appraisal);
        var appraisalSettings = await _sysSettingsAccessor.GetAppraisalSettingsAsync();

        var access = AppraisalReviewAccessRules.Evaluate(new AppraisalReviewViewer
        {
            Setting = setting,
            IsApprover = isApprover,
            IsAppraisee = appraisal.EmployeeId == me,
            IsReviewer = ownReviewer != null,
            ReviewerIsPeer = ownReviewer?.IsPeer ?? false,
            ReviewSubmitted = ownReviewer?.ReviewSubmitted ?? false,
            RequestStatus = request?.RequestStatus ?? appraisal.WorkflowStatus,
            EnableMarksInPAReview = appraisalSettings?.EnableMarksInPAReview ?? false
        });

        return new ReviewState
        {
            Me = me,
            Appraisal = appraisal,
            Request = request,
            Reviewers = reviewers,
            OwnReviewer = ownReviewer,
            IsApprover = isApprover,
            Setting = setting,
            AppraisalSettings = appraisalSettings,
            Access = access
        };
    }

    private static MyAppraisalAccessDto ToAccessDto(ReviewState state)
    {
        var access = state.Access;

        return new MyAppraisalAccessDto
        {
            IsOpen = access.IsOpen,
            IsAppraisee = state.Appraisal.EmployeeId == state.Me,
            IsApprover = state.IsApprover,
            IsReviewer = state.OwnReviewer != null,
            ReviewSubmitted = state.OwnReviewer?.ReviewSubmitted ?? false,
            CanCopyKpi = access.CanCopyKpi,
            CanSaveReview = access.CanSaveReview,
            CanSubmitReview = access.CanSubmitReview,
            CanAddPeer = access.CanAddPeer,
            ShowKpiDetails = access.ShowKpiDetails,
            ShowRatings = access.ShowRatings,
            ShowReviewers = access.ShowReviewers,
            ShowAverages = access.ShowAverages,
            UseMarks = access.UseMarks
        };
    }

    /// <summary>
    /// The signed-in employee's own lines always; other reviewers' lines only where the step lets a
    /// non-reviewer see them (non-peers with "Can View Ratings", peers with "Can View Peer Ratings").
    /// </summary>
    private async Task<List<MyAppraisalRatingLineDto>> GetVisibleRatingLinesAsync(ReviewState state)
    {
        var peerReviewerIds = state.Reviewers.Where(r => r.IsPeer).Select(r => r.Id).ToHashSet();
        var visibleReviewerIds = state.Reviewers
            .Where(r => r.EmployeeId == state.Me
                        || (state.Access.SeeNonPeerRatings && !peerReviewerIds.Contains(r.Id))
                        || (state.Access.SeePeerRatings && peerReviewerIds.Contains(r.Id)))
            .Select(r => r.Id)
            .ToList();

        var lines = await _ratingRepository.GetAll()
            .Include(r => r.PAEmployeeReviewer).ThenInclude(r => r.Employee)
            .Include(r => r.PAEmployeeDetail).ThenInclude(d => d.KpiInfo).ThenInclude(k => k.KpiCategory)
            .Include(r => r.Rating)
            .Where(r => r.PAEmployeeId == state.Appraisal.Id && visibleReviewerIds.Contains(r.PAEmployeeReviewerId))
            .OrderBy(r => r.PAEmployeeReviewerId).ThenBy(r => r.PAEmployeeDetailId)
            .ToListAsync();

        return lines.Select(r => new MyAppraisalRatingLineDto
        {
            Id = r.Id,
            PAEmployeeReviewerId = r.PAEmployeeReviewerId,
            ReviewerEmployeeId = r.PAEmployeeReviewer?.EmployeeId ?? 0,
            ReviewerNameEn = Display(r.PAEmployeeReviewer?.Employee?.Code, r.PAEmployeeReviewer?.Employee?.FullNameEn),
            ReviewerNameAr = Display(r.PAEmployeeReviewer?.Employee?.Code, r.PAEmployeeReviewer?.Employee?.FullNameAr),
            IsOwn = r.PAEmployeeReviewer?.EmployeeId == state.Me,
            PAEmployeeDetailId = r.PAEmployeeDetailId,
            KpiNameEn = r.PAEmployeeDetail?.KpiInfo?.NameEn,
            KpiNameAr = r.PAEmployeeDetail?.KpiInfo?.NameAr,
            KpiCategoryNameEn = r.PAEmployeeDetail?.KpiInfo?.KpiCategory?.NameEn,
            KpiCategoryNameAr = r.PAEmployeeDetail?.KpiInfo?.KpiCategory?.NameAr,
            KpiDescription = r.PAEmployeeDetail?.KpiInfo?.Comments,
            Weight = r.PAEmployeeDetail?.Weight ?? 0,
            Target = r.PAEmployeeDetail?.Target,
            RatingId = r.RatingId,
            RatingNameEn = r.Rating?.NameEn,
            RatingNameAr = r.Rating?.NameAr,
            Marks = r.Marks,
            Comments = r.Comments
        }).ToList();
    }

    /// <summary>The reviewer list, labelled as legacy ESS labelled it.</summary>
    private async Task<List<MyAppraisalReviewerDto>> GetReviewersAsync(ReviewState state)
    {
        var appraisal = state.Appraisal;
        var today = DateTime.Today;

        var currentManagerId = await _employeeManagerRepository.GetAll()
            .Where(m => m.EmployeeId == appraisal.EmployeeId && m.StartDate <= today && m.EndDate >= today)
            .OrderBy(m => m.Id)
            .Select(m => (int?)m.ManagerId)
            .FirstOrDefaultAsync();

        // Legacy looked the relationship up without a date: a reviewer added through a relationship that
        // has since ended still reads as that relationship.
        var relationships = await _employeeRelationshipRepository.GetAll()
            .Include(r => r.RelationshipType)
            .Where(r => r.EmployeeId == appraisal.EmployeeId)
            .OrderBy(r => r.Id)
            .ToListAsync();

        var reviewerIds = state.Reviewers.Select(r => r.Id).ToList();
        var reviewers = await _reviewerRepository.GetAll()
            .Include(r => r.Employee)
            .Include(r => r.AverageRating)
            .Where(r => reviewerIds.Contains(r.Id))
            .OrderBy(r => r.Id)
            .ToListAsync();

        return reviewers.Select(r =>
        {
            string reviewerType;
            if (r.EmployeeId == appraisal.EmployeeId)
            {
                reviewerType = L("Employee");
            }
            else if (r.EmployeeId == currentManagerId)
            {
                reviewerType = L("EmployeeManager");
            }
            else
            {
                var relationship = relationships.FirstOrDefault(x => x.RelatedEmployeeId == r.EmployeeId);
                reviewerType = relationship != null
                    ? (IsArabic ? relationship.RelationshipType?.NameAr : null) ?? relationship.RelationshipType?.NameEn
                    : L("Peer");
            }

            return new MyAppraisalReviewerDto
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId,
                EmployeeCode = r.Employee?.Code,
                FullNameEn = r.Employee?.FullNameEn,
                FullNameAr = r.Employee?.FullNameAr,
                ReviewerType = reviewerType,
                IsPeer = r.IsPeer,
                ReviewSubmitted = r.ReviewSubmitted,
                AverageScore = r.AverageScore,
                TotalMarks = r.TotalMarks,
                AverageRatingNameEn = r.AverageRating?.NameEn,
                AverageRatingNameAr = r.AverageRating?.NameAr
            };
        }).ToList();
    }

    /// <summary>
    /// The active ratings, with legacy's per-reviewer balance: a rating's limit less how many of the signed-in
    /// employee's lines already carry it, never below zero.
    /// </summary>
    private async Task<List<MyAppraisalRatingOptionDto>> GetRatingOptionsAsync(ReviewState state)
    {
        var ownCounts = state.OwnReviewer == null
            ? new Dictionary<int, int>()
            : (await _ratingRepository.GetAll()
                    .Where(r => r.PAEmployeeReviewerId == state.OwnReviewer.Id && r.RatingId != null)
                    .Select(r => r.RatingId!.Value)
                    .ToListAsync())
                .GroupBy(id => id)
                .ToDictionary(g => g.Key, g => g.Count());

        var ratings = await _paRatingRepository.GetAll()
            .Where(r => r.Status)
            .ToListAsync();

        return ratings
            .OrderBy(r => int.TryParse(r.Code, out var c) ? c : int.MaxValue)
            .ThenBy(r => r.Code)
            .Select(r => new MyAppraisalRatingOptionDto
            {
                Id = r.Id,
                Code = r.Code,
                NameEn = r.NameEn,
                NameAr = r.NameAr,
                DescriptionEn = r.DescriptionEn,
                DescriptionAr = r.DescriptionAr,
                IsCommentRequired = r.IsCommentRequired,
                MaxLimit = r.MaxLimit,
                RemainingLimit = Math.Max(0, r.MaxLimit - (ownCounts.TryGetValue(r.Id, out var used) ? used : 0)),
                FromRange = r.FromRange,
                ToRange = r.ToRange
            })
            .ToList();
    }

    /// <summary>
    /// Writes the signed-in reviewer's lines and free-text boxes, then rescores through the same calculator
    /// the admin grid uses. Refuses any line that is not theirs.
    /// </summary>
    private async Task<List<PAEmployeeRating>> WriteOwnLinesAsync(ReviewState state, SaveMyAppraisalReviewInput input)
    {
        var own = state.OwnReviewer;
        var useMarks = state.Access.UseMarks;

        var ownLines = await _ratingRepository.GetAll()
            .Where(r => r.PAEmployeeReviewerId == own.Id)
            .ToListAsync();

        var lines = input.Lines ?? new List<SaveMyAppraisalRatingLineInput>();

        if (lines.Any(l => ownLines.All(o => o.Id != l.Id)))
        {
            throw new UserFriendlyException(L("YouCanOnlyRateYourOwnLines"));
        }

        var newlyAssigned = new HashSet<int>();

        foreach (var line in lines)
        {
            var row = ownLines.First(o => o.Id == line.Id);

            if (useMarks)
            {
                row.Marks = line.Marks;
            }
            else
            {
                if (line.RatingId.HasValue && line.RatingId != row.RatingId)
                {
                    newlyAssigned.Add(line.RatingId.Value);
                }

                row.RatingId = line.RatingId;
            }

            row.Comments = line.Comments;
        }

        if (newlyAssigned.Count > 0)
        {
            await EnsureRatingLimitsAsync(ownLines, newlyAssigned);
        }

        own.CustomTextField1 = input.CustomTextField1;
        own.CustomTextField2 = input.CustomTextField2;
        own.CustomTextField3 = input.CustomTextField3;

        await _reviewerRepository.UpdateAsync(own);
        await CurrentUnitOfWork.SaveChangesAsync();

        await _scoreCalculator.UpdateAverageRatingAsync(own.Id);

        return ownLines;
    }

    /// <summary>
    /// Legacy <c>GetRatingBalanceLimit</c>: a rating may sit on no more of one reviewer's lines than its
    /// "Max Limit". Checked only for ratings newly chosen in this save, so a review saved before a limit was
    /// tightened is not suddenly unsaveable.
    ///
    /// <para>
    /// <b>Legacy oddity kept:</b> a limit of zero means the rating cannot be chosen at all in self-service --
    /// legacy's balance was then zero and it refused the selection. Pinned by a test.
    /// </para>
    /// </summary>
    private async Task EnsureRatingLimitsAsync(List<PAEmployeeRating> ownLines, HashSet<int> newlyAssigned)
    {
        var ratings = await _paRatingRepository.GetAll()
            .Where(r => newlyAssigned.Contains(r.Id))
            .ToListAsync();

        foreach (var ratingId in newlyAssigned)
        {
            var rating = ratings.FirstOrDefault(r => r.Id == ratingId);
            if (rating == null || !rating.Status)
            {
                throw new UserFriendlyException(L("ThisRatingCannotBeChosen"));
            }

            var used = ownLines.Count(l => l.RatingId == ratingId);
            if (used > rating.MaxLimit)
            {
                throw new UserFriendlyException(L("RatingLimitReached", IsArabic ? rating.NameAr : rating.NameEn, rating.MaxLimit));
            }
        }
    }

    /// <summary>
    /// Legacy <c>HasRatingCompleted</c> / <c>HasCommentCompleted</c>, which hid Submit Review until they held:
    /// every line rated (on the rating scale; marks are never required), and a comment wherever the chosen
    /// rating demands one. Enforced here rather than only by hiding the button.
    /// </summary>
    private async Task EnsureReviewCompleteAsync(ReviewState state, List<PAEmployeeRating> ownLines)
    {
        if (state.Access.UseMarks)
        {
            return;
        }

        if (ownLines.Any(l => !l.RatingId.HasValue))
        {
            throw new UserFriendlyException(L("EveryKpiNeedsARatingBeforeSubmitting"));
        }

        var ratingIds = ownLines.Select(l => l.RatingId!.Value).Distinct().ToList();
        var commentRequired = await _paRatingRepository.GetAll()
            .Where(r => ratingIds.Contains(r.Id) && r.IsCommentRequired)
            .Select(r => r.Id)
            .ToListAsync();

        if (ownLines.Any(l => commentRequired.Contains(l.RatingId!.Value) && l.Comments.IsNullOrWhiteSpace()))
        {
            throw new UserFriendlyException(L("ThisRatingNeedsAComment"));
        }
    }

    /// <summary>
    /// "Auto Submit On Last Fill": when this submission leaves nobody else pending, the step is approved as
    /// its first approver, as legacy did. Runs only on the submission that moves a reviewer from pending to
    /// submitted -- a second submission is refused before it gets here -- and only while the request is still
    /// open at the step whose settings asked for it, so it cannot approve twice.
    /// </summary>
    private async Task<bool> AutoApproveOnLastFillAsync(ReviewState state)
    {
        var request = state.Request;

        if (state.Setting == null
            || !state.Setting.AutoSubmitOnLastFill
            || request == null
            || request.RequestStatus != RequestStatus.Open
            || state.Setting.Step != request.WorkflowStep)
        {
            return false;
        }

        var stillPending = await _reviewerRepository.GetAll()
            .AnyAsync(r => r.PAEmployeeId == state.Appraisal.Id && r.EmployeeId != state.Me && !r.ReviewSubmitted);
        if (stillPending)
        {
            return false;
        }

        var firstApproverId = await _approverRepository.GetAll()
            .Where(a => a.WorkflowRequestId == request.Id
                        && a.WorkflowStep == request.WorkflowStep
                        && !a.IsApproved
                        && a.ApproverId != null)
            .OrderBy(a => a.Id)
            .Select(a => a.ApproverId)
            .FirstOrDefaultAsync();

        if (!firstApproverId.HasValue)
        {
            return false;
        }

        // A remark rather than none: a step that requires a comment to approve would otherwise refuse the
        // automatic approval, and with it the reviewer's submission, forever.
        await _workflowApprovalService.ApproveAsync(request.Id, firstApproverId.Value, L("ApprovedAutomaticallyWhenTheLastReviewWasSubmitted"));

        return true;
    }

    /// <summary>The KPI lines Copy KPI would take, validated as legacy validated the copy dialog.</summary>
    private async Task<List<(int KpiInfoId, double Weight, string Target)>> ReadKpiSourceAsync(CopyMyAppraisalKpiInput input, PAEmployee appraisal)
    {
        switch (input.KpiFrom)
        {
            case KpiFrom.JobTitle:
                if (!input.KpiByJobTitleId.HasValue)
                {
                    throw new UserFriendlyException(L("KpiByJobTitleCannotBeBlank"));
                }

                return (await _kpiByJobTitleDetailRepository.GetAll()
                        .Where(d => d.KpiByJobTitleId == input.KpiByJobTitleId.Value)
                        .OrderBy(d => d.Id)
                        .Select(d => new { d.KpiInfoId, d.Weight, d.Target })
                        .ToListAsync())
                    .Select(d => (d.KpiInfoId, d.Weight, d.Target))
                    .ToList();

            case KpiFrom.OrganizationUnit:
                if (!input.KpiByOrgUnitId.HasValue)
                {
                    throw new UserFriendlyException(L("KpiByOrgUnitCannotBeBlank"));
                }

                return (await _kpiByOrgUnitDetailRepository.GetAll()
                        .Where(d => d.KpiByOrgUnitId == input.KpiByOrgUnitId.Value)
                        .OrderBy(d => d.Id)
                        .Select(d => new { d.KpiInfoId, d.Weight, d.Target })
                        .ToListAsync())
                    .Select(d => (d.KpiInfoId, d.Weight, d.Target))
                    .ToList();

            case KpiFrom.Employee:
                if (!input.SourceEmployeeId.HasValue)
                {
                    throw new UserFriendlyException(L("EmployeeCannotBeBlank"));
                }

                // The same cycle, as legacy matched on Period.
                var sourceAppraisalId = await _appraisalRepository.GetAll()
                    .Where(a => a.EmployeeId == input.SourceEmployeeId.Value
                                && a.PACycleId == appraisal.PACycleId
                                && a.Id != appraisal.Id)
                    .OrderBy(a => a.Id)
                    .Select(a => (int?)a.Id)
                    .FirstOrDefaultAsync();

                if (!sourceAppraisalId.HasValue)
                {
                    throw new UserFriendlyException(L("NoAppraisalForThisEmployeeInThisCycle"));
                }

                return (await _detailRepository.GetAll()
                        .Where(d => d.PAEmployeeId == sourceAppraisalId.Value)
                        .OrderBy(d => d.Id)
                        .Select(d => new { d.KpiInfoId, d.Weight, d.Target })
                        .ToListAsync())
                    .Select(d => (d.KpiInfoId, d.Weight, d.Target))
                    .ToList();

            default:
                throw new UserFriendlyException(L("KpiFromCannotBeBlank"));
        }
    }

    /// <summary>
    /// Who cannot be offered as a peer: the signed-in employee, the appraisee, anyone already reviewing, and --
    /// when the appraisee is choosing -- their current manager and everyone related to them through a
    /// relationship type any step of the chain auto-adds (legacy ESS RequestForm), because those people
    /// arrive on their own and must not use up the employee's allowance.
    /// </summary>
    private async Task<HashSet<int>> GetExcludedPeerIdsAsync(ReviewState state, bool includeExistingReviewers = true)
    {
        var appraisal = state.Appraisal;
        var excluded = new HashSet<int> { state.Me, appraisal.EmployeeId };

        if (includeExistingReviewers)
        {
            excluded.UnionWith(state.Reviewers.Select(r => r.EmployeeId));
        }

        if (appraisal.EmployeeId != state.Me)
        {
            return excluded;
        }

        var today = DateTime.Today;

        var currentManagerId = await _employeeManagerRepository.GetAll()
            .Where(m => m.EmployeeId == appraisal.EmployeeId && m.StartDate <= today && m.EndDate >= today)
            .OrderBy(m => m.Id)
            .Select(m => (int?)m.ManagerId)
            .FirstOrDefaultAsync();

        if (currentManagerId.HasValue)
        {
            excluded.Add(currentManagerId.Value);
        }

        if (state.Setting != null)
        {
            var settingIds = _stepSettingRepository.GetAll()
                .Where(s => s.RequestMasterId == state.Setting.RequestMasterId)
                .Select(s => s.Id);

            var relationshipTypeIds = _stepSettingRelationshipRepository.GetAll()
                .Where(r => settingIds.Contains(r.AppraisalStepSettingId))
                .Select(r => r.RelationshipTypeId);

            excluded.UnionWith(await _employeeRelationshipRepository.GetAll()
                .Where(r => r.EmployeeId == appraisal.EmployeeId && relationshipTypeIds.Contains(r.RelationshipTypeId))
                .Select(r => r.RelatedEmployeeId)
                .ToListAsync());
        }

        return excluded;
    }

    private static string Display(string code, string name)
    {
        return string.Join(" - ", new[] { code, name }.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}
