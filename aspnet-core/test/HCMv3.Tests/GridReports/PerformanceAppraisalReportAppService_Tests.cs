using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.PerformanceReport;
using HCMv3.GridReports.PerformanceReport.Dto;
using HCMv3.PerformanceManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Performance Appraisal grid report (legacy <c>uscRptPerformanceAppraisal.ascx</c>,
/// <c>Table1()</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class PerformanceAppraisalReportAppService_Tests : AppTestBase
{
    private readonly IPerformanceAppraisalReportAppService _appService;

    public PerformanceAppraisalReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPerformanceAppraisalReportAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code)
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

    private void CreatePAForm(
        Employee employee,
        string periodName,
        DateTime timelineStart,
        DateTime timelineEnd,
        bool planSubmittedByAppraisee = false,
        bool annualReviewSubmittedByAppraiser = false)
    {
        UsingDbContext(context =>
        {
            var period = context.Set<PAPeriod>().Add(new PAPeriod
            {
                TenantId = TenantId,
                Name = periodName
            }).Entity;

            context.SaveChanges();

            context.Set<PATimeline>().Add(new PATimeline
            {
                TenantId = TenantId,
                PAPeriodId = period.Id,
                ActivityType = ActivityType.Plan,
                StartDate = timelineStart,
                EndDate = timelineEnd
            });

            context.SaveChanges();

            context.Set<PAForm>().Add(new PAForm
            {
                TenantId = TenantId,
                PAPeriodId = period.Id,
                EmployeeId = employee.Id,
                PlanSubmitByAppraisee = planSubmittedByAppraisee,
                ERSubmitByAppraiser = annualReviewSubmittedByAppraiser
            });
        });
    }

    private static GetPerformanceAppraisalReportInput Input(DateTime from, DateTime to, string employeeCode = null)
    {
        return new GetPerformanceAppraisalReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            MaxResultCount = 100,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_Only_Periods_Whose_Timeline_Overlaps_The_Window()
    {
        var inRange = CreateEmployee("PAP-IN");
        var outOfRange = CreateEmployee("PAP-OUT");

        CreatePAForm(inRange, "PAP-P-IN", new DateTime(2024, 3, 1), new DateTime(2024, 6, 30));
        CreatePAForm(outOfRange, "PAP-P-OUT", new DateTime(2023, 1, 1), new DateTime(2023, 6, 30));

        var result = await _appService.GetPerformanceAppraisalReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("PAP-IN");
        codes.ShouldNotContain("PAP-OUT");
    }

    [Fact]
    public async Task Should_Reproduce_The_Legacy_Submission_Status_Booleans()
    {
        var employee = CreateEmployee("PAP-STATUS");
        CreatePAForm(
            employee,
            "PAP-STATUS-PERIOD",
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            planSubmittedByAppraisee: true,
            annualReviewSubmittedByAppraiser: false);

        var result = await _appService.GetPerformanceAppraisalReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "PAP-STATUS"));

        var row = result.Items.Single();
        row.PlanSubmittedByAppraisee.ShouldBeTrue();
        row.AnnualReviewSubmittedByAppraiser.ShouldBeFalse();
    }
}
