using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Ess.Dto;

/// <summary>
/// One of the employee's own loan recovery exemptions -- a month whose instalment they asked to skip -- as
/// the self-service list shows it.
/// </summary>
public class MyLoanExemptionDto : MyRequestDtoBase
{
    public int EmployeeLoanId { get; set; }

    public int? LoanTypeId { get; set; }

    public string LoanTypeNameEn { get; set; }

    public string LoanTypeNameAr { get; set; }

    /// <summary>Any date inside the month being skipped.</summary>
    public DateTime ExemptionDate { get; set; }

    /// <summary>
    /// The instalment being deferred, copied from the loan at read time. Shown next to the request because
    /// what an exemption actually does is postpone a specific sum of money, and the employee should see the
    /// figure they are asking to defer as plainly as the approver does.
    /// </summary>
    public double InstalmentAmount { get; set; }

    /// <summary>The loan's recovery end date as it stands. It moves out by a month when an exemption is
    /// approved, which is the visible proof that the instalment was deferred rather than written off.</summary>
    public DateTime LoanEndDate { get; set; }

    public string Comments { get; set; }
}

/// <summary>
/// One of the employee's loans, reduced to what the exemption form needs to let them pick one.
///
/// <para>
/// A lookup rather than a free-text loan id: an exemption is always against a loan the employee already
/// holds, and a form that accepted an arbitrary id would be a form that could defer somebody else's
/// instalment.
/// </para>
/// </summary>
public class MyLoanForExemptionDto
{
    public int EmployeeLoanId { get; set; }

    public int LoanTypeId { get; set; }

    public string LoanTypeNameEn { get; set; }

    public string LoanTypeNameAr { get; set; }

    /// <summary>The instalment in force this month (from the loan's instalment schedule, else its fixed amount) --
    /// the amount a single exemption defers.</summary>
    public double FixedAmount { get; set; }

    /// <summary>What is still owed on the loan.</summary>
    public double BalanceDueAmount { get; set; }

    /// <summary>First month recovery can be skipped from.</summary>
    public DateTime RecoveryStartDate { get; set; }

    /// <summary>Last month recovery can be skipped in. An exemption outside this window is refused, because
    /// it would be recorded and then quietly do nothing.</summary>
    public DateTime EndDate { get; set; }
}

/// <summary>
/// What an employee fills in to ask for one month's loan recovery to be skipped.
///
/// <para>
/// There is no employee field, deliberately: the request is attributed to whoever is signed in, and the loan
/// is re-checked as theirs before anything is created.
/// </para>
/// </summary>
public class RequestLoanExemptionInput
{
    /// <summary>The loan to skip a recovery on. Must be one of the caller's own.</summary>
    [Range(1, int.MaxValue)]
    public int EmployeeLoanId { get; set; }

    /// <summary>Any date inside the month whose instalment should be skipped.</summary>
    public DateTime ExemptionDate { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}
