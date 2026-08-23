using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the reference grid-report port (legacy <c>uscRptEmployeeContract.ascx</c>). Each test
/// pins one legacy filter/projection semantic so the remaining ~139 reports have a template for what
/// "faithful" means.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeContractReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeContractReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeContractReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeContractReportAppService>();
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

    private ContractType CreateContractType(string code)
    {
        ContractType contractType = null;
        UsingDbContext(context =>
        {
            contractType = context.Set<ContractType>().Add(new ContractType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar"
            }).Entity;
        });

        return contractType;
    }

    private EmployeeContract CreateContract(Employee employee, ContractType contractType, DateTime start, DateTime end)
    {
        EmployeeContract contract = null;
        UsingDbContext(context =>
        {
            contract = context.Set<EmployeeContract>().Add(new EmployeeContract
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                ContractTypeId = contractType.Id,
                StartDate = start,
                EndDate = end
            }).Entity;
        });

        return contract;
    }

    /// <summary>
    /// Builds the OrgAssignment -> PayrollArea -> Country chain the legacy "Country" column walks.
    /// </summary>
    private void CreateOrgAssignmentWithCountry(Employee employee, DateTime start, DateTime end, string countryName)
    {
        UsingDbContext(context =>
        {
            var country = context.Set<Country>().Add(new Country
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = countryName,
                NameAr = countryName + "-Ar"
            }).Entity;

            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 3),
                NameEn = "Cur",
                NameAr = "Cur"
            }).Entity;

            var payrollPeriod = context.Set<PayrollPeriod>().Add(new PayrollPeriod
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "P",
                NameAr = "P"
            }).Entity;

            context.SaveChanges();

            var payrollArea = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = "PA",
                CurrencyId = currency.Id,
                PayrollPeriodId = payrollPeriod.Id,
                CountryId = country.Id
            }).Entity;

            context.SaveChanges();

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                PayrollAreaId = payrollArea.Id
            });
        });
    }

    private static GetEmployeeContractReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetEmployeeContractReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    [Fact]
    public async Task Should_Return_Only_Contracts_Overlapping_The_Requested_Window()
    {
        var contractType = CreateContractType("CT-OVL");
        var inRange = CreateEmployee("OVL-IN");
        var before = CreateEmployee("OVL-BEFORE");
        var after = CreateEmployee("OVL-AFTER");

        CreateContract(inRange, contractType, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        CreateContract(before, contractType, new DateTime(2023, 1, 1), new DateTime(2023, 6, 30));
        CreateContract(after, contractType, new DateTime(2025, 6, 1), new DateTime(2025, 12, 31));

        var result = await _appService.GetEmployeeContractReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("OVL-IN");
        codes.ShouldNotContain("OVL-BEFORE");
        codes.ShouldNotContain("OVL-AFTER");
    }

    [Fact]
    public async Task Should_Include_Contracts_Touching_The_Window_Boundary()
    {
        var contractType = CreateContractType("CT-EDGE");
        var startsOnToDate = CreateEmployee("EDGE-START");
        var endsOnFromDate = CreateEmployee("EDGE-END");

        // Legacy predicate is NOT(StartDate > To || EndDate < From), so both boundary rows are included.
        CreateContract(startsOnToDate, contractType, new DateTime(2024, 12, 31), new DateTime(2025, 12, 31));
        CreateContract(endsOnFromDate, contractType, new DateTime(2023, 1, 1), new DateTime(2024, 1, 1));

        var result = await _appService.GetEmployeeContractReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("EDGE-START");
        codes.ShouldContain("EDGE-END");
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        var contractType = CreateContractType("CT-ST");
        var active = CreateEmployee("ST-ACTIVE");
        var inactive = CreateEmployee("ST-INACTIVE", EmployeeStatus.Inactive);

        CreateContract(active, contractType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateContract(inactive, contractType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeContractReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), status: EmployeeStatus.Inactive));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("ST-INACTIVE");
        codes.ShouldNotContain("ST-ACTIVE");
    }

    [Fact]
    public async Task Should_Filter_By_Comma_Separated_Employee_Codes()
    {
        var contractType = CreateContractType("CT-CODE");
        var first = CreateEmployee("CODE-A");
        var second = CreateEmployee("CODE-B");
        var third = CreateEmployee("CODE-C");

        CreateContract(first, contractType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateContract(second, contractType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateContract(third, contractType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        // Whitespace around the separator is tolerated here (legacy did a bare Split(',')).
        var result = await _appService.GetEmployeeContractReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "CODE-A , CODE-C"));

        var codes = result.Items.Select(i => i.EmployeeCode).OrderBy(c => c).ToList();
        codes.ShouldBe(new List<string> { "CODE-A", "CODE-C" });
    }

    [Fact]
    public async Task Should_Project_Legacy_Columns_Including_Country_As_Of_From_Date()
    {
        var contractType = CreateContractType("CT-PROJ");
        var employee = CreateEmployee("PROJ-1");

        CreateContract(employee, contractType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        // Current as of the FROM date -> this is the one legacy picks.
        CreateOrgAssignmentWithCountry(employee, new DateTime(2023, 1, 1), new DateTime(2024, 6, 30), "Kuwait");
        // Starts after the FROM date -> legacy ignores it even though it overlaps the window.
        CreateOrgAssignmentWithCountry(employee, new DateTime(2024, 7, 1), new DateTime(2025, 12, 31), "Bahrain");

        var result = await _appService.GetEmployeeContractReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "PROJ-1"));

        var row = result.Items.Single();
        row.EmployeeCode.ShouldBe("PROJ-1");
        row.EmployeeName.ShouldBe("Full PROJ-1");
        row.ContractTypeName.ShouldBe("CT-PROJ-En");
        row.StartDate.ShouldBe(new DateTime(2024, 1, 1));
        row.EndDate.ShouldBe(new DateTime(2024, 12, 31));
        row.CountryName.ShouldBe("Kuwait");
    }

    [Fact]
    public async Task Should_Leave_Country_Null_When_No_Assignment_Is_Current_On_From_Date()
    {
        var contractType = CreateContractType("CT-NOCTRY");
        var employee = CreateEmployee("NOCTRY-1");

        CreateContract(employee, contractType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeContractReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "NOCTRY-1"));

        result.Items.Single().CountryName.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Page_Results()
    {
        var contractType = CreateContractType("CT-PAGE");
        var first = CreateEmployee("PAGE-A");
        var second = CreateEmployee("PAGE-B");

        CreateContract(first, contractType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateContract(second, contractType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var page = await _appService.GetEmployeeContractReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "PAGE-A,PAGE-B", maxResultCount: 1));

        page.TotalCount.ShouldBe(2);
        page.Items.Count.ShouldBe(1);
        page.Items.Single().EmployeeCode.ShouldBe("PAGE-A"); // default sort is EmployeeCode ASC
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var contractType = CreateContractType("CT-EXP");
        var first = CreateEmployee("EXP-A");
        var second = CreateEmployee("EXP-B");
        var third = CreateEmployee("EXP-C");
        var excluded = CreateEmployee("EXP-OUT");

        CreateContract(first, contractType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateContract(second, contractType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateContract(third, contractType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateContract(excluded, contractType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var input = Input(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            employeeCode: "EXP-A,EXP-B,EXP-C",
            maxResultCount: 1);

        var file = await _appService.GetEmployeeContractReportToExcel(input);

        file.FileName.ShouldBe("EmployeeContract.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "EXP-A", "EXP-B", "EXP-C" });
        exported.First().Keys.ShouldContain("Country");
    }
}
