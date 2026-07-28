using System.Collections.Generic;
using System.Linq;
using System.Text;
using static HCMv3.PayrollManagement.BankFiles.BankFileFieldFormat;

namespace HCMv3.PayrollManagement.BankFiles.Formatters;

/// <summary>
/// CBK-G1 -- Grid, all clients. Ported from <c>uscBankFile2.ascx.cs:637-700</c>.
/// Legacy orders by <c>Employee.Oid</c> (<c>:665</c>).
/// </summary>
public class CbkG1Formatter : IBankFileFormatter
{
    public virtual string FormatCode => BankFileFormatCodes.CbkG1;

    public string PayerBankCode => "CBK";

    internal const string SerialNumber = "Serial no.";
    internal const string EmployeeCode = "Code no.";
    internal const string EmployeeName = "Name";
    internal const string EmployeeCivilId = "Civil id no";
    internal const string Sponsor = "sponsor";
    internal const string EmployeeBank = "Employee Bank";
    internal const string AccountNo = "Account no.";
    internal const string IbanNo = "Iban no.";
    internal const string PaymentTypeColumn = "Payment type";
    internal const string Amount = "Amount.";

    /// <summary>CBK-G2 appends an unsplit grand-total row. Legacy <c>:764-785</c>.</summary>
    protected virtual bool AppendGrandTotalRow => false;

    public BankFileDocument Build(BankFileContext context)
    {
        var doc = new BankFileDocument
        {
            Kind = BankFileDocumentKind.Grid,
            FormatCode = FormatCode,
            Columns = new List<string>
            {
                SerialNumber, EmployeeCode, EmployeeName, EmployeeCivilId, Sponsor,
                EmployeeBank, AccountNo, IbanNo, PaymentTypeColumn, Amount
            }
        };

        foreach (var row in context.Rows.OrderBy(r => r.EmployeeId))
        {
            object iban = null;
            object accountNo = null;
            object paymentType = null;
            object employeeBank = null;

            if (row.HasBankRecord)
            {
                iban = row.Iban;
                accountNo = row.AccountNum;
                paymentType = PaymentTypeText(row.PaymentType);
                employeeBank = row.EmployeeBankNameEn ?? string.Empty;
            }

            doc.Rows.Add(new List<object>
            {
                doc.Rows.Count + 1,
                row.EmployeeCode,
                row.FullNameEn,
                row.CivilId.ToString(),
                row.HasSponsor ? row.SponsorNameEn ?? string.Empty : null,
                employeeBank,
                accountNo,
                iban,
                paymentType,
                row.Amount
            });
        }

        if (AppendGrandTotalRow && context.Rows.Count > 0)
        {
            // Legacy :767-783 -- every cell except Amount is left DBNull, and the amount is the
            // UNSPLIT grand total (salRegDetailList is never re-filtered when splitting per payee bank).
            doc.Rows.Add(new List<object>
            {
                null, null, null, null, null, null, null, null, null,
                context.GrandTotalAmountAllBanks
            });
        }

        return doc;
    }
}

/// <summary>
/// CBK-G2 -- Grid for KHAT. Same layout as CBK-G1 plus a trailing Net Amount total row.
/// Ported from <c>uscBankFile2.ascx.cs:704-790</c>.
/// </summary>
public class CbkG2Formatter : CbkG1Formatter
{
    public override string FormatCode => BankFileFormatCodes.CbkG2;

    protected override bool AppendGrandTotalRow => true;
}

/// <summary>
/// Shared shape of the CBK fixed-width text formats: one header line, N detail lines, one trailer line.
/// The seven CBK text variants differ only in record tags, the header's account source/padding, the detail
/// line's field composition and the trailer's spacing -- each is spelled out in the concrete class.
/// </summary>
public abstract class CbkTextFormatterBase : IBankFileFormatter
{
    public abstract string FormatCode { get; }

    public string PayerBankCode => "CBK";

    /// <summary>Legacy constant <c>Code = "002"</c> in every CBK text branch.</summary>
    protected const string Code = "002";

    /// <summary>Legacy constant <c>PaddingCount = 5</c> for the trailer's record count.</summary>
    protected const int CountPadding = 5;

    /// <summary>Legacy constant <c>countStr = "99999"</c> used in place of a running sequence.</summary>
    protected const string FixedCountToken = "99999";

