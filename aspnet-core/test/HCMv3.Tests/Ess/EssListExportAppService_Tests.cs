using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

// ReSharper disable once InconsistentNaming
public class EssListExportAppService_Tests : AppTestBase
{
    private readonly IEssListExportAppService _service;

    public EssListExportAppService_Tests()
    {
        _service = Resolve<IEssListExportAppService>();
    }

    [Fact]
    public async Task Should_Hand_Back_The_List_As_A_Workbook_Named_After_The_Screen()
    {
        var file = await _service.ExportToExcel(new EssListExportInput
        {
            Title = "Exit Permit",
            Columns = new List<EssListExportColumnDto>
            {
                new() { Header = "Exit Permit Number" },
                new() { Header = "Exit Date", Type = EssListExportColumnType.Date }
            },
            Rows = new List<List<string>> { new() { "EP-1", "2026-10-05" }, new() { "EP-2", "2026-11-01" } }
        });

        file.FileName.ShouldBe("Exit Permit.xlsx");

        var bytes = Resolve<ITempFileCacheManager>().GetFile(file.FileToken);
        using var book = new XLWorkbook(new MemoryStream(bytes));
        var sheet = book.Worksheets.Single();

        // Title, blank, headings, two rows.
        sheet.LastRowUsed().RowNumber().ShouldBe(5);
        sheet.Cell(4, 1).GetString().ShouldBe("EP-1");
    }

    [Fact]
    public async Task Should_Name_The_File_Safely_When_The_Title_Has_Characters_A_File_Cannot()
    {
        var file = await _service.ExportToExcel(new EssListExportInput
        {
            Title = "Loans / Advances",
            Columns = new List<EssListExportColumnDto> { new() { Header = "Loan Type" } },
            Rows = new List<List<string>>()
        });

        file.FileName.ShouldBe("Loans  Advances.xlsx");
    }
}
