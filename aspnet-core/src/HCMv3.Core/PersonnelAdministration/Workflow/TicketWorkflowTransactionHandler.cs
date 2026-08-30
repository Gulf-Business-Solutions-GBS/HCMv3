using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for air tickets (<see cref="EmployeeTicket"/>).
///
/// <para>
/// A ticket entitlement is periodic -- an employee earns one every so many years -- so the check that matters
/// is not a running balance but whether one has already been taken in the same window. Pending requests
/// count, otherwise two applications raised in the same week would each see the other as absent.
/// </para>
/// </summary>
public class TicketWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeTicket>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for tickets.</summary>
    public const string TicketRequestType = "EmployeeTicket";

    private readonly IRepository<TicketType> _ticketTypeRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public TicketWorkflowTransactionHandler(
        IRepository<EmployeeTicket> repository,
        IRepository<TicketType> ticketTypeRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _ticketTypeRepository = ticketTypeRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => TicketRequestType;

    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var ticket = await GetTargetAsync(context);
        if (ticket == null)
        {
            return;
        }

        if (ticket.Amount < 0)
        {
            throw new UserFriendlyException(L("TicketAmountCannotBeNegative"));
        }

        // Same employee, same ticket type, same calendar year. Coarse on purpose: the precise entitlement
        // period is a payroll-side calculation, and refusing an obvious repeat here guards against a
        // duplicate submission rather than replacing that calculation.
        var year = ticket.TicketDate.Year;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var alreadyTaken = Repository.GetAll()
                .Any(t => t.Id != ticket.Id
                          && t.EmployeeId == ticket.EmployeeId
                          && t.TicketTypeId == ticket.TicketTypeId
                          && t.TicketDate.Year == year
                          && (t.WorkflowStatus == null
                              || t.WorkflowStatus == RequestStatus.Approved
                              || t.WorkflowStatus == RequestStatus.Closed
                              || t.WorkflowStatus == RequestStatus.Open));

            if (alreadyTaken)
            {
                throw new UserFriendlyException(L("ATicketOfThisTypeHasAlreadyBeenTakenThisYear"));
            }
        }
    }

    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var ticket = await GetTargetAsync(context);
        if (ticket == null)
        {
            return null;
        }

        var ticketType = _ticketTypeRepository.GetAll().FirstOrDefault(t => t.Id == ticket.TicketTypeId);
        var on = ticket.TicketDate.ToString("dd MMM yyyy");
        var amount = ticket.Amount.ToString("0.###");

        return new WorkflowSummary(
            $"{ticketType?.NameEn ?? "Air Ticket"}, {on}, {amount}",
            $"{ticketType?.NameAr ?? "تذكرة"}، {on}، {amount}");
    }
}
