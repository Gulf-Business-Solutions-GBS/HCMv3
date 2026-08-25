using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Recruitment;
using HCMv3.GridReports.Recruitment.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.Recruitment;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Legacy's <c>Table1()</c> for this screen is byte-for-byte identical to
/// <c>uscRptVacancyStatus.ascx.cs</c>'s (see <see cref="VacancyStatusReportRowDto"/> remarks), so this test
/// class pins only that the SEPARATE app service/permission/route wiring works -- the query logic itself is
/// exhaustively covered by <c>VacancyStatusReportAppService_Tests</c> against the same shared
/// <c>VacancyStatusReportQueryHelper</c>.
/// </summary>
// ReSharper disable once InconsistentNaming
public class CandidateOnbInitReportAppService_Tests : AppTestBase
{
    private readonly ICandidateOnbInitReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public CandidateOnbInitReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ICandidateOnbInitReportAppService>();
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
            { TenantId = TenantId, ApplicantId = applicant.Id }).Entity;
            context.SaveChanges();

            context.Set<ApplicationStatus>().Add(new ApplicationStatus
            { TenantId = TenantId, VacancyId = vacancy.Id, ApplicantCVId = applicantCv.Id, AppStatus = status });
        });
    }

    [Fact]
    public async Task Should_Return_Vacancy_With_Per_Status_Counts()
    {
        var vacancy = CreateVacancy("ONB-1");
        AddApplication(vacancy, AppStatus.Applied);
        AddApplication(vacancy, AppStatus.Joined);

        var result = await _appService.GetCandidateOnbInitReport(new GetVacancyStatusReportInput { VacancyId = vacancy.Id });

        var row = result.Items.Single();
        row.StatusCounts.Single(c => c.Status == AppStatus.Applied).Count.ShouldBe(1);
        row.StatusCounts.Single(c => c.Status == AppStatus.Joined).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Vacancies()
    {
        var vacancy = CreateVacancy("ONB-EXP");
        AddApplication(vacancy, AppStatus.Applied);

        var file = await _appService.GetCandidateOnbInitReportToExcel(new GetVacancyStatusReportInput { VacancyId = vacancy.Id });

        file.FileName.ShouldBe("CandidateOnbInit.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
    }
}
