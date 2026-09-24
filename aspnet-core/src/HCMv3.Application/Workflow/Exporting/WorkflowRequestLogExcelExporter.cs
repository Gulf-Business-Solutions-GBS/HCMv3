using System.Collections.Generic;
using System.Threading.Tasks;
using HCMv3.DataExporting.Excel.MiniExcel;
using HCMv3.Dto;
using HCMv3.Storage;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow.Exporting;

/// <summary>
/// Writes one request's action trail to a spreadsheet.
///
/// <para>
/// The columns are legacy's, in legacy's order, because people who used the old screen will open this file
/// expecting them. The photo column is the one deliberate omission: it identified a person on screen, and
/// an image embedded in a spreadsheet identifies nobody while making the file large. Code and name do the
/// same job in a form Excel can sort and filter.
/// </para>
/// </summary>
public class WorkflowRequestLogExcelExporter : MiniExcelExcelExporterBase, IWorkflowRequestLogExcelExporter
{
    public WorkflowRequestLogExcelExporter(ITempFileCacheManager tempFileCacheManager)
        : base(tempFileCacheManager)
    {
    }

    public async Task<FileDto> ExportToFile(string requestNumber, List<WorkflowRequestLogEntryDto> entries, bool isArabic)
    {
        var items = new List<Dictionary<string, object>>();

        foreach (var entry in entries)
        {
            items.Add(new Dictionary<string, object>
            {
                { L("Code"), entry.ActionByCode },
                { L("ActionBy"), isArabic && !string.IsNullOrWhiteSpace(entry.ActionByNameAr) ? entry.ActionByNameAr : entry.ActionByNameEn },

                // Date and time apart, and the date as a real date rather than a formatted string, so the
                // column sorts chronologically in Excel instead of alphabetically.
                { L("ActionDate"), entry.ActionDateTime.Date },
                { L("ActionTime"), entry.ActionDateTime.ToString("HH:mm") },

                { L("ActionType"), L("RequestActionType_" + entry.ActionType) },
                { L("WorkflowStep"), entry.WorkflowStep },
                { L("ActionDays"), entry.ActionDays },
                { L("Platform"), L("WorkflowPlatform_" + entry.Platform) },
                { L("Comment"), entry.Comment }
            });
        }

        // Named after the request, so a folder of these is navigable without opening them.
        return await CreateExcelPackageAsync($"RequestLog_{requestNumber}.xlsx", items);
    }
}
