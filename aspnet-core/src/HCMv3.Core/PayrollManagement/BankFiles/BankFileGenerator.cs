using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.PayrollManagement.BankFiles.Formatters;

namespace HCMv3.PayrollManagement.BankFiles;

/// <summary>
/// Thrown when the payer bank's <c>Bank.Code</c> matches no legacy <c>switch</c> branch.
/// Mirrors legacy <c>uscBankFile2.ascx.cs:2498-2501</c>.
/// </summary>
public class BankFileFormatNotFoundException : Exception
{
    public BankFileFormatNotFoundException(string payerBankCode)
        : base($"File Format not found for Bank Code : '{payerBankCode}'. Please check Bank Code.")
    {
        PayerBankCode = payerBankCode;
    }

    public string PayerBankCode { get; }
}

/// <summary>
/// Selects and runs the correct <see cref="IBankFileFormatter"/> for a payer bank + assigned format code.
///
/// This is a faithful port of the legacy <c>switch (payerBankcode)</c> dispatch at
/// <c>uscBankFile2.ascx.cs:433-2496</c>, INCLUDING each branch's <c>else</c> default and the two branches
/// (KIB, WRB) that silently produce NOTHING when no format is assigned.
///
/// Format selection input (legacy <c>:418-430</c>): the format code is taken from the PAYEE bank's
/// <c>PayeeBankDetail.BankFile.NameEn</c> when splitting per payee bank, else from the payer
/// <c>Bank.BankFile.NameEn</c>. Pure domain service -- no repositories, no EF.
/// </summary>
public class BankFileGenerator
{
    private readonly IReadOnlyList<IBankFileFormatter> _formatters;

    public BankFileGenerator()
        : this(DefaultFormatters())
    {
    }

    public BankFileGenerator(IReadOnlyList<IBankFileFormatter> formatters)
    {
        _formatters = formatters;
    }

    /// <summary>All 28 ported formats.</summary>
    public static IReadOnlyList<IBankFileFormatter> DefaultFormatters()
    {
        return new IBankFileFormatter[]
        {
            new NbkG1Formatter(), new NbkG2Formatter(), new NbkG3Formatter(),
            new CbkG1Formatter(), new CbkG2Formatter(),
            new CbkT1Formatter(), new CbkT2Formatter(), new CbkT3Formatter(), new CbkT4Formatter(),
            new CbkT5Formatter(), new CbkT6Formatter(), new CbkT7Formatter(),
            new GbkT1Formatter(), new GbkG1Formatter(), new GbkG2Formatter(),
            new AbkG1Formatter(), new AbkG2Formatter(), new AbkG3Formatter(),
            new KibG1Formatter(),
            new WrbG1Formatter(),
            new BurT1Formatter(), new BurG1Formatter(), new BurG2Formatter(),
            new BurBdT1Formatter(), new BurBdG1Formatter(),
            new NbrG1Formatter(),
            new KfhG1Formatter(),
            new BbG1Formatter()
        };
    }

