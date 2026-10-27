using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <summary>
    /// Repairs leave and gratuity quotas the time engine created without a deduction window.
    ///
    /// <para>
    /// Legacy's time engine set the window on every quota it created (<c>Program.cs</c> 596-597, 696-697,
    /// 1124-1125): deduction starts on the entitlement date -- plus the tenant's "leave deduction start
    /// month" for annual leave, type code "1" -- and never ends. The port left both dates unset, so the quota
    /// accrued a balance that no leave could be booked against: applying failed with "No leave quota
    /// covering these dates". This writes the dates legacy would have written.
    /// </para>
    ///
    /// <para>
    /// Only rows whose window was never set are touched (a date before 1900 is the unset value). A quota HR
    /// entered or corrected by hand, or one imported with its dates, is left exactly as it is.
    /// </para>
    /// </summary>
    public partial class Backfilled_Quota_Deduction_Window : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE q
SET q.DeductionFrom = CASE
        WHEN t.Code = '1'
            THEN DATEADD(month, ISNULL(TRY_CAST(c.KeyValue AS int), 0), q.EntitlementFrom)
        ELSE q.EntitlementFrom
    END,
    q.DeductionTo = '9999-12-31'
FROM EmployeeAbsenceQuotas q
INNER JOIN AbsenceTypes t ON t.Id = q.AbsenceTypeId
OUTER APPLY (
    SELECT TOP 1 sc.KeyValue
    FROM SysTenantConfigs sc
    WHERE sc.TenantId = q.TenantId
      AND sc.[Key] = 'LEAVE_DEDUCTION_START_MONTH'
      AND sc.IsDeleted = 0
      AND sc.StartDate <= GETDATE()
    ORDER BY sc.StartDate DESC
) c
WHERE q.DeductionTo < '1900-01-01'
  AND q.EntitlementFrom >= '1900-01-01';
");

            migrationBuilder.Sql(@"
UPDATE EmployeeGratuityQuotas
SET DeductionFrom = EntitlementFrom,
    DeductionTo = '9999-12-31'
WHERE DeductionTo < '1900-01-01'
  AND EntitlementFrom >= '1900-01-01';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo: the rows had no deduction window before, and which ones those were is not
            // recorded. Clearing dates again would also clear any that HR has since corrected.
        }
    }
}
