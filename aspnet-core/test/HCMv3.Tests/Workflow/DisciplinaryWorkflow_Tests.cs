using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers disciplinary records through the workflow engine.
///
/// <para>
/// Routed but NOT self-service, on structural evidence rather than taste: legacy auto-filled the REPORTER
/// with the logged-in employee while the SUBJECT was chosen separately, so the two are never the same
/// person. The catalog row therefore carries no ESS route and submission happens in the admin service.
/// </para>
///
/// <para>
/// The bulk of these tests is the ESCALATION LADDER, because that is where the risk is. Approving a
/// disciplinary record does not merely make it visible -- it decides what the offence COSTS, by counting
/// the employee's prior offences of the same type inside the type's repeat window and reading the matching
/// penalty tier. Getting the window boundary or the tier selection wrong docks the wrong amount from
/// somebody's pay, quietly.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class DisciplinaryWorkflow_Tests : AppTestBase
{
    private readonly IEmployeeDisciplinariesAppService _disciplinaries;

    public DisciplinaryWorkflow_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _disciplinaries = Resolve<IEmployeeDisciplinariesAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string ShortCode() => Guid.NewGuid().ToString("N").Substring(0, 8);

    private int CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = ShortCode(),
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
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private int CreateWorkLocation()
    {
        WorkLocation location = null;
        UsingDbContext(context =>
        {
            location = context.Set<WorkLocation>().Add(new WorkLocation
            {
                TenantId = TenantId,
                Code = ShortCode(),
                NameEn = "Head Office",
                NameAr = "Head Office",
                IsActive = true
            }).Entity;
        });

        return location.Id;
    }

    private int CreateDeductionType(string nameEn)
    {
        DisciplinaryDeductionType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<DisciplinaryDeductionType>().Add(new DisciplinaryDeductionType
            {
                TenantId = TenantId,
                Code = ShortCode(),
                NameEn = nameEn,
                NameAr = nameEn,
                DisciplinaryDeductionUnit = DisciplinaryDeductionUnit.Days,
                DeductionValue = 1
            }).Entity;
        });

        return type.Id;
    }

    /// <summary>
    /// An offence type with a three-rung ladder: 1st offence -> Warning, 2nd -> One Day, 3rd -> Three Days.
    /// </summary>
    private (int TypeId, int Tier1, int Tier2, int Tier3) CreateLadderedType(int repeatDurationInDays, params int[] tierCounts)
    {
        var tier1 = CreateDeductionType("Warning Letter");
        var tier2 = CreateDeductionType("One Day Deduction");
        var tier3 = CreateDeductionType("Three Days Deduction");
        var deductionTypeIds = new[] { tier1, tier2, tier3 };

        DisciplinaryType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<DisciplinaryType>().Add(new DisciplinaryType
            {
                TenantId = TenantId,
                Code = ShortCode(),
                NameEn = "Late Attendance",
                NameAr = "Late Attendance",
                RepeatDurationInDays = repeatDurationInDays
            }).Entity;
        });

        UsingDbContext(context =>
        {
            for (var i = 0; i < tierCounts.Length; i++)
            {
                context.Set<DisciplinaryTypeDetail>().Add(new DisciplinaryTypeDetail
                {
                    TenantId = TenantId,
                    DisciplinaryTypeId = type.Id,
                    DisciplinaryCount = tierCounts[i],
                    DisciplinaryDeductionTypeId = deductionTypeIds[i]
                });
            }
        });

        return (type.Id, tier1, tier2, tier3);
    }

    /// <summary>Turns disciplinary records on for approval, with a real approver holding the step's role.</summary>
    private int EnableWorkflow(string approverCode)
    {
        var approverId = CreateEmployee(approverCode);

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
                Code = "DSC-STD",
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

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeDisciplinary");
            type.IsWorkflowEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    /// <summary>An already-recorded offence, planted directly so the ladder has a history to count.</summary>
    private int PlantPriorOffence(int employeeId, int disciplinaryTypeId, DateTime on, RequestStatus? status = RequestStatus.Approved)
    {
        EmployeeDisciplinary record = null;
        UsingDbContext(context =>
        {
            record = context.Set<EmployeeDisciplinary>().Add(new EmployeeDisciplinary
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DisciplinaryTypeId = disciplinaryTypeId,
                DisciplinaryDate = on.Date,
                DisciplinaryTime = on,
                Severity = Severity.High,
                WorkflowStatus = status
            }).Entity;
        });

        return record.Id;
    }

    private CreateOrEditEmployeeDisciplinaryDto RecordFor(
        int employeeId,
        int disciplinaryTypeId,
        int workLocationId,
        int reportedById,
        DateTime on)
    {
        return new CreateOrEditEmployeeDisciplinaryDto
        {
            EmployeeId = employeeId,
            DisciplinaryTypeId = disciplinaryTypeId,
            WorkLocationId = workLocationId,
            ReportedById = reportedById,
            DisciplinaryDate = on.Date,
            DisciplinaryTime = on,
            Severity = Severity.High,
            Comments = "Arrived 40 minutes late"
        };
    }

    private EmployeeDisciplinary RecordRow(int employeeId) =>
        UsingDbContext(c => c.Set<EmployeeDisciplinary>()
            .IgnoreQueryFilters()
            .Where(r => r.EmployeeId == employeeId && !r.IsDeleted)
            .OrderByDescending(r => r.Id)
            .First());

    private async Task ApproveTheOnlyRequestAsync(int approverId)
    {
        var requestId = UsingDbContext(c => c.Set<WorkflowRequest>()
            .Single(r => r.RequestType == "EmployeeDisciplinary").Id);

        await Resolve<IWorkflowApprovalService>().ApproveAsync(requestId, approverId, "Agreed");
    }

    // ---------------------------------------------------------------------------------------------------
    // Routing
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Record_Immediately_While_The_Type_Is_Not_Routed()
    {
        // Every type is seeded dormant, so this is the behaviour on a fresh install and the behaviour HR
        // relies on today. Nothing about adding workflow may change it.
        var employeeId = CreateEmployee("DS100");
        var (typeId, _, _, _) = CreateLadderedType(90, 1, 2, 3);

        await _disciplinaries.CreateOrEdit(
            RecordFor(employeeId, typeId, CreateWorkLocation(), CreateEmployee("DS100RPT"), DateTime.Today));

        RecordRow(employeeId).WorkflowStatus.ShouldBeNull();

        UsingDbContext(c => c.Set<WorkflowRequest>().Count(r => r.RequestType == "EmployeeDisciplinary"))
            .ShouldBe(0);
    }

    [Fact]
    public async Task Should_Route_For_Approval_Once_The_Type_Is_Enabled()
    {
        var employeeId = CreateEmployee("DS110");
        EnableWorkflow("DS110APP");
        var (typeId, _, _, _) = CreateLadderedType(90, 1, 2, 3);

        await _disciplinaries.CreateOrEdit(
            RecordFor(employeeId, typeId, CreateWorkLocation(), CreateEmployee("DS110RPT"), DateTime.Today));

        var request = UsingDbContext(c => c.Set<WorkflowRequest>()
            .Single(r => r.RequestType == "EmployeeDisciplinary"));

        request.RequestStatus.ShouldBe(RequestStatus.Open);
        // A number of its own, and nothing but digits: the per-type prefix went when the counter became
        // tenant-wide, so "which type is this" is no longer something the number answers.
        request.RequestNumber.ShouldNotBeNullOrWhiteSpace();
        request.RequestNumber.ShouldNotContain("-");
        int.Parse(request.RequestNumber).ShouldBeGreaterThan(0);

        // About the SUBJECT of the record, not the person in HR who typed it -- the same rule as every
        // other admin-raised type.
        request.EmployeeId.ShouldBe(employeeId);

        // The offence type is the sub-type, so a safety violation can be routed differently from a late
        // arrival.
        request.RequestSubTypeId.ShouldBe(typeId);
    }

    [Fact]
    public async Task An_Unapproved_Record_Should_Be_Invisible_To_The_Rest_Of_The_System()
    {
        // An accusation nobody has agreed to must not appear in an employee's disciplinary file, and must
        // not feed the escalation count of some other module reading the table normally.
        var employeeId = CreateEmployee("DS120");
        EnableWorkflow("DS120APP");
        var (typeId, _, _, _) = CreateLadderedType(90, 1, 2, 3);

        await _disciplinaries.CreateOrEdit(
            RecordFor(employeeId, typeId, CreateWorkLocation(), CreateEmployee("DS120RPT"), DateTime.Today));

        var repository = Resolve<IRepository<EmployeeDisciplinary>>();
        var unitOfWorkManager = Resolve<IUnitOfWorkManager>();

        using (unitOfWorkManager.Begin())
        {
            // ApprovedOnly is ON here, exactly as an employee-file screen would see it.
            repository.GetAll().Any(r => r.EmployeeId == employeeId)
                .ShouldBeFalse("An unapproved disciplinary record was visible to the rest of the system.");
        }
    }

    [Fact]
    public void Disciplinary_Records_Should_Not_Be_Offered_As_Self_Service()
    {
        // Legacy auto-set the reporter to the logged-in employee while the subject was picked separately:
        // the reporter is never the subject. Nobody raises a disciplinary record about themselves, so the
        // catalog row carries no ESS route at all.
        var type = UsingDbContext(c => c.Set<WorkflowTransactionType>()
            .First(t => t.Code == "EmployeeDisciplinary"));

        type.EssRoute.ShouldBeNull();
        type.EssPermissionName.ShouldBeNull();
        type.IsEssEnabled.ShouldBeFalse();
    }

    // ---------------------------------------------------------------------------------------------------
    // The escalation ladder
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_First_Offence_Should_Take_The_Lowest_Rung()
    {
        var employeeId = CreateEmployee("DS200");
        var approverId = EnableWorkflow("DS200APP");
        var (typeId, tier1, _, _) = CreateLadderedType(90, 1, 2, 3);

        await _disciplinaries.CreateOrEdit(
            RecordFor(employeeId, typeId, CreateWorkLocation(), CreateEmployee("DS200RPT"), DateTime.Today));

        await ApproveTheOnlyRequestAsync(approverId);

        var row = RecordRow(employeeId);
        row.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        row.DisciplinaryDeductionTypeId.ShouldBe(tier1);
    }

    [Fact]
    public async Task A_Third_Offence_Inside_The_Window_Should_Take_The_Third_Rung()
    {
        // This is the whole reason the type is worth routing: the same offence costs more the third time.
        var employeeId = CreateEmployee("DS210");
        var approverId = EnableWorkflow("DS210APP");
        var (typeId, _, _, tier3) = CreateLadderedType(90, 1, 2, 3);

        PlantPriorOffence(employeeId, typeId, DateTime.Today.AddDays(-30));
        PlantPriorOffence(employeeId, typeId, DateTime.Today.AddDays(-10));

        await _disciplinaries.CreateOrEdit(
            RecordFor(employeeId, typeId, CreateWorkLocation(), CreateEmployee("DS210RPT"), DateTime.Today));

        await ApproveTheOnlyRequestAsync(approverId);

        RecordRow(employeeId).DisciplinaryDeductionTypeId.ShouldBe(tier3);
    }

    [Fact]
    public async Task An_Offence_Outside_The_Repeat_Window_Should_Not_Count()
    {
        // The window is what makes this a rehabilitation ladder rather than a permanent record. Two late
        // arrivals four months ago, on a type that forgets after 90 days, must leave today's back at rung
        // one -- otherwise an employee is punished forever for something the policy says has aged out.
        var employeeId = CreateEmployee("DS220");
        var approverId = EnableWorkflow("DS220APP");
        var (typeId, tier1, _, _) = CreateLadderedType(90, 1, 2, 3);

        PlantPriorOffence(employeeId, typeId, DateTime.Today.AddDays(-200));
        PlantPriorOffence(employeeId, typeId, DateTime.Today.AddDays(-120));

        await _disciplinaries.CreateOrEdit(
            RecordFor(employeeId, typeId, CreateWorkLocation(), CreateEmployee("DS220RPT"), DateTime.Today));

        await ApproveTheOnlyRequestAsync(approverId);

        RecordRow(employeeId).DisciplinaryDeductionTypeId.ShouldBe(tier1);
    }

    [Fact]
    public async Task The_Window_Boundary_Should_Be_Inclusive()
    {
        // Exactly RepeatDurationInDays earlier is still INSIDE the window, matching legacy's
        // DisciplinaryDate >= DisciplinaryDate.AddDays(-RepeatDurationInDays). One day further out is not.
        // The two assertions together pin the boundary from both sides, which is the only way to catch an
        // off-by-one that would silently under- or over-punish everyone sitting on the edge.
        var employeeId = CreateEmployee("DS230");
        var approverId = EnableWorkflow("DS230APP");
        var (typeId, _, tier2, _) = CreateLadderedType(90, 1, 2, 3);

        PlantPriorOffence(employeeId, typeId, DateTime.Today.AddDays(-90));
        PlantPriorOffence(employeeId, typeId, DateTime.Today.AddDays(-91));

        await _disciplinaries.CreateOrEdit(
            RecordFor(employeeId, typeId, CreateWorkLocation(), CreateEmployee("DS230RPT"), DateTime.Today));

        await ApproveTheOnlyRequestAsync(approverId);

        // Only the -90 one counted, so this is the SECOND offence, not the third.
        RecordRow(employeeId).DisciplinaryDeductionTypeId.ShouldBe(tier2);
    }

    [Fact]
    public async Task A_Pending_Record_Should_Still_Count_Towards_The_Ladder()
    {
        // Otherwise the ladder is defeated by raising both offences before either is approved -- two late
        // arrivals in a week would each be treated as a first. Counting in-flight records is the
        // conservative direction and the correct one.
        var employeeId = CreateEmployee("DS240");
        var approverId = EnableWorkflow("DS240APP");
        var (typeId, _, tier2, _) = CreateLadderedType(90, 1, 2, 3);

        PlantPriorOffence(employeeId, typeId, DateTime.Today.AddDays(-5), RequestStatus.Open);

        await _disciplinaries.CreateOrEdit(
            RecordFor(employeeId, typeId, CreateWorkLocation(), CreateEmployee("DS240RPT"), DateTime.Today));

        await ApproveTheOnlyRequestAsync(approverId);

        RecordRow(employeeId).DisciplinaryDeductionTypeId.ShouldBe(tier2);
    }

    [Fact]
    public async Task A_Rejected_Record_Should_Not_Count_Towards_The_Ladder()
    {
        // The other side of counting in-flight records. An accusation an approver refused is not an
        // offence, and must not push the next one up a rung.
        var employeeId = CreateEmployee("DS250");
        var approverId = EnableWorkflow("DS250APP");
        var (typeId, tier1, _, _) = CreateLadderedType(90, 1, 2, 3);

        PlantPriorOffence(employeeId, typeId, DateTime.Today.AddDays(-5), RequestStatus.Rejected);

        await _disciplinaries.CreateOrEdit(
            RecordFor(employeeId, typeId, CreateWorkLocation(), CreateEmployee("DS250RPT"), DateTime.Today));

        await ApproveTheOnlyRequestAsync(approverId);

        RecordRow(employeeId).DisciplinaryDeductionTypeId.ShouldBe(tier1);
    }

    [Fact]
    public async Task An_Offence_Of_A_Different_Type_Should_Not_Count()
    {
        // The ladder is per offence TYPE. Being late twice says nothing about a safety violation.
        var employeeId = CreateEmployee("DS260");
        var approverId = EnableWorkflow("DS260APP");
        var (typeId, tier1, _, _) = CreateLadderedType(90, 1, 2, 3);
        var (otherTypeId, _, _, _) = CreateLadderedType(90, 1, 2, 3);

        PlantPriorOffence(employeeId, otherTypeId, DateTime.Today.AddDays(-5));
        PlantPriorOffence(employeeId, otherTypeId, DateTime.Today.AddDays(-3));

        await _disciplinaries.CreateOrEdit(
            RecordFor(employeeId, typeId, CreateWorkLocation(), CreateEmployee("DS260RPT"), DateTime.Today));

        await ApproveTheOnlyRequestAsync(approverId);

        RecordRow(employeeId).DisciplinaryDeductionTypeId.ShouldBe(tier1);
    }

    [Fact]
    public async Task Another_Employees_Offences_Should_Not_Count()
    {
        var employeeId = CreateEmployee("DS270");
        var approverId = EnableWorkflow("DS270APP");
        var (typeId, tier1, _, _) = CreateLadderedType(90, 1, 2, 3);

        var colleagueId = CreateEmployee("DS270COL");
        PlantPriorOffence(colleagueId, typeId, DateTime.Today.AddDays(-5));
        PlantPriorOffence(colleagueId, typeId, DateTime.Today.AddDays(-3));

        await _disciplinaries.CreateOrEdit(
            RecordFor(employeeId, typeId, CreateWorkLocation(), CreateEmployee("DS270RPT"), DateTime.Today));

        await ApproveTheOnlyRequestAsync(approverId);

        RecordRow(employeeId).DisciplinaryDeductionTypeId.ShouldBe(tier1);
    }

    [Fact]
    public async Task The_Ladder_Should_Hold_At_Its_Top_Rung()
    {
        // Tiers of 1/2/3 and a fifth offence: the highest tier at or below the offence number wins, so it
        // stays at the top rung rather than falling off the end and resolving to nothing.
        var employeeId = CreateEmployee("DS280");
        var approverId = EnableWorkflow("DS280APP");
        var (typeId, _, _, tier3) = CreateLadderedType(90, 1, 2, 3);

        for (var i = 1; i <= 4; i++)
        {
            PlantPriorOffence(employeeId, typeId, DateTime.Today.AddDays(-i * 5));
        }

        await _disciplinaries.CreateOrEdit(
            RecordFor(employeeId, typeId, CreateWorkLocation(), CreateEmployee("DS280RPT"), DateTime.Today));

        await ApproveTheOnlyRequestAsync(approverId);

        RecordRow(employeeId).DisciplinaryDeductionTypeId.ShouldBe(tier3);
    }

    [Fact]
    public async Task A_Ladder_That_Starts_Above_This_Offence_Should_Resolve_To_No_Penalty()
    {
        // A type whose first rung is the third offence says nothing about a first one. Leaving the penalty
        // unset is the honest answer; inventing one would deduct pay the policy never described.
        var employeeId = CreateEmployee("DS290");
        var approverId = EnableWorkflow("DS290APP");
        var (typeId, _, _, _) = CreateLadderedType(90, 3, 4, 5);

        await _disciplinaries.CreateOrEdit(
            RecordFor(employeeId, typeId, CreateWorkLocation(), CreateEmployee("DS290RPT"), DateTime.Today));

        await ApproveTheOnlyRequestAsync(approverId);

        var row = RecordRow(employeeId);
        row.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        row.DisciplinaryDeductionTypeId.ShouldBeNull();
    }

    [Fact]
    public async Task Approval_Should_Overrule_A_Penalty_Typed_Into_The_Form()
    {
        // The ladder decides what a repeat offence costs, not whoever filled in the form. Otherwise the
        // tiers configured on the type are a suggestion.
        var employeeId = CreateEmployee("DS300");
        var approverId = EnableWorkflow("DS300APP");
        var (typeId, tier1, _, tier3) = CreateLadderedType(90, 1, 2, 3);

        var input = RecordFor(employeeId, typeId, CreateWorkLocation(), CreateEmployee("DS300RPT"), DateTime.Today);
        input.DisciplinaryDeductionTypeId = tier3;

        await _disciplinaries.CreateOrEdit(input);
        await ApproveTheOnlyRequestAsync(approverId);

        RecordRow(employeeId).DisciplinaryDeductionTypeId.ShouldBe(tier1);
    }

    // ---------------------------------------------------------------------------------------------------
    // What the approver and the reporter are shown
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task The_Summary_Should_Name_The_Penalty_Being_Agreed_To()
    {
        // An approver of a disciplinary record is agreeing to what it COSTS, not merely that something
        // happened. A summary that names only the offence would have them sign off a deduction they were
        // never shown.
        var employeeId = CreateEmployee("DS310");
        EnableWorkflow("DS310APP");
        var (typeId, _, _, _) = CreateLadderedType(90, 1, 2, 3);

        PlantPriorOffence(employeeId, typeId, DateTime.Today.AddDays(-10));

        await _disciplinaries.CreateOrEdit(
            RecordFor(employeeId, typeId, CreateWorkLocation(), CreateEmployee("DS310RPT"), DateTime.Today));

        var request = UsingDbContext(c => c.Set<WorkflowRequest>()
            .Single(r => r.RequestType == "EmployeeDisciplinary"));

        request.SummaryEn.ShouldStartWith("Late Attendance");
        request.SummaryEn.ShouldContain("offence 2");
        request.SummaryEn.ShouldContain("One Day Deduction");
    }

    [Fact]
    public async Task The_Consequence_Should_Be_Visible_Before_The_Record_Is_Raised()
    {
        // The preview exists so the person filing a third late arrival knows they are about to trigger a
        // three-day deduction, rather than discovering it after an approver has agreed to it.
        var employeeId = CreateEmployee("DS320");
        var (typeId, _, _, tier3) = CreateLadderedType(90, 1, 2, 3);

        PlantPriorOffence(employeeId, typeId, DateTime.Today.AddDays(-30));
        PlantPriorOffence(employeeId, typeId, DateTime.Today.AddDays(-10));
        PlantPriorOffence(employeeId, typeId, DateTime.Today.AddDays(-200));

        var preview = await _disciplinaries.GetDisciplinaryEscalation(new GetDisciplinaryEscalationInput
        {
            EmployeeId = employeeId,
            DisciplinaryTypeId = typeId,
            DisciplinaryDate = DateTime.Today
        });

        preview.PriorDisciplinaryCount.ShouldBe(2);
        preview.OffenceNumber.ShouldBe(3);
        preview.DisciplinaryDeductionTypeId.ShouldBe(tier3);
        preview.DisciplinaryDeductionTypeNameEn.ShouldBe("Three Days Deduction");
        preview.RepeatDurationInDays.ShouldBe(90);
        preview.WindowStartDate.ShouldBe(DateTime.Today.AddDays(-90));
        preview.WindowEndDate.ShouldBe(DateTime.Today);

        // All three earlier offences are listed, but the aged-out one is marked as not counting -- "you had
        // one last year and it has expired" is exactly what the reporter needs to see.
        preview.History.Count.ShouldBe(3);
        preview.History.Count(h => h.IsWithinRepeatWindow).ShouldBe(2);
    }

    [Fact]
    public async Task The_Preview_Should_Not_Count_The_Record_Being_Edited()
    {
        // Reopening a record must not make it its own predecessor and bump itself up a rung on every save.
        var employeeId = CreateEmployee("DS330");
        var (typeId, tier1, _, _) = CreateLadderedType(90, 1, 2, 3);

        var existingId = PlantPriorOffence(employeeId, typeId, DateTime.Today);

        var forEdit = await _disciplinaries.GetEmployeeDisciplinaryForEdit(
            new Abp.Application.Services.Dto.EntityDto(existingId));

        forEdit.Escalation.ShouldNotBeNull();
        forEdit.Escalation.PriorDisciplinaryCount.ShouldBe(0);
        forEdit.Escalation.OffenceNumber.ShouldBe(1);
        forEdit.Escalation.DisciplinaryDeductionTypeId.ShouldBe(tier1);
    }
}
