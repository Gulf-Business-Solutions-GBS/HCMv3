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
/// Tests for the Employee Document Movement grid report (legacy <c>uscRptEmployeeDocMovement.ascx</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeDocMovementReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeDocMovementReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeDocMovementReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeDocMovementReportAppService>();
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

    private DocumentType CreateDocumentType(string code)
    {
        DocumentType documentType = null;
        UsingDbContext(context =>
        {
            documentType = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar"
            }).Entity;
        });
        return documentType;
    }

    private EmployeeDocument CreateDocument(Employee employee, DocumentType documentType, DateTime issue, DateTime expiry)
    {
        EmployeeDocument document = null;
        UsingDbContext(context =>
        {
            document = context.Set<EmployeeDocument>().Add(new EmployeeDocument
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                DocumentTypeId = documentType.Id,
                IssueDate = issue,
                ExpiryDate = expiry
            }).Entity;
        });
        return document;
    }

    private void AddOpenMovement(EmployeeDocument document, DateTime withdraw)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeDocumentMvmt>().Add(new EmployeeDocumentMvmt
            {
                TenantId = TenantId,
                EmployeeId = document.EmployeeId,
                EmployeeDocumentId = document.Id,
                WithdrawDate = withdraw,
                ReturnDate = withdraw,
                ActualReturnDate = default
            });
        });
    }

    private static GetEmployeeDocMovementReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        DocumentMvmtStatus? mvmtStatus = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetEmployeeDocMovementReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            DocumentMvmtStatus = mvmtStatus,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    [Fact]
    public async Task Should_Return_Only_Documents_Overlapping_The_Requested_Window()
    {
        var documentType = CreateDocumentType("DT-OVL");
        var inRange = CreateEmployee("DOC-IN");
        var before = CreateEmployee("DOC-BEFORE");

        CreateDocument(inRange, documentType, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        CreateDocument(before, documentType, new DateTime(2023, 1, 1), new DateTime(2023, 6, 30));

        var result = await _appService.GetEmployeeDocMovementReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("DOC-IN");
        codes.ShouldNotContain("DOC-BEFORE");
    }

    [Fact]
    public async Task Should_Compute_DocumentMvmtStatus_Out_When_Movement_Is_Open()
    {
        var documentType = CreateDocumentType("DT-OUT");
        var employee = CreateEmployee("DOC-OUT-1");
        var document = CreateDocument(employee, documentType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        AddOpenMovement(document, new DateTime(2024, 3, 1));

        var result = await _appService.GetEmployeeDocMovementReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "DOC-OUT-1"));

        result.Items.Single().DocumentMvmtStatus.ShouldBe(DocumentMvmtStatus.OUT);
    }

    [Fact]
    public async Task Should_Filter_By_DocumentMvmtStatus()
    {
        var documentType = CreateDocumentType("DT-FLT");
        var openEmployee = CreateEmployee("DOC-FLT-OUT");
        var closedEmployee = CreateEmployee("DOC-FLT-IN");
        var openDocument = CreateDocument(openEmployee, documentType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateDocument(closedEmployee, documentType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        AddOpenMovement(openDocument, new DateTime(2024, 3, 1));

        var result = await _appService.GetEmployeeDocMovementReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), mvmtStatus: DocumentMvmtStatus.OUT));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("DOC-FLT-OUT");
        codes.ShouldNotContain("DOC-FLT-IN");
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users but invisible to the compiler and to every other test.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var documentType = CreateDocumentType("DT-HDR");
        var employee = CreateEmployee("DOC-HDR");
        CreateDocument(employee, documentType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var file = await _appService.GetEmployeeDocMovementReportToExcel(
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
