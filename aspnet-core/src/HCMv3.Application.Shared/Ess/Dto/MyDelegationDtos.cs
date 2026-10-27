using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Ess.Dto;

/// <summary>
/// A delegation of duties as the delegating employee sees it in their own list.
///
/// <para>
/// The delegate's name is carried, not just the id. This list is read to answer "who is covering for me, and
/// when" -- a row showing only a number cannot answer either half of that.
/// </para>
/// </summary>
public class MyDelegationDto : MyRequestDtoBase
{
    public int DelegateToId { get; set; }

    public string DelegateToCode { get; set; }

    public string DelegateToFullNameEn { get; set; }

    public string DelegateToFullNameAr { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Comments { get; set; }

    /// <summary>
    /// Whether the delegation is in force right now -- approved (or recorded directly) and today falls
    /// inside the range. Computed rather than stored, because it changes with the date and not with any
    /// write to the row.
    /// </summary>
    public bool IsCurrentlyActive { get; set; }
}

/// <summary>
/// What an employee fills in to ask for cover.
///
/// <para>
/// There is deliberately no employee field: the delegator is taken from the session. A form that let you
/// name the delegator would let you give away somebody else's authority.
/// </para>
/// </summary>
public class RequestDelegationInput
{
    /// <summary>The colleague taking over. Must not be the requester -- refused by the handler, so the rule
    /// holds for an HR-raised delegation too.</summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int DelegateToId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [StringLength(1000)]
    public string Comments { get; set; }
}
