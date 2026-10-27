using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// On-demand Payroll Engine run request. Mirrors the legacy console entry point's argument list
/// (<c>HCM10.PayrollEngine/Program.cs</c> <c>Main(string[] args)</c>, lines 168-215:
/// <c>args[0]</c>=R/O run type, <c>args[1]</c>=employee code CSV, <c>args[2]</c>=pay-in date,
/// <c>args[3]</c>=simulation flag, <c>args[4]</c>=payroll area, <c>args[5]</c>=off-cycle payment date,
/// <c>args[6]</c>=off-cycle reason) minus the two the server owns
/// (<c>args[7]</c> UI culture, used only to pick between hardcoded English/Arabic message strings, and
/// <c>args[8]</c> user name, which now comes from the ABP session).
/// </summary>
public class RunPayrollEngineInput
{
    /// <summary>Legacy <c>args[0]</c>: "O" =&gt; OffCycle, anything else =&gt; Regular.</summary>
    public PayrollRunType PayrollRunType { get; set; }

    /// <summary>
    /// Legacy <c>args[1]</c>. Comma-separated employee codes; empty means "every employee in
    /// <see cref="PayrollAreaName"/>". <b>Mandatory for off-cycle runs</b> (legacy lines 285-291/314-320).
    /// </summary>
    [StringLength(4000)]
    public string EmployeeCodes { get; set; }

    /// <summary>Legacy <c>args[2]</c>. REQUIRED.</summary>
    [Required]
    public DateTime PayInDate { get; set; }

    /// <summary>Legacy <c>args[4]</c>: matched against <c>PayrollArea.Name</c> (NOT Code).</summary>
    [Required]
    [StringLength(256)]
    public string PayrollAreaName { get; set; }

    /// <summary>Legacy <c>args[5]</c>. Required for off-cycle runs unless time-engine autorun is on.</summary>
    public DateTime? OffCyclePaymentDate { get; set; }

    /// <summary>
    /// Legacy <c>args[6]</c>. Must be non-blank for off-cycle runs and is matched BY VALUE against
    /// <c>PayrollOffCycleReasons</c> ("End Of Service" / "Advance Leave" / "Leave Encashment").
    /// </summary>
    [StringLength(256)]
    public string OffCycleReason { get; set; }

    /// <summary>
    /// Legacy <c>args[3]</c> / the screen's Simulation check box (<c>chkSimulation</c>): a test run, written to
    /// the Simulation Salary Registers and leaving every live register and balance alone. Refused unless the
    /// tenant's payroll settings have Allow Simulation Run on.
    /// </summary>
    public bool IsSimulation { get; set; }

    /// <summary>
    /// Legacy <c>rdOtherPeriod</c> (simulation only). False = "Current": the run uses <see cref="PayInDate"/>, as
    /// a live run does. True = "Other": the period is <see cref="SimulationMonth"/>/<see cref="SimulationYear"/>
    /// of the payroll area's payroll year, and the server works out its pay date (legacy
    /// <c>uscPayrollEngine.SetDates</c> -> <c>PayrollControlRecord.GetStartEndDateFromPayrollPeriod</c>).
    /// </summary>
    public bool SimulationOtherPeriod { get; set; }

    /// <summary>Legacy <c>txtMonth</c>: the payroll month (1-12) of the other period. Required when <see cref="SimulationOtherPeriod"/> is set.</summary>
    [Range(1, 12)]
    public int? SimulationMonth { get; set; }

    /// <summary>Legacy <c>txtyear</c>: the payroll year of the other period. Required when <see cref="SimulationOtherPeriod"/> is set.</summary>
    [Range(1900, 9999)]
    public int? SimulationYear { get; set; }
}

/// <summary>
/// Acknowledgement returned by <see cref="IPayrollEngineAppService.RunPayrollEngine"/>. The run itself
/// happens on an ABP background job -- a payroll run spans thousands of employees x every wage type and must
/// never block an HTTP request -- so there is no result to return here.
///
/// <para>Progress and outcome are published on the <c>ExecutionLog</c> row the engine creates when the job
/// starts (<c>ExecutionStatus</c> INPRECESS -&gt; SUCCESS / PARTIAL / FAILED / ABORTED, plus
/// <c>TotalRecords</c>/<c>Successfull</c>/<c>Failed</c>), readable through the existing
/// <c>IExecutionLogsAppService</c>. Per-employee detail lands in <c>PayrollLog</c>
/// (<c>IPayrollLogsAppService</c>). This is exactly the mechanism legacy used
/// (<c>HCM10.PayrollEngine/Program.cs</c> lines 406-415 and 1566-1593).</para>
/// </summary>
public class RunPayrollEngineOutput
{
    /// <summary>True when the job was accepted onto the queue.</summary>
    public bool Queued { get; set; }
}
