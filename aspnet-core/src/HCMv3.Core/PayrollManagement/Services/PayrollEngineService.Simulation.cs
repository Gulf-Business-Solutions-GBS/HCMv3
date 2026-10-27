using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// SIMULATION (test) payroll run -- wave P7, the port of legacy
/// <c>HCM10.PayrollEngine/SimulationPayrollHelper.cs</c>, which the Payroll Engine screen ran instead of
/// <c>Program.Main</c> when its Simulation box was ticked (<c>uscPayrollEngine.ascx.cs</c> lines 210-217).
///
/// <para><b>What a simulation is.</b> The same calculation as a live run, written to
/// <see cref="SimulationSalaryRegister"/>/<see cref="SimulationSalaryRegisterDetail"/>/
/// <see cref="SimulationSalaryRegisterDaysDetail"/> instead of the live registers, and leaving every piece of
/// live data alone: no live register line, no loan recovery, fund, insurance, indemnity quota, carry-forward
/// payment or leave mark is written (legacy's <c>*_Simu</c> methods have every one of those writes commented
/// out). What it DOES write, as legacy does: the PayrollLog and ExecutionLog rows, and -- when time-engine
/// autorun is on -- the time engine's results, because legacy's simulation evaluates time for real.</para>
///
/// <para><b>Where it differs from a live run, all reproduced from the legacy clone:</b></para>
/// <list type="number">
/// <item><description>Before anything else, every earlier simulation register of the run's employees -- any
/// period -- is deleted (<c>DeleteAllPreviousSimulationResiters</c>, lines 2112-2136). A simulation table
/// therefore only ever holds the latest test run of each employee.</description></item>
/// <item><description>A regular run is NOT refused for an unlocked or non-current control record (lines
/// 263-273 commented out), nor for an employee whose area's control record is on another month (lines
/// 730-739).</description></item>
/// <item><description>Employees are always picked by the plain <c>GetEmployeeList</c>, never by the autorun
/// branches (line 440).</description></item>
/// <item><description>No "salary register locked" check, no "salary on hold" refusal (an all-held employee is
/// simply not calculated), no "ReturnDate not found" refusal, no time-register approval check.</description></item>
/// <item><description>Retro still runs, but without the 2023 "not before JoiningDate" clamp, and an off-cycle
/// never adds the EarliestMasterDataChange seed, not even for End Of Service (lines 1008-1041).</description></item>
/// <item><description>AccountedTo / EarliestMasterDataChange are not reset (lines 1117-1128 commented out), and
/// no 2005/2011 EmployeePayment rows are cleared or raised.</description></item>
/// <item><description>The "current" retro registers a rule compares with are the simulation's own (lines
/// 1445-1448); the previous month is read from the live registers (line 1441).</description></item>
/// </list>
///
/// <para><b>Not reproduced: the clone's drift inside the wage rules.</b> Legacy's simulation is a copy of the
/// engine and its <c>*_Simu</c> rules were not kept in step with the live ones (for example its loan recovery
/// caps at net pay and reads an installment schedule, and its indemnity-days "first register" test looks in the
/// simulation table). The rules here are the live rules, so a test run shows what the real run would pay. The
/// orchestration differences above are reproduced because they are what the operator sees: what is refused, and
/// what is kept.</para>
/// </summary>
public partial class PayrollEngineService
{
    /// <summary>
    /// The simulation tables. Property-injected (ABP/Castle) so the hand-built engines in the tests keep
    /// compiling; a simulation run without it is refused before anything is written.
    /// </summary>
    public ISimulationRegisterRows SimulationRows { get; set; }

    private ISimulationRegisterRows Simulation =>
        SimulationRows ?? throw new InvalidOperationException(
            "A simulation payroll run needs the simulation register tables (ISimulationRegisterRows), which are "
            + "not available. Nothing was written: a simulation must never fall back to the live registers.");

    private static bool IsSimulationRun(PayrollEngineRunInput input)
    {
        return input.RegisterTarget == PayrollRegisterTarget.Simulation;
    }

    /// <summary>
    /// Legacy shows the Simulation check box only when <c>ManagePayrollSettings.AllowSimulationRun</c> is on
    /// (<c>uscPayrollEngine.ascx.cs</c> line 88). Checked here as well as by the app service so no caller can
    /// start a test run the tenant has not allowed.
    /// </summary>
    private async Task EnsureSimulationAllowedAsync()
    {
        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        if (payrollSettings == null || !payrollSettings.AllowSimulationRun)
        {
            throw new UserFriendlyException(
                "Simulation run is not allowed. Turn on Allow Simulation Run in the payroll settings to use it.");
        }

        // Fail before the first delete rather than half-way through the first employee.
        _ = Simulation;
    }

