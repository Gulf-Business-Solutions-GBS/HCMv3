using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Organizations;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.TimeManagement;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// The department overtime batch (legacy <c>Workflow.Payroll.OvertimeRequest</c> + <c>OvertimeRequestDetail</c>,
/// raised from <c>HCM10.ESS</c> <c>OverTimeController</c>).
///
/// <para>
/// What legacy did and what these tests pin: one request per department and date range, lines per employee per
/// day loaded from the shift report (time register), only lines with approved hours or a comment kept, and on
/// approval one <c>EmployeeOvertime</c> per line -- hours rounded to two places, overtime type code "4"
/// whatever the line shows. What these tests keep OUT: legacy wrote the overtime rows from
/// <c>OnSaving</c> on every save of an approved request, so approving twice must not write twice.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class OvertimeBatchWorkflow_Tests : AppTestBase
{
    private static readonly DateTime Day1 = new(2026, 6, 1);
    private static readonly DateTime Day2 = new(2026, 6, 2);
    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly IOvertimeBatchesAppService _batches;
    private readonly IRepository<OvertimeBatch> _batchRepository;
    private readonly OvertimeBatchWorkflowTransactionHandler _handler;
    private readonly IWorkflowApprovalService _approvalService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public OvertimeBatchWorkflow_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _batches = Resolve<IOvertimeBatchesAppService>();
        _batchRepository = Resolve<IRepository<OvertimeBatch>>();
        _handler = Resolve<OvertimeBatchWorkflowTransactionHandler>();
        _approvalService = Resolve<IWorkflowApprovalService>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    #region Fixtures

    private int CreateEmployee(string code, long? userId = null, EmployeeStatus status = EmployeeStatus.Active)
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
                Status = status,
                UserId = userId,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    /// <summary>A batch is routed against the person who raised it, so the signed-in admin is linked here.</summary>
    private int CreateRequesterEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    private (long Id, string Code) CreateUnit(string name, (long Id, string Code)? parent = null)
    {
        OrganizationUnit unit = null;
        var segment = new Random().Next(10000, 99999).ToString();
        var code = parent.HasValue ? parent.Value.Code + "." + segment : segment;

        UsingDbContext(context =>
        {
            unit = context.OrganizationUnits.Add(new OrganizationUnit(TenantId, name, parent?.Id) { Code = code }).Entity;
        });

        return (unit.Id, code);
    }

    private void Assign(int employeeId, long unitId)
    {
        UsingDbContext(context => context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
        {
            TenantId = TenantId,
            EmployeeId = employeeId,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = OpenEnded,
            OrganizationUnitId = unitId
        }));
    }

    private int CreateOvertimeType(string code, string name = null)
    {
        var existing = UsingDbContext(c => c.Set<OvertimeType>().Where(t => t.Code == code).Select(t => (int?)t.Id).FirstOrDefault());
        if (existing.HasValue)
        {
            return existing.Value;
        }

        OvertimeType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<OvertimeType>().Add(new OvertimeType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = name ?? "OT " + code,
                NameAr = name ?? "OT " + code,
                OvertimeRate = 1.25
            }).Entity;
        });

        return type.Id;
    }

    /// <summary>The type legacy gave every approved batch row (OvertimeRequest.OnSaving line 168).</summary>
    private int CreateCodeFourType() => CreateOvertimeType(OvertimeBatchService.LegacyApprovalOvertimeTypeCode, "Normal Overtime");

    private int CreateTimeRegister(int employeeId, DateTime day, double overtimeMinutes, int? overtimeTypeId = null)
    {
        TimeRegister row = null;
        UsingDbContext(context =>
        {
            row = context.Set<TimeRegister>().Add(new TimeRegister
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DayDate = day,
                Status = TimeRegisterStatus.Opened,
                ShiftStartTime1 = day.AddHours(8),
                ShiftEndTime1 = day.AddHours(16),
                ActualIn = day.AddHours(7).AddMinutes(55),
                ActualOut = day.AddHours(19),
                PlannedWorkingMins = 480,
                ActualWorkingMins = 665,
                OvertimeMinutes = overtimeMinutes,
                OvertimeTypeId = overtimeTypeId,
                LateIn = 0,
                EarlyOut = 0
            }).Entity;
        });

        return row.Id;
    }

    private void EnsureCatalogRow(bool workflowEnabled, int? requestMasterId)
    {
        UsingDbContext(context =>
        {
            var type = context.Set<WorkflowTransactionType>()
                .FirstOrDefault(t => t.Code == OvertimeBatchWorkflowTransactionHandler.OvertimeBatchRequestType);

            if (type == null)
            {
                // The seed row (WorkflowTransactionTypeBuilder) is added separately; these tests do not depend on it.
                type = context.Set<WorkflowTransactionType>().Add(new WorkflowTransactionType
                {
                    TenantId = TenantId,
                    Code = OvertimeBatchWorkflowTransactionHandler.OvertimeBatchRequestType,
                    NameEn = "Overtime Batch",
                    NameAr = "دفعة عمل إضافي",
                    DisplayOrder = 280
                }).Entity;
            }

            type.IsActive = true;
            type.IsWorkflowEnabled = workflowEnabled;
            type.DefaultRequestMasterId = requestMasterId;
        });
    }

    /// <summary>Turns the type on for approval with one approver step held by a real employee.</summary>
    private int EnableWorkflow(string approverCode)
    {
        var approverId = CreateEmployee(approverCode);
        int masterId = 0;

        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Approver",
                NameAr = "Approver"
            }).Entity;

            var master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = "OB-STD-" + Guid.NewGuid().ToString("N").Substring(0, 4),
                NameEn = "Standard",
                NameAr = "Standard",
                WorkflowSteps = 1
            }).Entity;

            context.SaveChanges();

            context.Set<RequestDetail>().Add(new RequestDetail
            {
                TenantId = TenantId,
                RequestMasterId = master.Id,
                Step = 1,
                WorkflowRoleId = role.Id,
                ApproverPerson = WorkflowApproverPersonType.Person,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = true,
                CanSendBack = true
            });

            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = approverId,
                WorkflowRoleId = role.Id,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2)
            });

            masterId = master.Id;
        });

        EnsureCatalogRow(true, masterId);

        return approverId;
    }

    private sealed class Scenario
    {
        public (long Id, string Code) Unit;
        public int EmployeeA;
        public int EmployeeB;
        public int RegisterA1;
        public int RegisterA2;
        public int RegisterB1;
    }

    /// <summary>One unit, two employees in it, overtime on day 1 for both and day 2 for A.</summary>
    private Scenario CreateScenario(string prefix)
    {
        var s = new Scenario { Unit = CreateUnit(prefix + " Dept") };
        s.EmployeeA = CreateEmployee(prefix + "A");
        s.EmployeeB = CreateEmployee(prefix + "B");
        Assign(s.EmployeeA, s.Unit.Id);
        Assign(s.EmployeeB, s.Unit.Id);

        s.RegisterA1 = CreateTimeRegister(s.EmployeeA, Day1, 185);
        s.RegisterA2 = CreateTimeRegister(s.EmployeeA, Day2, 60);
        s.RegisterB1 = CreateTimeRegister(s.EmployeeB, Day1, 120);
        return s;
    }

    private static CreateOrEditOvertimeBatchDto BatchFor(
        (long Id, string Code) unit,
        params (int TimeRegisterId, double ApprovedHours, string Comments)[] lines)
    {
        return new CreateOrEditOvertimeBatchDto
        {
            OrganizationUnitId = unit.Id,
            StartDate = Day1,
            EndDate = Day2,
            Comments = "Stock count weekend",
            Details = lines.Select(l => new CreateOrEditOvertimeBatchLineDto
            {
                TimeRegisterId = l.TimeRegisterId,
                ApprovedHours = l.ApprovedHours,
                Comments = l.Comments
            }).ToList()
        };
    }

    private List<OvertimeBatchDetail> LinesOf(int batchId) =>
        UsingDbContext(c => c.Set<OvertimeBatchDetail>()
            .IgnoreQueryFilters()
            .Where(d => d.OvertimeBatchId == batchId && !d.IsDeleted)
            .OrderBy(d => d.EmployeeId).ThenBy(d => d.Date)
            .ToList());

    private List<EmployeeOvertime> OvertimesOf(int employeeId) =>
        UsingDbContext(c => c.Set<EmployeeOvertime>()
            .IgnoreQueryFilters()
            .Where(o => o.EmployeeId == employeeId && !o.IsDeleted)
            .OrderBy(o => o.EffectiveDate)
            .ToList());

    private OvertimeBatch BatchRow(int batchId) =>
        UsingDbContext(c => c.Set<OvertimeBatch>().IgnoreQueryFilters().Single(b => b.Id == batchId));

    private WorkflowRequest RequestFor(int batchId) =>
        UsingDbContext(c => c.Set<WorkflowRequest>().Single(
            r => r.RequestType == OvertimeBatchWorkflowTransactionHandler.OvertimeBatchRequestType
                 && r.TargetEntityId == batchId));

    private WorkflowTransactionContext ContextFor(int requestId)
    {
        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == requestId));
        return new WorkflowTransactionContext(request, request.EmployeeId ?? 0);
    }

    private async Task InUnitOfWorkAsync(Func<Task> action)
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            await action();
            await uow.CompleteAsync();
        }
    }

    private async Task<TResult> InUnitOfWorkAsync<TResult>(Func<Task<TResult>> action)
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            var result = await action();
            await uow.CompleteAsync();
            return result;
        }
    }

    private Task<bool> IsBatchVisibleAsync(int batchId) =>
        InUnitOfWorkAsync(() => _batchRepository.GetAll().AnyAsync(b => b.Id == batchId));

    private Task<List<OvertimeBatchLineDto>> CandidatesAsync((long Id, string Code) unit, bool onlyOvertimeDays = true, int? excludeBatchId = null) =>
        _batches.GetOvertimeBatchCandidates(new GetOvertimeBatchCandidatesInput
        {
            OrganizationUnitId = unit.Id,
            StartDate = Day1,
            EndDate = Day2,
            OnlyOvertimeDays = onlyOvertimeDays,
            ExcludeOvertimeBatchId = excludeBatchId
        });

    #endregion

    #region Loading the grid

    [Fact]
    public async Task Candidates_Should_Include_Active_People_In_The_Unit_And_Below_It_Only()
    {
        // Legacy matched the unit against all seven OrgLevel columns -- "this unit or anything under it" --
        // and only Active employees.
        var parent = CreateUnit("Ops");
        var child = CreateUnit("Ops Night", parent);
        var other = CreateUnit("Finance");

        var inParent = CreateEmployee("OBC100P");
        var inChild = CreateEmployee("OBC100C");
        var inactive = CreateEmployee("OBC100I", status: EmployeeStatus.Inactive);
        var elsewhere = CreateEmployee("OBC100E");
        Assign(inParent, parent.Id);
        Assign(inChild, child.Id);
        Assign(inactive, parent.Id);
        Assign(elsewhere, other.Id);

        foreach (var id in new[] { inParent, inChild, inactive, elsewhere })
        {
            CreateTimeRegister(id, Day1, 60);
        }

        var employees = (await CandidatesAsync(parent)).Select(c => c.EmployeeId).Distinct().ToList();

        employees.ShouldContain(inParent);
        employees.ShouldContain(inChild);
        employees.ShouldNotContain(inactive);
        employees.ShouldNotContain(elsewhere);
    }

    [Fact]
    public async Task Candidates_Should_List_Only_Overtime_Days_Unless_Asked_For_All()
    {
        // Legacy AllowOvertimeHoursOnly.
        var unit = CreateUnit("OBC110");
        var employeeId = CreateEmployee("OBC110A");
        Assign(employeeId, unit.Id);
        CreateTimeRegister(employeeId, Day1, 90);
        CreateTimeRegister(employeeId, Day2, 0);

        (await CandidatesAsync(unit)).Count.ShouldBe(1);
        (await CandidatesAsync(unit, onlyOvertimeDays: false)).Count.ShouldBe(2);
    }

    [Fact]
    public async Task Candidates_Should_Carry_The_Time_Register_Day_And_Start_With_Zero_Approved_Hours()
    {
        // Legacy shift mode: Shift Start/End, Sign In/Out, Planned and Overtime hours from the report, and
        // ApprovedHours = TimeSpan.Zero so an untouched grid is not mistaken for a decision.
        var unit = CreateUnit("OBC120");
        var employeeId = CreateEmployee("OBC120A");
        Assign(employeeId, unit.Id);
        var typeId = CreateOvertimeType("WK", "Weekend");
        CreateTimeRegister(employeeId, Day1, 180, typeId);

        var line = (await CandidatesAsync(unit)).ShouldHaveSingleItem();

        line.Date.ShouldBe(Day1);
        line.ShiftStart.ShouldBe(Day1.AddHours(8));
        line.ShiftEnd.ShouldBe(Day1.AddHours(16));
        line.SignIn.ShouldBe(Day1.AddHours(7).AddMinutes(55));
        line.SignOut.ShouldBe(Day1.AddHours(19));
        line.PlannedHours.ShouldBe(8);
        line.TotalHours.ShouldBe(3);
        line.OvertimeTypeId.ShouldBe(typeId);
        line.ApprovedHours.ShouldBe(0);
        line.HasExistingOvertime.ShouldBeFalse();
    }

    [Fact]
    public async Task Candidates_Should_Leave_Out_Days_Already_On_A_Live_Batch()
    {
        // Legacy's loader skipped employee/day pairs already on an Approved or Open request.
        CreateRequesterEmployee("OBC130REQ");
        CreateCodeFourType();
        EnableWorkflow("OBC130APP");
        var s = CreateScenario("OBC130");

        var batchId = await _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 3, null)));

        var registers = (await CandidatesAsync(s.Unit)).Select(c => c.TimeRegisterId).ToList();
        registers.ShouldNotContain(s.RegisterA1);
        registers.ShouldContain(s.RegisterB1);

        // The batch being edited sees its own days.
        (await CandidatesAsync(s.Unit, excludeBatchId: batchId)).Select(c => c.TimeRegisterId).ShouldContain(s.RegisterA1);
    }

    [Fact]
    public async Task Candidates_Should_Flag_A_Day_That_Already_Has_Overtime()
    {
        var s = CreateScenario("OBC140");
        var typeId = CreateCodeFourType();
        UsingDbContext(c => c.Set<EmployeeOvertime>().Add(new EmployeeOvertime
        {
            TenantId = TenantId,
            EmployeeId = s.EmployeeA,
            EffectiveDate = Day1,
            OvertimeTypeId = typeId,
            OvertimeHours = 2,
            ApprovedOvertimeHours = 2
        }));

        var candidates = await CandidatesAsync(s.Unit);

        candidates.Single(c => c.TimeRegisterId == s.RegisterA1).HasExistingOvertime.ShouldBeTrue();
        candidates.Single(c => c.TimeRegisterId == s.RegisterB1).HasExistingOvertime.ShouldBeFalse();
    }

    [Fact]
    public async Task Candidates_Should_Refuse_An_End_Date_Before_The_Start_Date()
    {
        var unit = CreateUnit("OBC150");

        await Should.ThrowAsync<UserFriendlyException>(() => _batches.GetOvertimeBatchCandidates(new GetOvertimeBatchCandidatesInput
        {
            OrganizationUnitId = unit.Id,
            StartDate = Day2,
            EndDate = Day1
        }));
    }

    #endregion

    #region Submitting

    [Fact]
    public async Task Submitting_Should_Create_The_Batch_Hidden_And_Write_No_Overtime_Yet()
    {
        var requesterId = CreateRequesterEmployee("OBS100REQ");
        CreateCodeFourType();
        EnableWorkflow("OBS100APP");
        var s = CreateScenario("OBS100");

        var batchId = await _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 3, null), (s.RegisterB1, 2, null)));

        BatchRow(batchId).WorkflowStatus.ShouldBe(RequestStatus.Open);
        (await IsBatchVisibleAsync(batchId)).ShouldBeFalse("A batch awaiting approval was visible to ordinary queries.");
        LinesOf(batchId).Count.ShouldBe(2);
        OvertimesOf(s.EmployeeA).ShouldBeEmpty("Overtime was written before anybody approved the batch.");

        // Routed against the person who raised it, as legacy recorded OvertimeRequest.Employee.
        RequestFor(batchId).EmployeeId.ShouldBe(requesterId);
    }

    [Fact]
    public async Task Legacy_Parity_Only_Lines_With_Approved_Hours_Or_A_Comment_Are_Kept()
    {
        // OverTimeController.OvertimeRequestSubmit: Where(x => x.ApprovedHours > 0 || !string.IsNullOrEmpty(x.Comment)).
        CreateRequesterEmployee("OBS110REQ");
        CreateCodeFourType();
        EnableWorkflow("OBS110APP");
        var s = CreateScenario("OBS110");

        var batchId = await _batches.CreateOvertimeBatch(BatchFor(
            s.Unit,
            (s.RegisterA1, 3, null),
            (s.RegisterA2, 0, null),
            (s.RegisterB1, 0, "Not authorised, recorded for the file")));

        LinesOf(batchId).Select(l => l.TimeRegisterId).ShouldBe(new int?[] { s.RegisterA1, s.RegisterB1 }, ignoreOrder: true);
    }

    [Fact]
    public async Task The_Time_Figures_Should_Come_From_The_Time_Register_Not_The_Screen()
    {
        CreateRequesterEmployee("OBS120REQ");
        CreateCodeFourType();
        EnableWorkflow("OBS120APP");
        var s = CreateScenario("OBS120");

        var batchId = await _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 2.5, null)));

        var line = LinesOf(batchId).ShouldHaveSingleItem();
        line.EmployeeId.ShouldBe(s.EmployeeA);
        line.Date.ShouldBe(Day1);
        line.TotalHours.ShouldBe(185 / 60.0);
        line.PlannedHours.ShouldBe(8);
        line.SignOut.ShouldBe(Day1.AddHours(19));
        line.ApprovedHours.ShouldBe(2.5);
    }

    [Fact]
    public async Task Should_Refuse_A_Batch_With_Nothing_Approved_Or_Commented()
    {
        // Legacy "Select atleast one record!".
        CreateRequesterEmployee("OBS130REQ");
        EnableWorkflow("OBS130APP");
        var s = CreateScenario("OBS130");

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 0, null))));
    }

    [Fact]
    public async Task Should_Refuse_A_Day_That_Already_Has_Overtime()
    {
        // Legacy EmployeeOvertime.PerformValidations "Record already exists" made the approval fail; here the
        // same rule refuses the batch before an approver is asked.
        CreateRequesterEmployee("OBS140REQ");
        var typeId = CreateCodeFourType();
        EnableWorkflow("OBS140APP");
        var s = CreateScenario("OBS140");
        UsingDbContext(c => c.Set<EmployeeOvertime>().Add(new EmployeeOvertime
        {
            TenantId = TenantId,
            EmployeeId = s.EmployeeA,
            EffectiveDate = Day1,
            OvertimeTypeId = typeId,
            OvertimeHours = 1
        }));

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 3, null))));
    }

    [Fact]
    public async Task Should_Refuse_A_Day_Already_On_Another_Live_Batch()
    {
        CreateRequesterEmployee("OBS150REQ");
        CreateCodeFourType();
        EnableWorkflow("OBS150APP");
        var s = CreateScenario("OBS150");

        await _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 3, null)));

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 1, null))));
    }

    [Fact]
    public async Task Should_Refuse_A_Day_Of_Somebody_Outside_The_Unit()
    {
        CreateRequesterEmployee("OBS160REQ");
        EnableWorkflow("OBS160APP");
        var s = CreateScenario("OBS160");
        var other = CreateUnit("OBS160 Other");
        var outsider = CreateEmployee("OBS160X");
        Assign(outsider, other.Id);
        var outsiderDay = CreateTimeRegister(outsider, Day1, 120);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _batches.CreateOvertimeBatch(BatchFor(s.Unit, (outsiderDay, 2, null))));
    }

    [Fact]
    public async Task With_Workflow_On_An_Unlinked_Account_Should_Be_Refused()
    {
        // Nobody to route by. Admin accounts are deliberately never linked to an employee.
        EnableWorkflow("OBS170APP");
        var s = CreateScenario("OBS170");

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 3, null))));
    }

    [Fact]
    public async Task The_Summary_Should_Name_The_Department_The_Days_And_The_Hours()
    {
        CreateRequesterEmployee("OBS180REQ");
        CreateCodeFourType();
        EnableWorkflow("OBS180APP");
        var s = CreateScenario("OBS180");

        var batchId = await _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 3, null), (s.RegisterB1, 1.5, null)));

        var summary = RequestFor(batchId).SummaryEn;
        summary.ShouldContain("OBS180 Dept");
        summary.ShouldContain("2 day(s)");
        summary.ShouldContain("4.5 hour(s)");
    }

    #endregion

    #region Approval

    [Fact]
    public async Task Approval_Should_Write_One_Overtime_Row_Per_Line_Exactly_As_Legacy_Did()
    {
        // Legacy OvertimeRequest.OnSaving lines 165-189: EffectiveDate = line date, OvertimeHours and
        // ActualOvertimeHours = round(TotalHours, 2), ApprovedOvertimeHours = round(ApprovedHours, 2).
        CreateRequesterEmployee("OBA100REQ");
        var codeFour = CreateCodeFourType();
        var approverId = EnableWorkflow("OBA100APP");
        var s = CreateScenario("OBA100");

        var batchId = await _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 2.555, null), (s.RegisterB1, 2, null)));
        await _approvalService.ApproveAsync(RequestFor(batchId).Id, approverId, "Agreed");

        BatchRow(batchId).WorkflowStatus.ShouldBe(RequestStatus.Approved);

        var a = OvertimesOf(s.EmployeeA).ShouldHaveSingleItem();
        a.EffectiveDate.ShouldBe(Day1);
        a.OvertimeHours.ShouldBe(Math.Round(185 / 60.0, 2));
        a.ActualOvertimeHours.ShouldBe(Math.Round(185 / 60.0, 2));
        a.ApprovedOvertimeHours.ShouldBe(Math.Round(2.555, 2));
        a.OvertimeTypeId.ShouldBe(codeFour);

        // Effective at once: visible to payroll's ordinary queries.
        a.WorkflowStatus.ShouldBeNull();

        OvertimesOf(s.EmployeeB).ShouldHaveSingleItem().ApprovedOvertimeHours.ShouldBe(2);

        // Each line records the row it produced.
        LinesOf(batchId).ShouldAllBe(l => l.EmployeeOvertimeId.HasValue);
    }

    [Fact]
    public async Task Legacy_Parity_Approval_Uses_Overtime_Type_Code_Four_Whatever_The_Line_Shows()
    {
        // Legacy added a type to each line and never changed the approval: it always used code "4". Pinned,
        // because the type decides the rate payroll pays.
        CreateRequesterEmployee("OBA110REQ");
        var codeFour = CreateCodeFourType();
        var weekend = CreateOvertimeType("WK", "Weekend");
        var approverId = EnableWorkflow("OBA110APP");
        var s = CreateScenario("OBA110");

        var input = BatchFor(s.Unit, (s.RegisterA1, 3, null));
        input.Details[0].OvertimeTypeId = weekend;
        var batchId = await _batches.CreateOvertimeBatch(input);

        LinesOf(batchId).Single().OvertimeTypeId.ShouldBe(weekend);

        await _approvalService.ApproveAsync(RequestFor(batchId).Id, approverId, "Agreed");

        OvertimesOf(s.EmployeeA).ShouldHaveSingleItem().OvertimeTypeId.ShouldBe(codeFour);
    }

    [Fact]
    public async Task Legacy_Bug_Approving_Twice_Must_Not_Write_Overtime_Twice()
    {
        // Legacy wrote the rows from OnSaving on every save of an approved request.
        CreateRequesterEmployee("OBA120REQ");
        CreateCodeFourType();
        var approverId = EnableWorkflow("OBA120APP");
        var s = CreateScenario("OBA120");

        var batchId = await _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 3, null)));
        var requestId = RequestFor(batchId).Id;
        await _approvalService.ApproveAsync(requestId, approverId, "Agreed");

        await InUnitOfWorkAsync(() => _handler.OnApprovedAsync(ContextFor(requestId)));

        OvertimesOf(s.EmployeeA).Count.ShouldBe(1, "Approving the batch a second time wrote the overtime again.");
    }

    [Fact]
    public async Task Rejecting_Should_Write_No_Overtime()
    {
        CreateRequesterEmployee("OBA130REQ");
        CreateCodeFourType();
        var approverId = EnableWorkflow("OBA130APP");
        var s = CreateScenario("OBA130");

        var batchId = await _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 3, null)));
        await _approvalService.RejectAsync(RequestFor(batchId).Id, approverId, "Not budgeted");

        OvertimesOf(s.EmployeeA).ShouldBeEmpty();
        BatchRow(batchId).WorkflowStatus.ShouldBe(RequestStatus.Rejected);
    }

    [Fact]
    public async Task Approval_Should_Fail_And_Roll_Back_When_The_Day_Was_Recorded_Meanwhile()
    {
        // Legacy: EmployeeOvertime.PerformValidations threw inside the approving save.
        CreateRequesterEmployee("OBA140REQ");
        var typeId = CreateCodeFourType();
        var approverId = EnableWorkflow("OBA140APP");
        var s = CreateScenario("OBA140");

        var batchId = await _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 3, null), (s.RegisterB1, 2, null)));

        UsingDbContext(c => c.Set<EmployeeOvertime>().Add(new EmployeeOvertime
        {
            TenantId = TenantId,
            EmployeeId = s.EmployeeB,
            EffectiveDate = Day1,
            OvertimeTypeId = typeId,
            OvertimeHours = 1
        }));

        var requestId = RequestFor(batchId).Id;

        // Production units of work are transactional; the test harness's are not (HCMv3TestBaseModule), so the
        // approval runs inside a transactional one here to see the rollback production would do.
        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            using (var uow = _unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true }))
            {
                await _approvalService.ApproveAsync(requestId, approverId, "Agreed");
                await uow.CompleteAsync();
            }
        });

        // Every line is checked before any is written, so this holds even without the rollback.
        OvertimesOf(s.EmployeeA).ShouldBeEmpty("Half a batch was posted.");
        BatchRow(batchId).WorkflowStatus.ShouldBe(RequestStatus.Open);
    }

    [Fact]
    public async Task Approval_Should_Fail_When_There_Is_No_Code_Four_Overtime_Type()
    {
        // Legacy assigned a null type and EmployeeOvertime refused it ("OvertimeType Cannot be blank.").
        CreateRequesterEmployee("OBA150REQ");
        var approverId = EnableWorkflow("OBA150APP");
        var s = CreateScenario("OBA150");

        var batchId = await _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 3, null)));

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _approvalService.ApproveAsync(RequestFor(batchId).Id, approverId, "Agreed"));

        OvertimesOf(s.EmployeeA).ShouldBeEmpty();
    }

    #endregion

    #region Workflow off

    [Fact]
    public async Task With_Workflow_Off_The_Batch_Should_Post_At_Once()
    {
        // Otherwise a batch raised with workflow off would never produce any overtime.
        CreateCodeFourType();
        EnsureCatalogRow(false, null);
        var s = CreateScenario("OBW100");

        var batchId = await _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 3, null)));

        BatchRow(batchId).WorkflowStatus.ShouldBeNull();
        (await IsBatchVisibleAsync(batchId)).ShouldBeTrue();
        OvertimesOf(s.EmployeeA).ShouldHaveSingleItem().ApprovedOvertimeHours.ShouldBe(3);
    }

    [Fact]
    public async Task A_Posted_Batch_Cannot_Be_Edited_Or_Deleted()
    {
        CreateCodeFourType();
        EnsureCatalogRow(false, null);
        var s = CreateScenario("OBW110");

        var batchId = await _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 3, null)));

        var edit = BatchFor(s.Unit, (s.RegisterA1, 1, null));
        edit.Id = batchId;

        await Should.ThrowAsync<UserFriendlyException>(() => _batches.UpdateOvertimeBatch(edit));
        await Should.ThrowAsync<UserFriendlyException>(() => _batches.Delete(new EntityDto(batchId)));
    }

    #endregion

    #region Sent back, corrected, resubmitted

    [Fact]
    public async Task A_Sent_Back_Batch_Can_Be_Corrected_And_Submitted_Again()
    {
        CreateRequesterEmployee("OBR100REQ");
        CreateCodeFourType();
        var approverId = EnableWorkflow("OBR100APP");
        var s = CreateScenario("OBR100");

        var batchId = await _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 3, null)));
        await _approvalService.SendBackAsync(RequestFor(batchId).Id, approverId, "Two hours only");

        BatchRow(batchId).WorkflowStatus.ShouldBe(RequestStatus.Draft);
        (await _batches.GetOvertimeBatchForEdit(new EntityDto(batchId))).CanResubmit.ShouldBeTrue();

        var edit = BatchFor(s.Unit, (s.RegisterA1, 2, null));
        edit.Id = batchId;
        await _batches.UpdateOvertimeBatch(edit);
        await _batches.ResubmitOvertimeBatch(new EntityDto(batchId));

        BatchRow(batchId).WorkflowStatus.ShouldBe(RequestStatus.Open);
        LinesOf(batchId).ShouldHaveSingleItem().ApprovedHours.ShouldBe(2);

        await _approvalService.ApproveAsync(RequestFor(batchId).Id, approverId, "Agreed");
        OvertimesOf(s.EmployeeA).ShouldHaveSingleItem().ApprovedOvertimeHours.ShouldBe(2);
    }

    [Fact]
    public async Task A_Batch_In_An_Inbox_Cannot_Be_Edited_Resubmitted_Or_Deleted()
    {
        CreateRequesterEmployee("OBR110REQ");
        CreateCodeFourType();
        EnableWorkflow("OBR110APP");
        var s = CreateScenario("OBR110");

        var batchId = await _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 3, null)));

        var edit = BatchFor(s.Unit, (s.RegisterA1, 1, null));
        edit.Id = batchId;

        await Should.ThrowAsync<UserFriendlyException>(() => _batches.UpdateOvertimeBatch(edit));
        await Should.ThrowAsync<UserFriendlyException>(() => _batches.ResubmitOvertimeBatch(new EntityDto(batchId)));
        await Should.ThrowAsync<UserFriendlyException>(() => _batches.Delete(new EntityDto(batchId)));
    }

    [Fact]
    public async Task Deleting_A_Sent_Back_Batch_Should_Close_Its_Request()
    {
        CreateRequesterEmployee("OBR120REQ");
        CreateCodeFourType();
        var approverId = EnableWorkflow("OBR120APP");
        var s = CreateScenario("OBR120");

        var batchId = await _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 3, null)));
        var requestId = RequestFor(batchId).Id;
        await _approvalService.SendBackAsync(requestId, approverId, "Wrong department");

        await _batches.Delete(new EntityDto(batchId));

        UsingDbContext(c => c.Set<OvertimeBatch>().Any(b => b.Id == batchId && !b.IsDeleted)).ShouldBeFalse();
        UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == requestId)).RequestStatus.ShouldBe(RequestStatus.Closed);
    }

    #endregion

    #region The list

    [Fact]
    public async Task The_List_Should_Show_In_Flight_Batches_With_Their_Status_And_Totals()
    {
        CreateRequesterEmployee("OBL100REQ");
        CreateCodeFourType();
        EnableWorkflow("OBL100APP");
        var s = CreateScenario("OBL100");

        var batchId = await _batches.CreateOvertimeBatch(BatchFor(s.Unit, (s.RegisterA1, 3, null), (s.RegisterB1, 1.5, null)));

        var row = (await _batches.GetAll(new GetAllOvertimeBatchesInput { MaxResultCount = 10 }))
            .Items.Single(i => i.Id == batchId);

        row.WorkflowStatus.ShouldBe(RequestStatus.Open);
        row.LineCount.ShouldBe(2);
        row.TotalApprovedHours.ShouldBe(4.5);
        row.OrganizationUnitName.ShouldBe("OBL100 Dept");
        row.RequestNumber.ShouldNotBeNullOrWhiteSpace();
        row.IsPosted.ShouldBeFalse();
    }

    #endregion
}
