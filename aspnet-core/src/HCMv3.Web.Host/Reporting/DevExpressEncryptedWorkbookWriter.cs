using System.Collections.Generic;
using System.Data;
using Abp.Dependency;
using HCMv3.GridReports.Exporting;
using HCMv3.Reports.Exporting;

namespace HCMv3.Web.Reporting
{
    /// <summary>
    /// Hands protected grid exports to the Reports project's DevExpress writer. Lives in the host because the
    /// host is the only project that references both the Application layer and the DevExpress packages.
    /// </summary>
    public class DevExpressEncryptedWorkbookWriter : IEncryptedWorkbookWriter, ITransientDependency
    {
        public byte[] WriteXlsx(DataTable table, ICollection<int> numericColumnIndexes, string numberFormat, string password)
        {
            return EncryptedXlsxWriter.Write(table, numericColumnIndexes, numberFormat, password);
        }
    }
}
