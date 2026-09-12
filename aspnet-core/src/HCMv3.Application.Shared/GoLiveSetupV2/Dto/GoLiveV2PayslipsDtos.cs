using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GoLiveSetupV2.Dto;

/// <summary>Which month's payslips, and for whose payroll area -- the ones chosen under Run payroll.</summary>
public class GoLiveV2PayslipsInput
{
    /// <summary>Any day of the month; only its month and year are used.</summary>
    [Required]
    public DateTime? PayDate { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string PayrollAreaName { get; set; }
}
