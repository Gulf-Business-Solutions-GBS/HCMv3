using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Sponsor grid-report port (legacy <c>uscRptEmployeeSponsor.ascx</c>,
/// <c>Table1()</c>). This report is AS-ON-DATE: the sponsor block is the record current on the From date
/// and there is no To date at all.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeSponsorReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeSponsorReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeSponsorReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeSponsorReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string NewCode() => Guid.NewGuid().ToString("N").Substring(0, 8);

    private Employee CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            var group = context.Set<NationalityGroup>().Add(new NationalityGroup
            {
                TenantId = TenantId,
                Code = NewCode(),
                NameEn = "Group-En",
                NameAr = "Group-Ar"
            }).Entity;

            context.SaveChanges();

            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = NewCode(),
                NameEn = "Nat-En",
                NameAr = "Nat-Ar",
                NationalityGroupId = group.Id
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

    /// <summary>Builds the whole Sponsor -&gt; GovtJobTitle -&gt; Currency chain the sponsor block walks.</summary>
    private void CreateSponsorRecord(Employee employee, DateTime start, DateTime end, string sponsorName)
    {
        UsingDbContext(context =>
        {
            var nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = NewCode(),
                NameEn = "SponsorNat",
                NameAr = "SponsorNat"
            }).Entity;

            var businessType = context.Set<BusinessType>().Add(new BusinessType
            {
                TenantId = TenantId,
                Code = NewCode(),
                NameEn = "BT",
                NameAr = "BT"
            }).Entity;

            var governorate = context.Set<Governorate>().Add(new Governorate
            {
                TenantId = TenantId,
                Code = NewCode(),
                NameEn = "Gov",
                NameAr = "Gov"
            }).Entity;

            var workVisaType = context.Set<WorkVisaType>().Add(new WorkVisaType
            {
                TenantId = TenantId,
                Code = NewCode(),
                NameEn = "WV",
                NameAr = "WV"
            }).Entity;

            var govtJobCategory = context.Set<GovtJobCategory>().Add(new GovtJobCategory
            {
                TenantId = TenantId,
                Code = NewCode(),
                NameEn = "Cat-En",
                NameAr = "Cat-Ar"
            }).Entity;

            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = NewCode().Substring(0, 3),
                NameEn = "Cur-En",
                NameAr = "Cur-Ar"
            }).Entity;

            var organizationUnit = context.OrganizationUnits.Add(new OrganizationUnit(TenantId, "Sponsor OU")
            {
                Code = NewCode()
            }).Entity;

            context.SaveChanges();

            var govtJobTitle = context.Set<GovtJobTitle>().Add(new GovtJobTitle
            {
                TenantId = TenantId,
                Code = NewCode(),
                NameEn = "Title-En",
                NameAr = "Title-Ar",
                GovtJobCategoryId = govtJobCategory.Id
            }).Entity;

            var sponsor = context.Set<Sponsor>().Add(new Sponsor
            {
                TenantId = TenantId,
                Code = NewCode(),
                NameEn = sponsorName,
                NameAr = sponsorName + "-Ar",
                NationalityId = nationality.Id,
                BusinessTypeId = businessType.Id,
                GovernorateId = governorate.Id,
                OrganizationUnitId = organizationUnit.Id
            }).Entity;

            context.SaveChanges();

            context.Set<EmployeeSponsor>().Add(new EmployeeSponsor
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                SponsorId = sponsor.Id,
                GovtJobTitleId = govtJobTitle.Id,
                WorkVisaTypeId = workVisaType.Id,
                SalaryCurrencyId = currency.Id,
                EmpSponsorRelation = "Direct",
                ContractSalary = 1500,
                ResidencyType = ResidencyType.NonCompany,
                StartDate = start,
                EndDate = end
            });
        });
    }

    private static GetEmployeeSponsorReportInput Input(
        DateTime asOn,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeSponsorReportInput
        {
            FromDate = asOn,
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Project_The_Sponsor_Record_Current_On_The_As_On_Date()
    {
        var employee = CreateEmployee("SPON-1");
        CreateSponsorRecord(employee, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "Sponsor A");

        var result = await _appService.GetEmployeeSponsorReport(
            Input(new DateTime(2024, 6, 1), employeeCode: "SPON-1"));

        var row = result.Items.Single();
        row.SponsorStatus.ShouldBe("Active");
        row.SponsorName.ShouldBe("Sponsor A");
        row.GovtJobTitleEn.ShouldBe("Title-En");
        row.GovtJobTitleAr.ShouldBe("Title-Ar");
        row.GovtJobTitleCategoryName.ShouldBe("Cat-En");
        row.ContractSalary.ShouldBe(1500);
        row.SalaryCurrencyName.ShouldBe("Cur-En");
        row.EmployeeSponsorRelation.ShouldBe("Direct");
        row.NationalityName.ShouldBe("Nat-En");
        row.NationalityGroupName.ShouldBe("Group-En");
        row.ResidencyType.ShouldBe(ResidencyType.NonCompany);
        row.StartDate.ShouldBe(new DateTime(2024, 1, 1));
        row.EndDate.ShouldBe(new DateTime(2024, 12, 31));
    }

    [Fact]
    public async Task Should_Include_Sponsor_Records_Touching_The_As_On_Date_Boundary()
    {
        var startsOnAsOn = CreateEmployee("SPONEDGE-START");
        var endsOnAsOn = CreateEmployee("SPONEDGE-END");

        // Legacy predicate is StartDate <= dtFrom && EndDate >= dtFrom -- both ends inclusive.
        CreateSponsorRecord(startsOnAsOn, new DateTime(2024, 6, 1), new DateTime(2025, 6, 1), "Edge Start");
        CreateSponsorRecord(endsOnAsOn, new DateTime(2023, 6, 1), new DateTime(2024, 6, 1), "Edge End");

        var result = await _appService.GetEmployeeSponsorReport(
            Input(new DateTime(2024, 6, 1), employeeCode: "SPONEDGE-START,SPONEDGE-END"));

        result.Items.Count.ShouldBe(2);
        result.Items.ShouldAllBe(r => r.SponsorStatus == "Active");
    }

    [Fact]
    public async Task Should_Report_Expired_When_No_Sponsor_Record_Is_Current_On_The_As_On_Date()
    {
        var employee = CreateEmployee("SPONEXP-1");
        CreateSponsorRecord(employee, new DateTime(2022, 1, 1), new DateTime(2022, 12, 31), "Old Sponsor");

        var result = await _appService.GetEmployeeSponsorReport(
            Input(new DateTime(2024, 6, 1), employeeCode: "SPONEXP-1"));

        var row = result.Items.Single();
        row.SponsorStatus.ShouldBe("Expired");
        row.SponsorName.ShouldBeNull();
        row.StartDate.ShouldBeNull();
        row.EndDate.ShouldBeNull();
        row.ContractSalary.ShouldBeNull();
        row.ResidencyType.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status_And_Codes()
    {
        var active = CreateEmployee("SPONST-ACTIVE");
        CreateEmployee("SPONST-INACTIVE", EmployeeStatus.Inactive);

        var byStatus = await _appService.GetEmployeeSponsorReport(
            Input(new DateTime(2024, 6, 1),
                employeeCode: "SPONST-ACTIVE,SPONST-INACTIVE",
                status: EmployeeStatus.Inactive));

        byStatus.Items.Select(i => i.EmployeeCode).ShouldBe(new List<string> { "SPONST-INACTIVE" });

        var byCode = await _appService.GetEmployeeSponsorReport(
            Input(new DateTime(2024, 6, 1), employeeCode: active.Code));

        byCode.Items.Select(i => i.EmployeeCode).ShouldBe(new List<string> { "SPONST-ACTIVE" });
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        CreateEmployee("SPONEXPORT-A");
        CreateEmployee("SPONEXPORT-B");
        CreateEmployee("SPONEXPORT-C");
        CreateEmployee("SPONEXPORT-OUT");

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var input = Input(new DateTime(2024, 6, 1),
            employeeCode: "SPONEXPORT-A,SPONEXPORT-B,SPONEXPORT-C", maxResultCount: 1);

        var file = await _appService.GetEmployeeSponsorReportToExcel(input);

        file.FileName.ShouldBe("EmployeeSponsor.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c)
            .ShouldBe(new List<string> { "SPONEXPORT-A", "SPONEXPORT-B", "SPONEXPORT-C" });
    }
}
