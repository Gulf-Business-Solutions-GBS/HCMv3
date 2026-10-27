using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Transaction;
using HCMv3.GridReports.Transaction.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Disciplinary grid report (legacy <c>uscRptEmployeeDisciplinary.ascx</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeDisciplinaryReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeDisciplinaryReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeDisciplinaryReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeDisciplinaryReportAppService>();
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

    private DisciplinaryType CreateDisciplinaryType(string code)
    {
        DisciplinaryType disciplinaryType = null;
        UsingDbContext(context =>
        {
            disciplinaryType = context.Set<DisciplinaryType>().Add(new DisciplinaryType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });

        return disciplinaryType;
    }

    private void CreateDisciplinary(Employee employee, DisciplinaryType type, DateTime date)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeDisciplinary>().Add(new EmployeeDisciplinary
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                DisciplinaryTypeId = type.Id,
                DisciplinaryDate = date,
                DisciplinaryTime = date
            });
        });
    }

    private static GetEmployeeDisciplinaryReportInput Input(DateTime from, DateTime to, string employeeCode = null)
    {
        return new GetEmployeeDisciplinaryReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            MaxResultCount = 100,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_Only_Disciplinary_Records_Within_The_Date_Range()
    {
        var employee = CreateEmployee("DIS-1");
        var type = CreateDisciplinaryType("DIS-TYPE-1");

        CreateDisciplinary(employee, type, new DateTime(2024, 6, 1));
        CreateDisciplinary(employee, type, new DateTime(2023, 1, 1));

        var result = await _appService.GetEmployeeDisciplinaryReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "DIS-1"));

        result.Items.Count.ShouldBe(1);
        result.Items.Single().DisciplinaryDate.ShouldBe(new DateTime(2024, 6, 1));
    }

    [Fact]
    public async Task Should_Filter_By_Disciplinary_Type_Code()
    {
        var employee = CreateEmployee("DIS-2");
        var matchType = CreateDisciplinaryType("DIS-TYPE-MATCH");
        var otherType = CreateDisciplinaryType("DIS-TYPE-OTHER");

        CreateDisciplinary(employee, matchType, new DateTime(2024, 6, 1));
        CreateDisciplinary(employee, otherType, new DateTime(2024, 6, 2));

        var input = Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "DIS-2");
        input.DisciplinaryTypeCode = "DIS-TYPE-MATCH";

        var result = await _appService.GetEmployeeDisciplinaryReport(input);

        result.Items.Count.ShouldBe(1);
        result.Items.Single().DisciplinaryTypeName.ShouldBe("DIS-TYPE-MATCH");
    }

    /// <summary>Every exported header must resolve to real text, not the "[KeyName]" missing-key fallback.</summary>
    [Fact]
    public async Task Should_Resolve_Every_Export_Header_To_Localized_Text_Not_A_Missing_Key_Placeholder()
    {
        var employee = CreateEmployee("DIS-3");
        var type = CreateDisciplinaryType("DIS-TYPE-3");
        CreateDisciplinary(employee, type, new DateTime(2024, 6, 1));

        var file = await _appService.GetEmployeeDisciplinaryReportToExcel(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "DIS-3"));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        foreach (var header in exported.First().Keys)
        {
            header.ShouldNotStartWith("[");
        }
    }
}
