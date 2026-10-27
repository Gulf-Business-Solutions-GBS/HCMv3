using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Workflow;
using HCMv3.GridReports.Workflow.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.Workflow;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Leave Request grid-report port (legacy <c>uscRptLeaveRequest.ascx</c>, <c>Table1()</c>).
/// Legacy unioned five concrete workflow-request subclasses; this port instead filters the standalone
/// <see cref="WorkflowRequest"/> table by <c>RequestType</c> and derives ActionType from it -- see
/// <see cref="LeaveRequestReportRowDto"/> remarks. Most tests here pin that RequestType-to-ActionType
/// mapping and the RequestType-set filter itself.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LeaveRequestReportAppService_Tests : AppTestBase
{
    private readonly ILeaveRequestReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public LeaveRequestReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ILeaveRequestReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string NewCode() => Guid.NewGuid().ToString("N").Substring(0, 8);

    private Employee CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = NewCode(),
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
                JoiningDate = new DateTime(2015, 1, 1),
                NationalityId = nationality.Id,
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    private WorkflowRequest CreateRequest(
        Employee employee,
        string requestType,
        RequestStatus status = RequestStatus.Open,
        string requestNumber = "1001")
    {
        WorkflowRequest request = null;
        UsingDbContext(context =>
        {
            request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                RequestType = requestType,
                RequestNumber = requestNumber,
                RequestStatus = status,
                WorkflowStep = 1
            }).Entity;
        });

        return request;
    }

    private static GetLeaveRequestReportInput Input(
        string employeeCode = null,
        RequestStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetLeaveRequestReportInput
        {
            EmployeeCode = employeeCode,
            RequestStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Theory]
    [InlineData("LeaveRequest", "Create")]
    [InlineData("LeaveAmendmentRequest", "Amendment")]
    [InlineData("LeaveCancellationRequest", "Cancellation")]
    [InlineData("ReturnFromLeaveRequest", "ReturnFromLeave")]
    public async Task Should_Derive_ActionType_From_RequestType(string requestType, string expectedActionType)
    {
        var employee = CreateEmployee("ELV-" + requestType);
        CreateRequest(employee, requestType);

        var result = await _appService.GetLeaveRequestReport(Input(employeeCode: employee.Code));

        result.Items.Single().ActionType.ShouldBe(expectedActionType);
    }

    [Fact]
    public async Task Should_Leave_ActionType_Empty_For_AbsenceRequest()
    {
        // Legacy read the concrete row's own ActionType field for AbsenceRequest, which does not exist on
        // the standalone WorkflowRequest -- see the class remarks.
        var employee = CreateEmployee("ELV-ABSENCE");
        CreateRequest(employee, "AbsenceRequest");

        var result = await _appService.GetLeaveRequestReport(Input(employeeCode: "ELV-ABSENCE"));

        result.Items.Single().ActionType.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Should_Only_Include_The_Five_Leave_Related_RequestTypes()
    {
        var employee = CreateEmployee("ELV-SCOPE");
        CreateRequest(employee, "LeaveRequest", requestNumber: "L1");
        CreateRequest(employee, "AssetRequest", requestNumber: "L2");
        CreateRequest(employee, "LoanRequest", requestNumber: "L3");

        var result = await _appService.GetLeaveRequestReport(Input(employeeCode: "ELV-SCOPE"));

        result.Items.Select(i => i.RequestNumber).ShouldBe(new List<string> { "L1" });
    }

    [Fact]
    public async Task Should_Filter_By_RequestStatus()
    {
        var employee = CreateEmployee("ELV-STATUS");
        CreateRequest(employee, "LeaveRequest", status: RequestStatus.Approved, requestNumber: "S1");
        CreateRequest(employee, "LeaveRequest", status: RequestStatus.Rejected, requestNumber: "S2");

        var result = await _appService.GetLeaveRequestReport(
            Input(employeeCode: "ELV-STATUS", status: RequestStatus.Approved));

        result.Items.Single().RequestNumber.ShouldBe("S1");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("ELVEXP");
        CreateRequest(employee, "LeaveRequest", requestNumber: "E1");
        CreateRequest(employee, "AbsenceRequest", requestNumber: "E2");
        CreateRequest(employee, "LeaveCancellationRequest", requestNumber: "E3");

        var file = await _appService.GetLeaveRequestReportToExcel(
            Input(employeeCode: "ELVEXP", maxResultCount: 1));

        file.FileName.ShouldBe("LeaveRequest.xlsx");
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
        var employee = CreateEmployee("ELVHDR");
        CreateRequest(employee, "LeaveRequest");

        var file = await _appService.GetLeaveRequestReportToExcel(Input(employeeCode: "ELVHDR"));

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
