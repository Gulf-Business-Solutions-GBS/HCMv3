using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.BenefitsManagement;
using HCMv3.GeneralMasterInfo.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>One projected insurance-installment detail row, shared by the company and employee variants.</summary>
public class InsuranceInstallmentRow
{
    public int Id { get; set; }

    public int EmployeeInsuranceId { get; set; }

    public DateTime InstallmentDate { get; set; }

    public double InstallmentAmount { get; set; }

    public InstallmentType InstallmentType { get; set; }
}

/// <summary>
/// Shared body of legacy <c>GetCompanyInsuranceRecoveryDed</c> (legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 3272-3428) and
/// <c>GetEmployeeInsuranceRecoveryDed</c> (lines 3449-3604) plus their identical calling branches (wage code
/// <b>11</b>, lines 656-698; wage code <b>13</b>, lines 700-742).
///
/// <para>The two legacy methods are character-for-character identical apart from four things, which are the
/// four abstract members below: the detail entity (<see cref="CompanyInsuranceDetail"/> vs
/// <see cref="EmployeeInsuranceDetail"/>), the contribution field
/// (<c>CompanyContribution</c>/<c>EmployeeContribution</c>), the balance field
/// (<c>BalanceCompanyContribution</c>/<c>BalanceEmployeeContribution</c>) and the per-installment amount
/// (<c>InstallmentCompanyDedAmount</c>/<c>InstallmentEmployeeDedAmount</c>). Factoring them apart is the whole
/// reason this base exists -- the legacy pair had already begun to drift in whitespace/comments and would have
/// drifted in behaviour next.</para>
///
/// <para><b>ORDERING REQUIREMENT.</b> This rule reads
/// <see cref="PayrollNetPayHelper.GetNetPayAsync"/> as a running budget (legacy lines 3302 / 3479), so its
/// result depends on which wage types have already been written to the register. Legacy relies on
/// <c>WageType.PayrollOrder</c> for that; the engine (wave P1b) must preserve it.</para>
///
/// <para><b>PORT DEVIATION (auditing).</b> Legacy's <c>SysCreator</c>/<c>SysModifier</c> stamping and
/// <c>IsSystemGenerated = true</c> flags on <c>EmployeeInsurance</c>/<c>*InsuranceDetail</c> are dropped --
/// ABP's <c>FullAuditedEntity</c> handles auditing and these entities carry no <c>IsSystemGenerated</c>
/// column in this port.</para>
/// </summary>
public abstract class InsuranceRecoveryDeductionCalculatorBase : HCMv3DomainServiceBase, IWageTypeCalculator
{
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly ICurrencyService _currencyService;

    protected InsuranceRecoveryDeductionCalculatorBase(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IRepository<EmployeeInsurance> employeeInsuranceRepository,
        IRepository<WageType> wageTypeRepository,
        ICurrencyService currencyService)
    {
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        EmployeeInsuranceRepository = employeeInsuranceRepository;
        _wageTypeRepository = wageTypeRepository;
        _currencyService = currencyService;
    }

    /// <inheritdoc/>
    public abstract string WageCode { get; }

    protected IRepository<EmployeeInsurance> EmployeeInsuranceRepository { get; }

    /// <summary>Projection over the variant's installment-detail table.</summary>
    protected abstract IQueryable<InsuranceInstallmentRow> QueryDetails();

    protected abstract Task DeleteDetailAsync(int detailId);

    protected abstract Task InsertDetailAsync(EmployeeInsurance insurance, DateTime installmentDate, double amount);

    /// <summary>Legacy <c>empInsurance.CompanyContribution</c> / <c>.EmployeeContribution</c>.</summary>
    protected abstract double GetContribution(EmployeeInsurance insurance);

    /// <summary>Legacy <c>empInsurance.BalanceCompanyContribution</c> / <c>.BalanceEmployeeContribution</c>.</summary>
    protected abstract double GetBalance(EmployeeInsurance insurance);

    protected abstract void SetBalance(EmployeeInsurance insurance, double value);