    /// <summary>
    /// Legacy <c>SimulationPayrollHelper.DeleteAllPreviousSimulationResiters</c> (lines 2112-2136): every
    /// simulation register of every employee <c>GetEmployeeList</c> picks for this run -- with the pay-in date as
    /// entered -- is deleted with its lines and day rows, whatever its period.
    /// </summary>
    private async Task DeletePreviousSimulationRegistersAsync(PayrollEngineRunInput input, DateTime payInDate)
    {
        var employees = await GetEmployeeListLegacyAsync(input.PayrollAreaName, input.EmployeeCodes, payInDate);
        await Simulation.DeleteAllForEmployeesAsync(employees.Select(e => e.Id).ToList());
    }

    /// <summary>
    /// Legacy <c>SimulationPayrollHelper.RunPayrollCalculation</c> lines 1445-1448: this run's other simulation
    /// registers (its retro months). Note the clone has NO run-type filter here, unlike the live line 1915.
    /// </summary>
    private List<SalaryRegister> GetCurrentSimulationRegisters(int employeeId, DateTime payInDate)
    {
        return Simulation.GetHeaderCopies(headers => headers
            .Where(r => r.EmployeeId == employeeId
                        && r.ForPeriod != payInDate
                        && r.InPeriod == payInDate
                        && r.PayrollResultType == PayrollResultType.Current)
            .OrderBy(r => r.ForPeriod));
    }

    private static PayrollRegisterRef BuildSimulationRegisterRef(SimulationSalaryRegister register)
    {
        if (register == null)
        {
            return null;
        }

        return new PayrollRegisterRef
        {
            RegisterId = register.Id,
            Target = PayrollRegisterTarget.Simulation,
            ForPeriod = register.ForPeriod,
            InPeriod = register.InPeriod,
            PayrollRunType = register.PayrollRunType
        };
    }

    #region << Regular (SimulationPayrollHelper lines 835-990) >>

    private async Task ProcessRegularSimulationEmployeeAsync(
        PayrollEngineRunInput input,
        Employee employee,
        DateTime payInDate,
        PayrollEngineSettings settings,
        PayrollOnHoldFlags onHold,
        bool isNotReturnFromLeave,
        EmployeePayrollStatus payrollStatus,
        PayrollArea payrollArea,
        List<EmployeeOrgAssignment> orgAssignments,
        PayrollEmployeeRunResult employeeResult)
    {
        // Lines 835-899: the EarliestRetroAccountingDate refusal and the EarliestEvaluationDate clamp are kept,
        // the JoiningDate clamp is not.
        var periods = CollectPayrollPeriods(
            employee, payrollStatus, payInDate, settings,
            includeMasterDataChangeSeed: true,
            throwWhenBeforeEarliestRetroAccountingDate: true,
            clampToJoiningDate: false);

        foreach (var payForDate in periods)
        {
            // Lines 904-909.
            if (payrollStatus.DoNoAccountAfter < payForDate && payrollStatus.DoNoAccountAfter != new DateTime(1, 1, 1))
            {
                continue;
            }

            var existing = Simulation.Headers
                .Where(r => r.EmployeeId == employee.Id
                            && r.ForPeriod == payForDate
                            && r.InPeriod == payInDate
                            && r.PayrollRunType == PayrollRunType.Regular)
                .ToList();

            if (existing.Count > 1)
            {
                // Line 920 (its placeholder is missing, as on the live line; the date is appended here).
                throw new UserFriendlyException(string.Format(
                    "Duplicate Simulation Salary Register found for {0}", payForDate.ToShortDateString()));
            }

            SimulationSalaryRegister register = null;

            if (existing.Count == 1)
            {
                // Lines 921-934.
                register = existing[0];
                register.PayrollRunType = PayrollRunType.Regular;
                register.PayrollResultType = PayrollResultType.Current;
                await Simulation.UpdateHeaderAsync(register);

                await _registerStore.ClearLinesAsync(BuildSimulationRegisterRef(register));
            }
            else if (!onHold.All)
            {
                // Lines 937-946.
                register = new SimulationSalaryRegister
                {
                    EmployeeId = employee.Id,
                    ForPeriod = payForDate,
                    InPeriod = payInDate,
                    PayrollRunType = PayrollRunType.Regular,
                    PayrollResultType = PayrollResultType.Current
                };

                register.Id = await Simulation.InsertHeaderAsync(register);
            }

            if (!onHold.All)
            {
                // Lines 948-961. No "ReturnDate not found" refusal on this arm -- the flag is passed through.
                await RunPayrollCalculationAsync(
                    input, employee, payForDate, payInDate, settings, onHold,
                    isNotReturnFromLeave, payrollArea, orgAssignments,
                    BuildSimulationRegisterRef(register));

                employeeResult.SalaryRegisterIds.Add(register.Id);
            }

            // Lines 963-977.
            await ApplySimulationResultTypeAgeingAsync(employee.Id, payForDate, payInDate);
        }
    }

