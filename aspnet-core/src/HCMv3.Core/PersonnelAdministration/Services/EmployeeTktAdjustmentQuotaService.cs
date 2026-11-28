using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.UI;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The values a saved <see cref="EmployeeTktAdjustment"/> had before it was changed -- legacy's
/// <c>prevEmployee</c> / <c>prevTicketType</c> / <c>prevEffectiveDate</c> / <c>prevQuotaAdjusted</c>.
/// </summary>
public sealed record TktAdjustmentQuotaSnapshot(int EmployeeId, int TicketTypeId, DateTime EffectiveDate, double QuotaAdjusted)
{
    public static TktAdjustmentQuotaSnapshot Of(EmployeeTktAdjustment adjustment)
        => new(adjustment.EmployeeId, adjustment.TicketTypeId, adjustment.EffectiveDate, adjustment.QuotaAdjusted);
}

/// <summary>
/// Keeps <see cref="EmployeeTicketQuota.QuotaAdjusted"/> in step with the ticket adjustments recorded against it.
/// Port of legacy <c>EmployeeTktAdjustment.UpdateQuotaBalance</c> (OnSaving) and <c>OnDeleting</c>.
/// </summary>
public interface IEmployeeTktAdjustmentQuotaService : IDomainService
{
    /// <summary>
    /// The employee's ticket quota whose entitlement period covers <paramref name="date"/>
    /// (<c>EntitlementFrom &lt;= date &lt;= EntitlementTo</c>), or null.
    /// </summary>
    Task<EmployeeTicketQuota> FindQuotaAsync(int employeeId, int ticketTypeId, DateTime date);

    /// <summary>
    /// Legacy OnSaving: gives the previous value back to the quota it was taken from (when
    /// <paramref name="previous"/> is set, i.e. editing), then takes the new value from the quota covering the
    /// new effective date, refusing more than that quota's balance.
    /// </summary>
    Task ApplyAsync(TktAdjustmentQuotaSnapshot previous, EmployeeTktAdjustment current);

    /// <summary>Legacy OnDeleting: gives the deleted adjustment's value back to its quota.</summary>
    Task ReverseAsync(TktAdjustmentQuotaSnapshot previous);
}

public class EmployeeTktAdjustmentQuotaService : HCMv3DomainServiceBase, IEmployeeTktAdjustmentQuotaService, ITransientDependency
{
    private readonly IRepository<EmployeeTicketQuota> _quotaRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<TicketType> _ticketTypeRepository;

    public EmployeeTktAdjustmentQuotaService(
        IRepository<EmployeeTicketQuota> quotaRepository,
        IRepository<Employee> employeeRepository,
        IRepository<TicketType> ticketTypeRepository)
    {
        _quotaRepository = quotaRepository;
        _employeeRepository = employeeRepository;
        _ticketTypeRepository = ticketTypeRepository;
    }

    /// <summary>Legacy <c>EmployeeTicketQuota.QuotaBalance</c>: <c>Round(Accrued - (Used + Adjusted), 3)</c>.</summary>
    public static double Balance(EmployeeTicketQuota quota)
        => Math.Round(quota.QuotaAccrued - (quota.QuotaUsed + quota.QuotaAdjusted), 3);

    public Task<EmployeeTicketQuota> FindQuotaAsync(int employeeId, int ticketTypeId, DateTime date)
    {
        var day = date.Date;
        var quota = _quotaRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                        && r.TicketTypeId == ticketTypeId
                        && r.EntitlementFrom <= day
                        && r.EntitlementTo >= day)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
        return Task.FromResult(quota);
    }

    public async Task ApplyAsync(TktAdjustmentQuotaSnapshot previous, EmployeeTktAdjustment current)
    {
        // Legacy "Update Previous Values": give the old value back.
        EmployeeTicketQuota previousQuota = null;
        if (previous != null)
        {
            previousQuota = await GiveBackAsync(previous);
        }

        // Legacy "Update New Values".
        if (current.QuotaAdjusted == 0)
        {
            return;
        }

        var quota = await FindQuotaAsync(current.EmployeeId, current.TicketTypeId, current.EffectiveDate);
        if (quota == null)
        {
            throw await QuotaNotFoundAsync(current.EmployeeId, current.TicketTypeId);
        }

        // Same quota row as the one just given back to: use that tracked instance, which already has the old
        // value returned, so the balance check sees it.
        if (previousQuota != null && previousQuota.Id == quota.Id)
        {
            quota = previousQuota;
        }

        var balance = Balance(quota);
        if (current.QuotaAdjusted > balance)
        {
            var employee = await _employeeRepository.GetAsync(current.EmployeeId);
            var ticketType = await _ticketTypeRepository.GetAsync(current.TicketTypeId);
            throw new UserFriendlyException(L("TicketQuotaNotAvailableForAdjustment", balance, employee.Code, ticketType.NameEn));
        }

        quota.QuotaAdjusted = Math.Round(quota.QuotaAdjusted + current.QuotaAdjusted, 3);
        await _quotaRepository.UpdateAsync(quota);
    }

    public async Task ReverseAsync(TktAdjustmentQuotaSnapshot previous)
    {
        await GiveBackAsync(previous);
    }

    private async Task<EmployeeTicketQuota> GiveBackAsync(TktAdjustmentQuotaSnapshot previous)
    {
        var quota = await FindQuotaAsync(previous.EmployeeId, previous.TicketTypeId, previous.EffectiveDate);
        if (quota == null)
        {
            // Legacy throws here even when nothing was taken. A zero adjustment took nothing from any quota,
            // so there is nothing to give back and no reason to block the edit or delete.
            if (previous.QuotaAdjusted == 0)
            {
                return null;
            }

            throw await QuotaNotFoundAsync(previous.EmployeeId, previous.TicketTypeId);
        }

        quota.QuotaAdjusted = Math.Round(quota.QuotaAdjusted - previous.QuotaAdjusted, 3);
        await _quotaRepository.UpdateAsync(quota);
        return quota;
    }

    private async Task<UserFriendlyException> QuotaNotFoundAsync(int employeeId, int ticketTypeId)
    {
        var employee = await _employeeRepository.FirstOrDefaultAsync(employeeId);
        var ticketType = await _ticketTypeRepository.FirstOrDefaultAsync(ticketTypeId);
        var employeeText = employee == null ? employeeId.ToString() : $"{employee.Code} - {employee.FullNameEn}";
        return new UserFriendlyException(L("TicketQuotaNotFoundForAdjustment", employeeText, ticketType?.NameEn ?? ticketTypeId.ToString()));
    }
}
