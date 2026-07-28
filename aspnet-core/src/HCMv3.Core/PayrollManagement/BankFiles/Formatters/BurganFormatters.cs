using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using static HCMv3.PayrollManagement.BankFiles.BankFileFieldFormat;

namespace HCMv3.PayrollManagement.BankFiles.Formatters;

/// <summary>
/// BUR-T1 -- Text for KHUFF / RCF. HDR/DTL/TRL.
/// Ported from <c>uscBankFile2.ascx.cs:1929-2021</c>.
///
/// Quirks preserved verbatim:
/// - the header's employee-bank code is <c>""</c> when an Employee Bank filter WAS chosen and the payer
///   bank's ExternalCode when it was NOT -- i.e. inverted from what the variable name suggests (<c>:1967</c>);
/// - <c>"CBKS"</c> is a hardcoded literal (<c>:1939</c>);
/// - the MOSAL ID comes from the SCREEN's selected sponsor, not the employee's own sponsor (<c>:1995</c>);
/// - the name is right-padded to 40 but NOT truncated, so a longer name shifts every later field (<c>:1992</c>);
/// - the amount is left-padded to 12 with '0' and then right-padded to 19 with '0' -- i.e. trailing zeros
///   are appended after the decimals (<c>:1994</c>);
/// - the trailer counts <c>SeqCount - 1</c> because the sequence starts at 1 (<c>:2010</c>).
/// </summary>
public class BurT1Formatter : IBankFileFormatter
{
    public string FormatCode => BankFileFormatCodes.BurT1;

    public string PayerBankCode => "BUR";

    public BankFileDocument Build(BankFileContext context)
    {
        var sb = new StringBuilder();

        var sponsorBankExtCode = context.PayerBankExternalCode ?? string.Empty;
        var empBankExtCode = context.EmployeeBankFilterSelected ? string.Empty : sponsorBankExtCode;

        sb.AppendLine("HDR" + " " + context.EndDate.ToString("ddMMyy") + " " + sponsorBankExtCode + " " +
                      empBankExtCode + " " + "CBKS");

        var seqCount = 1;
        double totalAmount = 0;

        var mosalIdStr = PadRight(
            context.SelectedSponsorMosalId.HasValue
                ? context.SelectedSponsorMosalId.Value.ToString(CultureInfo.InvariantCulture)
                : string.Empty,
            18, '0');

        foreach (var row in context.Rows)
        {
            // Legacy :1983 checks IBAN != null only (an empty string still passes).
            if (!row.HasBankRecord || row.Iban == null || row.Amount <= 0)
            {
                continue;
            }

            var srNo = seqCount.ToString().PadLeft(15, '0');
            var fullName = PadRight(row.FullNameEn, 40, ' ');
            var iban = PadRight(row.Iban, 30, '0');
            var amount = Amount3(row.Amount, 12).PadRight(19, '0');

            sb.AppendLine("DTL" + srNo + fullName + iban + amount + mosalIdStr);

            seqCount++;
            totalAmount += row.Amount;
        }

        sb.AppendLine("TRL" + " " + (seqCount - 1).ToString().PadLeft(8, '0') + " " + Amount3(totalAmount, 34));

        return new BankFileDocument
        {
            Kind = BankFileDocumentKind.Text,
            FormatCode = FormatCode,
            TextContent = sb.ToString()
        };
    }
}

/// <summary>
/// BUR-G2 -- Grid for NEC. CR #4546(NEC) "Request for the updation of Burgan Bank Salary Sheet Format".
/// Ported from <c>uscBankFile2.ascx.cs:2025-2082</c>.
/// </summary>
public class BurG2Formatter : IBankFileFormatter
{
    public string FormatCode => BankFileFormatCodes.BurG2;

    public string PayerBankCode => "BUR";

    internal const string Dtl = "DTL";
    internal const string EmployeeNumber = "EmployeeNumber";
    internal const string EmployeeName = "Employee Name";
    internal const string EmployeeAccountNumber = "Employee Account Number";
    internal const string SalaryAmount = "Salary Amount";
    internal const string CompanyCode = "Company Code";
    internal const string BankCode = "Bank Code";
    internal const string MosalId = "Mosal ID";

