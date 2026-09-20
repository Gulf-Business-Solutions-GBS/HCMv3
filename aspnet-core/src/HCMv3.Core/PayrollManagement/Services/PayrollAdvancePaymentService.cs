using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Services;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IPayrollAdvancePaymentService"/>
/// <remarks>
/// <c>HCMv3.Core</c> has no EF Core reference, so reads use the plain synchronous <c>IQueryable</c> from
/// <c>IRepository&lt;T&gt;.GetAll()</c> (same shape as <c>AccrualService</c>); writes use the repositories'
/// genuine async methods.
/// </remarks>
[UnitOfWork]
public class PayrollAdvancePaymentService : HCMv3DomainServiceBase, IPayrollAdvancePaymentService
{
    private readonly IRepository<EmployeePayment> _employeePaymentRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly ICurrencyService _currencyService;

    public PayrollAdvancePaymentService(
        IRepository<EmployeePayment> employeePaymentRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<Employee> employeeRepository,
        ICurrencyService currencyService)
    {
        _employeePaymentRepository = employeePaymentRepository;
        _wageTypeRepository = wageTypeRepository;
        _employeeRepository = employeeRepository;
        _currencyService = currencyService;
    }

    /// <inheritdoc/>
    public async Task AdvancePaymentDedAsync(
        int employeeId,
        DateTime paymentDate,
        DateTime offCyclePaymentDate,
        double amount,
        PayrollRunType payrollRunType,
        string offCycleReason,
        string advancePayComment,
        PayrollEngineSettings settings)
    {
        // Legacy line 2942: no-op on a zero amount.
        if (amount == 0)
        {
            return;
        }

        // Legacy lines 2944-2947: End-Of-Service off-cycle never creates an advance recovery.
        if (payrollRunType == PayrollRunType.OffCycle && offCycleReason == PayrollOffCycleReasons.EndOfService)
        {
            return;
        }

        var decimals = settings?.DecimalInSalaryRegister ?? 6;

        // Legacy's sentinel for "regular run" is default(DateTime); stored as NULL here.
        DateTime? offCycleKey = offCyclePaymentDate == default(DateTime)
            ? (DateTime?)null
            : offCyclePaymentDate;

        // Legacy lines 2948-2953 -- the FULL four-part upsert key. OffCyclePaymentDate is part of it:
        // dropping it merges recovery rows across different off-cycle runs and over-deducts.
        var existing = (from payment in _employeePaymentRepository.GetAll()
                        join wageType in _wageTypeRepository.GetAll() on payment.WageTypeId equals wageType.Id
                        where payment.EmployeeId == employeeId
                              && payment.PaymentDate == paymentDate
                              && payment.OffCyclePaymentDate == offCycleKey
                              && wageType.Code == PayrollEngineWageCodes.AdvancePaymentDeduction
                        select payment).FirstOrDefault();

        if (existing == null)
        {
            var wageType2005 = _wageTypeRepository.GetAll()
                .FirstOrDefault(w => w.Code == PayrollEngineWageCodes.AdvancePaymentDeduction);

            if (wageType2005 == null)
            {
                // Legacy Program.GetWageType (line 2748) throws when the code is missing.
                throw new InvalidOperationException(
                    $"WageType {PayrollEngineWageCodes.AdvancePaymentDeduction} not found");
            }

            var currency = await _currencyService.GetEmployeeCurrencyAsync(employeeId);
            if (currency == null)
            {
                // Legacy lines 2962-2963.
                var employeeCode = _employeeRepository.GetAll()
                    .Where(e => e.Id == employeeId)
                    .Select(e => e.Code)
                    .FirstOrDefault();

                throw new InvalidOperationException(
                    string.Format("Employee : {0} WageDetail has blank currency.", employeeCode));
            }

            var tenantId = _employeeRepository.GetAll()
                .Where(e => e.Id == employeeId)
                .Select(e => e.TenantId)
                .FirstOrDefault();

            existing = new EmployeePayment
            {
                TenantId = tenantId,
                EmployeeId = employeeId,
                PaymentDate = paymentDate,
                OffCyclePaymentDate = offCycleKey,
                WageTypeId = wageType2005.Id,
                CurrencyId = currency.Id,
                UnitType = UnitType.Amount,
                Comments = advancePayComment,
                Amount = 0
            };
        }

        // Legacy line 2982.
        existing.Amount = Math.Round(existing.Amount + amount, decimals);

        // Legacy line 2983.
        existing.IsSystemGenerated = true;

        await _employeePaymentRepository.InsertOrUpdateAsync(existing);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteAdvancePaymentDedAsync(int employeeId, DateTime offCyclePaymentDate)
    {
        // Legacy's sentinel for "no off-cycle" is default(DateTime); the column stores NULL. A run without a
        // payment date has nothing keyed to it, so there is nothing to delete.
        if (offCyclePaymentDate.Date == default(DateTime))
        {
            return 0;
        }

        var rows = AdvanceRecoveryRows()
            .Where(p => p.EmployeeId == employeeId && p.OffCyclePaymentDate == offCyclePaymentDate.Date)
            .ToList();

        foreach (var row in rows)
        {
            await _employeePaymentRepository.DeleteAsync(row);
        }

        return rows.Count;
    }

    /// <inheritdoc/>
    public async Task<int> ZeroFutureAdvanceRecoveryAsync(int employeeId, DateTime payDate, bool hasAdvancePaidLeaveOnPayDate)
    {
        // Legacy lines 1623-1630: the leave is still there, so its recovery must continue.
        if (hasAdvancePaidLeaveOnPayDate)
        {
            return 0;
        }

        var payDateOnly = payDate.Date;

        // Legacy lines 1632-1636. Note BOTH bounds: keyed to an off-cycle paid ON payDate, and only the
        // months AFTER it -- the current month's advance was really paid and is still owed.
        var rows = AdvanceRecoveryRows()
            .Where(p => p.EmployeeId == employeeId
                        && p.OffCyclePaymentDate == payDateOnly
                        && p.PaymentDate > payDateOnly)
            .ToList();

        return await ZeroAsync(rows);
    }

    /// <inheritdoc/>
    public async Task<int> ZeroAdvanceRecoveryInWindowAsync(int employeeId, DateTime payStartDate, DateTime offCycleEndDate)
    {
        var start = payStartDate.Date;
        var end = offCycleEndDate.Date;

        // Legacy lines 1800-1803: the window applies to PaymentDate and OffCyclePaymentDate alike.
        var rows = AdvanceRecoveryRows()
            .Where(p => p.EmployeeId == employeeId
                        && p.PaymentDate >= start
                        && p.PaymentDate <= end
                        && p.OffCyclePaymentDate >= start
                        && p.OffCyclePaymentDate <= end)
            .ToList();

        return await ZeroAsync(rows);
    }

    /// <inheritdoc/>
    public async Task<int> ZeroLastMonthRecoveryAsync(int employeeId, DateTime payDate)
    {
        // Legacy lines 1833-1834.
        var nextMonth = payDate.Date.AddMonths(1);
        var nextMonthPayDay = new DateTime(nextMonth.Year, nextMonth.Month, 1, 0, 0, 0);

        var rows = (from payment in _employeePaymentRepository.GetAll()
                    join wageType in _wageTypeRepository.GetAll() on payment.WageTypeId equals wageType.Id
                    where payment.EmployeeId == employeeId
                          && payment.PaymentDate == nextMonthPayDay
                          && wageType.Code == PayrollEngineWageCodes.LastMonthRecoveryDeduction
                    select payment).ToList();

        return await ZeroAsync(rows);
    }

    /// <summary>The employee's wage-code-2005 rows, joined the same way <see cref="AdvancePaymentDedAsync"/> does.</summary>
    private IQueryable<EmployeePayment> AdvanceRecoveryRows()
    {
        return from payment in _employeePaymentRepository.GetAll()
               join wageType in _wageTypeRepository.GetAll() on payment.WageTypeId equals wageType.Id
               where wageType.Code == PayrollEngineWageCodes.AdvancePaymentDeduction
               select payment;
    }

    /// <summary>
    /// Legacy's three clearing blocks all do the same two assignments (<c>Amount = 0</c>,
    /// <c>IsSystemGenerated = true</c>) and save. They ZERO rather than delete on purpose: the row stays
    /// visible on the employee's payments screen as evidence that a recovery was raised and then cancelled.
    /// </summary>
    private async Task<int> ZeroAsync(List<EmployeePayment> rows)
    {
        foreach (var row in rows)
        {
            row.Amount = 0;
            row.IsSystemGenerated = true;
            await _employeePaymentRepository.UpdateAsync(row);
        }

        return rows.Count;
    }

    /// <inheritdoc/>
    public List<DateTime> GetRecoveryPeriods(DateTime payStartDate, DateTime payEndDate)
    {
        var recoveryPeriods = new List<DateTime>();
        var cursor = new DateTime(payStartDate.Year, payStartDate.Month, 1, 0, 0, 0);

        while (cursor <= payEndDate)
        {
            var monthStart = new DateTime(cursor.Year, cursor.Month, 1, 0, 0, 0);
            if (!recoveryPeriods.Contains(monthStart))
            {
                recoveryPeriods.Add(monthStart);
            }

            var daysInMonth = DateTime.DaysInMonth(cursor.Year, cursor.Month);
            cursor = new DateTime(cursor.Year, cursor.Month, daysInMonth, 0, 0, 0).AddDays(1);
        }

        return recoveryPeriods;
    }
}
