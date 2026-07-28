using System.Collections.Generic;
using System.Linq;

namespace HCMv3.PayrollManagement.BankFiles.Formatters;

/// <summary>
/// KIB-G1 -- Grid, all clients. Ported from <c>uscBankFile2.ascx.cs:1798-1852</c>.
/// Ordered by employee id.
/// </summary>
public class KibG1Formatter : IBankFileFormatter
{
    public string FormatCode => BankFileFormatCodes.KibG1;

    public string PayerBankCode => "KIB";

    internal const string EmployeeIban = "IBAN";
    internal const string EmployeeName = "Name";
    internal const string EmployeeCivilId = "Civil ID";
    internal const string SalaryAmount = "Salary Amount";

    public BankFileDocument Build(BankFileContext context)
    {
        var doc = new BankFileDocument
        {
            Kind = BankFileDocumentKind.Grid,
            FormatCode = FormatCode,
            Columns = new List<string> { EmployeeIban, EmployeeName, EmployeeCivilId, SalaryAmount }
        };

        foreach (var row in context.Rows.OrderBy(r => r.EmployeeId))
        {
            doc.Rows.Add(new List<object>
            {
                row.HasBankRecord ? row.Iban : null,
                row.FullNameEn,
                row.CivilId.ToString(),
                row.Amount
            });
        }

        return doc;
    }
}

/// <summary>
/// WRB-G1 -- Grid, all clients. Ported from <c>uscBankFile2.ascx.cs:1864-1917</c>.
/// Same data as KIB-G1 but Civil ID leads and the IBAN sits third.
/// </summary>
public class WrbG1Formatter : IBankFileFormatter
{
    public string FormatCode => BankFileFormatCodes.WrbG1;

    public string PayerBankCode => "WRB";

    internal const string EmployeeCivilId = "Civil ID";
    internal const string EmployeeName = "Name";
    internal const string EmployeeIban = "IBAN";
    internal const string SalaryAmount = "Salary Amount";

    public BankFileDocument Build(BankFileContext context)
    {
        var doc = new BankFileDocument
        {
            Kind = BankFileDocumentKind.Grid,
            FormatCode = FormatCode,
            Columns = new List<string> { EmployeeCivilId, EmployeeName, EmployeeIban, SalaryAmount }
        };

        foreach (var row in context.Rows.OrderBy(r => r.EmployeeId))
        {
            doc.Rows.Add(new List<object>
            {
                row.CivilId.ToString(),
                row.FullNameEn,
                row.HasBankRecord ? row.Iban : null,
                row.Amount
            });
        }

        return doc;
    }
}

/// <summary>
/// NBR-G1 -- Grid, all clients. Ported from <c>uscBankFile2.ascx.cs:2318-2375</c>.
/// The legacy <c>formatList</c> test is commented out, so NBR always uses this single layout.
/// </summary>
public class NbrG1Formatter : IBankFileFormatter
{
    public string FormatCode => BankFileFormatCodes.NbrG1;

    public string PayerBankCode => "NBR";

    internal const string Prn = "PRN.";
    internal const string EmployeeName = "EMPLOYEE NAME";
    internal const string CivilIdNo = "CIVIL ID NO.";
    internal const string AccountNo = "ACCOUNT NO.";
    internal const string IbanCode = "IBAN CODE";
    internal const string BankName = "BANK NAME";
    internal const string BranchName = "BRANCH NAME";
    internal const string Amount = "AMOUNT";
    internal const string SwiftCode = "SWIFT CODE";

    public BankFileDocument Build(BankFileContext context)
    {
        var doc = new BankFileDocument
        {
            Kind = BankFileDocumentKind.Grid,
            FormatCode = FormatCode,
            Columns = new List<string>
            {
                Prn, EmployeeName, CivilIdNo, AccountNo, IbanCode, BankName, BranchName, Amount, SwiftCode
            }
        };

        foreach (var row in context.Rows)
        {
            doc.Rows.Add(new List<object>
            {
                row.EmployeeCode,
                row.FullNameEn,
                row.CivilId.ToString(),
                row.HasBankRecord ? row.AccountNum : null,
                row.HasBankRecord ? row.Iban : null,
                row.HasBankRecord ? (row.EmployeeBankCode != null ? row.EmployeeBankNameEn : string.Empty) : null,
                row.HasBankRecord ? row.BankBranch : null,
                row.Amount,
                row.HasBankRecord ? (row.EmployeeBankCode != null ? row.EmployeeBankSwiftCode : string.Empty) : null
            });
        }

        return doc;
    }
}

/// <summary>
/// KFH-G1 -- Grid, all clients. Ported from <c>uscBankFile2.ascx.cs:2387-2439</c>.
/// The Account column falls back to the IBAN when <c>AccountNum</c> is empty (legacy <c>:2423</c>).
/// The "Section" column is declared but never populated by legacy -- kept for layout fidelity.
/// </summary>
public class KfhG1Formatter : IBankFileFormatter
{
    public string FormatCode => BankFileFormatCodes.KfhG1;

    public string PayerBankCode => "KFH";

    internal const string SrNo = "No.";
    internal const string CivilId = "Civil ID";
    internal const string Name = "Name";
    internal const string AccountNo = "Account No.";
    internal const string Amount = "Amount";
    internal const string BankName = "Bank";
    internal const string Section = "Section";

    public BankFileDocument Build(BankFileContext context)
    {
        var doc = new BankFileDocument
        {
            Kind = BankFileDocumentKind.Grid,
            FormatCode = FormatCode,
            Columns = new List<string> { SrNo, CivilId, Name, AccountNo, Amount, BankName, Section }
        };

        foreach (var row in context.Rows)
        {
            object accountNo = null;
            object bankName = null;

            if (row.HasBankRecord)
            {
                accountNo = string.IsNullOrEmpty(row.AccountNum) ? row.Iban : row.AccountNum;
                bankName = row.EmployeeBankCode != null ? row.EmployeeBankNameEn : string.Empty;
            }

            doc.Rows.Add(new List<object>
            {
                // Legacy :2412 writes an int into a string column -> "1", "2", ...
                (doc.Rows.Count + 1).ToString(),
                row.CivilId.ToString(),
                row.FullNameEn,
                accountNo,
                row.Amount,
                bankName,
                null
            });
        }

        return doc;
    }
}

/// <summary>
/// BB-G1 (Boubyan) -- Grid, all clients. Ported from <c>uscBankFile2.ascx.cs:2451-2488</c>.
/// The legacy <c>formatList</c> test is commented out, so BB always uses this single layout.
/// </summary>
public class BbG1Formatter : IBankFileFormatter
{
    public string FormatCode => BankFileFormatCodes.BbG1;

    public string PayerBankCode => "BB";

    internal const string BeneficiaryAccountIban = "Beneficiary Account IBAN";
    internal const string BeneficiaryName = "Beneficiary Name";
    internal const string PaymentAmount = "Payment Amount";

    public BankFileDocument Build(BankFileContext context)
    {
        var doc = new BankFileDocument
        {
            Kind = BankFileDocumentKind.Grid,
            FormatCode = FormatCode,
            Columns = new List<string> { BeneficiaryAccountIban, BeneficiaryName, PaymentAmount }
        };

        foreach (var row in context.Rows)
        {
            doc.Rows.Add(new List<object>
            {
                row.HasBankRecord ? row.Iban : null,
                row.FullNameEn,
                row.Amount
            });
        }

        return doc;
    }
}
