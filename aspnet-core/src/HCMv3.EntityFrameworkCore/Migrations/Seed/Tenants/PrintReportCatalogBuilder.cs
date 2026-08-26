using System;
using System.Linq;
using HCMv3.EntityFrameworkCore;
using HCMv3.ReportManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Migrations.Seed.Tenants;

/// <summary>
/// Seeds the <see cref="PrintReport"/> catalog -- the rows the "Print Reports" launcher lists.
///
/// Only CATALOG ROWS are seeded. Whether a row is actually runnable is computed at request time from
/// <see cref="PortedReportCatalog"/> (the ported report classes in HCMv3.Reports), so seeding a row
/// for a legacy report that has not been ported yet is safe and deliberate: it shows up in the list
/// as "not yet available" rather than disappearing, which is what a migration status screen wants.
///
/// IDEMPOTENT: keyed on (TenantId, ReportName). Existing rows are left completely alone -- a tenant
/// may legitimately have renamed or deactivated a seeded row and re-running the seeder must not undo
/// that.
/// </summary>
public class PrintReportCatalogBuilder
{
    private readonly HCMv3DbContext _context;
    private readonly int _tenantId;

    /// <summary>
    /// (Code, ReportName, NameEn, NameAr, Category). ReportName must match the report class's own
    /// <c>ReportName</c> constant for rows that are ported -- it is what gets passed to /DXXRD and
    /// /DXXRDV as <c>reportUrl</c>.
    /// </summary>
    private static readonly (string Code, string ReportName, string NameEn, string NameAr, ReportCategory Category)[] Catalog =
    {
        // PORTED -- HCMv3.Reports.Payroll.rptPayslipEn.ReportName == "PayslipEn".
        // The literal is repeated here rather than referenced because HCMv3.EntityFrameworkCore does
        // not (and should not) reference the reports assembly.
        ("PR001", "PayslipEn", "Pay Slip English", "وصل الدفع - إنجليزي", ReportCategory.Default),

        // NOT YET PORTED -- catalog rows only, so the launcher can demonstrate the
        // "listed but not available" state. Do NOT add a report class for these here; add the class
        // to HCMv3.Reports and register it in HcmReportFactoryRegistry, and this row lights up.
        ("PR002", "SalaryCertificateEn", "Salary Certificate English", "شهادة راتب - إنجليزي", ReportCategory.Default),
        ("PR003", "SalaryCertificateAr", "Salary Certificate Arabic", "شهادة راتب - عربي", ReportCategory.Default),
        ("PR004", "PayslipAr", "Pay Slip Arabic", "وصل الدفع - عربي", ReportCategory.Default)
    };

    public PrintReportCatalogBuilder(HCMv3DbContext context, int tenantId)
    {
        _context = context;
        _tenantId = tenantId;
    }

    public void Create()
    {
        var existing = _context.PrintReports
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == _tenantId)
            .Select(r => r.ReportName)
            .ToList();

        var added = false;

        foreach (var entry in Catalog)
        {
            if (existing.Any(name => string.Equals(name, entry.ReportName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            _context.PrintReports.Add(new PrintReport
            {
                // SuppressAutoSetTenantId is on during seeding, so TenantId must be set explicitly.
                TenantId = _tenantId,
                Code = entry.Code,
                ReportName = entry.ReportName,
                NameEn = entry.NameEn,
                NameAr = entry.NameAr,
                ReportCategory = entry.Category,
                CustomPercentage = 0m,
                ReportCreatedOn = DateTime.Now,
                IsActive = true
            });

            added = true;
        }

        if (added)
        {
            _context.SaveChanges();
        }
    }
}
