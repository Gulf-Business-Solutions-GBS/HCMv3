using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HCMv3.PayrollManagement;

namespace HCMv3.GoLiveSetupV2.Dto;

/// <summary>The legacy export to set against HCMv3's salary registers for a month and payroll area.</summary>
public class GoLiveV2LegacyCompareInput
{
    [Required]
    public DateTime? PayDate { get; set; }

    [Required]
    public string PayrollAreaName { get; set; }

    /// <summary>The uploaded file's name: ".xlsx" is read as a workbook, anything else as delimited text.</summary>
    [Required]
    [StringLength(260)]
    public string FileName { get; set; }

    /// <summary>The file itself. Read for the comparison and never stored.</summary>
    [Required]
    public string Base64Content { get; set; }
}

/// <summary>How a wage type compares. Matched means rate, number and amount all agree.</summary>
public enum GoLiveV2CompareStatus
{
    Matched = 0,
    Different = 1,
    OnlyInLegacy = 2,
    OnlyInHcmv3 = 3,
}

/// <summary>
/// Where a difference most likely comes from -- the question the comparison is for: is it the data given to
/// HCMv3, or the way HCMv3 calculates?
/// </summary>
public enum GoLiveV2CompareCause
{
    None = 0,

    /// <summary>Under 0.010 apart: rounding, almost certainly.</summary>
    Rounding = 1,

    /// <summary>The inputs differ: the salary record, or an employee not loaded.</summary>
    Data = 2,

    /// <summary>The days paid or counted differ: joining date, absences, or the time engine.</summary>
    Days = 3,

    /// <summary>Same inputs, different result: HCMv3's calculation.</summary>
    Logic = 4,

    /// <summary>Something HCMv3 does not have set up, such as the wage type itself.</summary>
    Setup = 5,

    /// <summary>A total -- net pay -- that differs only because lines it adds up differ.</summary>
    FollowsOtherLines = 6,
}

/// <summary>One employee's wage type, side by side.</summary>
public class GoLiveV2LegacyCompareLineDto
{
    public string EmployeeCode { get; set; }

    public string WageTypeCode { get; set; }

    public string WageTypeName { get; set; }

    public BillingType? BillingType { get; set; }

    /// <summary>A time type: it counts days, so a different number is a difference in days.</summary>
    public bool ComparesDays { get; set; }

    public decimal? LegacyRate { get; set; }

    public decimal? LegacyNumber { get; set; }

    public decimal? LegacyAmount { get; set; }

    public decimal? Hcmv3Rate { get; set; }

    public decimal? Hcmv3Number { get; set; }

    public decimal? Hcmv3Amount { get; set; }

    /// <summary>
    /// The wage type on the employee's HCMv3 salary record on the month end, if it is there. Set against the
    /// legacy rate: for fixed pay, the legacy payroll's rate is the salary record's amount.
    /// </summary>
    public decimal? Hcmv3PayRecordAmount { get; set; }

    public bool RateMatches { get; set; }

    public bool NumberMatches { get; set; }

    public bool AmountMatches { get; set; }

    /// <summary>HCMv3's amount less legacy's.</summary>
    public decimal? Difference { get; set; }

    public GoLiveV2CompareStatus Status { get; set; }

    public GoLiveV2CompareCause Cause { get; set; }

    /// <summary>The likely reason, as a sentence.</summary>
    public string Reason { get; set; }

    /// <summary>
    /// A one-off earning or deduction the legacy payroll paid and HCMv3 did not, for an employee HCMv3 has: a
    /// payment for it on the Payment screen would make the next run pay it.
    /// </summary>
    public bool CanAddPayment { get; set; }
}

public class GoLiveV2LegacyCompareOutput
{
    public DateTime PayMonth { get; set; }

    /// <summary>Rows read from the file.</summary>
    public int FileRows { get; set; }

    /// <summary>Rows for a different month, left out.</summary>
    public int RowsForOtherMonths { get; set; }

    /// <summary>Rows without an employee code or wage type code, left out.</summary>
    public int RowsUnreadable { get; set; }

    /// <summary>Employees in the file.</summary>
    public int Employees { get; set; }

    /// <summary>HCMv3 employees in the payroll area the file did not include, so not compared.</summary>
    public int Hcmv3EmployeesNotInFile { get; set; }

    /// <summary>Employee codes in the file that HCMv3 does not have.</summary>
    public List<string> LegacyEmployeesNotInHcmv3 { get; set; } = new();

    public int Matched { get; set; }

    public int Different { get; set; }

    public int OnlyInLegacy { get; set; }

    public int OnlyInHcmv3 { get; set; }

    public List<GoLiveV2LegacyCompareLineDto> Lines { get; set; } = new();
}

/// <summary>The legacy export query, filled in for the month and the payroll area's employees.</summary>
public class GoLiveV2LegacyExportQueryDto
{
    public string FileName { get; set; }

    public string Sql { get; set; }
}
