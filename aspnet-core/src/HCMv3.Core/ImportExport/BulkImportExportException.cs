using Abp.UI;

namespace HCMv3.ImportExport;

/// <summary>
/// Operator-facing failure from the bulk import/export engine (bad entity name, unreadable workbook,
/// unknown column, unparseable cell, missing reference).
///
/// <para>Extends <see cref="UserFriendlyException"/> so the message reaches the Angular client verbatim --
/// legacy threw bare <c>Exception</c>s whose <c>.Message</c> was written straight into
/// <c>BulkUploadLog.Message</c> and shown in the grid, so the text IS the contract here.</para>
/// </summary>
public class BulkImportExportException : UserFriendlyException
{
    public BulkImportExportException(string message)
        : base(message)
    {
    }
}
