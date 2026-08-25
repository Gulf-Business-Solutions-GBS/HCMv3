using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.SurveyReport;
using HCMv3.GridReports.SurveyReport.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.SurveyManagement;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class SurveyStatusReportAppService_Tests : AppTestBase
{
    private readonly ISurveyStatusReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public SurveyStatusReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ISurveyStatusReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateActiveEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F" + code,
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Full " + code,
                FullNameAr = "عربي " + code,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id,
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    private Survey CreateSurvey(string code, DateTime start, DateTime end)
    {
        Survey survey = null;
        UsingDbContext(context =>
        {
            survey = context.Set<Survey>().Add(new Survey
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar",
                StartDate = start,
                EndDate = end
            }).Entity;
        });
        return survey;
    }

    private void RecordResponse(Employee employee, Survey survey)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeSurvey>().Add(new EmployeeSurvey
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                SurveyId = survey.Id
            });
        });
    }

    /// <summary>
    /// Pins legacy's exact integer-division completion percentage: 1 responder out of 2 active employees
    /// is 50 (1*100/2), not 50.0 -- see the row DTO remarks.
    /// </summary>
    [Fact]
    public async Task Should_Reproduce_The_Legacy_Integer_Division_Complete_Percentage()
    {
        var survey = CreateSurvey("SVS-PCT", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        var responder = CreateActiveEmployee("SVS-R1");
        CreateActiveEmployee("SVS-R2");
        RecordResponse(responder, survey);

        var result = await _appService.GetSurveyStatusReport(new GetSurveyStatusReportInput
        {
            FromDate = new DateTime(2024, 1, 1),
            ToDate = new DateTime(2024, 12, 31),
            SurveyCode = "SVS-PCT",
            MaxResultCount = 100
        });

        result.Items.Single().CompletePercent.ShouldBe(50);
    }

    [Fact]
    public async Task Should_Return_Only_Surveys_Overlapping_The_Requested_Window()
    {
        CreateSurvey("SVS-IN", new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        CreateSurvey("SVS-OUT", new DateTime(2023, 1, 1), new DateTime(2023, 1, 31));

        var result = await _appService.GetSurveyStatusReport(new GetSurveyStatusReportInput
        {
            FromDate = new DateTime(2024, 1, 1),
            ToDate = new DateTime(2024, 12, 31),
            MaxResultCount = 100
        });

        var codes = result.Items.Select(i => i.Code).ToList();
        codes.ShouldContain("SVS-IN");
        codes.ShouldNotContain("SVS-OUT");
    }

    [Fact]
    public async Task Should_Resolve_Every_Export_Header_To_Localized_Text_Not_A_Missing_Key_Placeholder()
    {
        CreateSurvey("SVS-HDR", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var file = await _appService.GetSurveyStatusReportToExcel(new GetSurveyStatusReportInput
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
