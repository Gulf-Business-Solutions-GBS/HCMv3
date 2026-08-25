using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.Administration;
using HCMv3.GridReports.Administration.Dto;
using HCMv3.Storage;
using HCMv3.SystemSecurity;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class ExecutionLogReportAppService_Tests : AppTestBase
{
    private readonly IExecutionLogReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public ExecutionLogReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IExecutionLogReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private void CreateLog(string userName, DateTime actionDateTime, string screenCaption, int totalRecords = 5)
    {
        UsingDbContext(context =>
        {
            context.Set<ExecutionLog>().Add(new ExecutionLog
            {
                TenantId = TenantId,
                UserName = userName,
                ActionDateTime = actionDateTime,
                Action = ExecutionLogAction.VIEW,
                Module = ExecutionLogModule.GridReport,
                ScreenName = "uscTest",
                ScreenCaption = screenCaption,
                Parameters = "From Date : 01/01/2024",
                ExecutionStatus = ExecutionLogStatus.SUCCESS,
                TotalRecords = totalRecords,
                Successfull = totalRecords,
                Failed = 0
            });
        });
    }

    [Fact]
    public async Task Should_Return_Only_Logs_In_The_Requested_Date_Window()
    {
        CreateLog("EL-USER1", new DateTime(2024, 6, 15), "In Range");
        CreateLog("EL-USER1", new DateTime(2023, 1, 1), "Out Of Range");

        var result = await _appService.GetExecutionLogReport(new GetExecutionLogReportInput
        {
            FromDate = new DateTime(2024, 1, 1),
            ToDate = new DateTime(2024, 12, 31),
            MaxResultCount = 100
        });

        var captions = result.Items.Select(i => i.ScreenCaption).ToList();
        captions.ShouldContain("In Range");
        captions.ShouldNotContain("Out Of Range");
    }

    [Fact]
    public async Task Should_Filter_By_UserName()
    {
        CreateLog("EL-USERA", new DateTime(2024, 6, 1), "By A");
        CreateLog("EL-USERB", new DateTime(2024, 6, 1), "By B");

        var result = await _appService.GetExecutionLogReport(new GetExecutionLogReportInput
        {
            FromDate = new DateTime(2024, 1, 1),
            ToDate = new DateTime(2024, 12, 31),
            LogUserName = "EL-USERA",
            MaxResultCount = 100
        });

        var captions = result.Items.Select(i => i.ScreenCaption).ToList();
        captions.ShouldContain("By A");
        captions.ShouldNotContain("By B");
    }

    [Fact]
    public async Task Should_Resolve_Every_Export_Header_To_Localized_Text_Not_A_Missing_Key_Placeholder()
    {
        CreateLog("EL-HDR", new DateTime(2024, 6, 1), "Header Test");

        var file = await _appService.GetExecutionLogReportToExcel(new GetExecutionLogReportInput
        {
            FromDate = new DateTime(2024, 1, 1),
            ToDate = new DateTime(2024, 12, 31),
            MaxResultCount = 100
        });

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        foreach (var header in exported.First().Keys)
        {
            header.ShouldNotStartWith("[");
        }
    }
}