    public abstract BankFileDocument Build(BankFileContext context);

    protected static BankFileDocument Text(string formatCode, StringBuilder sb)
    {
        return new BankFileDocument
        {
            Kind = BankFileDocumentKind.Text,
            FormatCode = formatCode,
            TextContent = sb.ToString()
        };
    }
}

/// <summary>
/// CBK-T1 -- Text for IMILL. HDRINT/DTLINT/TRLINT.
/// Ported from <c>uscBankFile2.ascx.cs:1235-1295</c>.
/// Keys off <c>AccountNum</c> (not IBAN), padded LEFT to 10 with SPACES, and the header account number is
/// NOT padded (legacy <c>:1253</c> assigns AccountNo raw, unlike T3/T5/T6 which PadLeft(10,'0')).
/// </summary>
public class CbkT1Formatter : CbkTextFormatterBase
{
    public override string FormatCode => BankFileFormatCodes.CbkT1;

    public override BankFileDocument Build(BankFileContext context)
    {
        var sb = new StringBuilder();

        var cbkAcNum = context.OrganizationBankAccountNo ?? string.Empty;
        sb.AppendLine("HDRINT" + " " + Code + " " + context.EndDate.ToString("ddMMyyyy") + " " +
                      context.EndDate.ToString("ddMMyyyy") + " " + cbkAcNum);

        var seqCount = 0;
        double totalAmount = 0;

        foreach (var row in context.Rows)
        {
            if (!row.HasBankRecord || string.IsNullOrEmpty(row.AccountNum))
            {
                continue;
            }

            if (row.Amount <= 0)
            {
                continue;
            }

            seqCount++;
            totalAmount += row.Amount;

            sb.AppendLine("DTLINT" + " " + Code + " " + seqCount.ToString().PadLeft(CountPadding, '0') + "    " +
                          PadLeft(row.AccountNum, 10, ' ') + "     " + Amount3(row.Amount, 12));
        }

        sb.AppendLine("TRLINT" + " " + Code + " " + seqCount.ToString().PadLeft(CountPadding, '0') + "        " +
                      Amount3(totalAmount, 12));

        return Text(FormatCode, sb);
    }
}

/// <summary>
/// CBK-T2 -- Text for 039-NAH. HDRABG/DTLABG/TRLABG.
/// Ported from <c>uscBankFile2.ascx.cs:1167-1231</c>.
/// Keys off <c>AccountNum</c> padded LEFT to 10 with ZEROS; detail uses the fixed "99999" token instead of
/// the sequence number; header account IS PadLeft(10,'0'); trailer uses two spaces before the total.
/// </summary>
public class CbkT2Formatter : CbkTextFormatterBase
{
    public override string FormatCode => BankFileFormatCodes.CbkT2;

    public override BankFileDocument Build(BankFileContext context)
    {
        var sb = new StringBuilder();

        var cbkAcNum = string.IsNullOrEmpty(context.OrganizationBankAccountNo)
            ? string.Empty
            : context.OrganizationBankAccountNo.PadLeft(10, '0');

        sb.AppendLine("HDRABG" + " " + Code + "  " + context.EndDate.ToString("ddMMyyyy") + " " +
                      context.EndDate.ToString("ddMMyyyy") + " " + cbkAcNum);

        var seqCount = 0;
        double totalAmount = 0;

        foreach (var row in context.Rows)
        {
            if (!row.HasBankRecord || string.IsNullOrEmpty(row.AccountNum) || row.Amount <= 0)
            {
                continue;
            }

            seqCount++;
            totalAmount += row.Amount;

            sb.AppendLine("DTLABG" + " " + Code + " " + FixedCountToken + " " + PadLeft(row.AccountNum, 10, '0') +
                          "     " + Amount3(row.Amount, 12) + "  " + row.FullNameEn);
        }

        sb.AppendLine("TRLABG" + " " + Code + " " + seqCount.ToString().PadLeft(CountPadding, '0') + "  " +
                      Amount3(totalAmount, 12));

        return Text(FormatCode, sb);
    }
}

/// <summary>
/// CBK-T3 -- Text for IMILL. HDRSMS/DTLSMS/TRLSMS.
/// Ported from <c>uscBankFile2.ascx.cs:1092-1163</c>.
/// Keys off IBAN (PadLeft 10 '0'); detail carries the PAYEE bank code; iterates PER REGISTER.
/// </summary>
public class CbkT3Formatter : CbkTextFormatterBase
{
    public override string FormatCode => BankFileFormatCodes.CbkT3;

