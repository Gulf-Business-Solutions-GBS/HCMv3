using System;
using System.Collections.Generic;
using System.Linq;

namespace HCMv3.ReportManagement;

/// <summary>
/// The set of <see cref="PrintReport.ReportName"/> values for which a DevExpress <c>XtraReport</c>
/// class has ACTUALLY been ported into <c>HCMv3.Reports</c>.
///
/// WHY THIS IS A STATIC AND NOT A DI SERVICE: the real registry (name -&gt; <c>Func&lt;XtraReport&gt;</c>)
/// lives in <c>HCMv3.Web.Host/Reporting/HcmReportFactoryRegistry</c>, because only Web.Host references
/// HCMv3.Reports. The Application layer must nevertheless be able to answer "is this catalog row
/// runnable?" without taking a reference on the reports assembly or on Web.Host. A DI abstraction
/// would have to be registered by Web.Host and would then be unresolvable in every other host
/// (tests, the migrator, background jobs), turning a missing registration into a hard resolve
/// failure. A process-wide set of NAMES is the smallest thing that crosses the layer boundary:
/// Core.Shared already exists precisely for constants shared by every layer.
///
/// Population is one-way and additive: <c>HcmReportFactoryRegistry</c> registers its keys once at
/// Web.Host start-up. Anything not registered simply computes as "not available", which is the
/// correct, fail-closed answer for a host that cannot run reports at all.
/// </summary>
public static class PortedReportCatalog
{
    private static readonly object SyncRoot = new object();

    private static HashSet<string> _names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Report names that have a ported report class in this process.</summary>
    public static IReadOnlyCollection<string> Names
    {
        get
        {
            lock (SyncRoot)
            {
                return _names.ToList();
            }
        }
    }

    /// <summary>Replaces the registered set. Called once, at start-up, by the owning registry.</summary>
    public static void SetPortedReportNames(IEnumerable<string> reportNames)
    {
        var replacement = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (reportNames != null)
        {
            foreach (var name in reportNames)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    replacement.Add(name.Trim());
                }
            }
        }

        lock (SyncRoot)
        {
            _names = replacement;
        }
    }

    /// <summary>
    /// True when <paramref name="reportName"/> can actually be instantiated and rendered. A
    /// <see cref="PrintReport"/> row whose <c>ReportName</c> returns false here is a catalog entry
    /// for a legacy report that has not been ported yet: it is listed, but not runnable.
    /// </summary>
    public static bool IsAvailable(string reportName)
    {
        if (string.IsNullOrWhiteSpace(reportName))
        {
            return false;
        }

        lock (SyncRoot)
        {
            return _names.Contains(reportName.Trim());
        }
    }
}
