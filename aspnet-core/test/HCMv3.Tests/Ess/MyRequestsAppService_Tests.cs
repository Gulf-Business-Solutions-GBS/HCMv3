using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Covers the central Requests screen -- the requester's side of workflow, as opposed to the approver's
/// inbox.
///
/// <para>
/// The behaviour worth protecting hardest is the scoping: this is the one endpoint that reads every request
/// type at once, so a leak here is a leak of everything. The employee id comes from the session, and the
/// first test exists to make sure it stays that way.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyRequestsAppService_Tests : AppTestBase
{
    private readonly IMyRequestsAppService _myRequests;

    public MyRequestsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _myRequests = Resolve<IMyRequestsAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateEmployee(string code, long? userId = null)
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
                FirstNameEn = code,
                LastNameEn = "T",
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code + " T",
                FullNameAr = "ت ت",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                UserId = userId,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    /// <summary>Links an employee to the signed-in user, which is how the service resolves "me".</summary>
    private int CreateCurrentUserEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    private int CreateRequest(
        int employeeId,
        string requestType = "EmployeeAbsence",
        RequestStatus status = RequestStatus.Open,
        string summary = "Annual Leave, 3 day(s)",
        string requestNumber = null,
        DateTime? createdOn = null)
    {
        WorkflowRequest request = null;
        UsingDbContext(context =>
        {
            request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                RequestType = requestType,
                RequestNumber = requestNumber ?? "REQ-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                RequestStatus = status,
                WorkflowStep = 1,
                SummaryEn = summary,
                SummaryAr = summary
            }).Entity;
        });

        var requestId = request.Id;

        // CreationTime is set by the audit interceptor on insert, so a controlled date has to be written
        // afterwards. Every date-range assertion depends on this, hence the separate round trip rather than
        // setting it on the new entity and hoping the interceptor leaves it alone.
        if (createdOn.HasValue)
        {
            UsingDbContext(context =>
            {
                var row = context.Set<WorkflowRequest>().Single(r => r.Id == requestId);
                row.CreationTime = createdOn.Value;
            });
        }

        return requestId;
    }

    private static GetMyRequestListInput Input(Action<GetMyRequestListInput> configure = null)
    {
        var input = new GetMyRequestListInput { MaxResultCount = 100 };
        configure?.Invoke(input);
        return input;
    }

    // ---------------------------------------------------------------------------------------------------
    // Scoping
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Return_Only_My_Own_Requests()
    {
        var me = CreateCurrentUserEmployee("ME-1");
        var someoneElse = CreateEmployee("OTHER-1");

        var mine = CreateRequest(me, summary: "Mine");
        CreateRequest(someoneElse, summary: "Theirs");

        var result = await _myRequests.GetMyRequests(Input());

        result.TotalCount.ShouldBe(1);
        result.Items.Single().Id.ShouldBe(mine);
        result.Items.Single().SummaryEn.ShouldBe("Mine");
    }

    /// <summary>
    /// A signed-in user with no employee record -- the tenant admin, by design -- gets an empty screen, not
    /// an exception. This screen is a landing page; failing it would make the admin think it was broken.
    /// </summary>
    [Fact]
    public async Task Should_Return_Empty_When_User_Has_No_Linked_Employee()
    {
        var someoneElse = CreateEmployee("OTHER-2");
        CreateRequest(someoneElse);

        var result = await _myRequests.GetMyRequests(Input());

        result.TotalCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Return_Empty_Counts_And_Types_When_User_Has_No_Linked_Employee()
    {
        var someoneElse = CreateEmployee("OTHER-3");
        CreateRequest(someoneElse);

        (await _myRequests.GetMyRequestStatusCounts()).ShouldBeEmpty();
        (await _myRequests.GetMyRequestTypes()).ShouldBeEmpty();
    }

    // ---------------------------------------------------------------------------------------------------
    // Status filter -- the filter the user actually asked for, one case per status
    // ---------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(RequestStatus.Open)]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Closed)]
    [InlineData(RequestStatus.Draft)]
    [InlineData(RequestStatus.Transferred)]
    public async Task Should_Filter_By_Status(RequestStatus status)
    {
        var me = CreateCurrentUserEmployee("ME-STATUS-" + (int)status);

        foreach (RequestStatus each in Enum.GetValues(typeof(RequestStatus)))
        {
            CreateRequest(me, status: each, summary: each.ToString());
        }

        var result = await _myRequests.GetMyRequests(Input(i => i.Status = status));

        result.TotalCount.ShouldBe(1);
        result.Items.Single().RequestStatus.ShouldBe(status);
    }

    [Fact]
    public async Task Should_Return_Every_Status_When_No_Status_Filter_Given()
    {
        var me = CreateCurrentUserEmployee("ME-ALL");

        CreateRequest(me, status: RequestStatus.Open);
        CreateRequest(me, status: RequestStatus.Draft);
        CreateRequest(me, status: RequestStatus.Approved);

        var result = await _myRequests.GetMyRequests(Input());

        result.TotalCount.ShouldBe(3);
    }

    // ---------------------------------------------------------------------------------------------------
    // Type filter
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Filter_By_Request_Type()
    {
        var me = CreateCurrentUserEmployee("ME-TYPE");

        CreateRequest(me, requestType: "EmployeeAbsence", summary: "Leave");
        CreateRequest(me, requestType: "EmployeeExitPermit", summary: "Permit");

        var result = await _myRequests.GetMyRequests(Input(i => i.RequestType = "EmployeeExitPermit"));

        result.TotalCount.ShouldBe(1);
        result.Items.Single().SummaryEn.ShouldBe("Permit");
    }

    /// <summary>
    /// The grid must be able to show "Exit Permit", not "EmployeeExitPermit". That display name comes from
    /// the catalog, which is the whole reason the service joins it.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Type_Display_Name_And_Route_From_The_Catalog()
    {
        var me = CreateCurrentUserEmployee("ME-CATALOG");
        CreateRequest(me, requestType: "EmployeeExitPermit");

        var item = (await _myRequests.GetMyRequests(Input())).Items.Single();

        item.RequestTypeNameEn.ShouldBe("Exit Permit");
        item.RequestTypeNameAr.ShouldNotBeNullOrWhiteSpace();
        item.EssRoute.ShouldBe("/app/main/ess/exit-permit");
    }

    /// <summary>A type with no catalog row still lists, rather than vanishing from the employee's history.</summary>
    [Fact]
    public async Task Should_Still_List_A_Request_Whose_Type_Has_No_Catalog_Row()
    {
        var me = CreateCurrentUserEmployee("ME-UNKNOWN");
        CreateRequest(me, requestType: "SomethingNotInTheCatalog");

        var item = (await _myRequests.GetMyRequests(Input())).Items.Single();

        item.RequestType.ShouldBe("SomethingNotInTheCatalog");
        item.RequestTypeNameEn.ShouldBeNull();
    }

    // ---------------------------------------------------------------------------------------------------
    // Date range
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Filter_By_Date_Range()
    {
        var me = CreateCurrentUserEmployee("ME-DATES");

        CreateRequest(me, summary: "Before", createdOn: new DateTime(2026, 1, 5, 9, 0, 0));
        CreateRequest(me, summary: "Inside", createdOn: new DateTime(2026, 2, 10, 9, 0, 0));
        CreateRequest(me, summary: "After", createdOn: new DateTime(2026, 3, 20, 9, 0, 0));

        var result = await _myRequests.GetMyRequests(Input(i =>
        {
            i.FromDate = new DateTime(2026, 2, 1);
            i.ToDate = new DateTime(2026, 2, 28);
        }));

        result.TotalCount.ShouldBe(1);
        result.Items.Single().SummaryEn.ShouldBe("Inside");
    }

    /// <summary>
    /// The end of the range is a whole day, not midnight. A request raised at 16:00 on the last day of the
    /// range is inside it -- getting this wrong hides today's requests from a range ending today, which is
    /// the range people pick most.
    /// </summary>
    [Fact]
    public async Task Should_Include_Requests_Raised_Later_In_The_Day_The_Range_Ends()
    {
        var me = CreateCurrentUserEmployee("ME-ENDDAY");

        CreateRequest(me, summary: "Late that day", createdOn: new DateTime(2026, 2, 28, 16, 30, 0));

        var result = await _myRequests.GetMyRequests(Input(i =>
        {
            i.FromDate = new DateTime(2026, 2, 1);
            i.ToDate = new DateTime(2026, 2, 28);
        }));

        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Filter_By_From_Date_Alone()
    {
        var me = CreateCurrentUserEmployee("ME-FROM");

        CreateRequest(me, summary: "Old", createdOn: new DateTime(2026, 1, 1, 9, 0, 0));
        CreateRequest(me, summary: "New", createdOn: new DateTime(2026, 6, 1, 9, 0, 0));

        var result = await _myRequests.GetMyRequests(Input(i => i.FromDate = new DateTime(2026, 5, 1)));

        result.TotalCount.ShouldBe(1);
        result.Items.Single().SummaryEn.ShouldBe("New");
    }

    // ---------------------------------------------------------------------------------------------------
    // Free text
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Search_By_Request_Number()
    {
        var me = CreateCurrentUserEmployee("ME-NUM");

        CreateRequest(me, requestNumber: "EXP-000123", summary: "Permit");
        CreateRequest(me, requestNumber: "LEA-000999", summary: "Leave");

        var result = await _myRequests.GetMyRequests(Input(i => i.Filter = "000123"));

        result.TotalCount.ShouldBe(1);
        result.Items.Single().RequestNumber.ShouldBe("EXP-000123");
    }

    [Fact]
    public async Task Should_Search_By_Summary()
    {
        var me = CreateCurrentUserEmployee("ME-SUMMARY");

        CreateRequest(me, summary: "Annual Leave, 3 day(s)");
        CreateRequest(me, summary: "Sick Leave, 1 day(s)");

        var result = await _myRequests.GetMyRequests(Input(i => i.Filter = "Sick"));

        result.TotalCount.ShouldBe(1);
        result.Items.Single().SummaryEn.ShouldBe("Sick Leave, 1 day(s)");
    }

    /// <summary>Search must not escape the scope -- it narrows my own list, it does not widen it.</summary>
    [Fact]
    public async Task Should_Not_Search_Beyond_My_Own_Requests()
    {
        var me = CreateCurrentUserEmployee("ME-SCOPED-SEARCH");
        var someoneElse = CreateEmployee("OTHER-SEARCH");

        CreateRequest(someoneElse, summary: "Confidential Loan, 5000");

        var result = await _myRequests.GetMyRequests(Input(i => i.Filter = "Confidential"));

        result.TotalCount.ShouldBe(0);
    }

    // ---------------------------------------------------------------------------------------------------
    // Sorting, paging, counts, type list
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Sort_Newest_First_By_Default()
    {
        var me = CreateCurrentUserEmployee("ME-SORT");

        CreateRequest(me, summary: "Oldest", createdOn: new DateTime(2026, 1, 1, 9, 0, 0));
        CreateRequest(me, summary: "Newest", createdOn: new DateTime(2026, 9, 1, 9, 0, 0));
        CreateRequest(me, summary: "Middle", createdOn: new DateTime(2026, 5, 1, 9, 0, 0));

        var items = (await _myRequests.GetMyRequests(Input())).Items;

        items.Select(i => i.SummaryEn).ShouldBe(new[] { "Newest", "Middle", "Oldest" });
    }

    [Fact]
    public async Task Should_Page_Without_Losing_The_Total()
    {
        var me = CreateCurrentUserEmployee("ME-PAGE");

        for (var i = 0; i < 5; i++)
        {
            CreateRequest(me, summary: "R" + i);
        }

        var result = await _myRequests.GetMyRequests(new GetMyRequestListInput { MaxResultCount = 2, SkipCount = 0 });

        result.TotalCount.ShouldBe(5);
        result.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Count_My_Requests_By_Status()
    {
        var me = CreateCurrentUserEmployee("ME-COUNTS");
        var someoneElse = CreateEmployee("OTHER-COUNTS");

        CreateRequest(me, status: RequestStatus.Open);
        CreateRequest(me, status: RequestStatus.Open);
        CreateRequest(me, status: RequestStatus.Draft);
        CreateRequest(someoneElse, status: RequestStatus.Open);

        var counts = await _myRequests.GetMyRequestStatusCounts();

        counts.Single(c => c.Status == RequestStatus.Open).Count.ShouldBe(2);
        counts.Single(c => c.Status == RequestStatus.Draft).Count.ShouldBe(1);
        counts.ShouldNotContain(c => c.Status == RequestStatus.Approved);
    }

    [Fact]
    public async Task Should_List_Only_The_Types_I_Have_Actually_Raised()
    {
        var me = CreateCurrentUserEmployee("ME-TYPES");
        var someoneElse = CreateEmployee("OTHER-TYPES");

        CreateRequest(me, requestType: "EmployeeAbsence");
        CreateRequest(me, requestType: "EmployeeAbsence");
        CreateRequest(someoneElse, requestType: "EmployeeExitPermit");

        var types = await _myRequests.GetMyRequestTypes();

        types.Count.ShouldBe(1);
        types.Single().RequestType.ShouldBe("EmployeeAbsence");
        types.Single().NameEn.ShouldBe("Leave Request");
        types.Single().Count.ShouldBe(2);
    }
}
