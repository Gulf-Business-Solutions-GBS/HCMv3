using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GoLiveSetupV2;

/// <summary>
/// From a comparison line the legacy payroll paid and HCMv3 did not, to the payment that makes the next run pay it:
/// a record on the Payment screen, for the month, for the legacy amount.
///
/// <para><b>Only one-off payments and deductions.</b> The payroll engine takes a wage type in a fixed order --
/// arrears, then what is on the salary record, then the Payment screen, then leave pay and the other calculated
/// pays -- so a payment on a salary wage type would never be paid as one, and a payment on leave pay would pay
/// it twice over. A wage type qualifies when it is an earning or a deduction and is not a salary wage type
/// ("include in basic pay"), an arrear, leave pay, or net pay.</para>
///
/// <para>Each payment carries <see cref="Comment"/>, so adding the same line again corrects that payment
/// instead of making a second one. Every rule is checked again here, whatever the screen offered.</para>
/// </summary>
public class GoLiveV2LegacyPayments : AbpServiceBase, ITransientDependency
{
    /// <summary>Marks a payment as one this comparison made. Also what the Payment screen shows.</summary>
    public const string Comment = "Go-Live Setup V2: from the legacy salary comparison";

    private const string NetPayCode = "9000";

    private readonly IEmployeePaymentsAppService _payments;
    private readonly IRepository<EmployeePayment> _paymentRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IEmployeeLeavePayCalculationService _leavePay;

    public GoLiveV2LegacyPayments(
        IEmployeePaymentsAppService payments,
        IRepository<EmployeePayment> paymentRepository,
        IRepository<Employee> employeeRepository,
        IRepository<WageType> wageTypeRepository,
        IEmployeeLeavePayCalculationService leavePay)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;

        _payments = payments;
        _paymentRepository = paymentRepository;
        _employeeRepository = employeeRepository;
        _wageTypeRepository = wageTypeRepository;
        _leavePay = leavePay;
    }

    /// <summary>The codes of the wage types a payment may be added for -- see the class remarks.</summary>
    public async Task<HashSet<string>> PayableWageTypeCodesAsync()
    {
        var leavePayIds = (await _leavePay.GetLeavePayWageTypeIdsAsync()).ToHashSet();

        var candidates = await _wageTypeRepository.GetAll()
            .Where(w => (w.BillingType == BillingType.Earning || w.BillingType == BillingType.Deduction)
                        && !w.IncludeInBasicPay
                        && !w.IncludeInArrearPay
                        && w.Code != NetPayCode)
            .Select(w => new { w.Id, w.Code })
            .ToListAsync();

        return candidates
            .Where(w => !leavePayIds.Contains(w.Id))
            .Select(w => w.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<GoLiveV2LegacyPaymentsOutput> AddAsync(GoLiveV2LegacyPaymentsInput input, DateTime monthEnd)
    {
        var output = new GoLiveV2LegacyPaymentsOutput();
        var lines = input.Lines ?? new List<GoLiveV2LegacyPaymentLineDto>();
        if (lines.Count == 0)
        {
            return output;
        }

        var monthStart = new DateTime(monthEnd.Year, monthEnd.Month, 1);
        var payable = await PayableWageTypeCodesAsync();

        var codes = lines.Select(l => l.EmployeeCode?.Trim()).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var employees = (await _employeeRepository.GetAll()
                .Where(e => codes.Contains(e.Code))
                .Select(e => new { e.Id, e.Code })
                .ToListAsync())
            .GroupBy(e => e.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var wageCodes = lines.Select(l => l.WageTypeCode?.Trim()).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var wageTypes = (await _wageTypeRepository.GetAll()
                .Where(w => wageCodes.Contains(w.Code))
                .Select(w => new { w.Id, w.Code })
                .ToListAsync())
            .GroupBy(w => w.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var employeeCode = line.EmployeeCode?.Trim() ?? string.Empty;
            var wageCode = line.WageTypeCode?.Trim() ?? string.Empty;

            string refusal = null;
            if (!employees.TryGetValue(employeeCode, out var employeeId))
            {
                refusal = L("GoLiveV2CompareEmployeeNotLoaded");
            }
            else if (!wageTypes.TryGetValue(wageCode, out _))
            {
                refusal = L("GoLiveV2CompareWageTypeMissing", wageCode);
            }
            else if (!payable.Contains(wageCode))
            {
                refusal = L("GoLiveV2PaymentNotPayable", wageCode);
            }
            else if (line.Amount == 0)
            {
                refusal = L("GoLiveV2PaymentNoAmount");
            }

            if (refusal != null)
            {
                output.Refused.Add(new GoLiveV2LegacyPaymentRefusalDto { EmployeeCode = employeeCode, WageTypeCode = wageCode, Reason = refusal });
                continue;
            }

            var wageTypeId = wageTypes[wageCode];
            var amount = Math.Round((double)line.Amount, 3);

            var existing = await _paymentRepository.GetAll()
                .Where(p => p.EmployeeId == employeeId
                            && p.WageTypeId == wageTypeId
                            && p.PaymentDate >= monthStart
                            && p.PaymentDate <= monthEnd
                            && p.Comments == Comment)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            if (existing != null && Math.Abs(existing.Amount - amount) < 0.0005)
            {
                output.AlreadyThere++;
                continue;
            }

            try
            {
                await _payments.CreateOrEdit(new CreateOrEditEmployeePaymentDto
                {
                    Id = existing?.Id,
                    EmployeeId = employeeId,
                    WageTypeId = wageTypeId,
                    Amount = amount,
                    PaymentDate = monthEnd,
                    UnitType = UnitType.Amount,
                    CurrencyId = existing?.CurrencyId,
                    CostCenterId = existing?.CostCenterId,
                    Comments = Comment,
                });
                await CurrentUnitOfWork.SaveChangesAsync();
            }
            catch (UserFriendlyException ex)
            {
                // The Payment screen's own checks, in its own words.
                output.Refused.Add(new GoLiveV2LegacyPaymentRefusalDto { EmployeeCode = employeeCode, WageTypeCode = wageCode, Reason = ex.Message });
                continue;
            }

            if (existing == null)
            {
                output.Added++;
            }
            else
            {
                output.Updated++;
            }
        }

        return output;
    }
}
