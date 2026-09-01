using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for HR service requests (<see cref="EmployeeHRService"/>) -- certificates, letters,
/// card replacements.
///
/// <para>
/// Thin on purpose. Legacy's <c>PerformValidation</c> checked three things and nothing else -- employee,
/// service date, service type -- and its approval branch was an empty <c>if</c> block. So this handler
/// restates those three checks and stops there; it does not override <c>OnApprovedAsync</c>, because
/// inventing a side effect the legacy system never had would change what approving one of these means.
/// </para>
///
/// <para>
/// There is deliberately no duplicate check either. Unlike an asset or a ticket, asking for a second salary
/// certificate is a perfectly ordinary thing to do, and refusing it would be a new rule rather than a port.
/// </para>
/// </summary>
public class HRServiceWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeHRService>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for HR services.</summary>
    public const string HRServiceRequestType = "EmployeeHRService";

    private readonly IRepository<HRServiceType> _serviceTypeRepository;

    public HRServiceWorkflowTransactionHandler(
        IRepository<EmployeeHRService> repository,
        IRepository<HRServiceType> serviceTypeRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _serviceTypeRepository = serviceTypeRepository;
    }

    public override string RequestType => HRServiceRequestType;

    /// <summary>
    /// The legacy required-field checks, restated where they hold for every path into the row rather than
    /// only the screen that happened to raise it.
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var service = await GetTargetAsync(context);
        if (service == null)
        {
            return;
        }

        if (service.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeIsRequired"));
        }

        if (service.HRServiceTypeId <= 0)
        {
            throw new UserFriendlyException(L("ServiceTypeIsRequired"));
        }

        if (service.ServiceDate == default)
        {
            throw new UserFriendlyException(L("ServiceDateIsRequired"));
        }
    }

    /// <summary>
    /// The service and when it is wanted -- "Salary Certificate, 12 Sep 2026". That pair is the whole
    /// decision for this type; there is nothing else an approver is weighing up.
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var service = await GetTargetAsync(context);
        if (service == null)
        {
            return null;
        }

        var serviceType = _serviceTypeRepository.GetAll()
            .Where(t => t.Id == service.HRServiceTypeId)
            .Select(t => new { t.NameEn, t.NameAr })
            .FirstOrDefault();

        var on = service.ServiceDate.ToString("dd MMM yyyy");

        return new WorkflowSummary(
            $"{serviceType?.NameEn ?? "HR service"}, {on}",
            $"{serviceType?.NameAr ?? "خدمة موارد بشرية"}، {on}");
    }
}
