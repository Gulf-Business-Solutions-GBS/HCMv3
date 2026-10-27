using System.Collections.Generic;
using System.Threading.Tasks;
using HCMv3.PayrollManagement.Costing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Outbound integration with the external finance system (Dynamics 365 Finance &amp; Operations OData).
/// Ported from legacy <c>uscCostingFile.ascx.cs</c> <c>PostTablewiseJV_D360</c> (lines 1079-1292),
/// <c>PostJournalHeader</c> (lines 1293-1381) and <c>PostMultiJournalLines</c> (lines 1382-1495).
///
/// <para>Isolated behind an interface so the JV assembly logic stays unit-testable without an HTTP
/// dependency, and so the D365-specific payload shape is replaceable.</para>
///
/// <para><b>Legacy behaviour deliberately NOT carried over:</b> the hardcoded Azure AD
/// <c>fpc</c>/<c>stsservicecookie</c>/<c>x-ms-gateway-slice</c> request cookies
/// (<c>uscCostingFile.ascx.cs</c> lines 1124-1128) are a captured browser session pinned to one tenant's
/// login and are not reproducible or safe to ship; and the hardcoded batch boundary literal
/// <c>"batch_d63a-e9be-2927"</c> (line 1176) is replaced by a per-request GUID, which is what the
/// multipart spec requires and which the server does not validate.</para>
/// </summary>
public interface IFinancePostingClient
{
    /// <summary>
    /// Creates one journal header and posts <paramref name="lines"/> as OData batched journal lines.
    ///
    /// <para>As legacy: a failed journal HEADER throws (nothing of that journal reached D365); a failed
    /// line BATCH is reported to <paramref name="observer"/> with D365's full reply and the lines that were
    /// sent, and the remaining batches are still posted (legacy <c>PostMultiJournalLines</c> logged the
    /// failure and returned, lines 1471-1494). Every header and batch, succeeded or failed, is reported to
    /// <paramref name="observer"/> as it happens, so what reached D365 is on record even if a later call
    /// fails or the process dies.</para>
    ///
    /// <para>Refuses before signing in when Dynamics 365 Finance is switched off in the Integration Center.</para>
    /// </summary>
    Task<FinanceJournalPostResult> PostJournalAsync(PayrollArea payrollArea, List<JournalVoucherLine> lines,
                                                    string journalHeaderName, IFinancePostingObserver observer);
}

/// <summary>What one <see cref="IFinancePostingClient.PostJournalAsync"/> call put into the finance system.</summary>
public class FinanceJournalPostResult
{
    /// <summary>The <c>JournalBatchNumber</c> D365 gave the journal header.</summary>
    public string JournalBatchNumber { get; set; }

    /// <summary>How many OData batches the lines were sent in (legacy <c>JsonstringList.Count</c>).</summary>
    public int BatchCount { get; set; }

    /// <summary>How many of those D365 rejected.</summary>
    public int FailedBatchCount { get; set; }
}

/// <summary>Receives each header / line-batch outcome the moment it is known (legacy wrote a <c>ProjectLog</c> row for each).</summary>
public interface IFinancePostingObserver
{
    Task OnPostingEventAsync(FinancePostingEvent postingEvent);
}

/// <summary>One header or line-batch outcome, with legacy's log wording.</summary>
public class FinancePostingEvent
{
    public bool Succeeded { get; set; }

    /// <summary>The <c>JournalBatchNumber</c> concerned; <c>null</c> when the header itself failed.</summary>
    public string JournalBatchNumber { get; set; }

    /// <summary>The full text for the log row.</summary>
    public string Message { get; set; }
}
