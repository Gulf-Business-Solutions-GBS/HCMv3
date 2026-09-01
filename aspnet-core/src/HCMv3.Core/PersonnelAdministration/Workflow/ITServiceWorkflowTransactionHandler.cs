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
/// Workflow handler for IT service requests (<see cref="EmployeeITService"/>).
///
/// <para>
/// The second type that is routed but NOT self-service, after <see cref="EmployeeActionWorkflowTransactionHandler"/>.
/// Legacy has a back-office screen for these and no ESS surface at all, so the catalog row carries no
/// <c>EssRoute</c> and no <c>EssPermissionName</c>, and submission happens in
/// <c>EmployeeITServicesAppService</c> through <c>IAdminTransactionSubmitter</c> rather than in an ESS
/// service. Nothing about the engine changes for that -- which is the point of the two flags on the catalog
/// row existing separately.
/// </para>
///
/// <para>
/// Validation is the legacy required-field trio and nothing more, and there is no approval side effect:
/// legacy's approval branch was an empty <c>if</c>. Notably, legacy ran even those three checks inside a
/// <c>catch (Exception) { }</c> that discarded them, so a request with no employee and no service type
/// saved. Here they actually refuse the submission.
/// </para>
/// </summary>
public class ITServiceWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeITService>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for IT services.</summary>
    public const string ITServiceRequestType = "EmployeeITService";

    private readonly IRepository<ITServiceType> _serviceTypeRepository;

    public ITServiceWorkflowTransactionHandler(
        IRepository<EmployeeITService> repository,
        IRepository<ITServiceType> serviceTypeRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _serviceTypeRepository = serviceTypeRepository;
    }

    public override string RequestType => ITServiceRequestType;

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

        if (service.ITServiceTypeId <= 0)
        {
            throw new UserFriendlyException(L("ServiceTypeIsRequired"));
        }

        if (service.ServiceDate == default)
        {
            throw new UserFriendlyException(L("ServiceDateIsRequired"));
        }
    }

    /// <summary>
    /// The service and when it is wanted -- "Laptop Replacement, 12 Sep 2026". The inbox shows who it is
    /// about separately, which matters more here than for a self-service type because somebody in IT or HR
    /// raised it on the employee's behalf.
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var service = await GetTargetAsync(context);
        if (service == null)
        {
            return null;
        }

        var serviceType = _serviceTypeRepository.GetAll()
            .Where(t => t.Id == service.ITServiceTypeId)
            .Select(t => new { t.NameEn, t.NameAr })
            .FirstOrDefault();

        var on = service.ServiceDate.ToString("dd MMM yyyy");

        return new WorkflowSummary(
            $"{serviceType?.NameEn ?? "IT service"}, {on}",
            $"{serviceType?.NameAr ?? "خدمة تقنية المعلومات"}، {on}");
    }
}
