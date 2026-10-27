using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HCMv3.PayrollManagement.Costing;

/// <summary>
/// A short fingerprint of a journal voucher preview: the inputs it was built from plus every value of every
/// line that is sent to the finance system.
///
/// <para><b>Why it exists (no legacy equivalent).</b> Legacy rebuilt the JV from the database at the moment
/// of posting (<c>uscCostingFile.ascx.cs</c> <c>CreatePostJV</c> -> <c>ExportTable</c>, line 785), whatever
/// the operator had looked at. If the cost engine was re-run, a wage account was edited, or a different area
/// or date was typed in between the preview and the Post button, D365 received a journal nobody had checked.
/// The screen sends back the fingerprint of the preview it showed; the server rebuilds the JV, fingerprints
/// it, and refuses to post when the two differ. Nothing about the journal itself changes.</para>
///
/// <para>Doubles are written with the round-trip "R" format and dates as ISO dates, all in the invariant
/// culture, so the same JV always gives the same fingerprint whatever the server's culture.</para>
/// </summary>
public static class JournalVoucherFingerprint
{
    private const char FieldSeparator = '\u001f';
    private const char LineSeparator = '\u001e';

    public static string Compute(CostingRunInput input, JournalVoucherResult result)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var builder = new StringBuilder();

        void Add(string value) => builder.Append(value ?? "").Append(FieldSeparator);
        void AddDouble(double value) => Add(value.ToString("R", CultureInfo.InvariantCulture));
        void AddDate(DateTime value) => Add(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        Add((input.PayrollAreaName ?? "").Trim());
        Add(((int)input.PayrollRunType).ToString(CultureInfo.InvariantCulture));
        AddDate(input.PayInDate);
        AddDate(input.PostingDate);
        Add(string.Join(",", JournalVoucherBuilder.ParseEmployeeCodes(input.EmployeeCode)));
        builder.Append(LineSeparator);

        foreach (var line in result?.Lines ?? Enumerable.Empty<JournalVoucherLine>())
        {
            Add(line.CompanyAccounts);
            Add(line.JournalBatchNumber);
            Add(line.LineNumber.ToString(CultureInfo.InvariantCulture));
            Add(line.LineNumberAccountType);
            Add(line.LedgerDimensionMainAccount);
            Add(line.DefaultDimensionLocation);
            Add(line.DefaultDimensionEmployees);
            Add(line.LedgerDimensionLocation);
            Add(line.LedgerDimensionBrand);
            Add(line.LedgerDimensionDepartment);
            Add(line.LedgerDimensionEmployees);
            Add(line.Currency);
            AddDouble(line.Debit);
            AddDouble(line.Credit);
            Add(line.Description);
            Add(line.OffsetLedgerDimensionMainAccount);
            Add(line.OffsetCompanyAccounts);
            Add(line.BillingType);
            AddDate(line.TransDate);
            Add(line.EmpCode);
            builder.Append(LineSeparator);
        }

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>Case-insensitive, whitespace-tolerant comparison; a blank fingerprint never matches.</summary>
    public static bool Matches(string expected, string actual)
    {
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
        {
            return false;
        }

        return string.Equals(expected.Trim(), actual.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
