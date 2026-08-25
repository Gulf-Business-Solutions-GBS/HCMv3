using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Timing;
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
/// Tests for the Employee Document grid report (legacy <c>uscRptEmployeeDocument.ascx</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeDocumentReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeDocumentReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeDocumentReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeDocumentReportAppService>();
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
                NameAr = nameEn
            }).Entity;
        });

        return documentType;
    }

    private EmployeeDocument CreateDocument(Employee employee, DocumentType documentType, DateTime issueDate, DateTime expiryDate)
    {
        EmployeeDocument document = null;
        UsingDbContext(context =>
        {
            document = context.Set<EmployeeDocument>().Add(new EmployeeDocument
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                DocumentTypeId = documentType.Id,
                IssueDate = issueDate,
                ExpiryDate = expiryDate,
                DocumentNum = "D-" + Guid.NewGuid().ToString("N").Substring(0, 6)
            }).Entity;
        });

        return document;
    }

    private static GetEmployeeDocumentReportInput Input(DateTime from, DateTime to, string employeeCode = null)
    {
        return new GetEmployeeDocumentReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            MaxResultCount = 100,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_Only_Documents_Overlapping_The_Requested_Window()
    {
        var employee = CreateEmployee("EDOC-1");
        var documentType = CreateDocumentType("PASSPORT");

        CreateDocument(employee, documentType, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        CreateDocument(employee, documentType, new DateTime(2020, 1, 1), new DateTime(2020, 12, 31));

        var result = await _appService.GetEmployeeDocumentReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "EDOC-1"));

        result.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Mark_Document_Active_When_Today_Is_Within_Issue_And_Expiry()
    {
        var employee = CreateEmployee("EDOC-2");
        var documentType = CreateDocumentType("VISA");

        var today = Clock.Now.Date;
        CreateDocument(employee, documentType, today.AddDays(-10), today.AddDays(10));

        var result = await _appService.GetEmployeeDocumentReport(
            Input(today.AddDays(-30), today.AddDays(30), "EDOC-2"));

        result.Items.Single().IsActive.ShouldBeTrue();
    }

    /// <summary>Every exported header must resolve to real text, not the "[KeyName]" missing-key fallback.</summary>
    [Fact]
    public async Task Should_Resolve_Every_Export_Header_To_Localized_Text_Not_A_Missing_Key_Placeholder()
    {
        var employee = CreateEmployee("EDOC-3");
        var documentType = CreateDocumentType("LICENSE");
        CreateDocument(employee, documentType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var file = await _appService.GetEmployeeDocumentReportToExcel(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "EDOC-3"));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        foreach (var header in exported.First().Keys)
        {
            header.ShouldNotStartWith("[");
        }
    }
}
