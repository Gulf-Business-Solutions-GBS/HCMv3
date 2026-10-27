using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Organizations;
using HCMv3.AiAgent;
using HCMv3.AiAgent.Dto;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AiAgent;

/// <summary>
/// The employee master-data tools: manager, contract, organisational assignment, contact details, duty, and the
/// read-only job details and punches. Each change is drafted by the tool and only saved on Confirm, through the
/// screen's own service.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AiAgentAdminEmployeeTools_Tests : AiAgentTestBase
{
    private static readonly DateTime January = new(2026, 1, 1);
    private static readonly DateTime October = new(2026, 10, 1);
    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly int _employeeId;
    private readonly int _unitTypeId;

    public AiAgentAdminEmployeeTools_Tests()
    {
        _employeeId = CreateEmployee("1002");

        OrganizationUnitType unitType = null;
        UsingDbContext(context => unitType = context.Set<OrganizationUnitType>().Add(new OrganizationUnitType
        {
            TenantId = TenantId, Code = OrganizationUnitType.Level1Code, NameEn = "Department", NameAr = "قسم"
        }).Entity);
        _unitTypeId = unitType.Id;
    }

    private async Task<SendAiMessageOutput> Ask(string tool, string argumentsJson)
    {
        Model.CallsTool(tool, argumentsJson).RepliesWith("Please confirm below.");
        return await Service.SendMessage(new SendAiMessageInput { Message = "please" });
    }

    private string LastToolResult() => Model.Requests.Last().Messages.Last().Content;

    private async Task<AiActionResultDto> Confirm(SendAiMessageOutput output)
    {
        output.AssistantMessage.PendingAction.ShouldNotBeNull(LastToolResult());
        return await Service.ConfirmAction(new EntityDto(output.AssistantMessage.PendingAction.Id));
    }

    private static void ShouldHaveCompleted(AiActionResultDto result) =>
        result.Action.Status.ShouldBe(AiPendingActionStatus.Completed, result.Action.ResultMessage);

    private static string Detail(AiPendingActionDto card, string label) =>
        card.Details.SingleOrDefault(d => d.Label == label)?.Value
        ?? throw new ShouldAssertException($"No '{label}' on the card. It has: {string.Join(", ", card.Details.Select(d => d.Label + "=" + d.Value))}");

    // ------------------------------------------------------------------ manager

    [Fact]
    public async Task A_Manager_Change_Should_Add_A_Record_And_End_The_Previous_One_On_Confirm()
    {
        var oldManager = CreateActiveEmployee("2001");
        CreateActiveEmployee("2002");
        CreateManagerRecord(_employeeId, oldManager, January);

        var output = await Ask("prepare_manager_change", @"{""employeeCode"":""1002"",""managerCode"":""2002"",""effectiveDate"":""2026-10-01""}");

        var card = output.AssistantMessage.PendingAction;
        card.Title.ShouldBe("Change manager");
        Detail(card, "Manager").ShouldBe("2001 · 2001 T → 2002 · 2002 T");
        Detail(card, "Current record ends").ShouldBe("30 Sep 2026");

        ShouldHaveCompleted(await Confirm(output));

        UsingDbContext(context =>
        {
            var records = context.Set<EmployeeManager>().Where(m => m.EmployeeId == _employeeId).OrderBy(m => m.StartDate).ToList();
            records.Count.ShouldBe(2);
            records[0].EndDate.ShouldBe(new DateTime(2026, 9, 30));
            records[1].StartDate.ShouldBe(October);
            records[1].EndDate.ShouldBe(OpenEnded);
        });
    }

    [Fact]
    public async Task An_Employee_Should_Not_Become_Their_Own_Manager()
    {
        var output = await Ask("prepare_manager_change", @"{""employeeCode"":""1002"",""managerCode"":""1002"",""effectiveDate"":""2026-10-01""}");

        output.AssistantMessage.PendingAction.ShouldBeNull();
        LastToolResult().ShouldContain("cannot be their own manager");
    }

    [Fact]
    public async Task A_Manager_Change_That_Would_Make_A_Loop_Should_Be_Refused()
    {
        var teamLead = CreateActiveEmployee("2003");
        CreateManagerRecord(teamLead, _employeeId, January); // 2003 reports to 1002

        var output = await Ask("prepare_manager_change", @"{""employeeCode"":""1002"",""managerCode"":""2003"",""effectiveDate"":""2026-10-01""}");

        output.AssistantMessage.PendingAction.ShouldBeNull();
        LastToolResult().ShouldContain("would create a loop");
    }

    [Fact]
    public async Task A_Manager_Change_Should_Refuse_On_Confirm_When_The_Records_Changed_Meanwhile()
    {
        CreateActiveEmployee("2004");
        var output = await Ask("prepare_manager_change", @"{""employeeCode"":""1002"",""managerCode"":""2004"",""effectiveDate"":""2026-10-01""}");

        CreateManagerRecord(_employeeId, CreateActiveEmployee("2005"), new DateTime(2026, 11, 1)); // someone else, on the screen

        var result = await Confirm(output);

        result.Action.Status.ShouldBe(AiPendingActionStatus.Failed);
        result.Action.ResultMessage.ShouldContain("changed after this draft");
    }

    // ------------------------------------------------------------------ contract

    [Fact]
    public async Task Extending_A_Contract_Should_Move_Its_End_Date_On_Confirm()
    {
        var fixedTerm = CreateContractType("FT", "Fixed Term");
        var contractId = CreateContract(fixedTerm, January, new DateTime(2026, 12, 31));

        var output = await Ask("prepare_contract_change", @"{""employeeCode"":""1002"",""endDate"":""2027-12-31""}");

        var card = output.AssistantMessage.PendingAction;
        card.Title.ShouldBe("Change contract end date");
        Detail(card, "To").ShouldBe("31 Dec 2026 → 31 Dec 2027");

        ShouldHaveCompleted(await Confirm(output));

        UsingDbContext(context => context.Set<EmployeeContract>().Single(c => c.Id == contractId).EndDate.ShouldBe(new DateTime(2027, 12, 31)));
    }

    [Fact]
    public async Task Renewing_As_Another_Contract_Type_Should_End_The_Current_One_And_Add_The_New_One()
    {
        var fixedTerm = CreateContractType("FT", "Fixed Term");
        var permanent = CreateContractType("PR", "Permanent");
        CreateContract(fixedTerm, January, new DateTime(2026, 12, 31));

        var output = await Ask("prepare_contract_change",
            @"{""employeeCode"":""1002"",""startDate"":""2026-10-01"",""openEnded"":true,""contractType"":""permanent""}");

        var card = output.AssistantMessage.PendingAction;
        card.Title.ShouldBe("New contract");
        Detail(card, "Contract Type").ShouldBe("Permanent");
        Detail(card, "To").ShouldBe("Open-ended");

        ShouldHaveCompleted(await Confirm(output));

        UsingDbContext(context =>
        {
            var contracts = context.Set<EmployeeContract>().Where(c => c.EmployeeId == _employeeId).OrderBy(c => c.StartDate).ToList();
            contracts.Count.ShouldBe(2);
            contracts[0].EndDate.ShouldBe(new DateTime(2026, 9, 30));
            contracts[1].ContractTypeId.ShouldBe(permanent);
            contracts[1].StartDate.ShouldBe(October);
            contracts[1].EndDate.ShouldBe(OpenEnded);
        });
    }

    [Fact]
    public async Task A_New_Contract_Type_Without_A_Start_Date_Should_Be_Asked_For()
    {
        CreateContract(CreateContractType("FT", "Fixed Term"), January, new DateTime(2026, 12, 31));
        CreateContractType("PR", "Permanent");

        var output = await Ask("prepare_contract_change", @"{""employeeCode"":""1002"",""openEnded"":true,""contractType"":""Permanent""}");

        output.AssistantMessage.PendingAction.ShouldBeNull();
        LastToolResult().ShouldContain("Ask the user from which date");
    }

    [Fact]
    public async Task An_Unknown_Contract_Type_Should_Be_Answered_With_The_Choices()
    {
        CreateContractType("FT", "Fixed Term");
        CreateContractType("PR", "Permanent");

        await Ask("prepare_contract_change", @"{""employeeCode"":""1002"",""startDate"":""2026-10-01"",""endDate"":""2027-09-30"",""contractType"":""seasonal""}");

        var result = LastToolResult();
        result.ShouldContain("No contract type matches 'seasonal'");
        result.ShouldContain("Fixed Term");
        result.ShouldContain("Permanent");
    }

    // ------------------------------------------------------------------ organisational assignment

    [Fact]
    public async Task A_Transfer_Should_Change_Only_What_Was_Named_And_Keep_The_Group_Fields()
    {
        var hr = CreateOrgUnit("HR", "Human Resources");
        var finance = CreateOrgUnit("FIN", "Finance");
        var accountant = CreateJobTitle("ACC", "Accountant");
        var costCenter = CreateCostCenter("CC1", "Head Office");
        var sourceId = CreateAssignment(January, hr, jobTitleId: null, costCenterId: costCenter);
        var groups = CreateGroupRows();
        SetGroupFields(sourceId, groups);

        var output = await Ask("prepare_org_assignment_change",
            @"{""employeeCode"":""1002"",""effectiveDate"":""2026-10-01"",""orgUnit"":""Finance department"",""jobTitle"":""accountant""}");

        var card = output.AssistantMessage.PendingAction;
        Detail(card, "Organization Unit").ShouldBe("Human Resources → Finance");
        Detail(card, "Job Title").ShouldBe("— → Accountant");
        Detail(card, "Cost Center").ShouldBe("Head Office");

        ShouldHaveCompleted(await Confirm(output));

        UsingDbContext(context =>
        {
            var rows = context.Set<EmployeeOrgAssignment>().Where(a => a.EmployeeId == _employeeId).OrderBy(a => a.StartDate).ToList();
            rows.Count.ShouldBe(2);
            rows[0].EndDate.ShouldBe(new DateTime(2026, 9, 30));

            var moved = rows[1];
            moved.StartDate.ShouldBe(October);
            moved.OrganizationUnitId.ShouldBe(finance);
            moved.JobTitleId.ShouldBe(accountant);
            moved.CostCenterId.ShouldBe(costCenter);

            // The screen's service would have left these empty.
            moved.EmployeeGroupId.ShouldBe(groups.EmployeeGroupId);
            moved.EmployeeSubGroupId.ShouldBe(groups.SubGroupId);
            moved.PayrollAreaId.ShouldBe(groups.PayrollAreaId);
            moved.WorkflowGroupId.ShouldBe(groups.WorkflowGroupId);
        });
    }

    [Fact]
    public async Task Two_Units_Matching_The_Same_Words_Should_Be_A_Question_Not_A_Guess()
    {
        CreateAssignment(January, CreateOrgUnit("HR", "Human Resources"));
        CreateOrgUnit("FIN1", "Finance Kuwait");
        CreateOrgUnit("FIN2", "Finance Dubai");

        var output = await Ask("prepare_org_assignment_change", @"{""employeeCode"":""1002"",""effectiveDate"":""2026-10-01"",""orgUnit"":""finance""}");

        output.AssistantMessage.PendingAction.ShouldBeNull();
        var result = LastToolResult();
        result.ShouldContain("matches more than one organisation unit");
        result.ShouldContain("Finance Kuwait");
        result.ShouldContain("Finance Dubai");
    }

    [Fact]
    public async Task A_Transfer_Before_A_Later_Assignment_Should_Be_Left_To_The_Screen()
    {
        var hr = CreateOrgUnit("HR", "Human Resources");
        CreateOrgUnit("FIN", "Finance");
        CreateAssignment(January, hr);
        CreateAssignment(new DateTime(2026, 12, 1), hr);

        var output = await Ask("prepare_org_assignment_change", @"{""employeeCode"":""1002"",""effectiveDate"":""2026-10-01"",""orgUnit"":""Finance""}");

        output.AssistantMessage.PendingAction.ShouldBeNull();
        LastToolResult().ShouldContain("Org Assignments screen");
    }

    // ------------------------------------------------------------------ personal data

    [Fact]
    public async Task Changing_A_Mobile_Should_Change_Only_That_Field()
    {
        UsingDbContext(context =>
        {
            var e = context.Set<Employee>().Single(x => x.Id == _employeeId);
            e.Email = "old@example.com";
            e.Mobile = 96511111111;
            e.EmergencyContact = "Ali";
        });

        var output = await Ask("prepare_personal_data_change", @"{""employeeCode"":""1002"",""mobile"":""+965 5555-1234""}");

        Detail(output.AssistantMessage.PendingAction, "Mobile").ShouldBe("96511111111 → 96555551234");

        ShouldHaveCompleted(await Confirm(output));

        UsingDbContext(context =>
        {
            var e = context.Set<Employee>().Single(x => x.Id == _employeeId);
            e.Mobile.ShouldBe(96555551234);
            e.Email.ShouldBe("old@example.com");
            e.EmergencyContact.ShouldBe("Ali");
            e.FirstNameEn.ShouldBe("1002");
        });
    }

    [Fact]
    public async Task A_Bad_Email_Or_Phone_Should_Be_Asked_Again_Not_Drafted()
    {
        var email = await Ask("prepare_personal_data_change", @"{""employeeCode"":""1002"",""email"":""not an email""}");
        email.AssistantMessage.PendingAction.ShouldBeNull();
        LastToolResult().ShouldContain("is not an email address");

        var phone = await Ask("prepare_personal_data_change", @"{""employeeCode"":""1002"",""mobile"":""call me""}");
        phone.AssistantMessage.PendingAction.ShouldBeNull();
        LastToolResult().ShouldContain("is not a phone number");
    }

    [Fact]
    public void Names_Civil_Id_And_Dates_Should_Not_Be_Changeable()
    {
        var keys = HCMv3.AiAgent.Tools.AiEmployeeFields.All.Select(f => f.Key).ToList();

        keys.ShouldNotContain(k => k.Contains("name", StringComparison.OrdinalIgnoreCase) && !k.StartsWith("emergency", StringComparison.Ordinal));
        keys.ShouldNotContain(k => k.Contains("civil", StringComparison.OrdinalIgnoreCase));
        keys.ShouldNotContain(k => k.Contains("date", StringComparison.OrdinalIgnoreCase));
        keys.ShouldNotContain(k => k.Contains("nationality", StringComparison.OrdinalIgnoreCase));
    }

    // ------------------------------------------------------------------ duty

    [Fact]
    public async Task A_Duty_Change_Should_Add_One_Override_Per_Day_On_Confirm()
    {
        var night = CreateDuty("N", "Night Shift", new TimeSpan(22, 0, 0), new TimeSpan(6, 0, 0));

        var output = await Ask("prepare_duty_change", @"{""employeeCode"":""1002"",""duty"":""night"",""fromDate"":""2026-10-04"",""toDate"":""2026-10-06""}");

        var card = output.AssistantMessage.PendingAction;
        Detail(card, "Duty").ShouldBe("Night Shift (22:00-06:00)");
        Detail(card, "Days").ShouldBe("3");

        ShouldHaveCompleted(await Confirm(output));

        UsingDbContext(context =>
            context.Set<EmployeeSubstituteDuty>().Where(s => s.EmployeeId == _employeeId).OrderBy(s => s.SubstituteDate)
                .Select(s => new { s.SubstituteDate, s.DutyId }).ToList()
                .ShouldBe(new[]
                {
                    new { SubstituteDate = new DateTime(2026, 10, 4), DutyId = night },
                    new { SubstituteDate = new DateTime(2026, 10, 5), DutyId = night },
                    new { SubstituteDate = new DateTime(2026, 10, 6), DutyId = night }
                }));
    }

    [Fact]
    public async Task A_Duty_Change_Over_Too_Many_Days_Should_Point_To_The_Screen()
    {
        CreateDuty("N", "Night Shift", new TimeSpan(22, 0, 0), new TimeSpan(6, 0, 0));

        var output = await Ask("prepare_duty_change", @"{""employeeCode"":""1002"",""duty"":""Night Shift"",""fromDate"":""2026-10-01"",""toDate"":""2026-12-31""}");

        output.AssistantMessage.PendingAction.ShouldBeNull();
        LastToolResult().ShouldContain("Employee Duty Periods screen");
    }

    // ------------------------------------------------------------------ read-only

    [Fact]
    public async Task Job_Details_Should_Show_Manager_Contract_And_Assignment_On_The_Date()
    {
        CreateManagerRecord(_employeeId, CreateActiveEmployee("2001"), January);
        CreateContract(CreateContractType("FT", "Fixed Term"), January, new DateTime(2026, 12, 31));
        CreateAssignment(January, CreateOrgUnit("HR", "Human Resources"));

        await Ask("get_employee_job_details", @"{""employeeCode"":""1002"",""asOfDate"":""2026-09-18""}");

        var result = LastToolResult();
        result.ShouldContain(@"""code"":""2001""");
        result.ShouldContain(@"""type"":""Fixed Term""");
        result.ShouldContain(@"""to"":""2026-12-31""");
        result.ShouldContain(@"""orgUnit"":""Human Resources""");
    }

    [Fact]
    public async Task Punches_Should_Be_Grouped_By_Day_With_First_In_And_Last_Out()
    {
        var clockIn = CreateTransType("1", "Clock In");
        var clockOut = CreateTransType("2", "Clock Out");
        var day = new DateTime(2026, 9, 16);
        CreatePunch(clockIn, day.AddHours(7).AddMinutes(2));
        CreatePunch(clockOut, day.AddHours(12));
        CreatePunch(clockIn, day.AddHours(13));
        CreatePunch(clockOut, day.AddHours(15).AddMinutes(31));

        await Ask("get_employee_punches", @"{""employeeCode"":""1002"",""fromDate"":""2026-09-16"",""toDate"":""2026-09-17""}");

        var result = LastToolResult();
        result.ShouldContain(@"""firstIn"":""07:02""");
        result.ShouldContain(@"""lastOut"":""15:31""");
        result.ShouldContain("2026-09-17 Thu"); // the day without punches is named, not silently missing
    }

    [Fact]
    public async Task An_Employee_Should_Not_Be_Offered_Any_Employee_Data_Tool()
    {
        AbpSession.UserId = CreateUser(AppPermissions.Ess, AppPermissions.Ess_AiAgent, AppPermissions.Ess_MyLeave);
        Model.RepliesWith("hi");

        await Service.SendMessage(new SendAiMessageInput { Message = "change 1002's manager to 2001" });

        var offered = Model.Requests.Single().Tools.Select(t => t.Name).ToList();
        offered.ShouldNotContain(n => n.StartsWith("prepare_") && n != "prepare_leave_request" && n != "prepare_leave_cancellation");
        offered.ShouldNotContain("get_employee_job_details");
        offered.ShouldNotContain("get_employee_punches");
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>The base helper leaves Status at its default, Inactive; a manager must be active.</summary>
    private int CreateActiveEmployee(string code)
    {
        var id = CreateEmployee(code);
        UsingDbContext(context => context.Set<Employee>().Single(e => e.Id == id).Status = EmployeeStatus.Active);
        return id;
    }

    private void CreateManagerRecord(int employeeId, int managerId, DateTime start) =>
        UsingDbContext(context => context.Set<EmployeeManager>().Add(new EmployeeManager
        {
            TenantId = TenantId, EmployeeId = employeeId, ManagerId = managerId, StartDate = start, EndDate = OpenEnded
        }));

    private int CreateContractType(string code, string name)
    {
        ContractType row = null;
        UsingDbContext(context => row = context.Set<ContractType>().Add(new ContractType { TenantId = TenantId, Code = code, NameEn = name, NameAr = name }).Entity);
        return row.Id;
    }

    private int CreateContract(int contractTypeId, DateTime start, DateTime end)
    {
        EmployeeContract row = null;
        UsingDbContext(context => row = context.Set<EmployeeContract>().Add(new EmployeeContract
        {
            TenantId = TenantId, EmployeeId = _employeeId, ContractTypeId = contractTypeId, StartDate = start, EndDate = end
        }).Entity);
        return row.Id;
    }

    private long CreateOrgUnit(string code, string name)
    {
        OrganizationUnit unit = null;
        UsingDbContext(context =>
        {
            unit = context.OrganizationUnits.Add(new OrganizationUnit(TenantId, name) { Code = code + "." + Guid.NewGuid().ToString("N")[..5] }).Entity;
            context.SaveChanges();
            context.Set<OrgUnitProfile>().Add(new OrgUnitProfile { TenantId = TenantId, OrganizationUnitId = unit.Id, Code = code, NameAr = name, OrganizationUnitTypeId = _unitTypeId });
        });
        return unit.Id;
    }

    private int CreateJobTitle(string code, string name)
    {
        JobTitle row = null;
        UsingDbContext(context => row = context.Set<JobTitle>().Add(new JobTitle { TenantId = TenantId, Code = code, NameEn = name, NameAr = name }).Entity);
        return row.Id;
    }

    private int CreateCostCenter(string code, string name)
    {
        CostCenter row = null;
        UsingDbContext(context => row = context.Set<CostCenter>().Add(new CostCenter { TenantId = TenantId, Code = code, NameEn = name, NameAr = name }).Entity);
        return row.Id;
    }

    private int CreateAssignment(DateTime start, long orgUnitId, int? jobTitleId = null, int? costCenterId = null)
    {
        EmployeeOrgAssignment row = null;
        UsingDbContext(context => row = context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
        {
            TenantId = TenantId, EmployeeId = _employeeId, StartDate = start, EndDate = OpenEnded,
            OrganizationUnitId = orgUnitId, JobTitleId = jobTitleId, CostCenterId = costCenterId
        }).Entity);
        return row.Id;
    }

    private record GroupRows(int EmployeeGroupId, int SubGroupId, int PayrollAreaId, int WorkflowGroupId);

    /// <summary>Real rows: the test database enforces foreign keys.</summary>
    private GroupRows CreateGroupRows()
    {
        GroupRows rows = null;
        UsingDbContext(context =>
        {
            var group = context.Set<EmployeeGroup>().Add(new EmployeeGroup { TenantId = TenantId, Code = "G1", NameEn = "Staff", NameAr = "موظفون" }).Entity;
            var currency = context.Set<Currency>().Add(new Currency { TenantId = TenantId, Code = "KWD", NameEn = "KWD", NameAr = "KWD", DecimalCount = 3 }).Entity;
            var period = context.Set<PayrollPeriod>().Add(new PayrollPeriod { TenantId = TenantId, Code = "M", NameEn = "Monthly", NameAr = "Monthly" }).Entity;
            var workflowGroup = context.Set<WorkflowGroup>().Add(new WorkflowGroup { TenantId = TenantId, Code = "WG1", NameEn = "HQ", NameAr = "HQ" }).Entity;
            context.SaveChanges();

            var subGroup = context.Set<EmployeeSubGroup>().Add(new EmployeeSubGroup
            {
                TenantId = TenantId, EmployeeGroupId = group.Id, Code = "SG1", NameEn = "Kuwaiti", NameAr = "كويتي"
            }).Entity;
            var area = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = TenantId, Code = "PA1", Name = "Main", PayrollPeriodId = period.Id, CurrencyId = currency.Id, CutOffDay = 25
            }).Entity;
            context.SaveChanges();

            rows = new GroupRows(group.Id, subGroup.Id, area.Id, workflowGroup.Id);
        });
        return rows;
    }

    private void SetGroupFields(int assignmentId, GroupRows groups) =>
        UsingDbContext(context =>
        {
            var row = context.Set<EmployeeOrgAssignment>().Single(a => a.Id == assignmentId);
            row.EmployeeGroupId = groups.EmployeeGroupId;
            row.EmployeeSubGroupId = groups.SubGroupId;
            row.PayrollAreaId = groups.PayrollAreaId;
            row.WorkflowGroupId = groups.WorkflowGroupId;
        });

    private int CreateDuty(string code, string name, TimeSpan start, TimeSpan end)
    {
        Duty row = null;
        UsingDbContext(context => row = context.Set<Duty>().Add(new Duty
        {
            TenantId = TenantId, Code = code, NameEn = name, NameAr = name, StartTime = start, EndTime = end,
            StartDate = January, EndDate = OpenEnded
        }).Entity);
        return row.Id;
    }

    private int CreateTransType(string code, string name)
    {
        TransType row = null;
        UsingDbContext(context => row = context.Set<TransType>().Add(new TransType { TenantId = TenantId, Code = code, NameEn = name, NameAr = name }).Entity);
        return row.Id;
    }

    private void CreatePunch(int transTypeId, DateTime when) =>
        UsingDbContext(context => context.Set<TimeEvent>().Add(new TimeEvent
        {
            TenantId = TenantId, EmployeeId = _employeeId, TransTypeId = transTypeId, TransDate = when.Date, TransTime = when,
            RecordSource = when.Ticks
        }));
}