    /// <summary>CBK-T5 differs from T3 only in the detail line's spacing before the amount. Legacy <c>:1008</c>.</summary>
    protected virtual string AmountSeparator => "     ";

    public override BankFileDocument Build(BankFileContext context)
    {
        var sb = new StringBuilder();

        var cbkAcNum = string.IsNullOrEmpty(context.OrganizationBankAccountNo)
            ? string.Empty
            : context.OrganizationBankAccountNo.PadLeft(10, '0');

        sb.AppendLine("HDRSMS" + " " + Code + "  " + context.EndDate.ToString("ddMMyyyy") + " " +
                      context.EndDate.ToString("ddMMyyyy") + " " + cbkAcNum);

        var seqCount = 0;
        double totalAmount = 0;

        foreach (var row in BankFileRowExpansion.PerRegister(context.Rows))
        {
            if (!row.HasBankRecord || string.IsNullOrEmpty(row.Iban) || row.Amount <= 0)
            {
                continue;
            }

            // Legacy :1131-1137 -- seeded from the payer bank's ExternalCode, then OVERWRITTEN by the
            // PayeeBankDetail lookup whenever the employee has a Bank FK (even when unmapped, yielding "").
            var bankCode = context.PayerBankExternalCode;
            if (row.EmployeeBankCode != null)
            {
                bankCode = row.PayeeBankCode ?? string.Empty;
            }

            seqCount++;
            totalAmount += row.Amount;

            sb.AppendLine("DTLSMS" + " " + bankCode + " " + FixedCountToken + " " + PadLeft(row.Iban, 10, '0') +
                          AmountSeparator + Amount3(row.Amount, 12) + "  " + row.FullNameEn);
        }

        sb.AppendLine("TRLSMS" + " " + Code + " " + seqCount.ToString().PadLeft(CountPadding, '0') + "  " +
                      Amount3(totalAmount, 12));

        return Text(FormatCode, sb);
    }
}

/// <summary>
/// CBK-T5 -- Text for RHH. Identical to CBK-T3 except the detail line uses a SINGLE space before the amount
/// instead of five. Ported from <c>uscBankFile2.ascx.cs:957-1024</c> (compare <c>:1008</c> vs <c>:1144</c>).
/// </summary>
public class CbkT5Formatter : CbkT3Formatter
{
    public override string FormatCode => BankFileFormatCodes.CbkT5;

    protected override string AmountSeparator => " ";
}

/// <summary>
/// CBK-T6 -- Text for RHH. CR #4488(RHH) "CBK Portal - Other Bank Beneficiaries Requirements".
/// Ported from <c>uscBankFile2.ascx.cs:883-953</c>.
/// Same as CBK-T5 plus the employee's Civil ID between the amount and the name.
/// </summary>
public class CbkT6Formatter : CbkTextFormatterBase
{
    public override string FormatCode => BankFileFormatCodes.CbkT6;

    public override BankFileDocument Build(BankFileContext context)
    {
        var sb = new StringBuilder();

        var cbkAcNum = string.IsNullOrEmpty(context.OrganizationBankAccountNo)
            ? string.Empty
            : context.OrganizationBankAccountNo.PadLeft(10, '0');

        sb.AppendLine("HDRSMS" + " " + Code + "  " + context.EndDate.ToString("ddMMyyyy") + " " +
                      context.EndDate.ToString("ddMMyyyy") + " " + cbkAcNum);

        var seqCount = 0;
        double totalAmount = 0;

        foreach (var row in BankFileRowExpansion.PerRegister(context.Rows))
        {
            if (!row.HasBankRecord || string.IsNullOrEmpty(row.Iban) || row.Amount <= 0)
            {
                continue;
            }

            var bankCode = context.PayerBankExternalCode;
            if (row.EmployeeBankCode != null)
            {
                bankCode = row.PayeeBankCode ?? string.Empty;
            }

            seqCount++;
            totalAmount += row.Amount;

            sb.AppendLine("DTLSMS" + " " + bankCode + " " + FixedCountToken + " " + PadLeft(row.Iban, 10, '0') +
                          " " + Amount3(row.Amount, 12) + " " + row.CivilId + "  " + row.FullNameEn);
        }

        sb.AppendLine("TRLSMS" + " " + Code + " " + seqCount.ToString().PadLeft(CountPadding, '0') + "  " +
                      Amount3(totalAmount, 12));

        return Text(FormatCode, sb);
    }
}