    public BankFileDocument Build(BankFileContext context)
    {
        var doc = new BankFileDocument
        {
            Kind = BankFileDocumentKind.Grid,
            FormatCode = FormatCode,
            Columns = new List<string>
            {
                Dtl, EmployeeNumber, EmployeeName, EmployeeAccountNumber,
                SalaryAmount, CompanyCode, BankCode, MosalId
            }
        };

        foreach (var row in context.Rows)
        {
            doc.Rows.Add(new List<object>
            {
                "DTL",
                (doc.Rows.Count + 1).ToString().PadLeft(5, '0'),
                row.FullNameEn,
                row.HasBankRecord ? row.Iban : null,
                row.Amount,
                context.PayerBankCompanyCode ?? string.Empty,
                context.PayerBankExternalCode ?? string.Empty,
                row.HasSponsor ? row.SponsorMosalId.ToString(CultureInfo.InvariantCulture).PadLeft(12, '0') : null
            });
        }

        return doc;
    }
}

/// <summary>
/// BUR-G1 -- Grid, all clients (legacy <c>else</c> branch).
/// Ported from <c>uscBankFile2.ascx.cs:2086-2158</c>. Uses <c>AccountNum</c> (not IBAN) and the payee-bank
/// mapping's code + name.
/// </summary>
public class BurG1Formatter : IBankFileFormatter
{
    public string FormatCode => BankFileFormatCodes.BurG1;

    public string PayerBankCode => "BUR";

    internal const string EmployeeCode = "Emp No.";
    internal const string EmployeeName = "Name";
    internal const string AccountNum = "Account No";
    internal const string Amount = "Amount";
    internal const string ExternalBankCode = "External Bank Code";
    internal const string ExternalBankName = "External Bank Name";
    internal const string MosalId = "MOSAL ID";

    public BankFileDocument Build(BankFileContext context)
    {
        var doc = new BankFileDocument
        {
            Kind = BankFileDocumentKind.Grid,
            FormatCode = FormatCode,
            Columns = new List<string>
            {
                EmployeeCode, EmployeeName, AccountNum, Amount, ExternalBankCode, ExternalBankName, MosalId
            }
        };

        foreach (var row in context.Rows)
        {
            object accountNum = null;
            object extBankCode = null;
            object extBankName = null;

            if (row.HasBankRecord)
            {
                accountNum = row.AccountNum;

                if (row.EmployeeBankCode != null)
                {
                    extBankCode = row.PayeeBankCode ?? string.Empty;
                    extBankName = row.PayeeBankNameEn ?? string.Empty;
                }
                else
                {
                    extBankCode = string.Empty;
                    extBankName = string.Empty;
                }
            }

            doc.Rows.Add(new List<object>
            {
                row.EmployeeCode,
                row.FullNameEn,
                accountNum,
                row.Amount,
                extBankCode,
                extBankName,
                row.HasSponsor ? row.SponsorMosalId.ToString(CultureInfo.InvariantCulture) : null
            });
        }

        return doc;
    }
}

/// <summary>
/// BURBD-G1 (Burgan B-Dinar) -- Grid, all clients. CR #4546(NEC).
/// Ported from <c>uscBankFile2.ascx.cs:2170-2222</c>. The card number is the employee's <c>AccountNum</c>.
/// </summary>
public class BurBdG1Formatter : IBankFileFormatter
{
    public string FormatCode => BankFileFormatCodes.BurBdG1;

    public string PayerBankCode => "BURBD";

    internal const string DetailRecord = "Detail Record";
    internal const string EmployeeNumber = "Employee Number";
    internal const string EmployeeName = "Employee Name";
    internal const string BDinarCardNumber = "B-Dinar Card Number";
    internal const string PaymentAmount = "Payment Amount";
    internal const string MosalId = "Mosal ID";

    public BankFileDocument Build(BankFileContext context)
    {
        var doc = new BankFileDocument
        {
            Kind = BankFileDocumentKind.Grid,
            FormatCode = FormatCode,
            Columns = new List<string>
            {
                DetailRecord, EmployeeNumber, EmployeeName, BDinarCardNumber, PaymentAmount, MosalId
            }
        };

        foreach (var row in context.Rows)
        {
            doc.Rows.Add(new List<object>
            {
                "1",
                (doc.Rows.Count + 1).ToString().PadLeft(5, '0'),
                row.FullNameEn,
                row.HasBankRecord ? row.AccountNum : null,
                row.Amount,
                row.HasSponsor ? row.SponsorMosalId.ToString(CultureInfo.InvariantCulture).PadLeft(12, '0') : null
            });
        }

        return doc;
    }
}

