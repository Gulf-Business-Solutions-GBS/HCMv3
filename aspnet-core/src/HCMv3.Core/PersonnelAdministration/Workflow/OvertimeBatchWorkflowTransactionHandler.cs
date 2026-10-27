using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Organizations;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for a department overtime batch (<see cref="OvertimeBatch"/>) -- the legacy
/// <c>Workflow.Payroll.OvertimeRequest</c> (legacy request code "23" in
/// <c>WorkflowGeneralService.GetRequestMaster</c>'s switch).
///
/// <para>
/// NOT self-service: a manager or HR puts forward a department's overtime. The catalog row therefore has no
/// ESS route and no ESS permission, like <c>EmployeePaymentBatch</c>, and the batch is submitted through
/// <c>IAdminTransactionSubmitter</c>, routed against the person who raised it.
/// </para>
///
/// <para>
/// <b>What approval does</b> is legacy's <c>OvertimeRequest.OnSaving</c> "RequestStatus = Approved" block
/// (lines 162-191): one <see cref="EmployeeOvertime"/> per line. Legacy ran that block on every save of an
/// approved request; here it runs once, on the final approval, through
/// <see cref="IOvertimeBatchService.PostOvertimesAsync"/>, which skips lines already posted.
/// </para>
/// </summary>
public class OvertimeBatchWorkflowTransactionHandler : WorkflowTransactionHandlerBase<OvertimeBatch>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for overtime batches.</summary>
    public const string OvertimeBatchRequestType = "OvertimeBatch";

    private readonly IRepository<OvertimeBatchDetail> _detailRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IOvertimeBatchService _overtimeBatchService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public OvertimeBatchWorkflowTransactionHandler(
        IRepository<OvertimeBatch> repository,
        IRepository<OvertimeBatchDetail> detailRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IOvertimeBatchService overtimeBatchService,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _detailRepository = detailRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _overtimeBatchService = overtimeBatchService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => OvertimeBatchRequestType;

    /// <summary>
    /// Re-checks the lines at submit (and at resubmit after a send-back). The screen checks the same before it
    /// saves; this is the check that still runs for any other way a batch comes to exist, and it is the last
    /// moment before an approver is asked.
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var batch = await GetTargetAsync(context);
        if (batch == null)
        {
            return;
        }

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var lines = _detailRepository.GetAll()
                .Where(d => d.OvertimeBatchId == batch.Id)
                .ToList();

            await _overtimeBatchService.EnsureLinesAreValidAsync(batch, lines);
        }
    }

    /// <summary>
    /// Stamps the batch Approved (base), then writes the overtime rows. An amendment never posts -- it would
    /// target a batch that already has -- the same guard as <c>PaymentBatchWorkflowTransactionHandler</c>.
    /// </summary>
    public override async Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        await base.OnApprovedAsync(context);

        if (context.IsAmendment)
        {
            return;
        }

        var batch = await GetRequiredTargetAsync(context);

        await _overtimeBatchService.PostOvertimesAsync(batch);
    }

    /// <summary>The inbox label: which department, which dates, how many employee-days, how many hours.</summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var batch = await GetTargetAsync(context);
        if (batch == null)
        {
            return null;
        }

        int lineCount;
        double approvedHours;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var hours = _detailRepository.GetAll()
                .Where(d => d.OvertimeBatchId == batch.Id)
                .Select(d => d.ApprovedHours)
                .ToList();

            lineCount = hours.Count;
            approvedHours = hours.Sum();
        }

        var unitName = _organizationUnitRepository.GetAll()
            .Where(u => u.Id == batch.OrganizationUnitId)
            .Select(u => u.DisplayName)
            .FirstOrDefault();

        var total = approvedHours.ToString("0.##");
        var from = batch.StartDate.ToString("dd MMM yyyy");
        var to = batch.EndDate.ToString("dd MMM yyyy");

        return new WorkflowSummary(
            $"Overtime, {unitName ?? "Department"}, {lineCount} day(s), {total} hour(s), {from} - {to}",
            $"عمل إضافي، {unitName ?? "القسم"}، {lineCount} يوم، {total} ساعة، {from} - {to}");
    }
}