/// <summary>
/// CBK-T4 -- Text for IMILL. HDRABC/DTLSIG/TRLABC.
/// Ported from <c>uscBankFile2.ascx.cs:1028-1088</c>.
/// Keys off IBAN padded LEFT to 10 with SPACES; header account is unpadded; trailer uses eight spaces.
/// </summary>
public class CbkT4Formatter : CbkTextFormatterBase
{
    public override string FormatCode => BankFileFormatCodes.CbkT4;

    public override BankFileDocument Build(BankFileContext context)
    {
        var sb = new StringBuilder();

        var cbkAcNum = context.OrganizationBankAccountNo ?? string.Empty;

        sb.AppendLine("HDRABC" + " " + Code + " " + context.EndDate.ToString("ddMMyyyy") + " " +
                      context.EndDate.ToString("ddMMyyyy") + " " + cbkAcNum);

        var seqCount = 0;
        double totalAmount = 0;

        foreach (var row in context.Rows)
        {
            if (!row.HasBankRecord || string.IsNullOrEmpty(row.Iban) || row.Amount <= 0)
            {
                continue;
            }

            seqCount++;
            totalAmount += row.Amount;

            sb.AppendLine("DTLSIG" + " " + Code + " " + seqCount.ToString().PadLeft(CountPadding, '0') + "    " +
                          PadLeft(row.Iban, 10, ' ') + "     " + Amount3(row.Amount, 12) + "  " + row.FullNameEn);
        }

        sb.AppendLine("TRLABC" + " " + Code + " " + seqCount.ToString().PadLeft(CountPadding, '0') + "        " +
                      Amount3(totalAmount, 12));

        return Text(FormatCode, sb);
    }
}

/// <summary>
/// CBK-T7 -- Text for NEC. CR #4847(NEC) "CBK Bank File Format For Salary Transfer".
/// Ported from <c>uscBankFile2.ascx.cs:794-879</c>.
/// HDRABC/DTL/TRLABC. Header account comes from the SPONSOR BANK (not OrganizationBankInfo -- legacy
/// explicitly commented that lookup out at <c>:811-820</c>). Detail = MOSAL ID (12, '0') + raw IBAN +
/// amount + civil ID + name truncated to 60. Filters on IBAN presence, iterates PER REGISTER.
/// </summary>
public class CbkT7Formatter : CbkTextFormatterBase
{
    public override string FormatCode => BankFileFormatCodes.CbkT7;

    public override BankFileDocument Build(BankFileContext context)
    {
        var sb = new StringBuilder();

        var cbkAcNum = string.IsNullOrEmpty(context.SponsorBankAccountNumber)
            ? string.Empty
            : context.SponsorBankAccountNumber.PadLeft(10, '0');

        sb.AppendLine("HDRABC" + " " + Code + " " + context.EndDate.ToString("ddMMyyyy") + " " +
                      context.EndDate.ToString("ddMMyyyy") + " " + cbkAcNum);

        var seqCount = 0;
        double totalAmount = 0;

        foreach (var row in BankFileRowExpansion.PerRegister(context.Rows))
        {
            if (row.Amount <= 0 || string.IsNullOrEmpty(row.Iban))
            {
                continue;
            }

            // Legacy :844-849 -- MOSALID of the EMPLOYEE's own sponsor; 0 when absent, then PadLeft(12,'0').
            var mosalId = (row.HasSponsor ? row.SponsorMosalId : 0).ToString().PadLeft(12, '0');

            seqCount++;
            totalAmount += row.Amount;

            sb.AppendLine("DTL" + " " + mosalId + " " + row.Iban + " " + Amount3(row.Amount, 12) + " " +
                          row.CivilId + "  " + Truncate(row.FullNameEn, 60));
        }

        sb.AppendLine("TRLABC" + " " + Code + " " + seqCount.ToString().PadLeft(CountPadding, '0') + " " +
                      Amount3(totalAmount, 12));

        return Text(FormatCode, sb);
    }
}
