using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Runtime.Validation;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Self-service mobile device registration -- an employee enrolling the handset the app may punch from.
///
/// <para>
/// The tests to read first are the two on a blank and a NULL device id. Legacy checked
/// <c>if (this.DeviceId == "")</c>, which is false for null, so a request carrying no device id at all
/// passed validation and was recorded. Both cases are asserted separately here precisely because they had
/// different outcomes in legacy and must now have the same one.
/// </para>
///
/// <para>
/// Everything else here is the single-table contract: the pending registration lives in the real table, so
/// it must be the <c>ApprovedOnly</c> filter -- not a second request table -- that keeps it from counting as
/// a usable device, and a rejected one must never start counting.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyMobileDeviceAppService_Tests : AppTestBase
{
    private readonly IMyMobileDeviceAppService _devices;

    public MyMobileDeviceAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _devices = Resolve<IMyMobileDeviceAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private const string TheDeviceId = "A1B2C3D4E5F6";
    private const string TheDeviceType = "iPhone 14";

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

    private int CreateCurrentUserEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    /// <summary>Turns workflow on for the device type, pointing it at a one-step chain with a real
    /// approver. Returns the approver's employee id.</summary>
    private int EnableMobileDeviceWorkflow(string approverCode)
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
                Code = "MDV-STD-" + Guid.NewGuid().ToString("N").Substring(0, 4),
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
                EndDate = DateTime.Today.AddYears(5)
            });

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeMobileDevice");
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    private static RegisterMyMobileDeviceInput DeviceInput(
        string deviceId = TheDeviceId,
        string deviceType = TheDeviceType,
        DateTime? effectiveDate = null) =>
        new RegisterMyMobileDeviceInput
        {
            DeviceId = deviceId,
            DeviceType = deviceType,
            EffectiveDate = effectiveDate,
            Comments = "My work phone"
        };

    private static GetMyRequestsInput ListInput() =>
        new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 };

    private async Task DecideTheOutstandingRequestAsync(int approverId, bool approve)
    {
        var requestId = UsingDbContext(c => c.Set<WorkflowRequest>()
            .Single(r => r.RequestType == "EmployeeMobileDevice").Id);

        var approval = Resolve<IWorkflowApprovalService>();

        if (approve)
        {
            await approval.ApproveAsync(requestId, approverId, "Agreed");
        }
        else
        {
            await approval.RejectAsync(requestId, approverId, "No");
        }
    }

    /// <summary>
    /// Reads the row through a repository inside a unit of work, because that is where ABP data filters
    /// live. A raw DbContext query would bypass <c>ApprovedOnly</c> entirely and pass whatever the filter
    /// did.
    /// </summary>
    private bool IsVisibleToTheRestOfTheSystem(int transactionId)
    {
        var repository = Resolve<Abp.Domain.Repositories.IRepository<EmployeeMobileDevice>>();
        var unitOfWorkManager = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>();

        using (unitOfWorkManager.Begin())
        {
            return repository.GetAll().Any(d => d.Id == transactionId);
        }
    }

    // ---------------------------------------------------------------- the single-table contract

    [Fact]
    public async Task A_Submitted_Registration_Should_Be_Hidden_Until_It_Is_Approved()
    {
        // The premise of the design: the pending row lives in the real table, so the ApprovedOnly filter is
        // what stops an unapproved handset counting as registered. In legacy the row was a request in a
        // request table and there was nothing for approval to produce at all.
        CreateCurrentUserEmployee("MDV100");
        var approverId = EnableMobileDeviceWorkflow("MDV100APP");

        var submission = await _devices.RegisterMyMobileDevice(DeviceInput());
        submission.SubmittedForApproval.ShouldBeTrue();

        IsVisibleToTheRestOfTheSystem(submission.TransactionId)
            .ShouldBeFalse("A pending device registration was visible to the rest of the system.");

        // But the employee sees their own.
        var mine = await _devices.GetMyMobileDevices(ListInput());
        mine.Items.Select(i => i.Id).ShouldContain(submission.TransactionId);
        mine.Items.Single(i => i.Id == submission.TransactionId).IsUsableToday
            .ShouldBeFalse("A device awaiting approval must not be shown as usable.");

        await DecideTheOutstandingRequestAsync(approverId, approve: true);

        IsVisibleToTheRestOfTheSystem(submission.TransactionId)
            .ShouldBeTrue("An approved device registration should be visible -- approval is the whole side effect.");

        var row = UsingDbContext(c => c.Set<EmployeeMobileDevice>().IgnoreQueryFilters()
            .Single(d => d.Id == submission.TransactionId));
        row.WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task A_Rejected_Registration_Should_Stay_Invisible()
    {
        // The direction that would be dangerous to get wrong: a refused handset must never become one the
        // punch lookup accepts.
        CreateCurrentUserEmployee("MDV110");
        var approverId = EnableMobileDeviceWorkflow("MDV110APP");

        var submission = await _devices.RegisterMyMobileDevice(DeviceInput());

        await DecideTheOutstandingRequestAsync(approverId, approve: false);

        var row = UsingDbContext(c => c.Set<EmployeeMobileDevice>().IgnoreQueryFilters()
            .Single(d => d.Id == submission.TransactionId));
        row.WorkflowStatus.ShouldBe(RequestStatus.Rejected);

        IsVisibleToTheRestOfTheSystem(submission.TransactionId)
            .ShouldBeFalse("A rejected device registration was visible to the rest of the system.");
    }

    // ---------------------------------------------------------------- the validation legacy got wrong

    [Fact]
    public async Task A_Blank_Device_Id_Should_Be_Refused()
    {
        // Legacy's own rule, reinstated. AbpValidationException rather than UserFriendlyException: the
        // [Required]/MinimumLength annotation on the input refuses this before the method body runs, which
        // is the earlier and friendlier of the two failures. The domain rule underneath is asserted directly
        // in MobileDeviceRegistrationService_Tests.
        CreateCurrentUserEmployee("MDV200");

        await Assert.ThrowsAsync<AbpValidationException>(() =>
            _devices.RegisterMyMobileDevice(DeviceInput(deviceId: "")));
    }

    [Fact]
    public async Task A_Null_Device_Id_Should_Be_Refused()
    {
        // THE legacy bug. `if (this.DeviceId == "")` is false when DeviceId is null, so legacy accepted a
        // registration for no device whatsoever -- a row that could never authorise anything and that
        // nobody would ever discover, because nothing read DeviceId back.
        CreateCurrentUserEmployee("MDV210");

        await Assert.ThrowsAsync<AbpValidationException>(() =>
            _devices.RegisterMyMobileDevice(DeviceInput(deviceId: null)));
    }

    [Fact]
    public async Task A_Whitespace_Device_Id_Should_Be_Refused()
    {
        // A mobile form posting a space. Refused, and it is worth recording WHERE: .NET's own
        // RequiredAttribute trims before testing for emptiness (AllowEmptyStrings defaults to false), so the
        // annotation catches this before the method body runs and the answer is AbpValidationException. The
        // domain rule underneath refuses it independently -- asserted with UserFriendlyException in
        // MobileDeviceRegistrationService_Tests -- and that is the one that matters, because HR's own screen
        // and the workflow handler reach the rule without ever going through this DTO.
        CreateCurrentUserEmployee("MDV220");

        await Assert.ThrowsAsync<AbpValidationException>(() =>
            _devices.RegisterMyMobileDevice(DeviceInput(deviceId: "   ")));
    }

    [Fact]
    public async Task A_Blank_Device_Type_Should_Be_Refused()
    {
        CreateCurrentUserEmployee("MDV230");

        await Assert.ThrowsAsync<AbpValidationException>(() =>
            _devices.RegisterMyMobileDevice(DeviceInput(deviceType: "")));
    }

    [Fact]
    public async Task A_Null_Device_Type_Should_Be_Refused()
    {
        // Same legacy bug on the second field: `if (this.DeviceType == "")` also let null through.
        CreateCurrentUserEmployee("MDV240");

        await Assert.ThrowsAsync<AbpValidationException>(() =>
            _devices.RegisterMyMobileDevice(DeviceInput(deviceType: null)));
    }

    [Fact]
    public async Task A_Whitespace_Device_Type_Should_Be_Refused()
    {
        // Same as the whitespace device id: RequiredAttribute trims, so this fails at the annotation. The
        // domain rule refuses it independently -- see MobileDeviceRegistrationService_Tests.
        CreateCurrentUserEmployee("MDV250");

        await Assert.ThrowsAsync<AbpValidationException>(() =>
            _devices.RegisterMyMobileDevice(DeviceInput(deviceType: " ")));
    }

    // ---------------------------------------------------------------- the duplicate rule

    [Fact]
    public async Task Registering_The_Same_Device_Twice_Should_Be_Refused_While_The_First_Is_Still_Pending()
    {
        // The first registration is Open, which ApprovedOnly hides -- so a guard that did not look past the
        // filter would accept the duplicate silently, exactly when a resubmission is most likely: the
        // employee did not see their first submission land.
        CreateCurrentUserEmployee("MDV300");
        EnableMobileDeviceWorkflow("MDV300APP");

        var first = await _devices.RegisterMyMobileDevice(DeviceInput());
        first.SubmittedForApproval.ShouldBeTrue();

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _devices.RegisterMyMobileDevice(DeviceInput()));
    }

    [Fact]
    public async Task Registering_The_Same_Device_Twice_Should_Be_Refused_When_The_First_Is_Already_Granted()
    {
        // The HR-entered case: WorkflowStatus null, i.e. effective immediately. A second row would grant
        // nothing the first does not and would turn deactivating the handset into a hunt for copies.
        var meId = CreateCurrentUserEmployee("MDV310");

        UsingDbContext(context =>
        {
            context.Set<EmployeeMobileDevice>().Add(new EmployeeMobileDevice
            {
                TenantId = TenantId,
                EmployeeId = meId,
                DeviceId = TheDeviceId,
                DeviceType = TheDeviceType,
                EffectiveDate = DateTime.Today.AddDays(-10),
                IsActive = true
            });
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _devices.RegisterMyMobileDevice(DeviceInput()));
    }

    [Fact]
    public async Task Registering_A_Device_That_Is_Live_For_Another_Employee_Should_Be_Refused()
    {
        // The deliberate call -- see MobileDeviceRegistrationService's remarks. This row authorises punching
        // AS somebody; shared between two employees, a punch from the handset cannot be attributed and
        // buddy-punching becomes untraceable. A genuinely shared wall tablet is an attendance TERMINAL and
        // belongs in the device configuration the attendance integration reads, not in one employee's
        // personal registration.
        CreateCurrentUserEmployee("MDV320");
        var otherId = CreateEmployee("MDV320OTHER");

        UsingDbContext(context =>
        {
            context.Set<EmployeeMobileDevice>().Add(new EmployeeMobileDevice
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                DeviceId = TheDeviceId,
                DeviceType = TheDeviceType,
                EffectiveDate = DateTime.Today.AddDays(-10),
                IsActive = true
            });
        });

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _devices.RegisterMyMobileDevice(DeviceInput()));

        // The other employee is not named: this endpoint must not read out which colleague owns any device
        // id the caller cares to try.
        exception.Message.ShouldNotContain("MDV320OTHER");
    }

    [Fact]
    public async Task Registering_A_Device_Another_Employee_Has_Deactivated_Should_Be_Allowed()
    {
        // The handset changed hands, or the colleague left. A retired registration is history, not a claim,
        // so it must not lock the device id out forever -- otherwise a second-hand phone could never be
        // enrolled again.
        CreateCurrentUserEmployee("MDV330");
        var otherId = CreateEmployee("MDV330OTHER");

        UsingDbContext(context =>
        {
            context.Set<EmployeeMobileDevice>().Add(new EmployeeMobileDevice
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                DeviceId = TheDeviceId,
                DeviceType = TheDeviceType,
                EffectiveDate = DateTime.Today.AddDays(-100),
                IsActive = false
            });
        });

        var result = await _devices.RegisterMyMobileDevice(DeviceInput());

        result.TransactionId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Registering_A_Different_Device_Should_Be_Allowed()
    {
        // The rule is per device id, not per employee: nobody is limited to one handset.
        CreateCurrentUserEmployee("MDV340");

        await _devices.RegisterMyMobileDevice(DeviceInput());
        var second = await _devices.RegisterMyMobileDevice(DeviceInput(deviceId: "SECOND-HANDSET"));

        second.TransactionId.ShouldBeGreaterThan(0);

        var mine = await _devices.GetMyMobileDevices(ListInput());
        mine.TotalCount.ShouldBe(2);
    }

    // ---------------------------------------------------------------- the shared contract

    [Fact]
    public async Task A_Registration_Should_Be_Attributed_To_The_Signed_In_Employee_And_Numbered_With_Its_Own_Prefix()
    {
        // There is no employee field on the input at all, deliberately: this row authorises punching as
        // somebody, so a form that could name its own employee could authorise punching as anybody.
        var meId = CreateCurrentUserEmployee("MDV400");
        EnableMobileDeviceWorkflow("MDV400APP");

        var result = await _devices.RegisterMyMobileDevice(DeviceInput());

        var row = UsingDbContext(c => c.Set<EmployeeMobileDevice>().IgnoreQueryFilters()
            .Single(d => d.Id == result.TransactionId));
        row.EmployeeId.ShouldBe(meId);
        row.IsActive.ShouldBeTrue();

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == result.WorkflowRequestId.Value));
        // A number of its own, and nothing but digits: the per-type prefix went when the counter became
        // tenant-wide, so "which type is this" is no longer something the number answers.
        request.RequestNumber.ShouldNotBeNullOrWhiteSpace();
        request.RequestNumber.ShouldNotContain("-");
        int.Parse(request.RequestNumber).ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task An_Unset_Effective_Date_Should_Default_To_Today()
    {
        // Legacy's AfterConstruction did the same. Left unset it would land on year 1, which reads to the
        // lookup as "in effect since forever".
        CreateCurrentUserEmployee("MDV410");

        var result = await _devices.RegisterMyMobileDevice(DeviceInput(effectiveDate: null));

        var row = UsingDbContext(c => c.Set<EmployeeMobileDevice>().IgnoreQueryFilters()
            .Single(d => d.Id == result.TransactionId));
        row.EffectiveDate.ShouldBe(DateTime.Today);
    }

    [Fact]
    public async Task Should_Record_Immediately_When_Workflow_Is_Off_For_The_Type()
    {
        // Every type is seeded dormant, so this is the behaviour on a fresh install. Note the device-id and
        // duplicate rules are STILL enforced with routing off (the tests above run without enabling
        // workflow) -- turning approval off must not turn the rules off with it.
        CreateCurrentUserEmployee("MDV420");

        var result = await _devices.RegisterMyMobileDevice(DeviceInput());

        result.SubmittedForApproval.ShouldBeFalse();
        result.WorkflowRequestId.ShouldBeNull();

        var row = UsingDbContext(c => c.Set<EmployeeMobileDevice>().IgnoreQueryFilters()
            .Single(d => d.Id == result.TransactionId));
        row.WorkflowStatus.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_List_Somebody_Elses_Devices()
    {
        CreateCurrentUserEmployee("MDV430");
        var otherId = CreateEmployee("MDV430OTHER");

        UsingDbContext(context =>
        {
            context.Set<EmployeeMobileDevice>().Add(new EmployeeMobileDevice
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                DeviceId = "SOMEBODY-ELSES",
                DeviceType = TheDeviceType,
                EffectiveDate = DateTime.Today,
                IsActive = true
            });
        });

        var mine = await _devices.GetMyMobileDevices(ListInput());

        mine.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Let_The_Employee_Withdraw_A_Pending_Registration()
    {
        CreateCurrentUserEmployee("MDV440");
        EnableMobileDeviceWorkflow("MDV440APP");

        var result = await _devices.RegisterMyMobileDevice(DeviceInput());

        await _devices.CancelMyMobileDevice(new Abp.Application.Services.Dto.EntityDto(result.TransactionId));

        var mine = await _devices.GetMyMobileDevices(ListInput());
        mine.Items.Select(i => i.Id).ShouldNotContain(result.TransactionId);
    }

    [Fact]
    public async Task Should_Refuse_A_User_With_No_Employee_Record()
    {
        // Admin accounts are never linked, so this is a state the shell must handle rather than an edge case.
        await Assert.ThrowsAsync<UserFriendlyException>(() => _devices.GetMyMobileDevices(ListInput()));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _devices.RegisterMyMobileDevice(DeviceInput()));
    }
}
