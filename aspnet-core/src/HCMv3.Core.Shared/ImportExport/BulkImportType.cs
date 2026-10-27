namespace HCMv3.ImportExport;

/// <summary>
/// Import mode selected on the bulk import/export screen (ported from legacy XAF
/// <c>HCM10.Web.UserControls.ImportExport.uscBulkImportExport.BindImportType()</c>, which bound the two
/// literal combo items <c>("Incremental","I")</c> and <c>("Purge","P")</c>).
/// </summary>
public enum BulkImportType
{
    /// <summary>Legacy value <c>"I"</c>: append the workbook rows to whatever is already there.</summary>
    Incremental = 0,

    /// <summary>
    /// Legacy value <c>"P"</c>: delete every existing row of the target entity first, then import.
    ///
    /// <para><b>DEVIATION.</b> Legacy executed
    /// <c>DELETE FROM [X]; DBCC CHECKIDENT ([X], RESEED, 0)</c> as raw SQL through the XPO session. This port
    /// deletes through <c>IRepository&lt;T&gt;</c> instead, which means (a) the delete is tenant-scoped
    /// (legacy was single-tenant and wiped the whole table), (b) soft-deleted entities are soft-deleted
    /// rather than physically removed, and (c) the identity seed is NOT reset -- resetting it is a DDL
    /// operation that cannot be issued from the Application/Core layers in this architecture.</para>
    /// </summary>
    Purge = 1
}
