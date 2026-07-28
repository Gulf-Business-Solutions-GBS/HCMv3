using System;
using System.Globalization;

namespace HCMv3.PayrollManagement.BankFiles;

/// <summary>One legacy bank-file layout. Pure: no I/O, no repositories, deterministic given a context.</summary>
public interface IBankFileFormatter
{
    /// <summary>Format code from <see cref="BankFileFormatCodes"/>.</summary>
    string FormatCode { get; }

    /// <summary>Payer <c>Bank.Code</c> this format belongs to (the legacy <c>switch</c> case label).</summary>
    string PayerBankCode { get; }

    BankFileDocument Build(BankFileContext context);
}

/// <summary>
/// Field-shaping helpers shared by the formatters. Every method mirrors an exact legacy expression --
/// widths, pad characters and alignment are load-bearing (a misplaced pad char = rejected transfer file).
/// </summary>
public static class BankFileFieldFormat
{
    /// <summary>
    /// Legacy <c>String.Format("{0:0.000}", Math.Round(amt, 3)).PadLeft(width, '0')</c>.
    ///
    /// DEVIATION (deliberate): legacy relied on the web server's ambient culture for the decimal separator.
    /// We pin <see cref="CultureInfo.InvariantCulture"/> so the separator is always '.' -- an
    /// Arabic/European server locale would otherwise emit ',' and produce a bank-rejected file.
    /// <c>Math.Round</c> keeps .NET's default banker's rounding (MidpointRounding.ToEven), same as legacy.
    /// </summary>
    public static string Amount3(double amount, int padLeftWidth)
    {
        return Math.Round(amount, 3).ToString("0.000", CultureInfo.InvariantCulture).PadLeft(padLeftWidth, '0');
    }

    /// <summary>Legacy <c>Math.Round(amt, 3)</c> with no padding.</summary>
    public static string Amount3(double amount)
    {
        return Math.Round(amount, 3).ToString("0.000", CultureInfo.InvariantCulture);
    }

    /// <summary>Null-safe left pad.</summary>
    public static string PadLeft(string value, int width, char pad) => (value ?? string.Empty).PadLeft(width, pad);

    /// <summary>Null-safe right pad.</summary>
    public static string PadRight(string value, int width, char pad) => (value ?? string.Empty).PadRight(width, pad);

    /// <summary>Truncate to <paramref name="maxLength"/> then right-pad to the same width (legacy 39/40 char names).</summary>
    public static string FixedWidth(string value, int width, char pad = ' ')
    {
        var v = value ?? string.Empty;
        if (v.Length > width)
        {
            v = v.Substring(0, width);
        }

        return v.PadRight(width, pad);
    }

    /// <summary>Truncate only, no padding (legacy CBK-T7 60-char name at <c>uscBankFile2.ascx.cs:855</c>).</summary>
    public static string Truncate(string value, int maxLength)
    {
        var v = value ?? string.Empty;
        return v.Length > maxLength ? v.Substring(0, maxLength) : v;
    }

    /// <summary>
    /// Legacy uses <c>PaymentType.ToString()</c> which on the XAF enum yields "BANK"/"CASH"/"NA".
    /// HCMv3's ported enum is Pascal-cased (Bank/Cash/Na), so we upper-case to preserve output fidelity.
    /// </summary>
    public static string PaymentTypeText(PersonnelAdministration.PaymentType paymentType)
    {
        return paymentType.ToString().ToUpperInvariant();
    }
}
