using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GoLiveSetupV2.Dto;

/// <summary>
/// What clearing the payroll area removes -- or, after Clear, what it removed. Counted before anything goes,
/// so the confirmation can say it in numbers.
/// </summary>
public class GoLiveV2ClearCompanyDto
{
    /// <summary>The company the area's employees are in: what has to be typed back to confirm. Null when empty.</summary>
    public string CompanyName { get; set; }

    public int Employees { get; set; }

    public int TimeRegisters { get; set; }

    public int SalaryRegisters { get; set; }

    /// <summary>Everything else that belonged to those employees: contracts, salaries, banks, balances, logs.</summary>
    public int OtherRecords { get; set; }

    /// <summary>The month end of the area's payroll control record, when it has one.</summary>
    public DateTime? PayrollMonthEnd { get; set; }

    public int CalendarMonths { get; set; }
}

public class GoLiveV2ClearCompanyInput
{
    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string PayrollAreaName { get; set; }

    /// <summary>The company's name, typed back. Checked here, not just on the screen.</summary>
    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string ConfirmCompanyName { get; set; }
}
