using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IPayrollRoundingService"/>
/// <remarks>
/// Ported line-for-line from legacy <c>HCM10.PayrollEngine/Program.SetRoundoff</c> (legacy <c>Program.cs</c>
/// lines 3002-3101) and <c>Program.GetDecimalRounding</c> (lines 3102-3121).
///
/// <para><b>ADAPTATION -- "the employee's currency".</b> Legacy read
/// <c>salaryRegister.Employee.CurrentCompany.PayrollArea.Currency.DecimalCount</c>, falling back to
/// <c>CurrencyService.GetDefaultCurrency(session)</c>. This port's <c>Employee</c> entity has no
/// <c>CurrentCompany</c> computed property (org assignment was collapsed into the date-bound
/// <see cref="EmployeeOrgAssignment"/> with a single <c>OrganizationUnitId</c> FK). The resolution used here --
/// most-recent org assignment -&gt; <see cref="OrgUnitProfile"/> -&gt; <c>PayrollArea.Currency.DecimalCount</c>,
/// falling back to <see cref="GeneralMasterInfo.Services.ICurrencyService.GetDefaultCurrencyAsync"/> -- is NOT
/// invented here: it is the identical adaptation already shipped and documented in
/// <c>HCMv3.GeneralMasterInfo.Services.CurrencyService.RoundAsync</c> for the very same legacy expression.</para>
///
/// <para><b>Exception swallowing is intentional and ported.</b> Legacy wraps the entire <c>SetRoundoff</c>
/// body in <c>try { } catch { /* exception while rounding .. skip */ }</c> (lines 3097-3100) and does the same
/// in <c>GetDecimalRounding</c> (lines 3116-3119). Since <c>SetRoundoff</c> is invoked from inside the 9000 Net
/// Pay branch, letting it throw would fail the whole employee's payroll over a formatting concern. Preserved
/// verbatim -- do not "fix" it.</para>
/// </remarks>
[UnitOfWork]
public class PayrollRoundingService : HCMv3DomainServiceBase, IPayrollRoundingService
{
    /// <summary>
    /// Legacy <c>string[] ALaccrualWT</c> (legacy <c>Program.cs</c> line 3025) -- the annual-leave/ticket
    /// accrual wage codes that keep 6 decimals even in the pre-<c>Decimal6RoundingFrom</c> mode.
    /// </summary>
    private static readonly string[] AccrualWageCodesAlwaysRoundedTo6 =
    {
        "4021", "5021", "4010", "5010", "4030", "5030", "4022", "5022", "4031", "5031"
    };

    /// <summary>Legacy's hardcoded 6 for the accrual wage codes above.</summary>
    private const int AccrualWageCodeDecimals = 6;

    /// <summary>Legacy <c>Int16 decimalRounding = 3;</c> initialiser in <c>GetDecimalRounding</c> (line 3104).</summary>
    private const int DefaultCurrencyDecimals = 3;

    /// <summary>Legacy's hardcoded 2 when <c>isNetPayRound</c> is on (line 1076 of EmployeeCalculatedPayService).</summary>
    private const int NetPayRoundedDecimals = 2;

    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<SalaryRegisterDaysDetail> _salaryRegisterDaysDetailRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;
    private readonly IRepository<Currency> _currencyRepository;

    public PayrollRoundingService(
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<SalaryRegisterDaysDetail> salaryRegisterDaysDetailRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository,
        IRepository<Currency> currencyRepository)
    {
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _salaryRegisterDaysDetailRepository = salaryRegisterDaysDetailRepository;
        _wageTypeRepository = wageTypeRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
        _currencyRepository = currencyRepository;
    }

