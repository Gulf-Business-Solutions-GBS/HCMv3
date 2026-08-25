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
/// Tests for the Employee Document (Custom033) grid report -- the customer-specific variant of
/// <see cref="EmployeeDocumentReportAppService_Tests"/>. Focuses on the diff: the added
/// <c>CivilId</c> column and the Sponsor -&gt; SponsorName rename (see
/// <c>EmployeeDocumentCustom033ReportRowDto</c> remarks for the full diff).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeDocumentCustom033ReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeDocumentCustom033ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeDocumentCustom033ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeDocumentCustom033ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, long civilId)
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
                Status = EmployeeStatus.Active,
                CivilID = civilId
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

    private void CreateDocument(Employee employee, DocumentType documentType, DateTime issueDate, DateTime expiryDate)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeDocument>().Add(new EmployeeDocument
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                DocumentTypeId = documentType.Id,
                IssueDate = issueDate,
                ExpiryDate = expiryDate,
                DocumentNum = "D-" + Guid.NewGuid().ToString("N").Substring(0, 6)
            });
        });
    }

    private static GetEmployeeDocumentCustom033ReportInput Input(DateTime from, DateTime to, string employeeCode = null)
    {
        return new GetEmployeeDocumentCustom033ReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            MaxResultCount = 100,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Include_CivilId_Which_The_Base_Report_Does_Not_Have()
    {
        var employee = CreateEmployee("EDOC33-1", 123456789);
        var documentType = CreateDocumentType("PASSPORT");
        CreateDocument(employee, documentType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeDocumentCustom033Report(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "EDOC33-1"));

        result.Items.Single().CivilId.ShouldBe(123456789);
    }

    /// <summary>Every exported header must resolve to real text, not the "[KeyName]" missing-key fallback.</summary>
    [Fact]
    public async Task Should_Resolve_Every_Export_Header_To_Localized_Text_Not_A_Missing_Key_Placeholder()
    {
        var employee = CreateEmployee("EDOC33-2", 987654321);
        var documentType = CreateDocumentType("LICENSE");
        CreateDocument(employee, documentType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var file = await _appService.GetEmployeeDocumentCustom033ReportToExcel(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "EDOC33-2"));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.First().Keys.ShouldContain("CivilId");
        foreach (var header in exported.First().Keys)
        {
            header.ShouldNotStartWith("[");
        }
    }
}
