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
/// Tests for the Employee Separation grid report (legacy <c>uscRptEmployeeSeparation.ascx</c>), filtered
/// on <c>EmployeeSeparation.LastDayInService</c>.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeSeparationReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeSeparationReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeSeparationReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeSeparationReportAppService>();
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

    private SeparationReason CreateSeparationReason(string code)
    {
        SeparationReason reason = null;
        UsingDbContext(context =>
        {
            reason = context.Set<SeparationReason>().Add(new SeparationReason
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar"
            }).Entity;
        });
        return reason;
    }

    private EmployeeSeparation CreateSeparation(
        Employee employee,
        SeparationReason reason,
        DateTime lastDayInService,
        SeparationType separationType = SeparationType.Resignation,
        DateTime? revokeDate = null)
    {
        EmployeeSeparation separation = null;
        UsingDbContext(context =>
        {
            separation = context.Set<EmployeeSeparation>().Add(new EmployeeSeparation
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                SubmissionDate = lastDayInService.AddDays(-30),
                LastDayInService = lastDayInService,
                PlannedLastDayInService = lastDayInService,
                NoticePeriodDays = 30,
                SeparationType = separationType,
                SeparationReasonId = reason.Id,
                RevokeDate = revokeDate
            }).Entity;
        });
        return separation;
    }

    private static GetEmployeeSeparationReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        SeparationType? separationType = null,
        bool? isRevoked = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetEmployeeSeparationReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            SeparationType = separationType,
            IsRevoked = isRevoked,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    [Fact]
    public async Task Should_Filter_By_LastDayInService_Window()
    {
        var reason = CreateSeparationReason("SEP-RSN");
        var inRange = CreateEmployee("SEP-IN");
        var outOfRange = CreateEmployee("SEP-OUT");

        CreateSeparation(inRange, reason, new DateTime(2024, 6, 1));
        CreateSeparation(outOfRange, reason, new DateTime(2023, 1, 1));

        var result = await _appService.GetEmployeeSeparationReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("SEP-IN");
        codes.ShouldNotContain("SEP-OUT");
    }

    [Fact]
    public async Task Should_Filter_By_SeparationType()
    {
        var reason = CreateSeparationReason("SEP-TYPE-RSN");
        var resignation = CreateEmployee("SEP-RES");
        var termination = CreateEmployee("SEP-TERM");

        CreateSeparation(resignation, reason, new DateTime(2024, 6, 1), SeparationType.Resignation);
        CreateSeparation(termination, reason, new DateTime(2024, 6, 1), SeparationType.Termination);

        var result = await _appService.GetEmployeeSeparationReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), separationType: SeparationType.Termination));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("SEP-TERM");
        codes.ShouldNotContain("SEP-RES");
    }

    [Fact]
    public async Task Should_Filter_By_IsRevoked()
    {
        var reason = CreateSeparationReason("SEP-REVOKE-RSN");
        var revoked = CreateEmployee("SEP-REVOKED");
        var active = CreateEmployee("SEP-ACTIVE");

        CreateSeparation(revoked, reason, new DateTime(2024, 6, 1), revokeDate: new DateTime(2024, 7, 1));
        CreateSeparation(active, reason, new DateTime(2024, 6, 1));

        var result = await _appService.GetEmployeeSeparationReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), isRevoked: true));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("SEP-REVOKED");
        codes.ShouldNotContain("SEP-ACTIVE");
    }

    /// <summary>
    /// Legacy "NoticePeriodEnd" column is a fixed 89-day offset from SubmissionDate, unrelated to
    /// NoticePeriodDays -- a pinned legacy quirk, not something to "fix" here.
    /// </summary>
    [Fact]
    public async Task Should_Reproduce_The_NoticePeriodEnd_89Day_Quirk()
    {
        var reason = CreateSeparationReason("SEP-QUIRK-RSN");
        var employee = CreateEmployee("SEP-QUIRK");

        CreateSeparation(employee, reason, new DateTime(2024, 6, 1));

        var result = await _appService.GetEmployeeSeparationReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "SEP-QUIRK"));

        var row = result.Items.Single();
        row.NoticePeriodEndQuirk.ShouldBe(row.SeparationDate.AddDays(89));
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users but invisible to the compiler and to every other test.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var reason = CreateSeparationReason("SEP-HDR-RSN");
        var employee = CreateEmployee("SEP-HDR");
        CreateSeparation(employee, reason, new DateTime(2024, 6, 1));

        var file = await _appService.GetEmployeeSeparationReportToExcel(
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
