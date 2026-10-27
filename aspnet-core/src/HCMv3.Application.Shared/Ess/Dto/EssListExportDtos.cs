using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Ess.Dto;

public enum EssListExportColumnType
{
    Text = 0,

    /// <summary>The cell is sent as an invariant number ("1234.5").</summary>
    Number = 1,

    /// <summary>The cell is sent as an ISO date ("2026-10-05").</summary>
    Date = 2
}

public class EssListExportColumnDto
{
    [Required]
    [StringLength(200)]
    public string Header { get; set; }

    public EssListExportColumnType Type { get; set; }
}

/// <summary>
/// A self-service request list as the employee has it on screen -- their own rows, filtered and sorted as they
/// left them -- to be handed back as a workbook. The server adds nothing to it and stores nothing from it.
/// </summary>
public class EssListExportInput
{
    /// <summary>The screen's title; it names the sheet and the file.</summary>
    [Required]
    [StringLength(100)]
    public string Title { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(50)]
    public List<EssListExportColumnDto> Columns { get; set; }

    /// <summary>One list of cells per row, in column order.</summary>
    [Required]
    [MaxLength(5000)]
    public List<List<string>> Rows { get; set; }
}

public class ExportMyLeavesInput
{
    /// <summary>
    /// The leaves the list is showing after its filters, in the order it shows them. Left out, every leave is
    /// exported.
    /// </summary>
    public List<int> LeaveIds { get; set; }
}
