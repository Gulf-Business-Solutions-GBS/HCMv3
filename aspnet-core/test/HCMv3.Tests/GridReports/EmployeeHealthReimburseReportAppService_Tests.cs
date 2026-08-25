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
/// Tests for the Employee Health Reimburse grid report -- reduced-scope port of legacy
/// <c>uscRptEmployeeHealthReimburse.ascx</c>. See <c>GetEmployeeHealthReimburseReportInput</c> remarks for
/// why this covers actual reimbursement transactions only (no eligibility/quota/accrual columns).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeHealthReimburseReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeHealthReimburseReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeHealthReimburseReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeHealthReimburseReportAppService>();
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

    private void CreateReimburse(Employee employee, DateTime paymentDate, double amount)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeHealthReimburse>().Add(new EmployeeHealthReimburse
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                PaymentDate = paymentDate,
                Amount = amount
            });
        });
    }

    private static GetEmployeeHealthReimburseReportInput Input(DateTime from, DateTime to, string employeeCode = null, EmployeeStatus? status = null)
    {
        return new GetEmployeeHealthReimburseReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = 100,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_Only_Payments_Within_The_Date_Range()
    {
        var employee = CreateEmployee("HR-1");

        CreateReimburse(employee, new DateTime(2024, 6, 1), 100);
        CreateReimburse(employee, new DateTime(2023, 1, 1), 200);

        var result = await _appService.GetEmployeeHealthReimburseReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "HR-1"));

        result.Items.Count.ShouldBe(1);
        result.Items.Single().Amount.ShouldBe(100);
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        var active = CreateEmployee("HR-2-ACTIVE", EmployeeStatus.Active);
        var inactive = CreateEmployee("HR-2-INACTIVE", EmployeeStatus.Inactive);

        CreateReimburse(active, new DateTime(2024, 6, 1), 100);
        CreateReimburse(inactive, new DateTime(2024, 6, 1), 100);

        var result = await _appService.GetEmployeeHealthReimburseReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), status: EmployeeStatus.Inactive));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("HR-2-INACTIVE");
        codes.ShouldNotContain("HR-2-ACTIVE");
    }

    /// <summary>Every exported header must resolve to real text, not the "[KeyName]" missing-key fallback.</summary>
    [Fact]
    public async Task Should_Resolve_Every_Export_Header_To_Localized_Text_Not_A_Missing_Key_Placeholder()
    {
        var employee = CreateEmployee("HR-3");
        CreateReimburse(employee, new DateTime(2024, 6, 1), 100);

        var file = await _appService.GetEmployeeHealthReimburseReportToExcel(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "HR-3"));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        foreach (var header in exported.First().Keys)
        {
            header.ShouldNotStartWith("[");
        }
    }
}
