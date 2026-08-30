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
/// Workflow handler for asset issue (<see cref="EmployeeAsset"/>).
///
/// <para>
/// The rule that matters here is exclusivity: a physical asset is one object, so it cannot be issued to two
/// people over the same dates. That check has to see pending rows, not just approved ones -- otherwise two
/// employees could each request the same laptop, both requests would validate, and both would be approved by
/// different managers who never saw the other request.
/// </para>
/// </summary>
public class AssetWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeAsset>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for assets.</summary>
    public const string AssetRequestType = "EmployeeAsset";

    private readonly IRepository<Asset> _assetRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public AssetWorkflowTransactionHandler(
        IRepository<EmployeeAsset> repository,
        IRepository<Asset> assetRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _assetRepository = assetRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => AssetRequestType;

    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var assignment = await GetTargetAsync(context);
        if (assignment == null)
        {
            return;
        }

        if (assignment.EndDate.Date < assignment.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        // In-flight rows must be visible for this to mean anything -- see the class remarks.
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var alreadyIssued = Repository.GetAll()
                .Any(a => a.Id != assignment.Id
                          && a.AssetId == assignment.AssetId
                          && a.StartDate <= assignment.EndDate
                          && a.EndDate >= assignment.StartDate
                          && (a.WorkflowStatus == null
                              || a.WorkflowStatus == RequestStatus.Approved
                              || a.WorkflowStatus == RequestStatus.Closed
                              || a.WorkflowStatus == RequestStatus.Open));

            if (alreadyIssued)
            {
                throw new UserFriendlyException(L("AssetIsAlreadyIssuedForThisPeriod"));
            }
        }
    }

    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var assignment = await GetTargetAsync(context);
        if (assignment == null)
        {
            return null;
        }

        var asset = _assetRepository.GetAll().FirstOrDefault(a => a.Id == assignment.AssetId);
        var from = assignment.StartDate.ToString("dd MMM yyyy");

        return new WorkflowSummary(
            $"{asset?.NameEn ?? "Asset"}, from {from}",
            $"{asset?.NameAr ?? "عهدة"}، من {from}");
    }
}
