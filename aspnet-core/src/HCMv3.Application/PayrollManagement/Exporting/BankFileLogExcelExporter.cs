using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.DataExporting.Excel.MiniExcel;
using HCMv3.Dto;
using HCMv3.PayrollManagement.Dto;
using HCMv3.Storage;

namespace HCMv3.PayrollManagement.Exporting;

public interface IBankFileLogExcelExporter
{
    Task<FileDto> ExportToFile(List<BankFileLogDto> logs);
}

/// <summary>
/// Legacy <c>uscBankFile.btnExportTimeLog_Click</c> (<c>uscBankFile.ascx.cs:3447</c>): the log grid as shown,
/// named <c>BankFileExecutionLog_yyyyMMdd_HHmmss</c>. Columns are the grid's visible ones (run date &amp; time,
/// user, status, parameters) plus the stored file's name, which this port offers for download.
/// </summary>
public class BankFileLogExcelExporter : MiniExcelExcelExporterBase, IBankFileLogExcelExporter
{
    public BankFileLogExcelExporter(ITempFileCacheManager tempFileCacheManager)
        : base(tempFileCacheManager)
    {
    }

    public async Task<FileDto> ExportToFile(List<BankFileLogDto> logs)
    {
        var items = new List<Dictionary<string, object>>();

        foreach (var log in logs ?? new List<BankFileLogDto>())
        {
            items.Add(new Dictionary<string, object>
            {
                // A real date-time, so the column sorts chronologically in Excel.
                { L("RunDateAndTime"), log.ActionDateTime },
                { L("User"), log.UserName },
                { L("Status"), log.Succeeded ? L("Successfull") : L("Failed") },
                { L("Parameters"), log.Parameters },
                { L("File"), log.FileName }
            });
        }

        return await CreateExcelPackageAsync(
            "BankFileExecutionLog_" + Clock.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx",
            items);
    }
}
