using System.Threading.Tasks;
using Abp.Domain.Services;
using HCMv3.Workflow;

namespace HCMv3.PerformanceManagement.Review;

/// <summary>
/// Finds the <see cref="AppraisalStepSetting"/> that governs an appraisal right now -- legacy
/// <c>PAEmployeeController.GetPARequest</c>.
/// </summary>
public interface IAppraisalStepSettingResolver : IDomainService
{
    /// <summary>
    /// The setting for the step the appraisal currently sits at, with its relationship types loaded, or
    /// null when the chain has none for that step.
    ///
    /// <para>
    /// Legacy read <c>WorkflowStep == 0 ? 1 : WorkflowStep</c> against the request master the appraisal
    /// routes to; the same here. An appraisal that has never been routed (workflow off for the type, or not
    /// yet submitted) is treated as sitting at step 1 of the chain it WOULD route to, and one that routes
    /// nowhere has no setting at all.
    /// </para>
    /// </summary>
    Task<AppraisalStepSetting> GetCurrentAsync(PAEmployee appraisal);

    /// <summary>The setting for a given step of the chain this request routes to, or null.</summary>
    Task<AppraisalStepSetting> GetForStepAsync(WorkflowRequest request, int step);

    /// <summary>The setting for a request master and step, or null.</summary>
    Task<AppraisalStepSetting> GetAsync(int requestMasterId, int step);
}
