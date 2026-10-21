using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HCMv3.ProjectManagement.Dto;
using HCMv3.ProjectManagement.Exporting;
using HCMv3.Storage;
using NSubstitute;
using Shouldly;
using Xunit;

using Fakes = HCMv3.Tests.ProjectManagement.ProjectCostEngineFakes;

namespace HCMv3.Tests.ProjectManagement;

/// <summary>
/// Legacy <c>uscProjectCosting.btnExportTimeLog_Click</c>: the log grid as an Excel file named
/// <c>ProjectCostEngineExecutionLog_yyyyMMdd_HHmmss</c>, with legacy's columns.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ProjectCostEngineLogExcelExporter_Tests
{
    [Fact]
    public async Task Should_Write_The_Log_Into_An_Excel_File()
    {
        var tempFiles = Substitute.For<ITempFileCacheManager>();
        byte[] saved = null;
        tempFiles.When(t => t.SetFile(Arg.Any<string>(), Arg.Any<byte[]>()))
            .Do(call => saved = call.ArgAt<byte[]>(1));
        var exporter = new ProjectCostEngineLogExcelExporter(tempFiles) { LocalizationManager = Fakes.LocalizationManager() };

        var file = await exporter.ExportToFile(new List<ProjectCostEngineLogDto>
        {
            new() { Id = 1, EmployeeCode = "E1", EmployeeName = "Ali", Status = 0, Message = "Successfull", InPeriod = new DateTime(2024, 1, 31), RunTimeStamp = new DateTime(2024, 2, 1, 9, 0, 0) },
            new() { Id = 2, EmployeeCode = "E2", EmployeeName = "Sara", Status = 1, Message = "Salary Register not found", InPeriod = new DateTime(2024, 1, 31), RunTimeStamp = new DateTime(2024, 2, 1, 9, 1, 0) }
        });

        file.FileName.ShouldStartWith("ProjectCostEngineExecutionLog_");
        file.FileName.ShouldEndWith(".xlsx");
        saved.ShouldNotBeNull();

        using var workbook = new XLWorkbook(new MemoryStream(saved));
        var sheet = workbook.Worksheets.First();
        var headers = sheet.Row(1).CellsUsed().Select(c => c.GetString()).ToList();
        headers.ShouldBe(new[] { "EmployeeCode", "EmployeeName", "RunTimeStamp", "Status", "Message", "InPeriod" });

        var failedRow = sheet.Row(3).CellsUsed().Select(c => c.GetString()).ToList();
        failedRow.ShouldContain("E2");
        failedRow.ShouldContain("Failed");
        failedRow.ShouldContain("Salary Register not found");
        failedRow.ShouldContain("31/01/2024");
        sheet.Row(2).CellsUsed().Select(c => c.GetString()).ShouldContain("Successfull");
    }
}
