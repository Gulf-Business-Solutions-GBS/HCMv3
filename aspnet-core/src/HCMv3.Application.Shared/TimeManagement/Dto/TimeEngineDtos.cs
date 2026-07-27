using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// On-demand Time Engine run request. Mirrors the legacy console entry point's argument list
/// (<c>HCM10.TimeEngine/Program.cs</c> <c>Main(string[] args)</c>, lines ~150-171:
/// <c>args[0]</c>=employee code CSV, <c>args[1]</c>=start date, <c>args[2]</c>=end date,
/// <c>args[3]</c>=payroll area, <c>args[4]</c>=current user name, <c>args[5]</c>=IsRanFromPayrollEngine)
/// minus the two the server owns itself (user name comes from the ABP session;
/// <c>IsRanFromPayrollEngine</c> is false for every request that arrives through this API).
/// </summary>
public class RunTimeEngineInput
{
    /// <summary>
    /// Legacy <c>args[0]</c>. Comma-separated employee codes. Empty/null means "every employee matched by
    /// <see cref="PayrollAreaName"/>", or every ACTIVE employee when that is empty too.
    /// </summary>
    [StringLength(4000)]
    public string EmployeeCodes { get; set; }

    /// <summary>
    /// Legacy <c>args[1]</c>. Null falls back to the 1st of the current month, exactly as legacy's
    /// <c>DBDtNull</c> sentinel did.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Legacy <c>args[2]</c>. REQUIRED -- legacy's only up-front validation is
    /// "No End Date defined for Time Calculation or Check Date format."
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>Legacy <c>args[3]</c>. Matched against <c>PayrollArea.Name</c>.</summary>
    [StringLength(256)]
    public string PayrollAreaName { get; set; }

    /// <summary>
    /// PORT ADAPTATION, no legacy equivalent -- see
    /// <c>HCMv3.TimeManagement.Services.TimeEngineRunInput.EmployeeSubGroupIds</c>. Legacy read the employee's
    /// EmployeeSubGroup from <c>EmployeeOrgAssignment.EmployeeSubGroup</c>; NO entity in this port carries an
    /// <c>EmployeeSubGroupId</c> for an employee, so the engine cannot resolve it and takes the mapping
    /// (employeeId -&gt; EmployeeSubGroup id) from the caller.
    ///
    /// <para><b>Leaving this empty makes every employee fail</b> with the legacy error
    /// "Employee Sub Group not found for Employee." recorded on that employee's <c>TimeLog</c> row. That is a
    /// deliberate loud gap, not an oversight: the missing FK is a schema change the orchestrator owns.</para>
    /// </summary>
    public Dictionary<int, int> EmployeeSubGroupIds { get; set; }
}

/// <summary>
/// Acknowledgement returned by <see cref="ITimeEngineAppService.RunTimeEngine"/>. The run itself happens on an
/// ABP background job -- a full run spans thousands of employee-days and must never block an HTTP request --
/// so there is no result to return here.
///
/// <para>Progress and outcome are published on the <c>ExecutionLog</c> row the engine creates when the job
/// starts (<c>ExecutionStatus</c> INPRECESS -&gt; SUCCESS / PARTIAL / FAILED / ABORTED, plus
/// <c>TotalRecords</c>/<c>Successfull</c>/<c>Failed</c>), readable through the existing
/// <c>IExecutionLogsAppService</c>. Per-employee-per-day detail lands in <c>TimeLog</c>
/// (<c>ITimeLogsAppService</c>). This is exactly the mechanism legacy used
/// (<c>HCM10.TimeEngine/Program.cs</c> line ~236).</para>
/// </summary>
public class RunTimeEngineOutput
{
    /// <summary>True when the job was accepted onto the queue.</summary>
    public bool Queued { get; set; }
}
