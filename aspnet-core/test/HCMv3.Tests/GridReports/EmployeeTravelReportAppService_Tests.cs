using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.TravelReport;
using HCMv3.GridReports.TravelReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.TravelManagement;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Travel grid report (legacy <c>uscRptEmployeeTravel.ascx</c>, <c>Table1()</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeTravelReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeTravelReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeTravelReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeTravelReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
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
                Status = status
            }).Entity;
        });

        return employee;
    }

    private Travel CreateTravel(Employee employee, DateTime start, DateTime end, double totalTravelCost = 500)
    {
        Travel travel = null;
        UsingDbContext(context =>
        {
            var travelType = context.Set<TravelType>().Add(new TravelType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Business",
                NameAr = "عمل"
            }).Entity;

            context.SaveChanges();

            travel = context.Set<Travel>().Add(new Travel
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                EmployeeId = employee.Id,
                TravelTypeId = travelType.Id,
                StartDate = start,
                EndDate = end,
                TotalTravelCost = totalTravelCost
            }).Entity;
        });

        return travel;
    }

    private void CreateTravelFeedback(Employee employee, Travel travel, double claimAmount)
    {
        UsingDbContext(context =>
        {
            context.Set<TravelFeedback>().Add(new TravelFeedback
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                TravelId = travel.Id,
                ClaimDate = travel.StartDate,
                ClaimAmount = claimAmount
            });
        });
    }

    private static GetEmployeeTravelReportInput Input(DateTime from, DateTime to, string employeeCode = null, int maxResultCount = 100)
    {
        return new GetEmployeeTravelReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_Only_Travels_Overlapping_The_Requested_Window()
    {
        var inRange = CreateEmployee("TRV-IN");
        var outOfRange = CreateEmployee("TRV-OUT");

        CreateTravel(inRange, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10));
        CreateTravel(outOfRange, new DateTime(2023, 1, 1), new DateTime(2023, 1, 10));

        var result = await _appService.GetEmployeeTravelReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("TRV-IN");
        codes.ShouldNotContain("TRV-OUT");
    }

    [Fact]
    public async Task Should_Sum_Travel_Feedback_Claim_Amounts_For_The_Travel()
    {
        var employee = CreateEmployee("TRV-CLAIM");
        var travel = CreateTravel(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10));
        CreateTravelFeedback(employee, travel, 100);
        CreateTravelFeedback(employee, travel, 50);

        var result = await _appService.GetEmployeeTravelReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "TRV-CLAIM"));

        result.Items.Single().ClaimAmount.ShouldBe(150);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var first = CreateEmployee("TRV-EXP-A");
        var second = CreateEmployee("TRV-EXP-B");

        CreateTravel(first, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10));
        CreateTravel(second, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10));

        var input = Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "TRV-EXP-A,TRV-EXP-B", maxResultCount: 1);

        var file = await _appService.GetEmployeeTravelReportToExcel(input);

        file.FileName.ShouldBe("EmployeeTravel.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(2);
        for (var level = 1; level <= 7; level++)
        {
            exported.First().Keys.ShouldContain("Org Level " + level);
        }
    }

    [Fact]
    public async Task Should_Show_An_Applicant_Trip_With_The_Applicant_Name_And_Its_Own_Claims()
    {
        // Legacy Table1(): an applicant's trip fills the Applicant column and leaves the employee columns blank.
        var employee = CreateEmployee("TRV-EMP-A");
        var employeeTravel = CreateTravel(employee, new DateTime(2024, 5, 1), new DateTime(2024, 5, 3));
        CreateTravelFeedback(employee, employeeTravel, 40);

        HCMv3.Recruitment.Applicant applicant = null;
        Travel applicantTravel = null;
        UsingDbContext(context =>
        {
            applicant = context.Set<HCMv3.Recruitment.Applicant>().Add(new HCMv3.Recruitment.Applicant
            {
                TenantId = TenantId,
                Code = "APP-T1",
                FirstNameEn = "Nora",
                LastNameEn = "Candidate",
                FirstNameAr = "نورة",
                LastNameAr = "مرشحة",
                BirthDate = new DateTime(1995, 1, 1)
            }).Entity;
            context.SaveChanges();

            applicantTravel = context.Set<Travel>().Add(new Travel
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                ApplicantId = applicant.Id,
                TravelTypeId = employeeTravel.TravelTypeId,
                StartDate = new DateTime(2024, 5, 10),
                EndDate = new DateTime(2024, 5, 12),
                TotalTravelCost = 300
            }).Entity;
            context.SaveChanges();

            context.Set<TravelFeedback>().Add(new TravelFeedback
            {
                TenantId = TenantId,
                ApplicantId = applicant.Id,
                TravelId = applicantTravel.Id,
                ClaimDate = new DateTime(2024, 5, 12),
                ClaimAmount = 25
            });
        });

        var result = await _appService.GetEmployeeTravelReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var applicantRow = result.Items.Single(r => r.Id == applicantTravel.Id);
        applicantRow.Applicant.ShouldContain("Nora");
        applicantRow.Applicant.ShouldContain("Candidate");
        applicantRow.EmployeeCode.ShouldBeNull();
        applicantRow.ClaimAmount.ShouldBe(25);

        var employeeRow = result.Items.Single(r => r.Id == employeeTravel.Id);
        employeeRow.Applicant.ShouldBe("");
        employeeRow.ClaimAmount.ShouldBe(40);
    }
}
