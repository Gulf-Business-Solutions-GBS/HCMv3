using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Transfer grid-report port (legacy <c>uscRptEmployeeTransfer.ascx</c>,
/// <c>Table1()</c> lines 464-640). Pins the "Transfer" personal-action match, the date-window boundary,
/// the current-vs-previous org assignment split, and that the export ignores the page size.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeTransferReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeTransferReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeTransferReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeTransferReportAppService>();
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

    /// <summary>
    /// Legacy matched the action by <c>PersonalAction.NameEn</c>, so the name -- not the code -- is what
    /// these tests vary.
    /// </summary>
    private (int PersonalActionId, int ReasonId) CreatePersonalAction(string nameEn)
    {
        var personalActionId = 0;
        var reasonId = 0;

        UsingDbContext(context =>
        {
            var personalAction = context.Set<PersonalAction>().Add(new PersonalAction
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameEn
            }).Entity;

            context.SaveChanges();
            personalActionId = personalAction.Id;

            var reason = context.Set<PersonalActionReason>().Add(new PersonalActionReason
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "R",
                NameAr = "R",
                PersonalActionId = personalAction.Id
            }).Entity;

            context.SaveChanges();
            reasonId = reason.Id;
        });

        return (personalActionId, reasonId);
    }

    private void CreateAction(Employee employee, (int PersonalActionId, int ReasonId) action, DateTime date)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAction>().Add(new EmployeeAction
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                PersonalActionId = action.PersonalActionId,
                ActionReasonId = action.ReasonId,
                StartDate = date
            });
        });
    }

    /// <summary>Source of unique <c>OrganizationUnit.Code</c> values across the whole test class.</summary>
    private static int _nextOrgUnitCodeNumber = 6000;

    /// <summary>
    /// Creates an org unit AND the <see cref="OrgUnitProfile"/> that classifies it at
    /// <paramref name="levelCode"/>, which is what <c>EmployeeOrgLevelNameResolver</c> prefix-matches on to
    /// fill the OrgLevel1..OrgLevel7 columns.
    /// </summary>
    private long CreateOrganizationUnit(string displayName, string levelCode = null)
    {
        levelCode ??= OrganizationUnitType.Level2Code;

        long id = 0;
        UsingDbContext(context =>
        {
            // OrganizationUnit.Code is normally assigned by OrganizationUnitManager; created directly here,
            // so a unique tree code is stamped explicitly (the resolver prefix-matches on it).
            var unit = context.OrganizationUnits.Add(
                new OrganizationUnit(AbpSession.TenantId, displayName, null)
                {
                    Code = OrganizationUnit.CreateCode(++_nextOrgUnitCodeNumber)
                }).Entity;
            context.SaveChanges();

            var unitType = context.Set<OrganizationUnitType>().FirstOrDefault(t => t.Code == levelCode);

            if (unitType == null)
            {
                unitType = context.Set<OrganizationUnitType>().Add(new OrganizationUnitType
                {
                    TenantId = TenantId,
                    Code = levelCode,
                    NameEn = levelCode,
                    NameAr = levelCode + "-Ar"
                }).Entity;
                context.SaveChanges();
            }

            context.Set<OrgUnitProfile>().Add(new OrgUnitProfile
            {
                TenantId = TenantId,
                OrganizationUnitId = unit.Id,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameAr = displayName + "-Ar",
                OrganizationUnitTypeId = unitType.Id
            });

            context.SaveChanges();
            id = unit.Id;
        });

        return id;
    }

    private void CreateAssignment(Employee employee, DateTime start, DateTime end, long organizationUnitId,
        string workLocationName = null)
    {
        UsingDbContext(context =>
        {
            int? workLocationId = null;
            if (workLocationName != null)
            {
                var workLocation = context.Set<WorkLocation>().Add(new WorkLocation
                {
                    TenantId = TenantId,
                    Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                    NameEn = workLocationName,
                    NameAr = workLocationName + "-Ar"
                }).Entity;
                context.SaveChanges();
                workLocationId = workLocation.Id;
            }

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                OrganizationUnitId = organizationUnitId,
                WorkLocationId = workLocationId
            });
        });
    }

    private static GetEmployeeTransferReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeTransferReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_Only_Transfer_Actions()
    {
        var transfer = CreatePersonalAction("Transfer");
        var promotion = CreatePersonalAction("Promotion");

        var transferred = CreateEmployee("TRF-YES");
        var promoted = CreateEmployee("TRF-NO");

        CreateAction(transferred, transfer, new DateTime(2024, 6, 1));
        CreateAction(promoted, promotion, new DateTime(2024, 6, 1));

        var result = await _appService.GetEmployeeTransferReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("TRF-YES");
        codes.ShouldNotContain("TRF-NO");
    }

    [Fact]
    public async Task Should_Include_Transfers_On_The_Window_Boundary_And_Exclude_Those_Outside()
    {
        var transfer = CreatePersonalAction("Transfer");

        var onFrom = CreateEmployee("TRF-EDGE-FROM");
        var onTo = CreateEmployee("TRF-EDGE-TO");
        var before = CreateEmployee("TRF-EDGE-BEFORE");
        var after = CreateEmployee("TRF-EDGE-AFTER");

        // EmployeeAction has no EndDate in HCMv3 (legacy's getter returned StartDate), so legacy's
        // NOT(Start > To || End < From) reduces to From <= StartDate <= To -- both edges inclusive.
        CreateAction(onFrom, transfer, new DateTime(2024, 1, 1));
        CreateAction(onTo, transfer, new DateTime(2024, 12, 31));
        CreateAction(before, transfer, new DateTime(2023, 12, 31));
        CreateAction(after, transfer, new DateTime(2025, 1, 1));

        var result = await _appService.GetEmployeeTransferReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("TRF-EDGE-FROM");
        codes.ShouldContain("TRF-EDGE-TO");
        codes.ShouldNotContain("TRF-EDGE-BEFORE");
        codes.ShouldNotContain("TRF-EDGE-AFTER");
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        var transfer = CreatePersonalAction("Transfer");
        var active = CreateEmployee("TRF-ST-ACTIVE");
        var inactive = CreateEmployee("TRF-ST-INACTIVE", EmployeeStatus.Inactive);

        CreateAction(active, transfer, new DateTime(2024, 6, 1));
        CreateAction(inactive, transfer, new DateTime(2024, 6, 1));

        var result = await _appService.GetEmployeeTransferReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), status: EmployeeStatus.Inactive));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("TRF-ST-INACTIVE");
        codes.ShouldNotContain("TRF-ST-ACTIVE");
    }

    [Fact]
    public async Task Should_Resolve_Current_And_Previous_Assignment_Around_The_Transfer_Date()
    {
        var transfer = CreatePersonalAction("Transfer");
        var employee = CreateEmployee("TRF-PROJ");

        var oldUnit = CreateOrganizationUnit("Old Unit");
        var newUnit = CreateOrganizationUnit("New Unit");

        // Previous: starts strictly before the transfer date.
        CreateAssignment(employee, new DateTime(2023, 1, 1), new DateTime(2024, 5, 31), oldUnit, "Old Site");
        // Current as of the transfer date.
        CreateAssignment(employee, new DateTime(2024, 6, 1), new DateTime(2025, 12, 31), newUnit, "New Site");

        CreateAction(employee, transfer, new DateTime(2024, 6, 1));

        var result = await _appService.GetEmployeeTransferReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "TRF-PROJ"));

        var row = result.Items.Single();
        row.EmployeeCode.ShouldBe("TRF-PROJ");
        row.EmployeeName.ShouldBe("Full TRF-PROJ");
        row.TransferDate.ShouldBe(new DateTime(2024, 6, 1));
        row.OrganizationUnitId.ShouldBe(newUnit);
        row.PreviousOrganizationUnitId.ShouldBe(oldUnit);

        // Legacy rendered OrgLevel1..7 plus "Previous OrgLevel2/3/4"; both unit fixtures are classified at
        // Level2, so exactly those two columns carry a value and the rest come back as "" (never null).
        row.OrgLevel2.ShouldBe("New Unit");
        row.PreviousOrgLevel2.ShouldBe("Old Unit");
        row.OrgLevel1.ShouldBe(string.Empty);
        row.OrgLevel7.ShouldBe(string.Empty);
        row.PreviousOrgLevel3.ShouldBe(string.Empty);
        row.PreviousOrgLevel4.ShouldBe(string.Empty);
        row.WorkLocationName.ShouldBe("New Site");
        row.PreviousWorkLocationName.ShouldBe("Old Site");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var transfer = CreatePersonalAction("Transfer");

        var first = CreateEmployee("TRF-EXP-A");
        var second = CreateEmployee("TRF-EXP-B");
        var third = CreateEmployee("TRF-EXP-C");
        var excluded = CreateEmployee("TRF-EXP-OUT");

        CreateAction(first, transfer, new DateTime(2024, 6, 1));
        CreateAction(second, transfer, new DateTime(2024, 6, 1));
        CreateAction(third, transfer, new DateTime(2024, 6, 1));
        CreateAction(excluded, transfer, new DateTime(2024, 6, 1));

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var input = Input(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            employeeCode: "TRF-EXP-A,TRF-EXP-B,TRF-EXP-C",
            maxResultCount: 1);

        var file = await _appService.GetEmployeeTransferReportToExcel(input);

        file.FileName.ShouldBe("EmployeeTransfer.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "TRF-EXP-A", "TRF-EXP-B", "TRF-EXP-C" });
        // The exporter writes LOCALIZED header cells, not the raw localization key.
        //
        // NOTE: this used to assert "Previous Organization Unit". Legacy renders TEN org columns on this
        // report -- OrgLevel1-7 plus Previous OrgLevel2/3/4 (uscRptEmployeeTransfer.ascx.cs lines 505-523)
        // -- so the single collapsed column was replaced by the real set. Asserting one of each proves the
        // localization keys resolve; a missing key would surface here as the raw "[PreviousOrgLevel2]".
        exported.First().Keys.ShouldContain("Org Level 1");
        exported.First().Keys.ShouldContain("Previous Org Level 2");
    }
}
