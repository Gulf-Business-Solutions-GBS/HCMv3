using System.Collections.Generic;
using System.Data;

namespace HCMv3.GridReports.Exporting;

/// <summary>
/// Writes a grid report table as a password-protected .xlsx. Implemented in the Web.Host, over DevExpress,
/// so the Application layer keeps no DevExpress reference. Only used when
/// <c>ManageSecuritySettings.EnableReportExportPwd</c> is on.
/// </summary>
public interface IEncryptedWorkbookWriter
{
    byte[] WriteXlsx(DataTable table, ICollection<int> numericColumnIndexes, string numberFormat, string password);
}
