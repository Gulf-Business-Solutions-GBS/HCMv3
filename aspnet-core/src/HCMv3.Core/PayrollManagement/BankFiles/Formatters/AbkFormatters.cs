using System;
using System.Collections.Generic;
using System.Linq;

namespace HCMv3.PayrollManagement.BankFiles.Formatters;

/// <summary>
/// ABK-G3 -- Grid for HAMRA. Ported from <c>uscBankFile2.ascx.cs:1519-1592</c>.
/// Unlike ABK-G1/G2 it applies NO bank-transfer on-hold filter and NO zero-amount filter, and does not
/// re-sort by bank code -- it simply orders by employee id.
/// </summary>
public class AbkG3Formatter : IBankFileFormatter
{
    public string FormatCode => BankFileFormatCodes.AbkG3;

    public string PayerBankCode => "ABK";

    internal const string EmployeeIban = "IBAN Number";
    internal const string EmployeeName = "Name";
    internal const string EmployeeCivilId = "Civil ID";
    internal const string Type = "TYPE";
    internal const string PaymentAmount = "Amount";
    internal const string BankCode = "Bank Code";

    public BankFileDocument Build(BankFileContext context)
    {
        var doc = new BankFileDocument
        {
            Kind = BankFileDocumentKind.Grid,
            FormatCode = FormatCode,
            Columns = new List<string>
            {
                EmployeeIban, EmployeeName, EmployeeCivilId, Type, PaymentAmount, BankCode
            }
        };

        foreach (var row in context.Rows.OrderBy(r => r.EmployeeId))
        {
            doc.Rows.Add(new List<object>
            {
                row.HasBankRecord ? row.Iban : null,
                row.FullNameEn,
                row.CivilId.ToString(),
                "SAL",
                row.Amount,
                row.HasBankRecord ? AbkBankCodeResolver.Resolve(context, row) : null
            });
        }

        return doc;
    }
}

/// <summary>
/// Legacy's three-way bank-code fallback, identical in ABK-G1/G2/G3
/// (<c>uscBankFile2.ascx.cs:1559-1570, 1649-1660, 1745-1756</c>).
/// </summary>
internal static class AbkBankCodeResolver
{
    public static string Resolve(BankFileContext context, BankFilePayeeRow row)
    {
        if (row.EmployeeBankCode != null && row.EmployeeBankIsPayerBank)
        {
            return context.PayerBankExternalCode;
        }

        if (row.EmployeeBankCode != null)
        {
            return row.PayeeBankCode ?? string.Empty;
        }

        return string.Empty;
    }
}

/// <summary>
/// ABK-G2 -- Grid for RCF. Ported from <c>uscBankFile2.ascx.cs:1596-1690</c>.
/// Drops employees whose bank transfer is on hold, drops zero-amount rows, then sorts by
/// (Bank Code, EmployeeCode) ascending. Retains the EmployeeCode column; only the internal EmployeeOid
/// helper column is removed.
/// </summary>
public class AbkG2Formatter : IBankFileFormatter
{
    public virtual string FormatCode => BankFileFormatCodes.AbkG2;

    public string PayerBankCode => "ABK";

    internal const string EmployeeCode = "EmployeeCode";
    internal const string EmployeeIban = "IBAN Number";
    internal const string EmployeeName = "Name";
    internal const string EmployeeCivilId = "Civil ID";
    internal const string Type = "TYPE";
    internal const string PaymentAmount = "Amount";
    internal const string BankCode = "Bank Code";

    /// <summary>ABK-G1 additionally removes the EmployeeCode column. Legacy <c>:1780</c>.</summary>
    protected virtual bool KeepEmployeeCodeColumn => true;

    public BankFileDocument Build(BankFileContext context)
    {
        var columns = new List<string>();
        if (KeepEmployeeCodeColumn)
        {
            columns.Add(EmployeeCode);
        }

        columns.AddRange(new[] { EmployeeIban, EmployeeName, EmployeeCivilId, Type, PaymentAmount, BankCode });

        var doc = new BankFileDocument
        {
            Kind = BankFileDocumentKind.Grid,
            FormatCode = FormatCode,
            Columns = columns
        };

        var ordered = context.Rows
            .Where(r => !r.IsBankTransferOnHold)
            .OrderBy(r => r.EmployeeId)
            .Select(r => new
            {
                Row = r,
                BankCodeValue = r.HasBankRecord ? AbkBankCodeResolver.Resolve(context, r) : null
            })
            // Legacy :1680-1681 -- DataView RowFilter "Amount <> 0" then Sort "Bank Code, EmployeeCode Asc".
            .Where(x => x.Row.Amount != 0)
            .OrderBy(x => x.BankCodeValue ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(x => x.Row.EmployeeCode ?? string.Empty, StringComparer.Ordinal);

        foreach (var item in ordered)
        {
            var cells = new List<object>();
            if (KeepEmployeeCodeColumn)
            {
                cells.Add(item.Row.EmployeeCode);
            }

            cells.Add(item.Row.HasBankRecord ? item.Row.Iban : null);
            cells.Add(item.Row.FullNameEn);
            cells.Add(item.Row.CivilId.ToString());
            cells.Add("SAL");
            cells.Add(item.Row.Amount);
            cells.Add(item.BankCodeValue);

            doc.Rows.Add(cells);
        }

        return doc;
    }
}

/// <summary>
/// ABK-G1 -- Grid, all clients (legacy <c>else</c> branch).
/// Ported from <c>uscBankFile2.ascx.cs:1694-1786</c>. Identical to ABK-G2 except the EmployeeCode column is
/// dropped from the output after sorting (it is still the sort key).
/// </summary>
public class AbkG1Formatter : AbkG2Formatter
{
    public override string FormatCode => BankFileFormatCodes.AbkG1;

    protected override bool KeepEmployeeCodeColumn => false;
}
