using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.PerformanceManagement.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PerformanceManagement.Review;

/// <inheritdoc cref="IAppraisalStepSettingResolver"/>
public class AppraisalStepSettingResolver : HCMv3DomainServiceBase, IAppraisalStepSettingResolver
{
    private readonly IRepository<AppraisalStepSetting> _settingRepository;
    private readonly IRepository<AppraisalStepSettingRelationship> _relationshipRepository;
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly IRequestMasterResolver _requestMasterResolver;

    public AppraisalStepSettingResolver(
        IRepository<AppraisalStepSetting> settingRepository,
        IRepository<AppraisalStepSettingRelationship> relationshipRepository,
        IRepository<WorkflowRequest> requestRepository,
        IRequestMasterResolver requestMasterResolver)
    {
        _settingRepository = settingRepository;
        _relationshipRepository = relationshipRepository;
        _requestRepository = requestRepository;
        _requestMasterResolver = requestMasterResolver;
    }

    public async Task<AppraisalStepSetting> GetCurrentAsync(PAEmployee appraisal)
    {
        if (appraisal == null)
        {
            return null;
        }

        if (appraisal.WorkflowRequestId.HasValue)
        {
            var request = _requestRepository.GetAll().FirstOrDefault(r => r.Id == appraisal.WorkflowRequestId.Value);
            if (request != null)
            {
                // Legacy: "WorkflowStep == 0 ? 1 : WorkflowStep".
                return await GetForStepAsync(request, request.WorkflowStep <= 0 ? 1 : request.WorkflowStep);
            }
        }

        // Never routed. Legacy still asked which chain the appraisal WOULD take and read its step 1, so an
        // unrouted appraisal was governed by the creator row's settings.
        var requestMaster = await TryResolveAsync(() => _requestMasterResolver.ResolveAsync(
            AppraisalWorkflowTransactionHandler.AppraisalRequestType,
            appraisal.EmployeeId,
            appraisal.FormTypeId));

        return requestMaster == null ? null : await GetAsync(requestMaster.Id, 1);
    }

    public async Task<AppraisalStepSetting> GetForStepAsync(WorkflowRequest request, int step)
    {
        if (request == null)
        {
            return null;
        }

        var requestMaster = await TryResolveAsync(() => _requestMasterResolver.ResolveAsync(request));

        return requestMaster == null ? null : await GetAsync(requestMaster.Id, step);
    }

    public Task<AppraisalStepSetting> GetAsync(int requestMasterId, int step)
    {
        // Lowest id first: the setup screen refuses a second row for the same step, but rows created before
        // that rule existed would otherwise be picked in whatever order the database returns them.
        var setting = _settingRepository.GetAll()
            .Where(s => s.RequestMasterId == requestMasterId && s.Step == step)
            .OrderBy(s => s.Id)
            .FirstOrDefault();

        if (setting != null)
        {
            setting.Relationships = _relationshipRepository.GetAll()
                .Where(r => r.AppraisalStepSettingId == setting.Id)
                .ToList();
        }

        return Task.FromResult(setting);
    }

    /// <summary>
    /// Legacy <c>GetRequestMaster</c> returned null when nothing matched, and every caller then treated the
    /// appraisal as having no step settings. HCMv3's resolver throws instead, which is right for routing but
    /// wrong for a question of "what may I see": an appraisal whose chain is not configured simply offers
    /// nothing, as it did in legacy, rather than failing to open.
    /// </summary>
    private static async Task<RequestMaster> TryResolveAsync(System.Func<Task<RequestMaster>> resolve)
    {
        try
        {
            return await resolve();
        }
        catch (UserFriendlyException)
        {
            return null;
        }
    }
}
