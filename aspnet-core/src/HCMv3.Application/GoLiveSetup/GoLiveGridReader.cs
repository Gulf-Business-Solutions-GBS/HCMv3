using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Abp.Dependency;
using ClosedXML.Excel;
using HCMv3.GoLiveSetup.Dto;

namespace HCMv3.GoLiveSetup;

/// <summary>
/// Turns a workbook this application just generated into rows the screen can draw.
///
/// <para>The screen shows what a report holds before anyone saves it. Reading the file back here, rather
/// than sending the bytes for the browser to open, means the table on screen is the file -- the same
/// columns in the same order, with every number and date formatted as the file formats it -- and the
/// browser needs nothing but a table to draw it.</para>
/// </summary>
public class GoLiveGridReader : ITransientDependency
{
    /// <summary>The first sheet: its heading row, then its rows, exactly as written.</summary>
    public GoLiveGridDto Read(byte[] workbook, string title)
    {
        var grid = new GoLiveGridDto { Title = title };

        if (workbook == null || workbook.Length == 0)
        {
            return grid;
        }

        using var stream = new MemoryStream(workbook);
        using var book = new XLWorkbook(stream);

        var sheet = book.Worksheets.FirstOrDefault();
        var used = sheet?.RangeUsed();
        if (used == null)
        {
            return grid;
        }

        var columns = used.ColumnCount();

        foreach (var row in used.Rows())
        {
            var cells = new List<string>(columns);
            for (var column = 1; column <= columns; column++)
            {
                // As the file formats it: a date reads as a date, an amount with its decimals.
                cells.Add(row.Cell(column).GetFormattedString() ?? string.Empty);
            }

            if (grid.Columns.Count == 0)
            {
                grid.Columns = cells;
                continue;
            }

            grid.Rows.Add(cells);
        }

        return grid;
    }

    /// <summary>A fixed-width file has no columns to speak of; its lines are what there is to read.</summary>
    public GoLiveGridDto ReadText(string text, string title) =>
        new()
        {
            Title = title,
            Text = text ?? string.Empty,
        };
}
