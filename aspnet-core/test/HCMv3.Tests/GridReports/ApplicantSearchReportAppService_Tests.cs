using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Recruitment;
using HCMv3.GridReports.Recruitment.Dto;
using HCMv3.Recruitment;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class ApplicantSearchReportAppService_Tests : AppTestBase
{
    private readonly IApplicantSearchReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public ApplicantSearchReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IApplicantSearchReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Applicant CreateApplicant(string code, int birthYear, Gender gender = Gender.Male, string firstName = "First", string lastName = "Last")
    {
        Applicant applicant = null;
        UsingDbContext(context =>
        {
            var nationality = context.Set<Nationality>().Add(new Nationality
            { TenantId = TenantId, Code = Guid.NewGuid().ToString("N").Substring(0, 8), NameEn = "N", NameAr = "N" }).Entity;
            var maritalStatus = context.Set<MaritalStatus>().Add(new MaritalStatus
            { TenantId = TenantId, Code = Guid.NewGuid().ToString("N").Substring(0, 8), NameEn = "M", NameAr = "M" }).Entity;
            context.SaveChanges();

            applicant = context.Set<Applicant>().Add(new Applicant
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = firstName,
                FirstNameAr = "ف",
                LastNameEn = lastName,
                LastNameAr = "ل",
                NationalityId = nationality.Id,
                MaritalStatusId = maritalStatus.Id,
                Gender = gender,
                BirthDate = new DateTime(birthYear, 1, 1),
                JoiningDate = new DateTime(2024, 1, 1),
                ProbationEndDate = new DateTime(2024, 4, 1),
                CivilID = 123456789,
                Mobile = 99999999
            }).Entity;
        });

        return applicant;
    }

    [Fact]
    public async Task Should_Filter_By_Gender()
    {
        CreateApplicant("APP-M", 1990, Gender.Male);
        CreateApplicant("APP-F", 1990, Gender.Female);

        var result = await _appService.GetApplicantSearchReport(new GetApplicantSearchReportInput { Gender = Gender.Female });

        var codes = result.Items.Select(i => i.Code).ToList();
        codes.ShouldContain("APP-F");
        codes.ShouldNotContain("APP-M");
    }

    [Fact]
    public async Task Should_Reproduce_The_Legacy_Birthday_Naive_Age_Calculation()
    {
        var currentYear = Clock.Now.Year;
        CreateApplicant("APP-AGE", currentYear - 30);

        var result = await _appService.GetApplicantSearchReport(new GetApplicantSearchReportInput { FirstName = "First" });

        result.Items.Single(i => i.Code == "APP-AGE").Age.ShouldBe(30);
    }

    [Fact]
    public async Task Should_Filter_By_Age_Range_When_Both_Bounds_Are_Supplied()
    {
        var currentYear = Clock.Now.Year;
        CreateApplicant("APP-YOUNG", currentYear - 20, firstName: "Young");
        CreateApplicant("APP-OLD", currentYear - 50, firstName: "Old");

        var result = await _appService.GetApplicantSearchReport(new GetApplicantSearchReportInput { AgeFrom = 25, AgeTo = 60 });

        var codes = result.Items.Select(i => i.Code).ToList();
        codes.ShouldContain("APP-OLD");
        codes.ShouldNotContain("APP-YOUNG");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        CreateApplicant("APP-EXP-A", 1990, firstName: "ExportA");
        CreateApplicant("APP-EXP-B", 1990, firstName: "ExportB");

        var input = new GetApplicantSearchReportInput { MaxResultCount = 1 };

        var file = await _appService.GetApplicantSearchReportToExcel(input);

        file.FileName.ShouldBe("ApplicantSearch.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBeGreaterThanOrEqualTo(2);
        exported.First().Keys.ShouldContain("Age");
    }
}
