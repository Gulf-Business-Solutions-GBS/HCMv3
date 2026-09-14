using System;
using System.Collections.Generic;

namespace HCMv3.GoLiveSetupV2.Dto;

/// <summary>The Load Employee Data grid as it stands on screen, unsaved edits included, to hand out as Excel.</summary>
public class GoLiveV2EmployeeSheetInput
{
    public List<GoLiveV2EmployeeRowDto> Employees { get; set; } = new();
}

/// <summary>The comparison lines as the Lines tab shows them, filters applied, to hand out as Excel.</summary>
public class GoLiveV2LegacyCompareExportInput
{
    public DateTime? PayMonth { get; set; }

    public List<GoLiveV2LegacyCompareLineDto> Lines { get; set; } = new();
}
