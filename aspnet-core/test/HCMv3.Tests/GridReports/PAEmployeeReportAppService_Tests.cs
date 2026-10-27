using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.PerformanceReport;
using HCMv3.GridReports.PerformanceReport.Dto;
using HCMv3.PerformanceManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the PA Employee grid report (legacy <c>uscRptPAEmployee.ascx</c>, <c>Table1()</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class PAEmployeeReportAppService_Tests : AppTestBase
{
    private readonly IPAEmployeeReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public PAEmployeeReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPAEmployeeReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
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

    private (FormType formType, PACycle cycle) CreateFormTypeAndCycle(string code)
    {
        FormType formType = null;
        PACycle cycle = null;
        UsingDbContext(context =>
        {
            formType = context.Set<FormType>().Add(new FormType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-Form",
                NameAr = code + "-Form-Ar"
            }).Entity;

            cycle = context.Set<PACycle>().Add(new PACycle
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-Cycle",
                NameAr = code + "-Cycle-Ar"
            }).Entity;
        });

        return (formType, cycle);
    }

    private PAEmployee CreatePAEmployee(Employee employee, FormType formType, PACycle cycle)
    {
        PAEmployee paEmployee = null;
        UsingDbContext(context =>
        {
            paEmployee = context.Set<PAEmployee>().Add(new PAEmployee
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                FormTypeId = formType.Id,
                PACycleId = cycle.Id,
                OverallAverageScore = 4.2,
                PeerAverageScore = 3.9,
                EmployeeAverageScore = 4.0
            }).Entity;
        });

        return paEmployee;
    }

    private void CreateReviewer(PAEmployee paEmployee, Employee reviewer, double averageScore = 4.5)
    {
        UsingDbContext(context =>
        {
            context.Set<PAEmployeeReviewer>().Add(new PAEmployeeReviewer
            {
                TenantId = TenantId,
                PAEmployeeId = paEmployee.Id,
                EmployeeId = reviewer.Id,
                AverageScore = averageScore
            });
        });
    }

    private static GetPAEmployeeReportInput Input(string formTypeCode = null, string periodCode = null, string employeeCode = null)
    {
        return new GetPAEmployeeReportInput
        {
            FormTypeCode = formTypeCode,
            PeriodCode = periodCode,
            EmployeeCode = employeeCode,
            MaxResultCount = 100,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Filter_By_FormType_And_Period_Code()
    {
        var (formTypeA, cycleA) = CreateFormTypeAndCycle("PAE-A");
        var (formTypeB, cycleB) = CreateFormTypeAndCycle("PAE-B");
        var empA = CreateEmployee("PAE-EMP-A");
        var empB = CreateEmployee("PAE-EMP-B");

        CreatePAEmployee(empA, formTypeA, cycleA);
        CreatePAEmployee(empB, formTypeB, cycleB);

        var result = await _appService.GetPAEmployeeReport(Input(formTypeCode: "PAE-A", periodCode: "PAE-A"));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("PAE-EMP-A");
        codes.ShouldNotContain("PAE-EMP-B");
    }

    [Fact]
    public async Task Should_Produce_One_Row_Per_Reviewer_Fan_Out()
    {
        var (formType, cycle) = CreateFormTypeAndCycle("PAE-FAN");
        var employee = CreateEmployee("PAE-FAN-EMP");
        var reviewer1 = CreateEmployee("PAE-FAN-R1");
        var reviewer2 = CreateEmployee("PAE-FAN-R2");

        var paEmployee = CreatePAEmployee(employee, formType, cycle);
        CreateReviewer(paEmployee, reviewer1, 4.0);
        CreateReviewer(paEmployee, reviewer2, 4.8);

        var result = await _appService.GetPAEmployeeReport(Input(employeeCode: "PAE-FAN-EMP"));

        result.Items.Count.ShouldBe(2);
        result.Items.All(i => i.EmployeeCode == "PAE-FAN-EMP").ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Produce_One_Blank_Reviewer_Row_When_No_Reviewer_Exists()
    {
        var (formType, cycle) = CreateFormTypeAndCycle("PAE-NOREV");
        var employee = CreateEmployee("PAE-NOREV-EMP");
        CreatePAEmployee(employee, formType, cycle);

        var result = await _appService.GetPAEmployeeReport(Input(employeeCode: "PAE-NOREV-EMP"));

        var row = result.Items.Single();
        row.Person.ShouldBe("");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_With_OrgLevel_Headers()
    {
        var (formType, cycle) = CreateFormTypeAndCycle("PAE-EXP");
        var employee = CreateEmployee("PAE-EXP-EMP");
        CreatePAEmployee(employee, formType, cycle);

        var file = await _appService.GetPAEmployeeReportToExcel(Input(employeeCode: "PAE-EXP-EMP"));

        file.FileName.ShouldBe("PAEmployee.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(1);
        for (var level = 1; level <= 7; level++)
        {
            exported.First().Keys.ShouldContain("Org Level " + level);
        }
    }
}
