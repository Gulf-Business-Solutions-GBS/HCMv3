using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.DataExporting.Excel.MiniExcel;
using HCMv3.Dto;
using HCMv3.Storage;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement.Exporting;

public interface ITimeEngineRunLogExcelExporter
{
    /// <param name="days">The run's log rows, already filtered by the caller.</param>
    Task<FileDto> ExportToFile(List<TimeEngineRunDayDto> days);
}

/// <summary>
/// Legacy <c>uscTimeEngine.btnExportTimeLog_Click</c> (<c>uscTimeEngine.ascx.cs</c> lines 974-985): the time log grid
/// written to Excel, named <c>TimeEngineExecutionLog_yyyyMMdd_HHmmss</c>. Columns are legacy's grid columns, in its
/// order: code, employee, run date and time, status, message, day date.
/// </summary>
public class TimeEngineRunLogExcelExporter : MiniExcelExcelExporterBase, ITimeEngineRunLogExcelExporter
{
    public TimeEngineRunLogExcelExporter(ITempFileCacheManager tempFileCacheManager)
        : base(tempFileCacheManager)
    {
    }

    public async Task<FileDto> ExportToFile(List<TimeEngineRunDayDto> days)
    {
        var items = new List<Dictionary<string, object>>();

        foreach (var day in days ?? new List<TimeEngineRunDayDto>())
        {
            items.Add(new Dictionary<string, object>
            {
                { L("EmployeeCode"), day.EmployeeCode },
                { L("EmployeeName"), day.EmployeeName },

                // Real date-times, so the columns sort chronologically in Excel.
                { L("PayrollRunRunTime"), day.RunTimeStamp },
                { L("Status"), day.Succeeded ? L("Successfull") : L("Failed") },
                { L("Message"), day.Message },
                { L("DayDate"), day.DayDate }
            });
        }

        return await CreateExcelPackageAsync(
            "TimeEngineExecutionLog_" + Clock.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx",
            items);
    }
}