    /// <summary>Lines 963-977: the live ageing (Last / Archive) applied to the simulation registers.</summary>
    private async Task ApplySimulationResultTypeAgeingAsync(int employeeId, DateTime forPeriod, DateTime payInDate)
    {
        var others = Simulation.Headers
            .Where(r => r.EmployeeId == employeeId && r.ForPeriod == forPeriod && r.InPeriod != payInDate)
            .OrderByDescending(r => r.InPeriod)
            .ToList();

        for (var i = 0; i < others.Count; i++)
        {
            others[i].PayrollResultType = i == 0 ? PayrollResultType.Last : PayrollResultType.Archive;
            await Simulation.UpdateHeaderAsync(others[i]);
        }
    }

    #endregion

    #region << OffCycle (SimulationPayrollHelper lines 994-1115) >>

    private async Task ProcessOffCycleSimulationEmployeeAsync(
        PayrollEngineRunInput input,
        Employee employee,
        DateTime payInDate,
        PayrollEngineSettings settings,
        bool isNotReturnFromLeave,
        EmployeePayrollStatus payrollStatus,
        PayrollArea payrollArea,
        List<EmployeeOrgAssignment> orgAssignments,
        PayrollEmployeeRunResult employeeResult)
    {
        // Lines 996-1007: the 2005 EmployeePayment delete is commented out. Lines 1017-1041: AccountedTo seed
        // only, whatever the reason.
        var periods = CollectPayrollPeriods(
            employee, payrollStatus, payInDate, settings,
            includeMasterDataChangeSeed: false,
            throwWhenBeforeEarliestRetroAccountingDate: false,
            clampToJoiningDate: false);

        // Lines 1066/1078: legacy assigns the lookup result as is, so an unknown reason leaves it empty.
        var reasonTypeId = _payrollReasonTypeRepository.GetAll()
            .Where(r => r.NameEn == input.OffCycleReason || r.NameAr == input.OffCycleReason)
            .Select(r => (int?)r.Id)
            .FirstOrDefault();

        foreach (var payForDate in periods)
        {
            // Lines 1047-1056.
            if (payrollStatus.DoNoAccountAfter < payForDate && payrollStatus.DoNoAccountAfter != new DateTime(1, 1, 1))
            {
                continue;
            }

            var existing = Simulation.Headers
                .Where(r => r.EmployeeId == employee.Id && r.ForPeriod == payForDate && r.InPeriod == payInDate)
                .ToList();

            // Lines 1059-1060, against the simulation registers.
            if (existing.Any(r => r.PayrollRunType == PayrollRunType.Regular))
            {
                throw new UserFriendlyException("Cannot run OffCycle Payroll for the month,as Regular Payroll already ran.");
            }

            SimulationSalaryRegister register;

            if (existing.Count == 1)
            {
                // Lines 1062-1074.
                register = existing[0];
                register.PayrollReasonTypeId = reasonTypeId;
                await Simulation.UpdateHeaderAsync(register);

                await _registerStore.ClearLinesAsync(BuildSimulationRegisterRef(register));
            }
            else
            {
                // Lines 1076-1088. The simulation register has no OffCyclePaymentDate column.
                register = new SimulationSalaryRegister
                {
                    EmployeeId = employee.Id,
                    ForPeriod = payForDate,
                    InPeriod = payInDate,
                    PayrollRunType = PayrollRunType.OffCycle,
                    PayrollResultType = PayrollResultType.Current,
                    PayrollReasonTypeId = reasonTypeId
                };

                register.Id = await Simulation.InsertHeaderAsync(register);
            }

            // Lines 1090-1092: all seven on-hold flags false, and no "ReturnDate not found" refusal.
            await RunPayrollCalculationAsync(
                input, employee, payForDate, payInDate, settings, new PayrollOnHoldFlags(),
                isNotReturnFromLeave, payrollArea, orgAssignments,
                BuildSimulationRegisterRef(register));

            employeeResult.SalaryRegisterIds.Add(register.Id);
        }
    }

    #endregion
}
