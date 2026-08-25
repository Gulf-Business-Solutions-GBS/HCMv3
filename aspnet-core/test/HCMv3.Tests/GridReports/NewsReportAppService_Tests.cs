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
/// Tests for the News grid report (legacy <c>uscNews.ascx</c>, <c>BindNewsDocument</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class NewsReportAppService_Tests : AppTestBase
{
    private readonly INewsReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public NewsReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<INewsReportAppService>();
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

    private void CreateEmployeeDocument(Employee employee, DocumentType documentType, string description, DateTime issueDate, DateTime expiryDate)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeDocument>().Add(new EmployeeDocument
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                DocumentTypeId = documentType.Id,
                Description = description,
                IssueDate = issueDate,
                ExpiryDate = expiryDate
            });
        });
    }

    private static GetNewsReportInput Input(DateTime from, DateTime to)
    {
        return new GetNewsReportInput { FromDate = from, ToDate = to, MaxResultCount = 100, SkipCount = 0 };
    }

    [Fact]
    public async Task Should_Return_Only_NEWS_Typed_Documents_Active_In_The_Window()
    {
        var employee = CreateEmployee("NEWS-1");
        var newsType = CreateDocumentType("NEWS");
        var otherType = CreateDocumentType("PASSPORT");

        CreateEmployeeDocument(employee, newsType, "Active news", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeDocument(employee, otherType, "Not news", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeDocument(employee, newsType, "Expired news", new DateTime(2020, 1, 1), new DateTime(2020, 12, 31));

        var result = await _appService.GetNewsReport(Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 1)));

        var descriptions = result.Items.Select(i => i.Description).ToList();
        descriptions.ShouldContain("Active news");
        descriptions.ShouldNotContain("Not news");
        descriptions.ShouldNotContain("Expired news");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("NEWS-2");
        var newsType = CreateDocumentType("NEWS");

        CreateEmployeeDocument(employee, newsType, "News A", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeDocument(employee, newsType, "News B", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var input = Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 1));
        input.MaxResultCount = 1;

        var file = await _appService.GetNewsReportToExcel(input);

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(2);
    }

    /// <summary>
    /// Every exported header must resolve to real text, not ABP's "[KeyName]" not-found fallback -- that
    /// fallback is what a missing localization key looks like in the exported file.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Export_Header_To_Localized_Text_Not_A_Missing_Key_Placeholder()
    {
        var employee = CreateEmployee("NEWS-3");
        var newsType = CreateDocumentType("NEWS");
        CreateEmployeeDocument(employee, newsType, "News header check", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var file = await _appService.GetNewsReportToExcel(Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 1)));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var headers = exported.First().Keys;
        foreach (var header in headers)
        {
            header.ShouldNotStartWith("[");
        }
    }
}
