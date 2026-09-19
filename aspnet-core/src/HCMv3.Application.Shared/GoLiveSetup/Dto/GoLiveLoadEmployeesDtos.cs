using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GoLiveSetup.Dto;

/// <summary>
/// The "Load Employee Data" step: one organisation unit name and a grid of employees. Everything else an
/// employee needs before payroll is created from Kuwait defaults -- see <see cref="IGoLiveSetupAppService.LoadEmployees"/>.
/// </summary>
public class GoLiveLoadEmployeesInput
{
    /// <summary>The company, e.g. "ABC Holding". Created as an organisation unit if the tenant has none by that name.</summary>
    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string OrganizationUnitName { get; set; }

    /// <summary>The payroll area every employee is put into -- the one chosen in the Run payroll dropdown.</summary>
    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string PayrollAreaName { get; set; }

    /// <summary>
    /// Whether the company gives Saturday as a rest day (counted as Rest Days, 4009). Friday is always the weekly
    /// off. Every loaded employee gets the matching working week.
    /// </summary>
    public bool SaturdayIsRestDay { get; set; }

    public List<GoLiveEmployeeRowDto> Employees { get; set; } = new();
}

/// <summary>
/// One grid row, as typed or pasted. Deliberately unvalidated here: a bad row is reported against its row
/// number and the rest of the sheet still loads, rather than one typo refusing the whole request.
/// </summary>
public class GoLiveEmployeeRowDto
{
    public string Code { get; set; }

    public string FullName { get; set; }

    public DateTime? JoiningDate { get; set; }

    public string CivilId { get; set; }

    /// <summary>Monthly, in Kuwaiti Dinar. Required: without it there is nothing to pay. Wage type 1000.</summary>
    public double? BasicSalary { get; set; }

    /// <summary>Monthly. Optional, like every allowance. Wage type 1027.</summary>
    public double? AccommodationAllowance { get; set; }

    /// <summary>Wage type 1011.</summary>
    public double? MobileAllowance { get; set; }

    /// <summary>Wage type 1002.</summary>
    public double? TransportAllowance { get; set; }

    /// <summary>Wage type 1038.</summary>
    public double? FoodAllowance { get; set; }

    /// <summary>Wage type 1001.</summary>
    public double? HousingAllowance { get; set; }

    /// <summary>Anything regular the named columns do not cover. Wage type 1018.</summary>
    public double? OtherAllowance { get; set; }

    /// <summary>
    /// Days of annual leave the employee already has at go-live. Kept as an Annual Leave Opening Balance time
    /// transfer, counted in the first payroll month's accrual and leave provision. Blank or zero for none.
    /// </summary>
    public double? AnnualLeaveOpeningBalance { get; set; }

    /// <summary>
    /// The bank the salary is paid into, by name as the tenant's bank list has it. Optional: without it the
    /// employee is paid, but left out of the bank file.
    /// </summary>
    public string BankName { get; set; }

    /// <summary>Needed with a bank. Spaces are dropped and letters capitalised; a Kuwaiti IBAN is 30 characters.</summary>
    public string Iban { get; set; }

    /// <summary>
    /// The account number at that bank, as the bank writes it. Optional -- but some banks' file layouts pay
    /// into the account number rather than the IBAN, and leave out anyone without one.
    /// </summary>
    public string AccountNumber { get; set; }
}

/// <summary>What was loaded before, in the grid's shape: the company, and the payroll area's employees.</summary>
public class GoLiveLoadedEmployeesOutput
{
    /// <summary>Null until a load has put someone in an organisation unit.</summary>
    public string OrganizationUnitName { get; set; }

    /// <summary>Whether the employees were loaded with Saturday as a rest day -- the most of them, if they differ.</summary>
    public bool SaturdayIsRestDay { get; set; }

    public List<GoLiveEmployeeRowDto> Employees { get; set; } = new();
}

/// <summary>What the load did -- including each default it created -- so the screen can say so.</summary>
public class GoLiveLoadEmployeesOutput
{
    public string OrganizationUnitName { get; set; }

    public bool OrganizationUnitCreated { get; set; }

    public string PayrollAreaName { get; set; }

    public bool PayrollAreaCreated { get; set; }

    public string EmployeeGroupName { get; set; }

    public bool EmployeeGroupCreated { get; set; }

    public string EmployeeSubGroupName { get; set; }

    public bool EmployeeSubGroupCreated { get; set; }

    /// <summary>Indemnity slabs added to the sub-group; 0 when it already had them.</summary>
    public int GratuitySlabsCreated { get; set; }

    /// <summary>Leave entitlements (annual, sick) added to the sub-group; 0 when it already had them.</summary>
    public int LeaveEntitlementsCreated { get; set; }

    /// <summary>The Kuwait art. 69 sick-leave deduction rule (four slabs after the 15 full-pay days) was created.</summary>
    public bool SickLeaveRuleCreated { get; set; }

    /// <summary>
    /// The company's working time was created: time-engine settings, holiday calendar, 08:00-17:00 duty, Friday
    /// weekly off, planned-time policy. False once it exists.
    /// </summary>
    public bool WorkingTimeCreated { get; set; }

    /// <summary>NO_ records (NO_NATIONALITY, NO_TITLE, ...) added to the employee lookups; 0 once they exist.</summary>
    public int PlaceholdersCreated { get; set; }

    public int EmployeesCreated { get; set; }

    /// <summary>Rows whose employee code was already in the system: brought up to date from the sheet.</summary>
    public int EmployeesAlreadyLoaded { get; set; }

    /// <summary>Of those, how many the sheet actually changed.</summary>
    public int EmployeesUpdated { get; set; }

    /// <summary>One per non-empty grid row, in grid order.</summary>
    public List<GoLiveEmployeeRowResultDto> Rows { get; set; } = new();
}

public class GoLiveEmployeeRowResultDto
{
    /// <summary>1-based position in the list sent, blank rows included -- the number the grid shows beside the row.</summary>
    public int RowNumber { get; set; }

    public string Code { get; set; }

    public bool Succeeded { get; set; }

    /// <summary>False for an employee who was already loaded.</summary>
    public bool Created { get; set; }

    /// <summary>The employee was already loaded and the sheet changed something: name, dates, pay or bank.</summary>
    public bool Updated { get; set; }

    /// <summary>Why the row was not loaded, in words the customer can act on.</summary>
    public string Message { get; set; }
}
