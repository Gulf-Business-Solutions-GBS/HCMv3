using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.ReportManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

/// <summary>
/// The Print Report picker on a document type: the report a letter of the type prints as is saved, read back,
/// cleared, and offered only from the reports that can actually be printed.
/// </summary>
// Shares the process-wide ported-report list with the print tests, so they never run at once.
[Collection(PortedReportCatalogCollection.Name)]
// ReSharper disable once InconsistentNaming
public class DocumentTypesAppService_PrintReport_Tests : AppTestBase, IDisposable
{
    private readonly IDocumentTypesAppService _documentTypes;

    public DocumentTypesAppService_PrintReport_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _documentTypes = Resolve<IDocumentTypesAppService>();
    }

    public void Dispose()
    {
        PortedReportCatalog.SetPortedReportNames(new string[0]);
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int AddReport(string code, string reportName, bool isActive = true)
    {
        var id = 0;

        UsingDbContext(context =>
        {
            var report = context.Set<PrintReport>().Add(new PrintReport
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + " EN",
                NameAr = code + " AR",
                ReportName = reportName,
                IsActive = isActive,
                ReportCategory = ReportCategory.Default
            }).Entity;

            context.SaveChanges();
            id = report.Id;
        });

        return id;
    }

    private DocumentType Stored(string nameEn) =>
        UsingDbContext(context => context.DocumentTypes.Single(t => t.NameEn == nameEn));

    [Fact]
    public async Task Should_Save_The_Print_Report_On_Create()
    {
        var reportId = AddReport("DTP010", "DtpSalaryCertificateEn");

        await _documentTypes.CreateOrEdit(new CreateOrEditDocumentTypeDto
        {
            NameEn = "Print report create",
            NameAr = "Print report create ar",
            IsLetterType = true,
            PrintReportId = reportId
        });

        Stored("Print report create").PrintReportId.ShouldBe(reportId);
    }

    [Fact]
    public async Task A_New_Type_Should_Have_No_Print_Report_Unless_One_Is_Chosen()
    {
        await _documentTypes.CreateOrEdit(new CreateOrEditDocumentTypeDto
        {
            NameEn = "Print report default",
            NameAr = "Print report default ar"
        });

        Stored("Print report default").PrintReportId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Change_And_Clear_The_Print_Report_On_Edit()
    {
        var first = AddReport("DTP020", "DtpFirst");
        var second = AddReport("DTP021", "DtpSecond");

        await _documentTypes.CreateOrEdit(new CreateOrEditDocumentTypeDto
        {
            NameEn = "Print report edit",
            NameAr = "Print report edit ar",
            PrintReportId = first
        });

        var id = Stored("Print report edit").Id;
        var edit = (await _documentTypes.GetDocumentTypeForEdit(new EntityDto(id))).DocumentType;

        edit.PrintReportId.ShouldBe(first);

        edit.PrintReportId = second;
        await _documentTypes.CreateOrEdit(edit);

        Stored("Print report edit").PrintReportId.ShouldBe(second);

        edit.PrintReportId = null;
        await _documentTypes.CreateOrEdit(edit);

        Stored("Print report edit").PrintReportId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Refuse_A_Print_Report_That_Does_Not_Exist()
    {
        await Should.ThrowAsync<UserFriendlyException>(() => _documentTypes.CreateOrEdit(new CreateOrEditDocumentTypeDto
        {
            NameEn = "Print report missing",
            NameAr = "Print report missing ar",
            PrintReportId = 987654
        }));
    }

    [Fact]
    public async Task Should_Return_The_Print_Report_In_The_List()
    {
        var reportId = AddReport("DTP030", "DtpListed");

        await _documentTypes.CreateOrEdit(new CreateOrEditDocumentTypeDto
        {
            NameEn = "Print report list",
            NameAr = "Print report list ar",
            PrintReportId = reportId
        });

        var listed = (await _documentTypes.GetAll(new GetAllDocumentTypesInput { Filter = "Print report list", MaxResultCount = 10 }))
            .Items.Single();

        listed.PrintReportId.ShouldBe(reportId);
        listed.PrintReportNameEn.ShouldBe("DTP030 EN");
        listed.PrintReportNameAr.ShouldBe("DTP030 AR");
    }

    [Fact]
    public async Task The_Picker_Should_Offer_Only_Reports_That_Can_Be_Printed()
    {
        var printable = AddReport("DTP040", "DtpPrintable");
        var inactive = AddReport("DTP041", "DtpInactive", isActive: false);
        var notPorted = AddReport("DTP042", "DtpNotPorted");
        PortedReportCatalog.SetPortedReportNames(new[] { "DtpPrintable", "DtpInactive" });

        var offered = (await _documentTypes.GetPrintReportsForLookup()).Items.Select(r => r.Id).ToList();

        offered.ShouldContain(printable);
        offered.ShouldNotContain(inactive);
        offered.ShouldNotContain(notPorted);
    }
}