    /// <summary>Legacy <c>empInsurance.InstallmentCompanyDedAmount</c> / <c>.InstallmentEmployeeDedAmount</c>.</summary>
    protected abstract double GetInstallmentDedAmount(EmployeeInsurance insurance);

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 659-661 / 703-705.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context))
        {
            return;
        }

        var amount = await GetRecoveryDeductionAsync(context);

        // Legacy lines 666 / 710.
        if (amount == 0)
        {
            return;
        }

        await PayrollRegisterLineEditor.SetAmountAsync(
            _registerStore, context.Register, context.WageType, amount);

        // Legacy lines 687-693 / 731-737: posted NEGATED.
        if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
        {
            await _advancePaymentService.AdvancePaymentDedAsync(
                context.EmployeeId,
                PayrollRunGates.MonthStart(context.PayDate),
                context.OffCyclePaymentDate,
                (-1) * amount,
                context.PayrollRunType,
                context.OffCycleReason,
                context.AdvancePayComment,
                context.Settings);
        }
    }

    /// <summary>Legacy lines 3272-3428 / 3449-3604.</summary>
    private async Task<double> GetRecoveryDeductionAsync(PayrollCalculationContext context)
    {
        double totalInsuranceRecoveryDed = 0;

        var payDate = context.PayDate;
        var payStartDate = PayrollRunGates.MonthStart(payDate);
        var payEndDate = payDate;

        // Legacy lines 3282-3287 / 3459-3464: End-of-Service installments already recorded for this month.
        if (context.PayrollRunType == PayrollRunType.OffCycle
            && context.IsEndOfService
            && context.Register.InPeriod == context.Register.ForPeriod)
        {
            var employeeInsuranceIds = EmployeeInsuranceRepository.GetAll()
                .Where(i => i.EmployeeId == context.EmployeeId)
                .Select(i => i.Id)
                .ToList();

            totalInsuranceRecoveryDed = QueryDetails()
                .Where(d => employeeInsuranceIds.Contains(d.EmployeeInsuranceId)
                            && d.InstallmentDate >= payStartDate
                            && d.InstallmentDate <= payEndDate
                            && d.InstallmentType == InstallmentType.EndOfService)
                .Sum(d => d.InstallmentAmount);
        }

        // Legacy lines 3292-3298 / 3469-3475.
        var candidateIds = EmployeeInsuranceRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId
                        && r.InstallmentStartDate <= payEndDate
                        && r.StartDate <= payEndDate
                        && r.EndDate >= payEndDate)
            .OrderBy(r => r.StartDate)
            .Select(r => r.Id)
            .ToList();

        var insuranceList = new List<EmployeeInsurance>();
        foreach (var id in candidateIds)
        {
            var insurance = EmployeeInsuranceRepository.GetAll().FirstOrDefault(r => r.Id == id);
            if (insurance == null)
            {
                continue;
            }

            var hasThroughPayrollRow = QueryDetails()
                .Any(d => d.EmployeeInsuranceId == id
                          && d.InstallmentDate == payEndDate
                          && d.InstallmentType == InstallmentType.ThroughPayroll);

            if (GetBalance(insurance) > 0 || hasThroughPayrollRow)
            {
                insuranceList.Add(insurance);
            }
        }

        if (!insuranceList.Any())
        {
            return totalInsuranceRecoveryDed;
        }

        // Legacy lines 3302 / 3479.
        var netAmtToDedRecovery = await PayrollNetPayHelper.GetNetPayAsync(
            _registerStore, context.Register, _wageTypeRepository);

        foreach (var insurance in insuranceList)
        {
            double recoveryAmount = 0;

            // Legacy lines 3311-3312 / 3488-3489.
            if (netAmtToDedRecovery == 0)
            {
                break;
            }

            // Legacy lines 3317-3326 / 3494-3503: drop this month's previously generated installments.
            var staleIds = QueryDetails()
                .Where(d => d.EmployeeInsuranceId == insurance.Id
                            && d.InstallmentDate == payEndDate
                            && d.InstallmentType == InstallmentType.ThroughPayroll)
                .Select(d => d.Id)
                .ToList();

            // SIMULATION RUN: the insurance and its installment rows are read but never written (legacy
            // GetCompanyInsuranceRecoveryDed_Simu / GetEmployeeInsuranceRecoveryDed_Simu have the delete, the
            // balance save and the new installment commented out). The same arithmetic runs in memory.
            var isSimulation = PayrollSimulationScope.IsActive;

            if (!isSimulation)
            {
                foreach (var staleId in staleIds)
                {
                    await DeleteDetailAsync(staleId);
                }
            }

            // Legacy lines 3329-3338 / 3506-3515.
            var totalPaid = QueryDetails()
                .Where(d => d.EmployeeInsuranceId == insurance.Id)
                .Sum(d => d.InstallmentAmount);

            var balance = await _currencyService.RoundAsync(
                context.EmployeeId, GetContribution(insurance) - totalPaid);

            if (!isSimulation)
            {
                SetBalance(insurance, balance);
                await EmployeeInsuranceRepository.UpdateAsync(insurance);
            }

            // Legacy lines 3345-3359 / 3522-3536. The Fixed/Variable switch is commented out in legacy;
            // only the fixed installment amount survives.
            recoveryAmount = Math.Round(GetInstallmentDedAmount(insurance), 3);

            // Legacy lines 3362-3368 / 3539-3545.
            if (netAmtToDedRecovery < recoveryAmount)
            {
                recoveryAmount = netAmtToDedRecovery;
                netAmtToDedRecovery = 0;
            }
            else
            {
                netAmtToDedRecovery -= recoveryAmount;
            }

            // Legacy lines 3373-3414 / 3550-3590.
            if (recoveryAmount > 0)
            {
                if (balance < recoveryAmount)
                {
                    recoveryAmount = await _currencyService.RoundAsync(context.EmployeeId, balance);
                }

                if (recoveryAmount > 0)
                {
                    // A simulation takes the deduction without recording the installment.
                    if (!isSimulation)
                    {
                        // Legacy uses payDate (not payEndDate) for the new row; identical here.
                        await InsertDetailAsync(insurance, payDate, recoveryAmount);

                        var totPaid = QueryDetails()
                            .Where(d => d.EmployeeInsuranceId == insurance.Id)
                            .Sum(d => d.InstallmentAmount);

                        SetBalance(insurance, await _currencyService.RoundAsync(
                            context.EmployeeId, GetContribution(insurance) - totPaid));
                        await EmployeeInsuranceRepository.UpdateAsync(insurance);
                    }
                }
                else
                {
                    // Legacy lines 3410-3413 / 3586-3589: a negative rounded balance becomes zero.
                    recoveryAmount = 0;
                }
            }

            totalInsuranceRecoveryDed += recoveryAmount;
        }

        return totalInsuranceRecoveryDed;
    }
}
