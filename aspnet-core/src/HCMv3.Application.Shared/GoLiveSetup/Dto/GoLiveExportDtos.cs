using System;
using System.Collections.Generic;

namespace HCMv3.GoLiveSetup.Dto;

/// <summary>The Load Employee Data grid as it stands on screen, unsaved edits included, to hand out as Excel.</summary>
public class GoLiveEmployeeSheetInput
{
    public List<GoLiveEmployeeRowDto> Employees { get; set; } = new();
}

/// <summary>The comparison lines as the Lines tab shows them, filters applied, to hand out as Excel.</summary>
public class GoLiveLegacyCompareExportInput
{
    public DateTime? PayMonth { get; set; }

    public List<GoLiveLegacyCompareLineDto> Lines { get; set; } = new();
}
