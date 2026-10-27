using System.Collections.Generic;

namespace HCMv3.DataExporting;

public interface IExcelColumnSelectionInput
{
    List<string> SelectedColumns { get; set; }
}

