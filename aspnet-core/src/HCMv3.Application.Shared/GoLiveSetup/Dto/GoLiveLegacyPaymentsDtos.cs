using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.GoLiveSetup.Dto;

/// <summary>Comparison lines to turn into payments on the Payment screen for the month.</summary>
public class GoLiveLegacyPaymentsInput
{
    [Required]
    public DateTime? PayDate { get; set; }

    public List<GoLiveLegacyPaymentLineDto> Lines { get; set; } = new();
}

/// <summary>One payment to add: whose, which wage type, and the amount the legacy payroll paid.</summary>
public class GoLiveLegacyPaymentLineDto
{
    public string EmployeeCode { get; set; }

    public string WageTypeCode { get; set; }

    public decimal Amount { get; set; }
}

public class GoLiveLegacyPaymentsOutput
{
    public int Added { get; set; }

    /// <summary>Payments this comparison had made before, corrected to the amount sent.</summary>
    public int Updated { get; set; }

    /// <summary>Payments this comparison had made before with the same amount, left as they were.</summary>
    public int AlreadyThere { get; set; }

    public List<GoLiveLegacyPaymentRefusalDto> Refused { get; set; } = new();
}

/// <summary>A line no payment was added for, and why.</summary>
public class GoLiveLegacyPaymentRefusalDto
{
    public string EmployeeCode { get; set; }

    public string WageTypeCode { get; set; }

    public string Reason { get; set; }
}
