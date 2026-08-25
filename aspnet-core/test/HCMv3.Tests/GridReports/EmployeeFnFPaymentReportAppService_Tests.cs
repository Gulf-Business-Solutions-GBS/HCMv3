using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.Status;
using HCMv3.GridReports.Status.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Full &amp; Final Payment grid report (legacy <c>uscRptEmployeeFnFPayment.ascx</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeFnFPaymentReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeFnFPaymentReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeFnFPaymentReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeFnFPaymentReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code)
    {
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
                Status = EmployeeStatus.Active
            }).Entity;
        });
        return employee;
    }

    private EmployeeFnFPayment CreatePayment(Employee employee, DateTime compensationDate)
    {
        EmployeeFnFPayment payment = null;
        UsingDbContext(context =>
        {
            payment = context.Set<EmployeeFnFPayment>().Add(new EmployeeFnFPayment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                CompensationDate = compensationDate
            }).Entity;
        });
        return payment;
    }

    private void CreateSeparation(Employee employee, DateTime submissionDate, SeparationType separationType)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeSeparation>().Add(new EmployeeSeparation
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                SubmissionDate = submissionDate,
                LastDayInService = submissionDate,
                SeparationType = separationType
            });
        });
    }

    private static GetEmployeeFnFPaymentReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        SeparationType? separationType = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetEmployeeFnFPaymentReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            SeparationType = separationType,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    [Fact]
    public async Task Should_Filter_By_CompensationDate_Window()
    {
        var inRange = CreateEmployee("FNF-IN");
        var outOfRange = CreateEmployee("FNF-OUT");

        CreatePayment(inRange, new DateTime(2024, 6, 1));
        CreatePayment(outOfRange, new DateTime(2023, 1, 1));

        var result = await _appService.GetEmployeeFnFPaymentReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("FNF-IN");
        codes.ShouldNotContain("FNF-OUT");
    }

    [Fact]
    public async Task Should_Filter_By_SeparationType_Of_Employee_Separated_In_Window()
    {
        var resignation = CreateEmployee("FNF-RES");
        var termination = CreateEmployee("FNF-TERM");

        CreatePayment(resignation, new DateTime(2024, 6, 1));
        CreatePayment(termination, new DateTime(2024, 6, 1));
        CreateSeparation(resignation, new DateTime(2024, 6, 1), SeparationType.Resignation);
        CreateSeparation(termination, new DateTime(2024, 6, 1), SeparationType.Termination);

        var result = await _appService.GetEmployeeFnFPaymentReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), separationType: SeparationType.Resignation));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("FNF-RES");
        codes.ShouldNotContain("FNF-TERM");
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users but invisible to the compiler and to every other test.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var employee = CreateEmployee("FNF-HDR");
        CreatePayment(employee, new DateTime(2024, 6, 1));

        var file = await _appService.GetEmployeeFnFPaymentReportToExcel(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var unresolved = exported.First().Keys
            .Where(k => k.StartsWith("[") && k.EndsWith("]"))
            .ToList();

        unresolved.ShouldBeEmpty();
    }
}
