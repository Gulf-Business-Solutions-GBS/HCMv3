using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 23 -- Fund Deduction.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 744-786 (branch) and 3633-3849
/// (<c>GetFundDed</c>).
///
/// <para><b>Side effects are the point of this rule.</b> Unlike most calculators it MUTATES master data:
/// it creates the employee's <see cref="EmployeeFund"/> row if absent, deletes and re-creates the month's
/// <c>Opening</c> and <c>Deduction</c> <see cref="EmployeeFundDetail"/> rows, and re-totals
/// Amount/PaidAmount/BalanceAmount. Legacy does all of this too (lines 3653-3832).</para>
///
/// <para><b>ORDERING REQUIREMENT.</b> Legacy calls <c>Program.SetRoundoff(salaryRegister)</c> (line 3723)
/// mid-rule -- i.e. the whole register is rounded BEFORE 9000 Net Pay also rounds it. Preserved via
/// <see cref="IPayrollRoundingService.ApplyRoundingAsync"/>. The deduction itself is driven by
/// <c>WageType.IncludeInFund</c> Rate totals already on the register, so 23 must run after the wage types
/// that populate them (legacy relies on <c>WageType.PayrollOrder</c>).</para>
///
/// <para><b>PRESERVED LEGACY DEAD CODE.</b> <c>netAmtToDedFund</c> (legacy line 3724) is computed and never
/// used -- legacy's clamp is commented out at lines 3789-3790. The computation is kept because the
/// <c>SetRoundoff</c> that precedes it is NOT dead, and dropping the pair would hide that.</para>
///
/// <para><b>PORT GAP.</b> Requires <see cref="IEmployeeSubGroupResolver"/>, which currently throws --
/// <c>EmployeeOrgAssignment.EmployeeSubGroupId</c> was deferred in Wave B. See that interface's remarks.</para>
///
/// <para><b>PORT DEVIATION (auditing).</b> Legacy's <c>SysCreator</c>/<c>SysModifier</c>/
/// <c>IsSystemGenerated</c> stamping is dropped throughout (ABP <c>FullAuditedEntity</c>; these entities carry
/// no <c>IsSystemGenerated</c> column in this port).</para>
/// </summary>
public class FundDeductionWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly IPayrollRoundingService _roundingService;
    private readonly IEmployeeSubGroupResolver _subGroupResolver;
    private readonly IRepository<EmployeeSubGroupFund> _subGroupFundRepository;
    private readonly IRepository<EmployeeFund> _employeeFundRepository;
    private readonly IRepository<EmployeeFundDetail> _employeeFundDetailRepository;
    private readonly IRepository<EmployeeFundExemption> _employeeFundExemptionRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<TimeTransfer> _timeTransferRepository;
    private readonly IRepository<TimeTransferType> _timeTransferTypeRepository;
    private readonly IRepository<WageType> _wageTypeRepository;

    public FundDeductionWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IPayrollRoundingService roundingService,
        IEmployeeSubGroupResolver subGroupResolver,
        IRepository<EmployeeSubGroupFund> subGroupFundRepository,
        IRepository<EmployeeFund> employeeFundRepository,
        IRepository<EmployeeFundDetail> employeeFundDetailRepository,
        IRepository<EmployeeFundExemption> employeeFundExemptionRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<Employee> employeeRepository,
        IRepository<TimeTransfer> timeTransferRepository,
        IRepository<TimeTransferType> timeTransferTypeRepository,
        IRepository<WageType> wageTypeRepository)
    {
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        _roundingService = roundingService;
        _subGroupResolver = subGroupResolver;
        _subGroupFundRepository = subGroupFundRepository;
        _employeeFundRepository = employeeFundRepository;
        _employeeFundDetailRepository = employeeFundDetailRepository;
        _employeeFundExemptionRepository = employeeFundExemptionRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeeRepository = employeeRepository;
        _timeTransferRepository = timeTransferRepository;
        _timeTransferTypeRepository = timeTransferTypeRepository;
        _wageTypeRepository = wageTypeRepository;
    }

    /// <inheritdoc/>
    public string WageCode => "23";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 747-749.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context))
        {
            return;
        }

        var amount = await GetFundDedAsync(context);

        // Legacy line 754.
        if (amount == 0)
        {
            return;
        }

        await PayrollRegisterLineEditor.SetAmountAsync(
            _registerStore, context.Register, context.WageType, amount);

        // Legacy lines 775-781: posted NEGATED.
        if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
        {
            await _advancePaymentService.AdvancePaymentDedAsync(
                context.EmployeeId, PayrollRunGates.MonthStart(context.PayDate), context.OffCyclePaymentDate,
                (-1) * amount, context.PayrollRunType, context.OffCycleReason, context.AdvancePayComment,
                context.Settings);
        }
    }

    /// <summary>Legacy <c>GetFundDed</c> (legacy lines 3633-3849).</summary>
    private async Task<double> GetFundDedAsync(PayrollCalculationContext context)
    {
        double fundDedAmount = 0;

        var payDate = context.PayDate;
        var payStartDate = PayrollRunGates.MonthStart(payDate);
        var payEndDate = payDate;

        // Legacy lines 3642-3651.
        var subGroupId = await _subGroupResolver.GetEmployeeSubGroupIdAsync(context.EmployeeId, payDate);
        if (subGroupId == null)
        {
            throw new InvalidOperationException("Employee SubGroup Not found.");
        }

        var subGroupFundList = _subGroupFundRepository.GetAll()
            .Where(r => r.EmployeeSubGroupId == subGroupId.Value)
            .ToList();

        // Legacy lines 3653-3716.
        EmployeeFund empFund = null;
        if (subGroupFundList.Count > 0)
        {
            empFund = _employeeFundRepository.GetAll().FirstOrDefault(r => r.EmployeeId == context.EmployeeId);

            if (empFund == null)
            {
                var employee = _employeeRepository.GetAll().FirstOrDefault(e => e.Id == context.EmployeeId);
                if (employee == null)
                {
                    throw new InvalidOperationException($"Employee {context.EmployeeId} not found.");
                }

                empFund = new EmployeeFund
                {
                    TenantId = employee.TenantId,
                    EmployeeId = context.EmployeeId,
                    StartDate = employee.JoiningDate,
                    EndDate = new DateTime(9999, 12, 31)
                };

                // InsertAndGetIdAsync (not InsertAsync): the identity is read immediately below for the
                // EmployeeFundDetail FKs.
                empFund.Id = await _employeeFundRepository.InsertAndGetIdAsync(empFund);
            }

            // Legacy lines 3675-3684: drop this month's system-generated Opening rows.
            var staleOpeningIds = _employeeFundDetailRepository.GetAll()
                .Where(r => r.EmployeeFundId == empFund.Id
                            && r.TransactionDate >= payStartDate
                            && r.TransactionDate <= payEndDate
                            && r.FundPayType == FundPayType.Opening)
                .Select(r => r.Id)
                .ToList();

            foreach (var id in staleOpeningIds)
            {
                await _employeeFundDetailRepository.DeleteAsync(id);
            }

            // Legacy lines 3686-3690: Program.empTimeTransferList, TransferType code "6".
            var transferTypeIds = _timeTransferTypeRepository.GetAll()
                .Where(t => t.Code == "6")
                .Select(t => t.Id)
                .ToList();

            var openingFundAmount = _timeTransferRepository.GetAll()
                .Where(r => r.EmployeeId == context.EmployeeId
                            && transferTypeIds.Contains(r.TransferTypeId)
                            && r.TransferDate >= payStartDate
                            && r.TransferDate <= payEndDate)
                .Sum(r => r.TransferCount);

            if (openingFundAmount > 0)
            {
                await _employeeFundDetailRepository.InsertAsync(new EmployeeFundDetail
                {
                    TenantId = empFund.TenantId,
                    EmployeeFundId = empFund.Id,
                    TransactionDate = payStartDate,
                    Amount = openingFundAmount,
                    FundPayType = FundPayType.Opening
                });

                // Legacy lines 3708-3712. NOTE this first re-total deliberately EXCLUDES ExternalDeduction
                // and applies no Math.Round -- unlike the two later re-totals (lines 3743-3748, 3816-3822),
                // which include it and round to 2. Legacy inconsistency, preserved.
                var details = DetailsOf(empFund.Id);
                empFund.Amount = details
                    .Where(r => r.FundPayType == FundPayType.Opening || r.FundPayType == FundPayType.Deduction)
                    .Sum(r => r.Amount);
                empFund.PaidAmount = details.Where(r => r.FundPayType == FundPayType.Payment).Sum(r => r.Amount);
                empFund.BalanceAmount = empFund.Amount - empFund.PaidAmount;
                await _employeeFundRepository.UpdateAsync(empFund);
            }
        }

        if (empFund == null)
        {
            return 0;
        }

        // Legacy lines 3721-3729. SetRoundoff is a REAL side effect and must stay; netAmtToDedFund is
        // legacy dead code (its clamp at lines 3789-3790 is commented out) and is kept only to document that.
        await _roundingService.ApplyRoundingAsync(context.Register, context.Settings);
        var netAmtToDedFund = await PayrollNetPayHelper.GetNetPayAsync(
            _registerStore, context.Register, _wageTypeRepository);
        _ = netAmtToDedFund;

        // Legacy lines 3731-3751: drop this month's Deduction rows and re-total.
        var staleDeductionIds = _employeeFundDetailRepository.GetAll()
            .Where(r => r.EmployeeFundId == empFund.Id
                        && r.TransactionDate >= payStartDate
                        && r.TransactionDate <= payEndDate
                        && r.FundPayType == FundPayType.Deduction)
            .Select(r => r.Id)
            .ToList();

        foreach (var id in staleDeductionIds)
        {
            await _employeeFundDetailRepository.DeleteAsync(id);
        }

        RetotalFund(empFund);
        await _employeeFundRepository.UpdateAsync(empFund);

        // Legacy lines 3753-3760.
        var skipFundDeduction = _employeeFundExemptionRepository.GetAll()
            .Any(r => r.EmployeeFundId == empFund.Id && r.StartDate <= payEndDate && r.EndDate >= payEndDate);

        // Legacy lines 3762-3768.
        if (!skipFundDeduction)
        {
            var lastDayInService = _employeeSeparationRepository.GetAll()
                .Where(r => r.EmployeeId == context.EmployeeId && r.RevokeDate == null)
                .OrderByDescending(r => r.LastDayInService)
                .Select(r => (DateTime?)r.LastDayInService)
                .FirstOrDefault();

            if (lastDayInService != null && payStartDate > lastDayInService.Value)
            {
                skipFundDeduction = true;
            }
        }

        // Legacy lines 3770-3795.
        if (!skipFundDeduction)
        {
            var lines = await _registerStore.GetLinesAsync(context.Register);
            var includeInFundIds = WageTypeFlagLookup.WageTypeIdsWith(_wageTypeRepository, w => w.IncludeInFund);
            var totalBasicPay = lines.Where(l => includeInFundIds.Contains(l.WageTypeId)).Sum(l => l.Rate);

            var band = subGroupFundList
                .FirstOrDefault(r => r.FromPay <= totalBasicPay && r.ToPay >= totalBasicPay);

            if (band != null)
            {
                if (band.FundDedType == FundDedType.FixAmount)
                {
                    fundDedAmount = band.FixAmount;
                }
                else if (band.FundDedType == FundDedType.Percentage)
                {
                    fundDedAmount = (totalBasicPay * band.Percentage) / 100;
                }
                else if (band.FundDedType == FundDedType.PercentageWithinLimit)
                {
                    fundDedAmount = (totalBasicPay * band.Percentage) / 100;
                    fundDedAmount = fundDedAmount > band.MaxAmount ? band.MaxAmount : fundDedAmount;
                    fundDedAmount = fundDedAmount < band.MinAmount ? band.MinAmount : fundDedAmount;
                }

                // Legacy line 3792 -- 2 decimals, NOT the currency's decimal count.
                fundDedAmount = Math.Round(fundDedAmount, 2);
            }
        }

        // Legacy lines 3797-3832.
        if (fundDedAmount > 0)
        {
            await _employeeFundDetailRepository.InsertAsync(new EmployeeFundDetail
            {
                TenantId = empFund.TenantId,
                EmployeeFundId = empFund.Id,
                TransactionDate = payEndDate,
                Amount = fundDedAmount,
                FundPayType = FundPayType.Deduction
            });

            RetotalFund(empFund);
            await _employeeFundRepository.UpdateAsync(empFund);
        }

        return fundDedAmount;
    }

    /// <summary>Legacy lines 3743-3748 / 3816-3822 (identical): Math.Round(..., 2) on all three totals.</summary>
    private void RetotalFund(EmployeeFund empFund)
    {
        var details = DetailsOf(empFund.Id);

        empFund.Amount = Math.Round(details
            .Where(r => r.FundPayType == FundPayType.Opening
                        || r.FundPayType == FundPayType.Deduction
                        || r.FundPayType == FundPayType.ExternalDeduction)
            .Sum(r => r.Amount), 2);

        empFund.PaidAmount = Math.Round(details
            .Where(r => r.FundPayType == FundPayType.Payment)
            .Sum(r => r.Amount), 2);

        empFund.BalanceAmount = Math.Round(empFund.Amount - empFund.PaidAmount, 2);
    }

    private List<EmployeeFundDetail> DetailsOf(int employeeFundId)
    {
        return _employeeFundDetailRepository.GetAll()
            .Where(r => r.EmployeeFundId == employeeFundId)
            .ToList();
    }
}
