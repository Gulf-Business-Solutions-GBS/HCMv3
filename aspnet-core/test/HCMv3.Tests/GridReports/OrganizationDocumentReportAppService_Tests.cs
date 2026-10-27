using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.GridReports.Organization;
using HCMv3.GridReports.Organization.Dto;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Organization Document grid-report port (legacy <c>uscRptOrganizationDocument.ascx</c>,
/// <c>Table1()</c>, ascx.cs:484-586).
/// </summary>
// ReSharper disable once InconsistentNaming
public class OrganizationDocumentReportAppService_Tests : AppTestBase
{
    private readonly IOrganizationDocumentReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;
    private int _nextOrgUnitCodeNumber = 1;

    public OrganizationDocumentReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IOrganizationDocumentReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string NewCode() => Guid.NewGuid().ToString("N").Substring(0, 8);

    private long CreateOrgUnit(string displayName)
    {
        long organizationUnitId = 0;

        UsingDbContext(context =>
        {
            var unit = context.Set<OrganizationUnit>().Add(new OrganizationUnit(TenantId, displayName)
            {
                Code = OrganizationUnit.CreateCode(++_nextOrgUnitCodeNumber)
            }).Entity;
            context.SaveChanges();
            organizationUnitId = unit.Id;
        });

        return organizationUnitId;
    }

    private DocumentType CreateDocumentType()
    {
        DocumentType documentType = null;
        UsingDbContext(context =>
        {
            documentType = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = TenantId,
                Code = NewCode(),
                NameEn = "Trade License",
                NameAr = "رخصة تجارية"
            }).Entity;
        });

        return documentType;
    }

    private OrganizationDocument CreateDocument(
        long organizationUnitId,
        DocumentType documentType,
        DateTime issueDate,
        DateTime expiryDate)
    {
        OrganizationDocument document = null;
        UsingDbContext(context =>
        {
            document = context.Set<OrganizationDocument>().Add(new OrganizationDocument
            {
                TenantId = TenantId,
                OrganizationUnitId = organizationUnitId,
                DocumentTypeId = documentType.Id,
                DocumentNum = NewCode(),
                IssueDate = issueDate,
                ExpiryDate = expiryDate,
                DocumentPlace = "Kuwait",
                Comments = "Test document"
            }).Entity;
        });

        return document;
    }

    private static GetOrganizationDocumentReportInput Input(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? documentTypeId = null,
        int maxResultCount = 100)
    {
        return new GetOrganizationDocumentReportInput
        {
            FromDate = fromDate,
            ToDate = toDate,
            DocumentTypeId = documentTypeId,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_A_Document_Whose_Range_Overlaps_The_Filter_Range()
    {
        var orgUnit = CreateOrgUnit("Org-" + NewCode());
        var documentType = CreateDocumentType();
        var today = DateTime.Now.Date;
        CreateDocument(orgUnit, documentType, today.AddDays(-10), today.AddDays(10));

        var result = await _appService.GetOrganizationDocumentReport(
            Input(fromDate: today, toDate: today, documentTypeId: documentType.Id));

        result.Items.Single().DocumentTypeName.ShouldBe("Trade License");
    }

    [Fact]
    public async Task Should_Filter_By_DocumentType()
    {
        var orgUnit = CreateOrgUnit("Org-" + NewCode());
        var matchingType = CreateDocumentType();
        var otherType = CreateDocumentType();
        var today = DateTime.Now.Date;

        CreateDocument(orgUnit, matchingType, today.AddDays(-1), today.AddDays(1));
        CreateDocument(orgUnit, otherType, today.AddDays(-1), today.AddDays(1));

        var result = await _appService.GetOrganizationDocumentReport(
            Input(fromDate: today, toDate: today, documentTypeId: matchingType.Id));

        result.Items.Single().DocumentTypeName.ShouldBe(matchingType.NameEn);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var orgUnit = CreateOrgUnit("Org-" + NewCode());
        var documentType = CreateDocumentType();
        var today = DateTime.Now.Date;

        CreateDocument(orgUnit, documentType, today.AddDays(-1), today.AddDays(1));
        CreateDocument(orgUnit, documentType, today.AddDays(-1), today.AddDays(1));
        CreateDocument(orgUnit, documentType, today.AddDays(-1), today.AddDays(1));

        var file = await _appService.GetOrganizationDocumentReportToExcel(
            Input(fromDate: today, toDate: today, documentTypeId: documentType.Id, maxResultCount: 1));

        file.FileName.ShouldBe("OrganizationDocument.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
    }

    /// <summary>
    /// Pins the header-resolution requirement: every exported column's localization key must resolve to
    /// real text, never render as the literal "[Key]" ABP produces for a missing key.
    /// </summary>
    [Fact]
    public async Task Should_Not_Render_Any_Exported_Header_As_A_Literal_Localization_Key()
    {
        var orgUnit = CreateOrgUnit("Org-" + NewCode());
        var documentType = CreateDocumentType();
        var today = DateTime.Now.Date;
        CreateDocument(orgUnit, documentType, today.AddDays(-1), today.AddDays(1));

        var file = await _appService.GetOrganizationDocumentReportToExcel(
            Input(fromDate: today, toDate: today, documentTypeId: documentType.Id));

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
