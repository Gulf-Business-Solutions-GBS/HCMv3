using System.Collections.Generic;
using System.Threading.Tasks;
using HCMv3.PayrollManagement.Costing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Outbound integration with the external finance system (Dynamics 365 Finance &amp; Operations OData).
/// Ported from legacy <c>uscCostingFile.ascx.cs</c> <c>PostTablewiseJV_D360</c> (lines 1091-1304),
/// <c>PostJournalHeader</c> (lines 1305-1400) and <c>PostMultiJournalLines</c> (lines 1401-1507).
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
    /// Returns the <c>JournalBatchNumber</c> assigned by the finance system.
    /// </summary>
    Task<string> PostJournalAsync(PayrollArea payrollArea, List<JournalVoucherLine> lines, string journalHeaderName);
}
