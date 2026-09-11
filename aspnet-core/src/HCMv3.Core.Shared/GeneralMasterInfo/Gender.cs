namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.Gender</c>.
/// </summary>
public enum Gender
{
    Male = 0,
    Female = 1,

    /// <summary>
    /// Not in legacy. Given to employees loaded without one (Go-Live Setup V2 asks only code, name, joining
    /// date and civil ID), so a missing gender reads as missing rather than as a guessed "Male". Rules that
    /// depend on gender test for <see cref="Female"/>, so nothing is granted or withheld by this value.
    /// </summary>
    NotSpecified = 2
}