    /// <summary>
    /// Resolves which format code the legacy switch would use for the given payer bank + assigned format.
    /// Returns <c>null</c> when the branch produces no file (legacy KIB/WRB with no format assigned).
    /// Throws <see cref="BankFileFormatNotFoundException"/> for an unknown payer bank code.
    /// </summary>
    public static string ResolveFormatCode(string payerBankCode, string assignedFormatCode)
    {
        bool Is(string code) => assignedFormatCode == code;

        switch (payerBankCode)
        {
            // Legacy :436-628
            case "NBK":
                if (Is(BankFileFormatCodes.NbkG2)) return BankFileFormatCodes.NbkG2;
                if (Is(BankFileFormatCodes.NbkG3)) return BankFileFormatCodes.NbkG3;
                return BankFileFormatCodes.NbkG1;

            // Legacy :633-1298 -- note the else-if order: G1, G2, T7, T6, T5, T4, T3, T2, else T1.
            case "CBK":
                if (Is(BankFileFormatCodes.CbkG1)) return BankFileFormatCodes.CbkG1;
                if (Is(BankFileFormatCodes.CbkG2)) return BankFileFormatCodes.CbkG2;
                if (Is(BankFileFormatCodes.CbkT7)) return BankFileFormatCodes.CbkT7;
                if (Is(BankFileFormatCodes.CbkT6)) return BankFileFormatCodes.CbkT6;
                if (Is(BankFileFormatCodes.CbkT5)) return BankFileFormatCodes.CbkT5;
                if (Is(BankFileFormatCodes.CbkT4)) return BankFileFormatCodes.CbkT4;
                if (Is(BankFileFormatCodes.CbkT3)) return BankFileFormatCodes.CbkT3;
                if (Is(BankFileFormatCodes.CbkT2)) return BankFileFormatCodes.CbkT2;
                return BankFileFormatCodes.CbkT1;

            // Legacy :1303-1510 -- "GB" and "GBK" share one branch.
            case "GB":
            case "GBK":
                if (Is(BankFileFormatCodes.GbkT1)) return BankFileFormatCodes.GbkT1;
                if (Is(BankFileFormatCodes.GbkG2)) return BankFileFormatCodes.GbkG2;
                return BankFileFormatCodes.GbkG1;

            // Legacy :1515-1789
            case "ABK":
                if (Is(BankFileFormatCodes.AbkG3)) return BankFileFormatCodes.AbkG3;
                if (Is(BankFileFormatCodes.AbkG2)) return BankFileFormatCodes.AbkG2;
                return BankFileFormatCodes.AbkG1;

            // Legacy :1794-1855 -- NO else branch: without KIB-G1 assigned, no file is produced at all.
            case "KIB":
                return Is(BankFileFormatCodes.KibG1) ? BankFileFormatCodes.KibG1 : null;

            // Legacy :1860-1920 -- same "no else" behaviour as KIB.
            case "WRB":
                return Is(BankFileFormatCodes.WrbG1) ? BankFileFormatCodes.WrbG1 : null;

            // Legacy :1925-2161
            case "BUR":
                if (Is(BankFileFormatCodes.BurT1)) return BankFileFormatCodes.BurT1;
                if (Is(BankFileFormatCodes.BurG2)) return BankFileFormatCodes.BurG2;
                return BankFileFormatCodes.BurG1;

            // Legacy :2166-2309
            case "BURBD":
                return Is(BankFileFormatCodes.BurBdG1) ? BankFileFormatCodes.BurBdG1 : BankFileFormatCodes.BurBdT1;

            // Legacy :2314-2378, :2383-2442, :2447-2491 -- the formatList test is commented out in all three,
            // so these banks always use their single layout regardless of what is assigned.
            case "NBR":
                return BankFileFormatCodes.NbrG1;

            case "KFH":
                return BankFileFormatCodes.KfhG1;

            case "BB":
                return BankFileFormatCodes.BbG1;

            default:
                throw new BankFileFormatNotFoundException(payerBankCode);
        }
    }

    /// <summary>
    /// Builds the document for <paramref name="context"/> using the format assigned via
    /// <paramref name="assignedFormatCode"/>. Returns <c>null</c> when the legacy branch emits no file.
    /// </summary>
    public BankFileDocument Generate(BankFileContext context, string assignedFormatCode)
    {
        var formatCode = ResolveFormatCode(context.PayerBankCode, assignedFormatCode);
        if (formatCode == null)
        {
            return null;
        }

        return GenerateByFormatCode(context, formatCode);
    }

    /// <summary>Builds the document for an explicit format code, bypassing the legacy fallback chain.</summary>
    public BankFileDocument GenerateByFormatCode(BankFileContext context, string formatCode)
    {
        var formatter = _formatters.FirstOrDefault(f => f.FormatCode == formatCode);
        if (formatter == null)
        {
            throw new BankFileFormatNotFoundException(context.PayerBankCode);
        }

        return formatter.Build(context);
    }
}
