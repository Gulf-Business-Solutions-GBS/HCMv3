namespace HCMv3.TimeManagement;

/// <summary>
/// The two <c>TransType.Code</c> values that carry meaning to the Time Engine rather than merely being
/// labels an administrator chose.
///
/// <para>
/// A <c>TransType</c> row is otherwise a free lookup -- a tenant can name and add whatever punch types it
/// likes -- but the engine has to know which of them is "arrived" and which is "left", and it decides that
/// by code: <c>TimeEngineService</c> resolves <c>MasterData.ClockInTransTypeId</c> as the type whose
/// <c>Code</c> is "1" and <c>ClockOutTransTypeId</c> as "2" (legacy <c>clockInOid</c>/<c>clockOutOid</c>),
/// and every punch-resolution strategy then selects punches by those ids.
/// </para>
///
/// <para>
/// Named here so that code which has to make the same In/Out distinction -- deriving the time of a missing
/// punch from the duty's start or end, for instance -- states it once and against the same definition the
/// engine uses, instead of repeating a bare <c>"1"</c> whose meaning is only obvious to whoever wrote it.
/// </para>
/// </summary>
public static class TransTypeCodes
{
    /// <summary>Clock in / arrival. Legacy <c>clockInOid</c>.</summary>
    public const string ClockIn = "1";

    /// <summary>Clock out / departure. Legacy <c>clockOutOid</c>.</summary>
    public const string ClockOut = "2";

    /// <summary>Overtime in. Legacy ESS geo punch <c>TransTypeOtInCode</c>.</summary>
    public const string OvertimeIn = "3";

    /// <summary>Overtime out. Legacy ESS geo punch <c>TransTypeOtOutCode</c>.</summary>
    public const string OvertimeOut = "4";
}
