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
