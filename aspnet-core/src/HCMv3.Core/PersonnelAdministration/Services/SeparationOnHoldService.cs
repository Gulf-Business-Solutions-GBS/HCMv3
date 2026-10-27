using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="ISeparationOnHoldService"/>
public class SeparationOnHoldService : HCMv3DomainServiceBase, ISeparationOnHoldService, ITransientDependency
{
    private readonly IRepository<EmployeeOnHold> _onHoldRepository;
    private readonly IEmployeeOnHoldService _onHoldService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public SeparationOnHoldService(
        IRepository<EmployeeOnHold> onHoldRepository,
        IEmployeeOnHoldService onHoldService,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _onHoldRepository = onHoldRepository;
        _onHoldService = onHoldService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>True when any of legacy's eight hold flags is ticked (legacy SaveOtherInfo, lines 510-513).</summary>
    public static bool HasAnyHoldFlag(EmployeeSeparation separation)
    {
        return separation.OnHoldAll
               || separation.OnHoldBasicPay
               || separation.OnHoldAirTicketPay
               || separation.OnHoldLoanDeduction
               || separation.OnHoldLeavePay
               || separation.OnHoldIndemnityPay
               || separation.OnHoldAllEarnings
               || separation.OnHoldAllDeductions;
    }

    /// <inheritdoc/>
    public Task EnsureNoLaterOnHoldAsync(
        EmployeeSeparation separation,
        int? previousEmployeeId,
        DateTime? previousOnHoldFromDate)
    {
        // Legacy ValidatePreviousEntry (lines 642-662): runs for an existing separation, on edit and on delete,
        // against the values as they were stored.
        if (previousEmployeeId.HasValue && previousOnHoldFromDate.HasValue)
        {
            var previousFrom = previousOnHoldFromDate.Value.Date;
            var later = _onHoldRepository.GetAll()
                .Where(o => o.EmployeeId == previousEmployeeId.Value && o.StartDate.Date > previousFrom)
                .Select(o => new { o.StartDate, o.EndDate })
                .FirstOrDefault();

            if (later != null)
            {
                ThrowLaterOnHold(later.StartDate, later.EndDate);
            }
        }

        // Legacy PerformValidations (lines 418-447): the values being saved, ignoring the hold this separation
        // itself created last time -- that one is about to be replaced.
        if (separation?.OnHoldFromDate != null)
        {
            var excludeId = FindOwnHold(previousEmployeeId, previousOnHoldFromDate)?.Id ?? 0;
            var from = separation.OnHoldFromDate.Value.Date;

            var later = _onHoldRepository.GetAll()
                .Where(o => o.EmployeeId == separation.EmployeeId
                            && o.StartDate.Date > from
                            && o.Id != excludeId)
                .Select(o => new { o.StartDate, o.EndDate })
                .FirstOrDefault();

            if (later != null)
            {
                ThrowLaterOnHold(later.StartDate, later.EndDate);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task ApplyAsync(EmployeeSeparation separation, int? previousEmployeeId, DateTime? previousOnHoldFromDate)
    {
        // Legacy lines 500-508: the previous hold goes first, whatever happens next.
        await DeleteOwnHoldAsync(previousEmployeeId, previousOnHoldFromDate);

        // Legacy lines 510-513: a from-date AND at least one flag. A from-date alone creates nothing.
        if (!separation.OnHoldFromDate.HasValue || !HasAnyHoldFlag(separation))
        {
            return;
        }

        // Legacy lines 515-518, writing back onto the separation itself. Whole months, first to last day.
        // A missing till-date is legacy's default(DateTime): it normalises to 31 Jan 0001 and the hold's own
        // date check refuses it, exactly as it did there.
        var from = separation.OnHoldFromDate.Value;
        var till = separation.OnHoldTillDate ?? DateTime.MinValue;

        separation.OnHoldFromDate = new DateTime(from.Year, from.Month, 1);
        separation.OnHoldTillDate = new DateTime(till.Year, till.Month, DateTime.DaysInMonth(till.Year, till.Month));

        // Legacy lines 521-536, field for field. Comments and EmployeeAction were not copied there either.
        var onHold = new EmployeeOnHold
        {
            TenantId = separation.TenantId,
            EmployeeId = separation.EmployeeId,
            StartDate = separation.OnHoldFromDate.Value,
            EndDate = separation.OnHoldTillDate.Value,
            OnHold = separation.OnHoldAll,
            OnHoldBasicPay = separation.OnHoldBasicPay,
            OnHoldAirTicketPay = separation.OnHoldAirTicketPay,
            OnHoldLoanDeduction = separation.OnHoldLoanDeduction,
            OnHoldLeavePay = separation.OnHoldLeavePay,
            OnHoldIndemnityPay = separation.OnHoldIndemnityPay,
            OnHoldAllEarnings = separation.OnHoldAllEarnings,
            OnHoldAllDeductions = separation.OnHoldAllDeductions,
            BankTransfer = separation.BankTransfer
        };

        // The same rules a hand-entered hold meets (legacy: EmployeeOnHold.OnSaving ran for this row too).
        await _onHoldService.ValidateAsync(onHold);
        await _onHoldRepository.InsertAsync(onHold);
    }

    /// <inheritdoc/>
    public Task RemoveAsync(int employeeId, DateTime? onHoldFromDate)
    {
        // Legacy OnDeleting, lines 332-340.
        return DeleteOwnHoldAsync(employeeId, onHoldFromDate);
    }

    private EmployeeOnHold FindOwnHold(int? employeeId, DateTime? onHoldFromDate)
    {
        if (!employeeId.HasValue || !onHoldFromDate.HasValue)
        {
            return null;
        }

        var from = onHoldFromDate.Value.Date;

        return _onHoldRepository.GetAll()
            .FirstOrDefault(o => o.EmployeeId == employeeId.Value && o.StartDate.Date == from);
    }

    private async Task DeleteOwnHoldAsync(int? employeeId, DateTime? onHoldFromDate)
    {
        var existing = FindOwnHold(employeeId, onHoldFromDate);
        if (existing == null)
        {
            return;
        }

        await _onHoldRepository.DeleteAsync(existing);

        // Saved now, not at the end of the request: the overlap check that guards the replacement hold reads
        // the database, and an unsaved soft delete would make the old hold conflict with its own replacement.
        await _unitOfWorkManager.Current.SaveChangesAsync();
    }

    private void ThrowLaterOnHold(DateTime startDate, DateTime endDate)
    {
        throw new UserFriendlyException(L(
            "SeparationBlockedByLaterOnHold",
            startDate.ToString("dd/MM/yyyy"),
            endDate.ToString("dd/MM/yyyy")));
    }
}
