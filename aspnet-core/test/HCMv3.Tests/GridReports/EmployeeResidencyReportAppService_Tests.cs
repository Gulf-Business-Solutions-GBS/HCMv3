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
/// Tests for the Employee Residency grid report (legacy <c>uscRptEmployeeResidency.ascx</c>). One row per
/// CIVIL-ID DOCUMENT, not per employee -- see <c>GetEmployeeResidencyReportInput</c> remarks.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeResidencyReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeResidencyReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeResidencyReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeResidencyReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
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
                CivilID = 123456789,
                Status = status
            }).Entity;
        });
        return employee;
    }

    private DocumentType CreateDocumentType(string nameEn)
    {
        DocumentType documentType = null;
        UsingDbContext(context =>
        {
            documentType = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameEn + "-Ar"
            }).Entity;
        });
        return documentType;
    }

    private void CreateDocument(Employee employee, DocumentType documentType, DateTime issue, DateTime expiry)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeDocument>().Add(new EmployeeDocument
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                DocumentTypeId = documentType.Id,
                IssueDate = issue,
                ExpiryDate = expiry
            });
        });
    }

    private static GetEmployeeResidencyReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetEmployeeResidencyReportInput
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
    public async Task Should_Only_Include_CivilId_Documents_Overlapping_The_Window()
    {
        var civilIdType = CreateDocumentType("CID");
        var otherType = CreateDocumentType("Passport");
        var withCivilId = CreateEmployee("RES-YES");
        var withoutCivilId = CreateEmployee("RES-NO");

        CreateDocument(withCivilId, civilIdType, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        CreateDocument(withoutCivilId, otherType, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeResidencyReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("RES-YES");
        codes.ShouldNotContain("RES-NO");
    }

    [Fact]
    public async Task Should_Project_CivilId_And_Dates_From_The_Document()
    {
        var civilIdType = CreateDocumentType("Civil Id");
        var employee = CreateEmployee("RES-PROJ");
        CreateDocument(employee, civilIdType, new DateTime(2024, 3, 1), new DateTime(2026, 3, 1));

        var result = await _appService.GetEmployeeResidencyReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "RES-PROJ"));

        var row = result.Items.Single();
        row.CivilId.ShouldBe(123456789);
        row.CivilIdIssueDate.ShouldBe(new DateTime(2024, 3, 1));
        row.CivilIdExpiryDate.ShouldBe(new DateTime(2026, 3, 1));
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users but invisible to the compiler and to every other test.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var civilIdType = CreateDocumentType("CID");
        var employee = CreateEmployee("RES-HDR");
        CreateDocument(employee, civilIdType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var file = await _appService.GetEmployeeResidencyReportToExcel(
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
