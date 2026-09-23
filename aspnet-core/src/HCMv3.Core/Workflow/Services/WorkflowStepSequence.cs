using System.Linq;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;

namespace HCMv3.Workflow.Services;

/// <inheritdoc cref="IWorkflowStepSequence"/>
[UnitOfWork]
public class WorkflowStepSequence : HCMv3DomainServiceBase, IWorkflowStepSequence
{
    private readonly IRepository<RequestDetail> _requestDetailRepository;

    public WorkflowStepSequence(IRepository<RequestDetail> requestDetailRepository)
    {
        _requestDetailRepository = requestDetailRepository;
    }

    public int GetFirstApproverStep(RequestMaster requestMaster)
    {
        if (requestMaster == null)
        {
            return 1;
        }

        // DO NOT "simplify" this to a constant. It is not 1 (that may be the creator row, which nominates
        // nobody and would leave the request assigned to the default approver or to no one), and it is not 2
        // (LEAVE-STD and MANPOWER-STD have a real approver ON step 1 and no step 2 at all). It is whichever
        // step first names an approver, which gives the right answer for both shapes.
        var firstApproverStep = _requestDetailRepository.GetAll()
            .Where(d => d.RequestMasterId == requestMaster.Id
                        && d.ApproverPerson != WorkflowApproverPersonType.None)
            .OrderBy(d => d.Step)
            .Select(d => (int?)d.Step)
            .FirstOrDefault();

        // No row names an approver -- either the master has no step rows at all, or every row is a creator
        // row. Step 1 is then what the engine has always used, and routing falls through to the master's
        // default approver exactly as before.
        return firstApproverStep ?? 1;
    }

    public int? GetNextApproverStep(RequestMaster requestMaster, int currentStep)
    {
        if (requestMaster == null)
        {
            return null;
        }

        var laterSteps = _requestDetailRepository.GetAll()
            .Where(d => d.RequestMasterId == requestMaster.Id && d.Step > currentStep);

        var nextApproverStep = laterSteps
            .Where(d => d.ApproverPerson != WorkflowApproverPersonType.None)
            .OrderBy(d => d.Step)
            .Select(d => (int?)d.Step)
            .FirstOrDefault();

        if (nextApproverStep.HasValue)
        {
            return nextApproverStep;
        }

        // Nothing above names an approver. If rows DO exist above this step they are creator rows, so the
        // chain genuinely ends here -- advancing onto one would leave the request waiting on nobody.
        var hasLaterRows = laterSteps.Any();
        if (hasLaterRows)
        {
            return null;
        }

        // No step rows above this one at all. This is the pre-existing shape where a master is configured
        // only by its WorkflowSteps count and routing falls to the default approver, so the old arithmetic
        // is what keeps those chains running to their configured length.
        return currentStep < requestMaster.WorkflowSteps ? currentStep + 1 : (int?)null;
    }

    public RequestDetail GetCreatorStep(RequestMaster requestMaster)
    {
        if (requestMaster == null)
        {
            return null;
        }

        return _requestDetailRepository.GetAll()
            .Where(d => d.RequestMasterId == requestMaster.Id
                        && d.ApproverPerson == WorkflowApproverPersonType.None)
            .OrderBy(d => d.Step)
            .FirstOrDefault();
    }
}
