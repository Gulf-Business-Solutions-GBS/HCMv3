using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Recruitment;
using HCMv3.GridReports.Recruitment.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.Recruitment;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class VacancyStatusReportAppService_Tests : AppTestBase
{
    private readonly IVacancyStatusReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public VacancyStatusReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IVacancyStatusReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Vacancy CreateVacancy(string jobTitleCode)
    {
        Vacancy vacancy = null;
        UsingDbContext(context =>
        {
            var jobTitle = context.Set<JobTitle>().Add(new JobTitle
            { TenantId = TenantId, Code = jobTitleCode, NameEn = "Title-" + jobTitleCode, NameAr = "Title-" + jobTitleCode }).Entity;
            context.SaveChanges();

            vacancy = context.Set<Vacancy>().Add(new Vacancy
            {
                TenantId = TenantId,
                Code = jobTitleCode,
                RequestionDate = new DateTime(2024, 1, 1),
                JobTitleId = jobTitle.Id,
                PublishOn = new DateTime(2024, 1, 5),
                ClosingDate = new DateTime(2024, 2, 5),
                VacancyTitle = "Vacancy-" + jobTitleCode,
                TotalVacancy = 1
            }).Entity;
        });

        return vacancy;
    }

    private void AddApplication(Vacancy vacancy, AppStatus status)
    {
        UsingDbContext(context =>
        {
            var applicant = context.Set<Applicant>().Add(new Applicant
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                FirstNameEn = "F",
                FirstNameAr = "ف",
                LastNameEn = "L",
                LastNameAr = "ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2024, 1, 1),
                ProbationEndDate = new DateTime(2024, 4, 1),
                CivilID = 123456789
            }).Entity;
            context.SaveChanges();

            var applicantCv = context.Set<ApplicantCV>().Add(new ApplicantCV
            {
                TenantId = TenantId,
                ApplicantId = applicant.Id
            }).Entity;
            context.SaveChanges();

            context.Set<ApplicationStatus>().Add(new ApplicationStatus
            {
                TenantId = TenantId,
                VacancyId = vacancy.Id,
                ApplicantCVId = applicantCv.Id,
                AppStatus = status
            });
        });
    }

    [Fact]
    public async Task Should_Return_One_Row_Per_Vacancy_Regardless_Of_Application_Count()
    {
        var vacancy = CreateVacancy("VAC-1");
        AddApplication(vacancy, AppStatus.Applied);
        AddApplication(vacancy, AppStatus.Shortlisted);
        AddApplication(vacancy, AppStatus.Applied);

        var result = await _appService.GetVacancyStatusReport(new GetVacancyStatusReportInput());

        result.Items.Count(i => i.VacancyId == vacancy.Id).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Reproduce_The_Legacy_Per_Status_Application_Counts()
    {
        var vacancy = CreateVacancy("VAC-2");
        AddApplication(vacancy, AppStatus.Applied);
        AddApplication(vacancy, AppStatus.Applied);
        AddApplication(vacancy, AppStatus.Hired);

        var result = await _appService.GetVacancyStatusReport(new GetVacancyStatusReportInput { VacancyId = vacancy.Id });

        var row = result.Items.Single();
        row.StatusCounts.Single(c => c.Status == AppStatus.Applied).Count.ShouldBe(2);
        row.StatusCounts.Single(c => c.Status == AppStatus.Hired).Count.ShouldBe(1);
        row.StatusCounts.Single(c => c.Status == AppStatus.Rejected).Count.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Filter_By_Vacancy_Status()
    {
        var vacancy1 = CreateVacancy("VAC-3");
        var vacancy2 = CreateVacancy("VAC-4");
        AddApplication(vacancy1, AppStatus.Hired);
        AddApplication(vacancy2, AppStatus.Applied);

        var result = await _appService.GetVacancyStatusReport(new GetVacancyStatusReportInput { VacancyStatus = AppStatus.Hired });

        var vacancyIds = result.Items.Select(i => i.VacancyId).ToList();
        vacancyIds.ShouldContain(vacancy1.Id);
        vacancyIds.ShouldNotContain(vacancy2.Id);
    }

    [Fact]
    public async Task Should_Export_With_One_Column_Header_Per_AppStatus_Value()
    {
        var vacancy = CreateVacancy("VAC-EXP");
        AddApplication(vacancy, AppStatus.Applied);

        var file = await _appService.GetVacancyStatusReportToExcel(new GetVacancyStatusReportInput { VacancyId = vacancy.Id });

        file.FileName.ShouldBe("VacancyStatus.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(1);
        exported.First().Keys.ShouldContain(nameof(AppStatus.Applied));
        exported.First().Keys.ShouldContain(nameof(AppStatus.Hired));
    }
}
