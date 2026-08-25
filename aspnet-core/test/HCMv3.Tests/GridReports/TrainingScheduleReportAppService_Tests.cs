using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.TrainingReport;
using HCMv3.GridReports.TrainingReport.Dto;
using HCMv3.Storage;
using HCMv3.TrainingManagement;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class TrainingScheduleReportAppService_Tests : AppTestBase
{
    private readonly ITrainingScheduleReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public TrainingScheduleReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ITrainingScheduleReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private TrainingCourse CreateTrainingCourse(string code)
    {
        TrainingCourse course = null;
        UsingDbContext(context =>
        {
            course = context.Set<TrainingCourse>().Add(new TrainingCourse
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar",
                Active = true
            }).Entity;
        });
        return course;
    }

    private TrainingSchedule CreateSchedule(string code, TrainingCourse course, DateTime start, DateTime end)
    {
        TrainingSchedule schedule = null;
        UsingDbContext(context =>
        {
            schedule = context.Set<TrainingSchedule>().Add(new TrainingSchedule
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar",
                TrainingCourseId = course.Id,
                StartDate = start,
                EndDate = end
            }).Entity;
        });
        return schedule;
    }

    private static GetTrainingScheduleReportInput Input(DateTime from, DateTime to, string trainingCourseCode = null)
    {
        return new GetTrainingScheduleReportInput
        {
            FromDate = from,
            ToDate = to,
            TrainingCourseCode = trainingCourseCode,
            MaxResultCount = 100
        };
    }

    [Fact]
    public async Task Should_Return_Only_Schedules_Overlapping_The_Requested_Window()
    {
        var course = CreateTrainingCourse("TS-CRS");
        CreateSchedule("TS-IN", course, new DateTime(2024, 6, 1), new DateTime(2024, 6, 5));
        CreateSchedule("TS-BEFORE", course, new DateTime(2023, 1, 1), new DateTime(2023, 1, 5));
        CreateSchedule("TS-AFTER", course, new DateTime(2025, 1, 1), new DateTime(2025, 1, 5));

        var result = await _appService.GetTrainingScheduleReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.Code).ToList();
        codes.ShouldContain("TS-IN");
        codes.ShouldNotContain("TS-BEFORE");
        codes.ShouldNotContain("TS-AFTER");
    }

    [Fact]
    public async Task Should_Filter_By_Training_Course_Code()
    {
        var courseA = CreateTrainingCourse("TS-CA");
        var courseB = CreateTrainingCourse("TS-CB");
        CreateSchedule("TS-SA", courseA, new DateTime(2024, 1, 1), new DateTime(2024, 1, 5));
        CreateSchedule("TS-SB", courseB, new DateTime(2024, 1, 1), new DateTime(2024, 1, 5));

        var result = await _appService.GetTrainingScheduleReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "TS-CA"));

        var codes = result.Items.Select(i => i.Code).ToList();
        codes.ShouldContain("TS-SA");
        codes.ShouldNotContain("TS-SB");
    }

    [Fact]
    public async Task Should_Resolve_Every_Export_Header_To_Localized_Text_Not_A_Missing_Key_Placeholder()
    {
        var course = CreateTrainingCourse("TS-HDR-C");
        CreateSchedule("TS-HDR", course, new DateTime(2024, 1, 1), new DateTime(2024, 1, 5));

        var file = await _appService.GetTrainingScheduleReportToExcel(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        foreach (var header in exported.First().Keys)
        {
            header.ShouldNotStartWith("[");
        }
    }
}
