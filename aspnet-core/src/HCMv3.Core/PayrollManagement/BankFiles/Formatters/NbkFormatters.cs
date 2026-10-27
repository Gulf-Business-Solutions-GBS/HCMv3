using System.Collections.Generic;

namespace HCMv3.PayrollManagement.BankFiles.Formatters;

/// <summary>
/// NBK-G2 -- Grid for LINADINA / HAMRA / KSEC.
/// Ported from <c>uscBankFile2.ascx.cs:440-496</c>.
/// Account column carries <c>AccountNum</c> when the employee banks with NBK, otherwise the IBAN.
/// </summary>
public class NbkG2Formatter : IBankFileFormatter
{
    public virtual string FormatCode => BankFileFormatCodes.NbkG2;

    public string PayerBankCode => "NBK";

    internal const string PaymentSerialNumber = "Payment Serial Number";
    internal const string BeneficiaryName = "Beneficiary Name";
    internal const string BeneficiaryCivilId = "Beneficiary Civil Id";
    internal const string AccountForNbk = "Account # for NBK A/C & IBAN for other Bank";
    internal const string BeneficiaryBank = "Beneficiary Bank (Refer next sheet 'Bank Codes' for list of Banks)";
    internal const string PaymentCurrency = "Payment Currency (only KWD)";
    internal const string PaymentAmount = "Payment Amount";

    /// <summary>When true (NBK-G3) the account column always uses IBAN. Legacy <c>:542</c>.</summary>
    protected virtual bool AlwaysUseIban => false;

    public virtual BankFileDocument Build(BankFileContext context)
    {
        var doc = new BankFileDocument
        {
            Kind = BankFileDocumentKind.Grid,
            FormatCode = FormatCode,
            Columns = new List<string>
            {
                PaymentSerialNumber, BeneficiaryName, BeneficiaryCivilId, AccountForNbk,
                BeneficiaryBank, PaymentCurrency, PaymentAmount
            }
        };

        foreach (var row in context.Rows)
        {
            object account = null;
            object beneficiaryBank = null;

            if (row.HasBankRecord && row.EmployeeBankCode != null)
            {
                account = AlwaysUseIban || row.EmployeeBankCode != "NBK" ? row.Iban : row.AccountNum;
                beneficiaryBank = row.EmployeeBankCode;
            }

            doc.Rows.Add(new List<object>
            {
                doc.Rows.Count + 1,
                row.FullNameEn,
                row.CivilId.ToString(),
                account,
                beneficiaryBank,
                "KWD",
                row.Amount
            });
        }

        return doc;
    }
}

/// <summary>
/// NBK-G3 -- Grid for AMK. Identical to NBK-G2 except the account column is ALWAYS the IBAN
/// (legacy comments out the NBK/AccountNum branch). Ported from <c>uscBankFile2.ascx.cs:501-557</c>.
/// </summary>
public class NbkG3Formatter : NbkG2Formatter
{
    public override string FormatCode => BankFileFormatCodes.NbkG3;

    protected override bool AlwaysUseIban => true;
}

/// <summary>
/// NBK-G1 -- Grid, default for all clients (legacy <c>else</c> branch).
/// Ported from <c>uscBankFile2.ascx.cs:561-625</c>.
/// </summary>
public class NbkG1Formatter : IBankFileFormatter
{
    public string FormatCode => BankFileFormatCodes.NbkG1;

    public string PayerBankCode => "NBK";

    internal const string EmployeeCode = "Employee Number";
    internal const string EmployeeName = "Employee Name";
    internal const string EmployeeIban = "Employee IBAN Number";
    internal const string PaymentAmount = "Payment Amount";
    internal const string BankCode = "Bank Code";
    internal const string EmployeeCivilId = "Employee Civil ID";
    internal const string MosalId = "MOSAL ID";

    public BankFileDocument Build(BankFileContext context)
    {
        var doc = new BankFileDocument
        {
            Kind = BankFileDocumentKind.Grid,
            FormatCode = FormatCode,
            Columns = new List<string>
            {
                EmployeeCode, EmployeeName, EmployeeIban, PaymentAmount, BankCode, EmployeeCivilId, MosalId
            }
        };

        foreach (var row in context.Rows)
        {
            object iban = null;
            object bankCode = null;
            object mosalId = null;

            if (row.HasBankRecord)
            {
                iban = row.Iban;
                // Legacy :599-605 -- "" when the payee-bank mapping is missing, never null.
                bankCode = row.EmployeeBankCode != null ? row.PayeeBankCode ?? string.Empty : string.Empty;
            }

            if (row.HasSponsor)
            {
                // Legacy :613 -- MOSALID, or literal 0 when the sponsor FK is null.
                mosalId = row.SponsorMosalId.ToString();
            }

            doc.Rows.Add(new List<object>
            {
                row.EmployeeCode,
                row.FullNameEn,
                iban,
                row.Amount,
                bankCode,
                row.CivilId.ToString(),
                mosalId
            });
        }

        return doc;
    }
}
