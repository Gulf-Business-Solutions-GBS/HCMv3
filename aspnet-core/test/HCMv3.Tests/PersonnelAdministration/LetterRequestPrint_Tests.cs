using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Uow;
using Abp.UI;
using Castle.MicroKernel.Registration;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Services;
using HCMv3.Storage;
using HCMv3.Tests.Ess;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Printing the letter a request asked for, as legacy's Print Letter action did it: only an approved request
/// prints, the report is found from the document type's English name, and the report is given the employee's
/// code and today's date. Plus the one addition -- the PDF is kept on the request.
///
/// <para>
/// The report engine is not loaded in the tests, so a recording renderer stands in for it: these tests are
/// about which report is asked for, with what, and what is done with the result.
/// </para>
/// </summary>
// Shares the process-wide ported-report list with the print report launcher's tests, so the two never run at once.
[Collection(PortedReportCatalogCollection.Name)]
// ReSharper disable once InconsistentNaming
public class LetterRequestPrint_Tests : EssRequestDraftTestBase, IDisposable
{
    private readonly RecordingRenderer _renderer = new RecordingRenderer();
    private readonly IEmployeeLetterRequestsAppService _letterRequests;
    private readonly ILetterRequestPrintService _printService;
    private readonly IAttachmentManager _attachments;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public LetterRequestPrint_Tests()
    {
        LocalIocManager.IocContainer.Register(
            Component.For<IPrintReportPdfRenderer>().Instance(_renderer).IsDefault());

        _letterRequests = Resolve<IEmployeeLetterRequestsAppService>();
        _printService = Resolve<ILetterRequestPrintService>();
        _attachments = Resolve<IAttachmentManager>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    /// <summary>The files kept on a request, with their content. Read inside a unit of work, as the manager needs.</summary>
    private async Task<List<(string FileName, string Content)>> KeptOnAsync(int letterId)
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            var kept = new List<(string, string)>();

            foreach (var attachment in await _attachments.GetListAsync(LetterRequestPrintService.AttachmentEntityType, letterId.ToString()))
            {
                kept.Add((attachment.FileName, Encoding.ASCII.GetString(await _attachments.ReadContentAsync(attachment))));
            }

            await uow.CompleteAsync();

            return kept;
        }
    }

    public void Dispose()
    {
        PortedReportCatalog.SetPortedReportNames(new string[0]);
    }

    /// <summary>Stands in for the report engine: remembers what it was asked for and returns a small file.</summary>
    private class RecordingRenderer : IPrintReportPdfRenderer
    {
        public string ReportName { get; private set; }

        public IReadOnlyDictionary<string, object> Parameters { get; private set; }

        public int Calls { get; private set; }

        public Task<byte[]> RenderAsync(string reportName, IReadOnlyDictionary<string, object> parameters)
        {
            Calls++;
            ReportName = reportName;
            Parameters = parameters;

            return Task.FromResult(Encoding.ASCII.GetBytes("%PDF-1.4 print " + Calls));
        }
    }

    private void AddReport(string code, string reportName, bool isActive = true)
    {
        UsingDbContext(context =>
        {
            context.Set<PrintReport>().Add(new PrintReport
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + " EN",
                NameAr = code + " AR",
                ReportName = reportName,
                IsActive = isActive,
                ReportCategory = ReportCategory.Default
            });
        });
    }

    private int InsertLetter(int employeeId, int documentTypeId, RequestStatus? status)
    {
        EmployeeLetterRequest letter = null;
        UsingDbContext(context =>
        {
            letter = context.Set<EmployeeLetterRequest>().Add(new EmployeeLetterRequest
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DocumentTypeId = documentTypeId,
                EffectiveDate = DateTime.Today,
                AddressToEn = "National Bank of Kuwait",
                AddressToAr = "بنك الكويت الوطني",
                WorkflowStatus = status
            }).Entity;
        });

        return letter.Id;
    }

    /// <summary>An approved request for a "Zed Letter", with one ported report the tenant holds for it.</summary>
    private int GivenAnApprovedLetter(string employeeCode, string typeName = "Zed Letter", string reportName = "ZedLetter")
    {
        var employeeId = CreateEmployee(employeeCode);
        var typeId = CreateDocumentType(typeName, isLetterType: true);

        AddReport("ZR001", reportName);
        PortedReportCatalog.SetPortedReportNames(new[] { reportName });

        return InsertLetter(employeeId, typeId, RequestStatus.Approved);
    }

    // ---------------------------------------------------------------- who may be printed

    [Theory]
    [InlineData(RequestStatus.Open)]
    [InlineData(RequestStatus.Draft)]
    [InlineData(RequestStatus.Rejected)]
    public async Task Should_Refuse_To_Print_A_Request_That_Is_Not_Approved(RequestStatus status)
    {
        var employeeId = CreateEmployee("LP1" + (int)status);
        var typeId = CreateDocumentType("Zed Letter", isLetterType: true);
        AddReport("ZR001", "ZedLetter");
        PortedReportCatalog.SetPortedReportNames(new[] { "ZedLetter" });

        var letterId = InsertLetter(employeeId, typeId, status);

        await Should.ThrowAsync<UserFriendlyException>(() => _letterRequests.PrintLetter(new EntityDto(letterId)));

        _renderer.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Refuse_To_Print_A_Letter_HR_Recorded_Without_Approval()
    {
        // Legacy printed only RequestStatus.Approved; a row with no workflow status was never approved.
        var employeeId = CreateEmployee("LP150");
        var typeId = CreateDocumentType("Zed Letter", isLetterType: true);
        AddReport("ZR001", "ZedLetter");
        PortedReportCatalog.SetPortedReportNames(new[] { "ZedLetter" });

        var letterId = InsertLetter(employeeId, typeId, null);

        await Should.ThrowAsync<UserFriendlyException>(() => _letterRequests.PrintLetter(new EntityDto(letterId)));
    }

    // ---------------------------------------------------------------- which report, with what

    [Fact]
    public async Task Should_Print_The_Report_Named_For_The_Document_Type_With_The_Legacy_Parameters()
    {
        var letterId = GivenAnApprovedLetter("LP200");

        var printed = await _letterRequests.PrintLetter(new EntityDto(letterId));

        _renderer.ReportName.ShouldBe("ZedLetter");
        _renderer.Parameters.Keys.OrderBy(k => k).ShouldBe(new[] { "PrmDate", "PrmEmpCode" });
        _renderer.Parameters["PrmEmpCode"].ShouldBe("LP200");
        ((DateTime)_renderer.Parameters["PrmDate"]).Date.ShouldBe(DateTime.Today);

        printed.FileName.ShouldEndWith(".pdf");
        Convert.FromBase64String(printed.Base64Content).Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Not_Pass_The_Addressee_To_The_Report()
    {
        // No certificate layout prints it, in legacy or here; it stays on the request for HR to read.
        var letterId = GivenAnApprovedLetter("LP210");

        await _letterRequests.PrintLetter(new EntityDto(letterId));

        _renderer.Parameters.Values.ShouldNotContain("National Bank of Kuwait");
    }

    [Fact]
    public async Task Should_Find_The_Report_From_The_Type_Name_With_Spaces_Removed()
    {
        CreateDocumentType("Zed Salary Certificate", isLetterType: true);
        AddReport("ZR002", "ZedSalaryCertificateEn");
        PortedReportCatalog.SetPortedReportNames(new[] { "ZedSalaryCertificateEn" });

        var report = await _printService.FindReportAsync("Zed Salary Certificate");

        report.ReportName.ShouldBe("ZedSalaryCertificateEn");
    }

    [Fact]
    public async Task Should_Take_The_First_By_Code_Among_Language_Variants()
    {
        AddReport("ZR020", "ZedCertAr");
        AddReport("ZR010", "ZedCertEn");
        PortedReportCatalog.SetPortedReportNames(new[] { "ZedCertAr", "ZedCertEn" });

        var report = await _printService.FindReportAsync("Zed Cert");

        report.ReportName.ShouldBe("ZedCertEn");
    }

    [Fact]
    public async Task Should_Prefer_The_Report_Named_Exactly_For_The_Type()
    {
        AddReport("ZR010", "ZedCertEn");
        AddReport("ZR020", "ZedCert");
        PortedReportCatalog.SetPortedReportNames(new[] { "ZedCertEn", "ZedCert" });

        var report = await _printService.FindReportAsync("Zed Cert");

        report.ReportName.ShouldBe("ZedCert");
    }

    [Fact]
    public async Task Should_Ignore_A_Report_That_Is_Inactive_Or_Not_Ported()
    {
        AddReport("ZR010", "ZedCertEn", isActive: false);
        AddReport("ZR020", "ZedCertAr");
        PortedReportCatalog.SetPortedReportNames(new[] { "ZedCertEn" });

        (await _printService.FindReportAsync("Zed Cert")).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Refuse_When_No_Report_Is_Named_For_The_Document_Type()
    {
        var employeeId = CreateEmployee("LP300");
        var typeId = CreateDocumentType("Zed Unknown Letter", isLetterType: true);
        var letterId = InsertLetter(employeeId, typeId, RequestStatus.Approved);

        var exception = await Should.ThrowAsync<UserFriendlyException>(() =>
            _letterRequests.PrintLetter(new EntityDto(letterId)));

        exception.Message.ShouldContain("Zed Unknown Letter");
        _renderer.Calls.ShouldBe(0);
    }

    // ---------------------------------------------------------------- the report chosen on the document type

    /// <summary>A letter type with a report picked for it on the Document Type screen.</summary>
    private int CreateDocumentTypePrintingAs(string nameEn, string reportName)
    {
        var typeId = CreateDocumentType(nameEn, isLetterType: true);

        UsingDbContext(context =>
        {
            var reportId = context.Set<PrintReport>().Single(r => r.ReportName == reportName).Id;
            context.DocumentTypes.Single(t => t.Id == typeId).PrintReportId = reportId;
        });

        return typeId;
    }

    [Fact]
    public async Task Should_Print_The_Report_Chosen_On_The_Document_Type_Whatever_Its_Name()
    {
        // The name rule would find ZedPickedCertEn; HR chose a report with an unrelated name.
        AddReport("ZR310", "ZedPickedCertEn");
        AddReport("ZR320", "ZedExperienceLetter");
        PortedReportCatalog.SetPortedReportNames(new[] { "ZedPickedCertEn", "ZedExperienceLetter" });

        var employeeId = CreateEmployee("LP310");
        var typeId = CreateDocumentTypePrintingAs("Zed Picked Cert", "ZedExperienceLetter");
        var letterId = InsertLetter(employeeId, typeId, RequestStatus.Approved);

        await _letterRequests.PrintLetter(new EntityDto(letterId));

        _renderer.ReportName.ShouldBe("ZedExperienceLetter");
    }

    [Fact]
    public async Task Should_Refuse_Rather_Than_Fall_Back_To_The_Name_When_The_Chosen_Report_Is_Inactive()
    {
        AddReport("ZR330", "ZedSwitchedCertEn");
        AddReport("ZR340", "ZedSwitchedOff", isActive: false);
        PortedReportCatalog.SetPortedReportNames(new[] { "ZedSwitchedCertEn", "ZedSwitchedOff" });

        var employeeId = CreateEmployee("LP320");
        var typeId = CreateDocumentTypePrintingAs("Zed Switched Cert", "ZedSwitchedOff");
        var letterId = InsertLetter(employeeId, typeId, RequestStatus.Approved);

        await Should.ThrowAsync<UserFriendlyException>(() => _letterRequests.PrintLetter(new EntityDto(letterId)));

        _renderer.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Refuse_When_The_Chosen_Report_Is_Not_In_This_Build()
    {
        AddReport("ZR350", "ZedNotPorted");
        PortedReportCatalog.SetPortedReportNames(new string[0]);

        var employeeId = CreateEmployee("LP330");
        var typeId = CreateDocumentTypePrintingAs("Zed Missing Cert", "ZedNotPorted");
        var letterId = InsertLetter(employeeId, typeId, RequestStatus.Approved);

        await Should.ThrowAsync<UserFriendlyException>(() => _letterRequests.PrintLetter(new EntityDto(letterId)));

        _renderer.Calls.ShouldBe(0);
    }

    // ---------------------------------------------------------------- produced at approval

    private async Task<(int LetterId, int WorkflowRequestId, int ApproverId)> GivenALetterAwaitingApprovalAsync(
        string employeeCode, string typeName)
    {
        CreateCurrentUserEmployee(employeeCode);
        var approverId = EnableWorkflowFor("EmployeeLetterRequest", employeeCode + "APP");
        var typeId = CreateDocumentType(typeName, isLetterType: true);

        var result = await Resolve<HCMv3.Ess.IMyLetterRequestAppService>().RequestLetter(new HCMv3.Ess.Dto.RequestLetterInput
        {
            DocumentTypeId = typeId,
            EffectiveDate = DateTime.Today,
            AddressToEn = "National Bank of Kuwait",
            AddressToAr = "بنك الكويت الوطني"
        });

        return (result.TransactionId, result.WorkflowRequestId.Value, approverId);
    }

    [Fact]
    public async Task Approving_A_Letter_Request_Should_Produce_The_Letter_And_Keep_It_On_The_Request()
    {
        AddReport("ZR600", "ZedAutoCertEn");
        PortedReportCatalog.SetPortedReportNames(new[] { "ZedAutoCertEn" });
        var letter = await GivenALetterAwaitingApprovalAsync("LP600", "Zed Auto Cert");

        (await KeptOnAsync(letter.LetterId)).ShouldBeEmpty();

        await Resolve<HCMv3.Workflow.Services.IWorkflowApprovalService>()
            .ApproveAsync(letter.WorkflowRequestId, letter.ApproverId, "ok");

        _renderer.ReportName.ShouldBe("ZedAutoCertEn");
        (await KeptOnAsync(letter.LetterId)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Approval_Should_Still_Go_Through_When_The_Letter_Cannot_Be_Produced()
    {
        // No report is named for the type, so there is nothing to print.
        var letter = await GivenALetterAwaitingApprovalAsync("LP610", "Zed Reportless Letter");

        await Resolve<HCMv3.Workflow.Services.IWorkflowApprovalService>()
            .ApproveAsync(letter.WorkflowRequestId, letter.ApproverId, "ok");

        RequestRow(letter.WorkflowRequestId).RequestStatus.ShouldBe(RequestStatus.Approved);
        _renderer.Calls.ShouldBe(0);
        (await KeptOnAsync(letter.LetterId)).ShouldBeEmpty();
    }

    // ---------------------------------------------------------------- the copy kept on the request

    [Fact]
    public async Task Should_Keep_The_Pdf_On_The_Request()
    {
        var letterId = GivenAnApprovedLetter("LP400");

        var printed = await _letterRequests.PrintLetter(new EntityDto(letterId));

        printed.SavedOnRequest.ShouldBeTrue();

        var kept = await KeptOnAsync(letterId);

        kept.Count.ShouldBe(1);
        kept[0].FileName.ShouldBe(printed.FileName);
    }

    [Fact]
    public async Task Printing_Again_Should_Replace_The_Earlier_Copy_Rather_Than_Add_Another()
    {
        var letterId = GivenAnApprovedLetter("LP410");

        await _letterRequests.PrintLetter(new EntityDto(letterId));
        await _letterRequests.PrintLetter(new EntityDto(letterId));

        var kept = await KeptOnAsync(letterId);

        kept.Count.ShouldBe(1);
        kept[0].Content.ShouldEndWith("print 2");
    }

    // ---------------------------------------------------------------- the list HR prints from

    [Fact]
    public async Task The_List_Should_Offer_Print_Only_On_An_Approved_Request()
    {
        var employeeId = CreateEmployee("LP500");
        var typeId = CreateDocumentType("Zed Letter", isLetterType: true);
        var approved = InsertLetter(employeeId, typeId, RequestStatus.Approved);
        var open = InsertLetter(employeeId, typeId, RequestStatus.Open);

        var rows = (await _letterRequests.GetAll(new HCMv3.PersonnelAdministration.Dto.GetAllEmployeeLetterRequestsInput
        {
            EmployeeId = employeeId,
            IncludePending = true,
            MaxResultCount = 20
        })).Items;

        rows.Single(r => r.Id == approved).CanPrint.ShouldBeTrue();
        rows.Single(r => r.Id == open).CanPrint.ShouldBeFalse();
    }
}
