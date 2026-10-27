using System.Collections.Generic;
using System.Threading.Tasks;
using HCMv3.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow.Exporting;

public interface IWorkflowRequestLogExcelExporter
{
    /// <param name="requestNumber">Names the file. Not read for anything else.</param>
    /// <param name="entries">Already access-checked by the caller; this writes what it is given.</param>
    /// <param name="isArabic">Which of the two stored names to write into the single name column.</param>
    Task<FileDto> ExportToFile(string requestNumber, List<WorkflowRequestLogEntryDto> entries, bool isArabic);
}