    public async Task ApplyRoundingAsync(PayrollRegisterRef register, PayrollEngineSettings settings)
    {
        // Legacy Program.SetRoundoff swallows everything -- see class remarks.
        try
        {
            if (register == null)
            {
                return;
            }

            if (register.Target == PayrollRegisterTarget.Simulation)
            {
                // Legacy SimulationPayrollHelper.SetRoundoff (lines 2469-2570): the same rounding, on the
                // simulation register. Never redirected to the live one.
                await ApplySimulationRoundingAsync(register, settings);
                return;
            }

            var salaryRegister = _salaryRegisterRepository.GetAll().FirstOrDefault(r => r.Id == register.RegisterId);
            if (salaryRegister == null)
            {
                return;
            }

            var effectiveSettings = settings ?? new PayrollEngineSettings();

            // Legacy line 3010: old mode only when Decimal6RoundingFrom is configured AND ForPeriod precedes it.
            var useOldCurrencyDecimalMode =
                effectiveSettings.Decimal6RoundingFrom != default(DateTime)
                && salaryRegister.ForPeriod < effectiveSettings.Decimal6RoundingFrom;

            var currencyDecimals = useOldCurrencyDecimalMode
                ? await GetCurrencyDecimalRoundingAsync(salaryRegister.EmployeeId)
                : 0;

            var details = (from detail in _salaryRegisterDetailRepository.GetAll()
                           join wageType in _wageTypeRepository.GetAll() on detail.WageTypeId equals wageType.Id
                           where detail.SalaryRegisterId == register.RegisterId
                           select new { Detail = detail, wageType.Code }).ToList();

            foreach (var row in details)
            {
                int digits;
                if (!useOldCurrencyDecimalMode)
                {
                    // Legacy lines 3074-3091: current mode, no wage-code exceptions.
                    digits = effectiveSettings.DecimalInSalaryRegister;
                }
                else
                {
                    // Legacy lines 3029-3066: old mode, with the ten accrual wage-code exceptions.
                    digits = AccrualWageCodesAlwaysRoundedTo6.Contains(row.Code)
                        ? AccrualWageCodeDecimals
                        : currencyDecimals;
                }

                var detail = row.Detail;
                detail.Rate = Math.Round(detail.Rate, digits);
                detail.Number = Math.Round(detail.Number, digits);
                detail.Amount = Math.Round(detail.Amount, digits);
                await _salaryRegisterDetailRepository.UpdateAsync(detail);

                var dayRows = _salaryRegisterDaysDetailRepository.GetAll()
                    .Where(d => d.SalaryRegisterDetailId == detail.Id)
                    .ToList();

                foreach (var dayRow in dayRows)
                {
                    dayRow.Rate = Math.Round(dayRow.Rate, digits);
                    dayRow.Number = Math.Round(dayRow.Number, digits);
                    dayRow.Amount = Math.Round(dayRow.Amount, digits);
                    await _salaryRegisterDaysDetailRepository.UpdateAsync(dayRow);
                }
            }
        }
        catch (SimulationTablesMissingException)
        {
            // A simulation without its tables must NOT be swallowed -- a silently un-rounded simulation is worse
            // than a loud error.
            throw;
        }
        catch (Exception)
        {
            // Legacy Program.cs lines 3097-3100: "//exception while rounding .. skip".
        }
    }

    /// <summary>The simulation tables. Property-injected so the hand-built test services keep compiling.</summary>
    public ISimulationRegisterRows SimulationRows { get; set; }

    /// <summary>Thrown when a simulation register is to be rounded but the simulation tables are not wired.</summary>
    public sealed class SimulationTablesMissingException : InvalidOperationException
    {
        public SimulationTablesMissingException()
            : base("PayrollRoundingService was asked to round a simulation register but has no ISimulationRegisterRows; "
                   + "nothing was rounded, and the live register was not touched.")
        {
        }
    }

