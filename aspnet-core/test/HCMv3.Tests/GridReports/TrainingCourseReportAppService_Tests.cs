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
public class TrainingCourseReportAppService_Tests : AppTestBase
{
    private readonly ITrainingCourseReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public TrainingCourseReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ITrainingCourseReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private TrainingCourse CreateTrainingCourse(string code, bool active = true)
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
                DeliveryMethod = TrainingDeliveryMethod.Classroom,
                Fees = 100,
                Active = active
            }).Entity;
        });
        return course;
    }

    [Fact]
    public async Task Should_Filter_By_Training_Course_Code()
    {
        CreateTrainingCourse("TCR-A");
        CreateTrainingCourse("TCR-B");

        var result = await _appService.GetTrainingCourseReport(new GetTrainingCourseReportInput
        {
            TrainingCourseCode = "TCR-A",
            MaxResultCount = 100
        });

        var codes = result.Items.Select(i => i.Code).ToList();
        codes.ShouldContain("TCR-A");
        codes.ShouldNotContain("TCR-B");
    }

    [Fact]
    public async Task Should_Return_All_Courses_When_No_Filter_Supplied()
    {
        CreateTrainingCourse("TCR-ALL-1");
        CreateTrainingCourse("TCR-ALL-2");

        var result = await _appService.GetTrainingCourseReport(new GetTrainingCourseReportInput { MaxResultCount = 100 });

        var codes = result.Items.Select(i => i.Code).ToList();
        codes.ShouldContain("TCR-ALL-1");
        codes.ShouldContain("TCR-ALL-2");
    }

    [Fact]
    public async Task Should_Resolve_Every_Export_Header_To_Localized_Text_Not_A_Missing_Key_Placeholder()
    {
        CreateTrainingCourse("TCR-HDR");

        var file = await _appService.GetTrainingCourseReportToExcel(new GetTrainingCourseReportInput
        {
            TrainingCourseCode = "TCR-HDR",
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
