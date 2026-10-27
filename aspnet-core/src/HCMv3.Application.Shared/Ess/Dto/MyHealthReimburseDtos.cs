using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Ess.Dto;

/// <summary>
/// One of the employee's own health reimbursement claims, as the self-service list shows it.
/// </summary>
public class MyHealthReimburseDto : MyRequestDtoBase
{
    public DateTime PaymentDate { get; set; }

    public double Amount { get; set; }

    public string Comments { get; set; }
}

/// <summary>
/// What an employee fills in to claim a medical expense back.
///
/// <para>
/// There is no employee field, deliberately: the claim is attributed to whoever is signed in. A self-service
/// form that could name its own employee is a self-service form that can claim out of somebody else's
/// entitlement.
/// </para>
/// </summary>
public class RequestHealthReimburseInput
{
    /// <summary>The date the expense was paid. It decides which entitlement year the claim comes out of, so
    /// it is not merely descriptive.</summary>
    public DateTime PaymentDate { get; set; }

    /// <summary>
    /// The amount claimed. The upper bound is a sanity limit on the form only -- what actually constrains a
    /// claim is the employee's remaining entitlement, checked server-side.
    /// </summary>
    [Range(0.001, 100000000)]
    public double Amount { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

/// <summary>
/// Which entitlement year to report on. Defaults to today, which is the year a claim filed now would come
/// out of; a date is accepted so an employee filing a late claim can see the right year's figures.
/// </summary>
public class GetMyHealthReimburseBalanceInput
{
    public DateTime? AsOf { get; set; }
}

/// <summary>
/// What the employee has, has spent, and can still claim -- shown BEFORE they file anything.
///
/// <para>
/// In the legacy system this arithmetic existed but was only ever surfaced inside the text of a rejection.
/// The only way to learn your own entitlement was to claim too much and read the refusal, which is a poor
/// way to find out and worse when the expense has already been paid out of pocket. Hence a first-class
/// endpoint.
/// </para>
/// </summary>
public class MyHealthReimburseBalanceDto
{
    /// <summary>
    /// False when the employee's salary profile carries no health-reimbursement entitlement for the date. The
    /// screen should say so plainly rather than show a row of zeroes, because zero-because-unconfigured and
    /// zero-because-spent call for completely different actions.
    /// </summary>
    public bool HasEntitlement { get; set; }

    public string EmployeeSubGroupNameEn { get; set; }

    public string EmployeeSubGroupNameAr { get; set; }

    /// <summary>Start of the entitlement year the figures below cover.</summary>
    public DateTime PeriodFrom { get; set; }

    /// <summary>End of that entitlement year.</summary>
    public DateTime PeriodTo { get; set; }

    /// <summary>The salary profile's health-reimbursement entitlement for the period.</summary>
    public double Entitlement { get; set; }

    /// <summary>Unspent balance rolled in from earlier years, when the company allows carry-forward.</summary>
    public double CarriedForward { get; set; }

    /// <summary>Entitlement plus carry-forward, after the sub-group's ceiling is applied.</summary>
    public double Quota { get; set; }

    /// <summary>Already claimed and approved.</summary>
    public double Used { get; set; }

    /// <summary>Claimed and still awaiting a decision. Not spent, but spoken for.</summary>
    public double Pending { get; set; }

    /// <summary>What is left after approved claims -- shown so the employee can see why the claimable figure
    /// below is lower.</summary>
    public double Remaining { get; set; }

    /// <summary>What they can still claim today. The number a new claim is checked against.</summary>
    public double Claimable { get; set; }
}