    /// <summary>
    /// Simulation twin of the live body above: same digits, same exceptions, applied to
    /// <see cref="SimulationSalaryRegisterDetail"/>/<see cref="SimulationSalaryRegisterDaysDetail"/>.
    /// </summary>
    private async Task ApplySimulationRoundingAsync(PayrollRegisterRef register, PayrollEngineSettings settings)
    {
        var simulation = SimulationRows ?? throw new SimulationTablesMissingException();

        var header = simulation.GetHeaderCopy(register.RegisterId);
        if (header == null)
        {
            return;
        }

        var effectiveSettings = settings ?? new PayrollEngineSettings();

        var useOldCurrencyDecimalMode =
            effectiveSettings.Decimal6RoundingFrom != default(DateTime)
            && header.ForPeriod < effectiveSettings.Decimal6RoundingFrom;

        var currencyDecimals = useOldCurrencyDecimalMode
            ? await GetCurrencyDecimalRoundingAsync(header.EmployeeId)
            : 0;

        var lines = (from detail in simulation.Details
                     join wageType in _wageTypeRepository.GetAll() on detail.WageTypeId equals wageType.Id
                     where detail.SimulationSalaryRegisterId == register.RegisterId
                     select new { detail.Id, wageType.Code }).ToList();

        foreach (var row in lines)
        {
            var digits = !useOldCurrencyDecimalMode
                ? effectiveSettings.DecimalInSalaryRegister
                : AccrualWageCodesAlwaysRoundedTo6.Contains(row.Code) ? AccrualWageCodeDecimals : currencyDecimals;

            var detail = simulation.GetDetailCopy(row.Id);
            detail.Rate = Math.Round(detail.Rate, digits);
            detail.Number = Math.Round(detail.Number, digits);
            detail.Amount = Math.Round(detail.Amount, digits);
            await simulation.UpdateDetailAsync(detail);

            foreach (var dayRow in simulation.GetDayDetailCopies(row.Id))
            {
                dayRow.Rate = Math.Round(dayRow.Rate, digits);
                dayRow.Number = Math.Round(dayRow.Number, digits);
                dayRow.Amount = Math.Round(dayRow.Amount, digits);
                await simulation.UpdateDayDetailAsync(dayRow);
            }
        }
    }

    public Task<int> GetCurrencyDecimalRoundingAsync(int employeeId)
    {
        // Legacy Program.GetDecimalRounding swallows everything and returns its 3 initialiser.
        try
        {
            var latestAssignment = _employeeOrgAssignmentRepository.GetAll()
                .Where(a => a.EmployeeId == employeeId && a.OrganizationUnitId != null)
                .OrderByDescending(a => a.EndDate)
                .FirstOrDefault();

            if (latestAssignment?.OrganizationUnitId != null)
            {
                var payrollAreaCurrencyDecimals = _orgUnitProfileRepository
                    .GetAllIncluding(p => p.PayrollArea, p => p.PayrollArea.Currency)
                    .Where(p => p.OrganizationUnitId == latestAssignment.OrganizationUnitId.Value
                                && p.PayrollAreaId != null
                                && p.PayrollArea.Currency != null)
                    .Select(p => (int?)p.PayrollArea.Currency.DecimalCount)
                    .FirstOrDefault();

                if (payrollAreaCurrencyDecimals.HasValue)
                {
                    return Task.FromResult(payrollAreaCurrencyDecimals.Value);
                }
            }

            // Legacy fallback: CurrencyService.GetDefaultCurrency(session).DecimalCount.
            var defaultCurrency = _currencyRepository.GetAll().FirstOrDefault(c => c.Code == "KWD");
            if (defaultCurrency != null)
            {
                return Task.FromResult(defaultCurrency.DecimalCount);
            }
        }
        catch (Exception)
        {
            // Legacy Program.cs lines 3116-3119: "//exception while rounding .. skip".
        }

        return Task.FromResult(DefaultCurrencyDecimals);
    }

    public async Task<int> GetNetPayRoundingDigitsAsync(int employeeId, PayrollEngineSettings settings)
    {
        // Legacy EmployeeCalculatedPayService.cs line 1076.
        if (settings != null && settings.IsNetPayRound)
        {
            return NetPayRoundedDecimals;
        }

        return await GetCurrencyDecimalRoundingAsync(employeeId);
    }

    /// <summary>Exposed for tests/documentation: legacy's ten always-6-decimals accrual wage codes.</summary>
    public static IReadOnlyList<string> AccrualWageCodes => AccrualWageCodesAlwaysRoundedTo6;
}
