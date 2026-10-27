using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.TimeManagement;

namespace HCMv3.Ess.Dto;

/// <summary>
/// Self-service missing-punch ("skip punch") DTOs -- the port of legacy <c>SkipPunchRequest</c>'s form.
///
/// <para>
/// Kept in their own file rather than appended to <c>MyRequestDtos.cs</c>: that file is edited by every
/// self-service wave at once and has already lost classes to a bad merge.
/// </para>
/// </summary>
public class MyPunchRequestDto : MyRequestDtoBase
{
    public int TransTypeId { get; set; }

    public string TransTypeNameEn { get; set; }

    public string TransTypeNameAr { get; set; }

    /// <summary>Whether this is the arrival punch, so the screen can show In/Out without knowing about
    /// TransType codes.</summary>
    public bool IsClockIn { get; set; }

    public DateTime TransDate { get; set; }

    /// <summary>
    /// Null while the employee has not said what time it was and the request has not been granted yet. Once
    /// granted, it is either the time they gave or the one derived from the duty they were rostered on.
    /// </summary>
    public DateTime? TransTime { get; set; }

    /// <summary>
    /// <c>System</c> means the time was derived from the duty rather than stated -- worth showing, because
    /// it explains why the punch sits exactly on the shift boundary.
    /// </summary>
    public TransSource TransSource { get; set; }
}

/// <summary>
/// "I was here on this day but the clock has no record of it."
///
/// <para>
/// There is no employee field, on purpose: a self-service form must never be able to raise a punch on
/// somebody else's behalf. The employee comes from the session.
/// </para>
/// </summary>
public class RequestMissingPunchInput
{
    /// <summary>Which punch is missing. The catalog routes this type on <c>TransType</c>, so an arrival can
    /// require a different approval chain from a departure.</summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int TransTypeId { get; set; }

    /// <summary>The day the punch is missing from. Time-of-day is ignored.</summary>
    [Required]
    public DateTime TransDate { get; set; }

    /// <summary>
    /// When they actually clocked, if they know. Optional on purpose -- an employee who forgot to badge in
    /// usually does not know the minute -- and when it is left out the handler stamps the start (or end) of
    /// the duty they were rostered on at the moment the request is granted.
    /// </summary>
    public DateTime? TransTime { get; set; }
}