/// <summary>
/// BURBD-T1 (Burgan B-Dinar) -- Text, all clients (legacy <c>else</c> branch).
/// Ported from <c>uscBankFile2.ascx.cs:2226-2306</c>. Record types are the literals "0"/"1"/"2".
///
/// Quirks preserved verbatim:
/// - <c>"0000RECAFCO"</c> is a hardcoded client literal in the header (<c>:2237</c>);
/// - the IBAN is computed but never written to the detail line (<c>:2263</c> is dead) -- the card/account
///   number is used instead;
/// - the amount is split into an integer part (10, '0') and a FILS part built by
///   <c>decPart.ToString().Replace("0.", "")</c> then right-padded -- 6 chars in the detail line but only
///   3 in the trailer (<c>:2268</c> vs <c>:2294</c>);
/// - the trailer carries a hardcoded 73-char constant captured from a production file (<c>:2295</c>),
///   which means the trailer's tail is NOT derived from the current run. Reported as a legacy defect;
///   ported as-is because changing it would alter byte layout the bank may be parsing positionally.
/// </summary>
public class BurBdT1Formatter : IBankFileFormatter
{
    public string FormatCode => BankFileFormatCodes.BurBdT1;

    public string PayerBankCode => "BURBD";

    /// <summary>Legacy <c>:2295</c> -- verbatim, including the trailing spaces.</summary>
    public const string LegacyTrailerConstant =
        "000000000000000000000000000223260757.925                                 ";

    public BankFileDocument Build(BankFileContext context)
    {
        var sb = new StringBuilder();

        var companyText = "0" + "0000RECAFCO";
        sb.AppendLine(companyText.PadRight(50, ' ') + context.EndDate.ToString("MMyyyy").PadRight(43, ' '));

        var seqCount = 1;
        double totalAmount = 0;

        foreach (var row in context.Rows)
        {
            if (!row.HasBankRecord)
            {
                continue;
            }

            var netAmount = Math.Round(row.Amount, 3);
            if (netAmount <= 0)
            {
                continue;
            }

            var empCode = PadLeft(row.EmployeeCode, 11, '0');
            var fullName = FixedWidth(row.FullNameEn, 40);
            var accountNum = PadLeft(row.AccountNum, 16, '0');

            var intPart = (int)netAmount;
            var decPart = Math.Round(netAmount - intPart, 3);
            var intPartStr = intPart.ToString(CultureInfo.InvariantCulture).PadLeft(10, '0');
            var filsPartStr = FilsPart(decPart, 6);

            var mosalIdStr = PadLeft(
                row.HasSponsor ? row.SponsorMosalId.ToString(CultureInfo.InvariantCulture) : string.Empty,
                9, '0');

            sb.AppendLine("1" + empCode + fullName + accountNum + intPartStr + filsPartStr + mosalIdStr);

            seqCount++;
            totalAmount += netAmount;
        }

        var totalNetPay = Math.Round(totalAmount, 3);
        var totIntPart = (int)totalNetPay;
        var totDecPart = Math.Round(totalNetPay - totIntPart, 3);

        sb.AppendLine("2" + (seqCount - 1).ToString().PadLeft(5, '0') +
                      totIntPart.ToString(CultureInfo.InvariantCulture).PadLeft(10, '0') +
                      FilsPart(totDecPart, 3) + " " + LegacyTrailerConstant);

        return new BankFileDocument
        {
            Kind = BankFileDocumentKind.Text,
            FormatCode = FormatCode,
            TextContent = sb.ToString()
        };
    }

    /// <summary>
    /// Legacy <c>decPart.ToString().Replace("0.", "").PadRight(width, '0')</c>.
    /// 0.54 -&gt; "540000"; 0.5 -&gt; "500000"; 0 -&gt; "000000" (because "0".Replace("0.","") == "0").
    /// Culture is pinned to invariant so the '.' separator matches -- see <see cref="BankFileFieldFormat.Amount3(double)"/>.
    /// </summary>
    private static string FilsPart(double decimalPart, int width)
    {
        return decimalPart.ToString(CultureInfo.InvariantCulture).Replace("0.", string.Empty).PadRight(width, '0');
    }
}
