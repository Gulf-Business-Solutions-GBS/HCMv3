using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GoLiveSetupV2.Dto;

/// <summary>
/// The "Load Employee Data" step: one organisation unit name and a grid of employees. Everything else an
/// employee needs before payroll is created from Kuwait defaults -- see <see cref="IGoLiveV2AppService.LoadEmployees"/>.
/// </summary>
public class GoLiveV2LoadEmployeesInput
{
    /// <summary>The company, e.g. "ABC Holding". Created as an organisation unit if the tenant has none by that name.</summary>
    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string OrganizationUnitName { get; set; }

    /// <summary>The payroll area every employee is put into -- the one chosen in the Run payroll dropdown.</summary>
    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string PayrollAreaName { get; set; }

    public List<GoLiveV2EmployeeRowDto> Employees { get; set; } = new();
}

/// <summary>
/// One grid row, as typed or pasted. Deliberately unvalidated here: a bad row is reported against its row
/// number and the rest of the sheet still loads, rather than one typo refusing the whole request.
/// </summary>
public class GoLiveV2EmployeeRowDto
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
}

/// <summary>What was loaded before, in the grid's shape: the company, and the payroll area's employees.</summary>
public class GoLiveV2LoadedEmployeesOutput
{
    /// <summary>Null until a load has put someone in an organisation unit.</summary>
    public string OrganizationUnitName { get; set; }

    public List<GoLiveV2EmployeeRowDto> Employees { get; set; } = new();
}

/// <summary>What the load did -- including each default it created -- so the screen can say so.</summary>
public class GoLiveV2LoadEmployeesOutput
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

    /// <summary>Rows whose employee code was already in the system: left as they were.</summary>
    public int EmployeesAlreadyLoaded { get; set; }

    /// <summary>One per non-empty grid row, in grid order.</summary>
    public List<GoLiveV2EmployeeRowResultDto> Rows { get; set; } = new();
}

public class GoLiveV2EmployeeRowResultDto
{
    /// <summary>1-based position in the list sent, blank rows included -- the number the grid shows beside the row.</summary>
    public int RowNumber { get; set; }

    public string Code { get; set; }

    public bool Succeeded { get; set; }

    /// <summary>False for an employee who was already loaded.</summary>
    public bool Created { get; set; }

    /// <summary>Why the row was not loaded, in words the customer can act on.</summary>
    public string Message { get; set; }
}
