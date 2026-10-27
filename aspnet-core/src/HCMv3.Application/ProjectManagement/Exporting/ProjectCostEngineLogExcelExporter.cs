using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.DataExporting.Excel.MiniExcel;
using HCMv3.Dto;
using HCMv3.ProjectManagement.Dto;
using HCMv3.Storage;

namespace HCMv3.ProjectManagement.Exporting;

public interface IProjectCostEngineLogExcelExporter
{
    /// <param name="logs">The log grid's rows, already filtered by the caller.</param>
    Task<FileDto> ExportToFile(List<ProjectCostEngineLogDto> logs);
}

/// <summary>
/// Legacy <c>uscProjectCosting.btnExportTimeLog_Click</c> (<c>uscProjectCosting.ascx.cs</c> lines 2340-2351): the log
/// grid written to Excel, named <c>ProjectCostEngineExecutionLog_yyyyMMdd_HHmmss</c>, with legacy's columns (code,
/// employee, run time, status, message, in period). Same Zero MiniExcel pattern as
/// <c>PayrollRunEmployeesExcelExporter</c>.
/// </summary>
public class ProjectCostEngineLogExcelExporter : MiniExcelExcelExporterBase, IProjectCostEngineLogExcelExporter
{
    public ProjectCostEngineLogExcelExporter(ITempFileCacheManager tempFileCacheManager)
        : base(tempFileCacheManager)
    {
    }

    public async Task<FileDto> ExportToFile(List<ProjectCostEngineLogDto> logs)
    {
        var items = new List<Dictionary<string, object>>();

        foreach (var log in logs ?? new List<ProjectCostEngineLogDto>())
        {
            items.Add(new Dictionary<string, object>
            {
                { L("EmployeeCode"), log.EmployeeCode },
                { L("EmployeeName"), log.EmployeeName },

                // A real date-time, so the column sorts chronologically in Excel.
                { L("RunTimeStamp"), log.RunTimeStamp },
                { L("Status"), log.Status == 0 ? L("Successfull") : L("Failed") },
                { L("Message"), log.Message },

                // The period, as the screen shows it.
                { L("InPeriod"), log.InPeriod.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) }
            });
        }

        return await CreateExcelPackageAsync(
            "ProjectCostEngineExecutionLog_" + Clock.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".xlsx",
            items);
    }
}
