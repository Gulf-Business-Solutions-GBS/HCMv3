using System;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>What the Bank File screen asks for when the payroll area, the Current/Other choice or the month/year changes.</summary>
public class GetBankFilePeriodInput
{
    public int? PayrollAreaId { get; set; }

    public BankFilePeriodType PeriodType { get; set; }

    /// <summary>Other only; today's month when blank (legacy).</summary>
    public int? PayrollMonth { get; set; }

    /// <summary>Other only; today's year when blank (legacy).</summary>
    public int? PayrollYear { get; set; }
}

/// <summary>
/// The period the file will be made for, worked out on the server. The screen shows the dates read-only
/// (legacy <c>txtStartDate</c>/<c>txtEndDate</c> were read-only) and sends back only the period type, payroll
/// area, month and year -- never the dates themselves.
/// </summary>
public class BankFilePeriodDto
{
    /// <summary>False when no period can be worked out yet; <see cref="Message"/> says why.</summary>
    public bool HasPeriod { get; set; }

    public string Message { get; set; }

    public BankFilePeriodType PeriodType { get; set; }

    public int? PayrollMonth { get; set; }

    public int? PayrollYear { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    /// <summary>dd/MM/yyyy, formatted on the server so the browser's time zone cannot move it.</summary>
    public string StartDateText { get; set; }

    /// <summary>dd/MM/yyyy, formatted on the server so the browser's time zone cannot move it.</summary>
    public string EndDateText { get; set; }
}
