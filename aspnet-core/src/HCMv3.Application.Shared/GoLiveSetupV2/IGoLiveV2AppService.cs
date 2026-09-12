using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GoLiveSetupV2.Dto;

namespace HCMv3.GoLiveSetupV2;

/// <summary>
/// Go-Live Setup V2, built backwards from its goal. It starts from the one thing a customer actually wants --
/// a payroll that runs -- and lets the payroll engine say what is missing. Each refusal becomes the next
/// setup step.
///
/// <para>Deliberately separate from Go-Live Setup V1, which stays as it is so the two approaches can be
/// compared side by side.</para>
/// </summary>
public interface IGoLiveV2AppService : IApplicationService
{
    /// <summary>
    /// Runs the real payroll engine now, not on the background queue, and reports how far it got.
    ///
    /// <para>A refusal is the expected answer here, not an error, so it comes back as a result instead of
    /// being thrown -- the screen's whole job is to show it. Every input is optional for the same reason:
    /// the first press sends nothing, and the engine's reply says what to add.</para>
    ///
    /// <para>Inline execution holds the request open for the whole run, which suits a go-live test tenant
    /// with a handful of employees. The production runner remains the queued
    /// <c>IPayrollEngineAppService.RunPayrollEngine</c>.</para>
    /// </summary>
    Task<GoLiveV2RunPayrollOutput> RunPayroll(GoLiveV2RunPayrollInput input);

    /// <summary>
    /// Answers "Payroll Control Record not found": makes the payroll area the run named, if the tenant does not
    /// have it, and its control record for the month, locked as the engine requires for a regular run. An area
    /// that already exists is used as it is, and one that already has a control record keeps it.
    ///
    /// <para>A new area gets Kuwait defaults -- Kuwaiti Dinar, a January to December pay year, cut-off at month
    /// end -- and the result names them. Both records go through their own services, so every check those
    /// screens make applies here too.</para>
    /// </summary>
    Task<GoLiveV2SetUpPayrollMonthOutput> SetUpPayrollMonth(GoLiveV2SetUpPayrollMonthInput input);

    /// <summary>
    /// Answers an empty payroll area. From four columns per employee -- code, full name, joining date, civil ID
    /// -- and one organisation unit name, creates each employee with an open-ended contract and org assignment
    /// from the joining date, and around them whatever the tenant does not yet have: the organisation unit, the
    /// payroll area, and an employee group "Default Normal Staff" with sub-group "Default Grade" set up as
    /// Kuwait labour law says (indemnity 15 then 26 days a year, capped at 18 months; 30 days' annual leave;
    /// 15 days' sick leave).
    ///
    /// <para>A bad row is reported and skipped; the rest load. An employee code already in the system is left
    /// alone, so loading the same sheet again is safe. Every record goes through its own service.</para>
    /// </summary>
    Task<GoLiveV2LoadEmployeesOutput> LoadEmployees(GoLiveV2LoadEmployeesInput input);

    /// <summary>
    /// What Load Employee Data loaded before, so the step opens with it: the company, and each employee in the
    /// payroll area in the grid's columns.
    /// </summary>
    Task<GoLiveV2LoadedEmployeesOutput> GetLoadedEmployees(string payrollAreaName);

    /// <summary>
    /// Step 3: what the month's payroll paid -- one line per employee in the payroll area with earnings,
    /// deductions and net pay, including anyone the payroll did not reach, and the totals.
    /// </summary>
    Task<GoLiveV2PayrollResultsOutput> GetPayrollResults(GoLiveV2PayrollResultsInput input);

    /// <summary>One employee's Salary Register for the month: every wage type with rate, number and amount.</summary>
    Task<GoLiveV2SalaryRegisterDto> GetSalaryRegister(GoLiveV2SalaryRegisterInput input);

    /// <summary>The month's register lines for everyone in the payroll area, one row per wage type, as Excel.</summary>
    Task<FileDto> GetSalaryRegisterExcel(GoLiveV2PayrollResultsInput input);

    /// <summary>The tenant's payroll areas by name, for the Run payroll dropdown.</summary>
    Task<ListResultDto<string>> GetPayrollAreaNames();

    /// <summary>The tenant's banks by name, for the Bank column of Load Employee Data.</summary>
    Task<ListResultDto<string>> GetBankNames();

    /// <summary>
    /// Step 4: the banks the month's salary file can be made for, each with its layouts and what they need,
    /// and the employees a bank file cannot pay for want of a bank or an IBAN.
    /// </summary>
    Task<GoLiveV2BankFileSetupOutput> GetBankFileSetup(GoLiveV2BankFileSetupInput input);

    /// <summary>
    /// Step 4: the month's salary file for the payroll area, from the chosen bank in the chosen layout, through
    /// the same generator the Bank File screen uses. Sets the bank up for that layout first -- its layout, and
    /// the company account or codes the layout needs -- so the Bank File screen makes the same file afterwards.
    /// </summary>
    Task<GoLiveV2GenerateBankFileOutput> GenerateBankFile(GoLiveV2GenerateBankFileInput input);
}
