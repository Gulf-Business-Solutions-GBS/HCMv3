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
/// Tests for the Employee Requests grid-report port (legacy <c>uscRptEmployeeRequests.ascx</c>,
/// <c>Table1()</c>). HCMv3's standalone <see cref="WorkflowRequest"/> has no concrete-subclass hierarchy
/// (see its class remarks), so LeaveType/WorkflowComments/DelegatedEmployee are not exercised here -- see
/// <see cref="EmployeeRequestsReportRowDto"/> remarks for why.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeRequestsReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeRequestsReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeRequestsReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeRequestsReportAppService>();
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
        string requestType = "SomeRequest",
        RequestStatus status = RequestStatus.Open,
        int workflowStep = 1,
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
                WorkflowStep = workflowStep
            }).Entity;
        });

        return request;
    }

    private void AddApprover(WorkflowRequest request, Employee approver)
    {
        UsingDbContext(context =>
        {
            context.Set<WorkFlowApprover>().Add(new WorkFlowApprover
            {
                TenantId = TenantId,
                WorkflowRequestId = request.Id,
                ApproverId = approver.Id
            });
        });
    }

    private static GetEmployeeRequestsReportInput Input(
        string employeeCode = null,
        RequestStatus? status = null,
        string approverCode = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeRequestsReportInput
        {
            EmployeeCode = employeeCode,
            RequestStatus = status,
            ApproverCode = approverCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_A_Request_Created_Today_By_Default()
    {
        var employee = CreateEmployee("EREQ-BASIC");
        CreateRequest(employee, requestType: "AssetRequest", requestNumber: "5001");

        var result = await _appService.GetEmployeeRequestsReport(Input(employeeCode: "EREQ-BASIC"));

        var row = result.Items.Single();
        row.RequestNumber.ShouldBe("5001");
        row.RequestType.ShouldBe("AssetRequest");
        row.EmployeeCode.ShouldBe("EREQ-BASIC");
    }

    [Fact]
    public async Task Should_Filter_By_RequestStatus()
    {
        var employee = CreateEmployee("EREQ-STATUS");
        CreateRequest(employee, status: RequestStatus.Approved);
        CreateRequest(employee, status: RequestStatus.Rejected);

        var result = await _appService.GetEmployeeRequestsReport(
            Input(employeeCode: "EREQ-STATUS", status: RequestStatus.Approved));

        result.Items.Single().RequestStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task Should_Join_Approver_Codes_And_Names_With_A_Comma()
    {
        var employee = CreateEmployee("EREQ-APPR");
        var approver1 = CreateEmployee("EREQ-APPR-1");
        var approver2 = CreateEmployee("EREQ-APPR-2");
        var request = CreateRequest(employee);
        AddApprover(request, approver1);
        AddApprover(request, approver2);

        var result = await _appService.GetEmployeeRequestsReport(Input(employeeCode: "EREQ-APPR"));

        var row = result.Items.Single();
        row.ApproverNames.ShouldContain("EREQ-APPR-1");
        row.ApproverNames.ShouldContain("EREQ-APPR-2");
        row.ApproverNames.ShouldContain(", ");
    }

    [Fact]
    public async Task Should_Filter_By_Approver_Code()
    {
        var employee = CreateEmployee("EREQ-APPRF");
        var matchingApprover = CreateEmployee("EREQ-APPRF-MATCH");
        var otherApprover = CreateEmployee("EREQ-APPRF-OTHER");

        var matchingRequest = CreateRequest(employee, requestNumber: "7001");
        AddApprover(matchingRequest, matchingApprover);

        var otherRequest = CreateRequest(employee, requestNumber: "7002");
        AddApprover(otherRequest, otherApprover);

        var result = await _appService.GetEmployeeRequestsReport(
            Input(employeeCode: "EREQ-APPRF", approverCode: "EREQ-APPRF-MATCH"));

        result.Items.Single().RequestNumber.ShouldBe("7001");
    }

    [Fact]
    public async Task Should_Not_Return_A_Request_With_No_Employee()
    {
        // Legacy line 530: r.Employee != null is an unconditional part of the base query.
        UsingDbContext(context =>
        {
            context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = null,
                RequestType = "Orphan",
                RequestStatus = RequestStatus.Open,
                WorkflowStep = 1
            });
        });

        var result = await _appService.GetEmployeeRequestsReport(Input());

        result.Items.ShouldNotContain(r => r.RequestType == "Orphan");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("EREQEXP");
        CreateRequest(employee, requestNumber: "A1");
        CreateRequest(employee, requestNumber: "A2");
        CreateRequest(employee, requestNumber: "A3");

        var file = await _appService.GetEmployeeRequestsReportToExcel(
            Input(employeeCode: "EREQEXP", maxResultCount: 1));

        file.FileName.ShouldBe("EmployeeRequests.xlsx");
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
        var employee = CreateEmployee("EREQHDR");
        CreateRequest(employee);

        var file = await _appService.GetEmployeeRequestsReportToExcel(Input(employeeCode: "EREQHDR"));

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
