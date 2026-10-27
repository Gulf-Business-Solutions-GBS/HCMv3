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
/// Tests for the Employee Passport grid report (legacy <c>uscRptEmployeePassport.ascx</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeePassportReportAppService_Tests : AppTestBase
{
    private readonly IEmployeePassportReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeePassportReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeePassportReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
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

    private static GetEmployeePassportReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetEmployeePassportReportInput
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
    public async Task Should_Only_Include_Passport_Type_Documents()
    {
        var passportType = CreateDocumentType("Passport");
        var otherType = CreateDocumentType("CivilId");
        var withPassport = CreateEmployee("PP-YES");
        var withoutPassport = CreateEmployee("PP-NO");

        CreateDocument(withPassport, passportType, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        CreateDocument(withoutPassport, otherType, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeePassportReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("PP-YES");
        codes.ShouldNotContain("PP-NO");
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        var passportType = CreateDocumentType("Passport");
        var active = CreateEmployee("PP-ACTIVE");
        var inactive = CreateEmployee("PP-INACTIVE", EmployeeStatus.Inactive);

        CreateDocument(active, passportType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateDocument(inactive, passportType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeePassportReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), status: EmployeeStatus.Inactive));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("PP-INACTIVE");
        codes.ShouldNotContain("PP-ACTIVE");
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users but invisible to the compiler and to every other test.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var passportType = CreateDocumentType("Passport");
        var employee = CreateEmployee("PP-HDR");
        CreateDocument(employee, passportType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var file = await _appService.GetEmployeePassportReportToExcel(
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
