using System.Collections.Generic;

namespace HCMv3.GoLiveSetupV2.Dto;

/// <summary>
/// A report as a table: its heading row and its rows, read out of the file that was generated, so what
/// the screen shows and what the file holds cannot drift apart.
/// </summary>
public class GoLiveV2GridDto
{
    public string Title { get; set; }

    public List<string> Columns { get; set; } = new();

    public List<List<string>> Rows { get; set; } = new();

    /// <summary>Set instead of the rows for a fixed-width text file, whose lines are all there is.</summary>
    public string Text { get; set; }
}
