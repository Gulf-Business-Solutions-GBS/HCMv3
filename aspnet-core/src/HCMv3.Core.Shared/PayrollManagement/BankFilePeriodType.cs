namespace HCMv3.PayrollManagement;

/// <summary>
/// Which period the Bank File screen makes the file for -- legacy <c>uscBankFile.ascx</c> radio pair
/// <c>rdCurrent</c> ("Current", checked by default) / <c>rdOtherPeriod</c> ("Other").
/// </summary>
public enum BankFilePeriodType
{
    /// <summary>
    /// The payroll area's open period, read from its control record (legacy <c>SetCurrentDates</c>,
    /// <c>uscBankFile.ascx.cs:2551</c>).
    /// </summary>
    Current = 0,

    /// <summary>
    /// A payroll month and year the user types, turned into dates through the payroll area's payroll
    /// period (legacy <c>SetDates</c>, <c>uscBankFile.ascx.cs:2528</c>).
    /// </summary>
    Other = 1
}
