using System.Collections.Generic;
using System.Text;
using static HCMv3.PayrollManagement.BankFiles.BankFileFieldFormat;

namespace HCMv3.PayrollManagement.BankFiles.Formatters;

/// <summary>
/// GBK-T1 -- Text for KPHC. HDR/DTL/TRL.
/// Ported from <c>uscBankFile2.ascx.cs:1308-1376</c>.
///
/// Notable legacy quirks preserved verbatim:
/// - the header date is <c>DateTime.Now</c>, NOT the period end (<c>:1314</c>);
/// - <c>"003 003 CBKS 90413911"</c> and <c>"000000003Y111303268258"</c> are hardcoded literals (<c>:1315, :1341</c>);
/// - the row is gated on <c>AccountNum</c> being non-empty, but the line actually emits the IBAN (<c>:1338-1340</c>);
/// - the trailer is appended WITHOUT a trailing newline (<c>sb.Append</c>, not <c>AppendLine</c>, at <c>:1370</c>).
/// </summary>
public class GbkT1Formatter : IBankFileFormatter
{
    public string FormatCode => BankFileFormatCodes.GbkT1;

    public string PayerBankCode => "GBK";

    private const string HeaderFixed = "003 003 CBKS 90413911";
    private const string DetailFixed = "000000003Y111303268258";

    public BankFileDocument Build(BankFileContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine("HDR" + " " + context.GeneratedOn.Date.ToString("ddMMyy") + " " + HeaderFixed + " " +
                      "S" + " " + context.EndDate.ToString("MMyy") + " " + "KWD");

        var count = 0;
        double totalAmount = 0;

        foreach (var row in context.Rows)
        {
            if (!row.HasBankRecord || string.IsNullOrEmpty(row.AccountNum) || row.Amount <= 0)
            {
                continue;
            }

            totalAmount += row.Amount;
            count++;

            // Legacy pads to 39 then truncates to 39 -- net effect is a fixed 39-char field.
            var empName = FixedWidth(row.FullNameEn, 39);
            var companyName = (row.CurrentCompanyNameEn ?? string.Empty).ToUpperInvariant();

            sb.AppendLine("DTL" + row.CivilId + "   " + empName + " " + row.Iban + string.Empty +
                          Amount3(row.Amount, 12) + " " + DetailFixed + "                       " + companyName);
        }

        var footer = "TRL" + " " + count.ToString().PadLeft(8, '0') + " " + Amount3(totalAmount, 14) + " " +
                     string.Empty.PadLeft(30, '0');
        footer += string.Empty.PadRight(52, ' ');
        sb.Append(footer);

        return new BankFileDocument
        {
            Kind = BankFileDocumentKind.Text,
            FormatCode = FormatCode,
            TextContent = sb.ToString(),
            RecordCount = count,
            TotalAmount = totalAmount
        };
    }
}

/// <summary>
/// GBK-G1 -- Grid, all clients. Ported from <c>uscBankFile2.ascx.cs:1452-1507</c>.
/// Rows WITHOUT an effective non-cash EmployeeBank record are skipped entirely (legacy <c>:1479</c>).
/// </summary>
public class GbkG1Formatter : IBankFileFormatter
{
    public virtual string FormatCode => BankFileFormatCodes.GbkG1;

    public string PayerBankCode => "GBK";

    internal const string EmployeeAcNoIban = "Employee AC No/ IBAN";
    internal const string CivilId = "CIVILID";
    internal const string NameInEnglish = "Name in English";
    internal const string AmountKwd = "Amount (KWD)";
    internal const string BankName = "BankName";
    internal const string ReportMosal = "Report Mosal";
    internal const string MosalId = "MOSAL ID";
    internal const string EmployerEnglishName = "Employer English Name";

    /// <summary>GBK-G2 (HAMRA) adds the two sponsor columns. Legacy <c>:1389-1400</c>.</summary>
    protected virtual bool IncludeSponsorColumns => false;

    public BankFileDocument Build(BankFileContext context)
    {
        var columns = new List<string>
        {
            EmployeeAcNoIban, CivilId, NameInEnglish, AmountKwd, BankName, ReportMosal
        };

        if (IncludeSponsorColumns)
        {
            columns.Add(MosalId);
            columns.Add(EmployerEnglishName);
        }

        var doc = new BankFileDocument
        {
            Kind = BankFileDocumentKind.Grid,
            FormatCode = FormatCode,
            Columns = columns
        };

        foreach (var row in context.Rows)
        {
            // Legacy filters the EmployeeBank lookup with PaymentType != CASH, and skips the row entirely
            // when nothing matches (:1475-1479).
            if (!row.HasBankRecord || row.PaymentType == PersonnelAdministration.PaymentType.Cash)
            {
                continue;
            }

            var cells = new List<object>
            {
                row.Iban,
                row.CivilId,
                row.FullNameEn,
                row.Amount,
                row.EmployeeBankCode != null ? row.EmployeeBankNameEn : "N/A",
                "Yes"
            };

            if (IncludeSponsorColumns)
            {
                if (row.HasSponsor)
                {
                    cells.Add(row.SponsorMosalId);
                    cells.Add(row.SponsorNameEn);
                }
                else
                {
                    cells.Add(null);
                    cells.Add(null);
                }
            }

            doc.Rows.Add(cells);
        }

        return doc;
    }
}

/// <summary>
/// GBK-G2 -- Grid for HAMRA. GBK-G1 plus MOSAL ID and Employer English Name.
/// Ported from <c>uscBankFile2.ascx.cs:1380-1448</c>.
/// </summary>
public class GbkG2Formatter : GbkG1Formatter
{
    public override string FormatCode => BankFileFormatCodes.GbkG2;

    protected override bool IncludeSponsorColumns => true;
}
