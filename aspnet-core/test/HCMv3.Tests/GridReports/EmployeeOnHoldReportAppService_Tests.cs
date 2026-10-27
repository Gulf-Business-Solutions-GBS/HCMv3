using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Status;
using HCMv3.GridReports.Status.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee On-Hold grid report (legacy <c>uscRptEmployeeOnHold.ascx</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeOnHoldReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeOnHoldReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeOnHoldReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeOnHoldReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
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
                NationalityId = nationality.Id,
                Status = EmployeeStatus.Active
            }).Entity;
        });
        return employee;
    }

    private EmployeeOnHold CreateOnHold(
        Employee employee,
        DateTime start,
        DateTime end,
        OnHoldStatus bankTransfer = OnHoldStatus.NA)
    {
        EmployeeOnHold onHold = null;
        UsingDbContext(context =>
        {
            onHold = context.Set<EmployeeOnHold>().Add(new EmployeeOnHold
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                OnHold = true,
                OnHoldBasicPay = true,
                BankTransfer = bankTransfer
            }).Entity;
        });
        return onHold;
    }

    private static GetEmployeeOnHoldReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        OnHoldStatus? bankTransfer = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetEmployeeOnHoldReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            BankTransfer = bankTransfer,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    [Fact]
    public async Task Should_Return_Only_OnHolds_Overlapping_The_Requested_Window()
    {
        var inRange = CreateEmployee("OH-IN");
        var before = CreateEmployee("OH-BEFORE");

        CreateOnHold(inRange, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        CreateOnHold(before, new DateTime(2023, 1, 1), new DateTime(2023, 6, 30));

        var result = await _appService.GetEmployeeOnHoldReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("OH-IN");
        codes.ShouldNotContain("OH-BEFORE");
    }

    [Fact]
    public async Task Should_Filter_By_BankTransfer_Status()
    {
        var held = CreateEmployee("OH-HOLD");
        var unheld = CreateEmployee("OH-UNHOLD");

        CreateOnHold(held, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), OnHoldStatus.Hold);
        CreateOnHold(unheld, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), OnHoldStatus.Unhold);

        var result = await _appService.GetEmployeeOnHoldReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), bankTransfer: OnHoldStatus.Hold));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("OH-HOLD");
        codes.ShouldNotContain("OH-UNHOLD");
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users but invisible to the compiler and to every other test.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var employee = CreateEmployee("OH-HDR");
        CreateOnHold(employee, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var file = await _appService.GetEmployeeOnHoldReportToExcel(
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
