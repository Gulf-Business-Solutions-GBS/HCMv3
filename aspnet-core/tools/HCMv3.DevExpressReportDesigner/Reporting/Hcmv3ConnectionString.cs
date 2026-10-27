using System;

namespace HCMv3.DevExpressReportDesigner.Reporting;

/// <summary>
/// The one connection string this whole tool ever uses: the live HCMv3 SQL Server database.
///
/// Mirrors <c>HCMv3.MrtRender</c>'s convention (see
/// aspnet-core/tools/HCMv3.ReportMigrator/HCMv3.MrtRender/Program.cs) -- read from the
/// <c>ConnectionStrings__Default</c> environment variable if present, otherwise fall back to the
/// default local dev SQL Server instance. appsettings.json is deliberately never involved.
/// </summary>
internal static class Hcmv3ConnectionString
{
    public static string Value =>
        Environment.GetEnvironmentVariable("ConnectionStrings__Default")
        ?? "Server=localhost\\SQLEXPRESS;Database=HCMv3;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true";
}
