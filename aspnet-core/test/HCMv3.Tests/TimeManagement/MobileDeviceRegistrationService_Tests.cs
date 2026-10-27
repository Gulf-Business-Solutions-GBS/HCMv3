using System;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// The lookup legacy never built: "is this device id registered, approved and active for this employee as of
/// this date?"
///
/// <para>
/// Legacy wrote <c>DeviceId</c> onto a request row and then never read it back anywhere -- the only other
/// occurrence of the name in the entire legacy codebase is a duplicated validation string. These tests are
/// therefore the ones that give the transaction type a purpose: without an answerable question, an approved
/// device registration is a form that does nothing.
/// </para>
///
/// <para>
/// Note what is NOT tested here, because it deliberately does not exist: nothing in <see cref="TimeEvent"/>
/// or the existing attendance path calls this. Enforcing device registration on the punch path would lock
/// out every employee currently punching from an unregistered handset, which is a separate decision.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MobileDeviceRegistrationService_Tests : AppTestBase
{
    private readonly IMobileDeviceRegistrationService _registrations;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public MobileDeviceRegistrationService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _registrations = Resolve<IMobileDeviceRegistrationService>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private const string TheDeviceId = "HANDSET-0001";
    private static readonly DateTime Today = new DateTime(2026, 6, 15);

    private int CreateEmployee(string code)
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
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private int RegisterDevice(
        int employeeId,
        RequestStatus? status,
        bool isActive = true,
        DateTime? effectiveDate = null,
        string deviceId = TheDeviceId)
    {
        EmployeeMobileDevice device = null;

        UsingDbContext(context =>
        {
            device = context.Set<EmployeeMobileDevice>().Add(new EmployeeMobileDevice
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DeviceId = deviceId,
                DeviceType = "iPhone 14",
                EffectiveDate = effectiveDate ?? Today.AddDays(-30),
                IsActive = isActive,
                WorkflowStatus = status
            }).Entity;
        });

        return device.Id;
    }

    /// <summary>
    /// The domain service is resolved from the container but is not an application service, so no unit of
    /// work is started for it automatically. Each call is wrapped in an explicit ambient scope, which is
    /// also where the <c>ApprovedOnly</c> filter lives.
    /// </summary>
    private async Task<T> RunInUnitOfWorkAsync<T>(Func<Task<T>> action)
    {
        using var uow = _unitOfWorkManager.Begin();
        var result = await action();
        await uow.CompleteAsync();
        return result;
    }

    private Task<bool> IsRegisteredAsync(int employeeId, string deviceId = TheDeviceId, DateTime? asOf = null) =>
        RunInUnitOfWorkAsync(() => _registrations.IsDeviceRegisteredAsync(employeeId, deviceId, asOf ?? Today));

    // ---------------------------------------------------------------- the one case that answers yes

    [Fact]
    public async Task Should_Accept_An_Approved_Active_In_Effect_Device()
    {
        var meId = CreateEmployee("MDL100");
        RegisterDevice(meId, RequestStatus.Approved);

        (await IsRegisteredAsync(meId)).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Accept_A_Device_HR_Entered_Directly()
    {
        // WorkflowStatus null means the row was not workflow-managed -- HR keyed it in, or the type has
        // routing switched off. That is effective, exactly as the ApprovedOnly filter reads it. Treating
        // null as "not approved" would break every tenant running without workflow.
        var meId = CreateEmployee("MDL110");
        RegisterDevice(meId, status: null);

        (await IsRegisteredAsync(meId)).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Accept_A_Device_That_Became_Effective_Today()
    {
        // The boundary is inclusive: "from the 15th" means the 15th is in.
        var meId = CreateEmployee("MDL120");
        RegisterDevice(meId, RequestStatus.Approved, effectiveDate: Today);

        (await IsRegisteredAsync(meId)).ShouldBeTrue();
    }

    // ---------------------------------------------------------------- everything that answers no

    [Fact]
    public async Task Should_Refuse_A_Device_Still_Awaiting_Approval()
    {
        // The important one. A registration that has been asked for is not one that has been granted, and
        // the whole point of putting the workflow columns on this row is that the difference is enforceable.
        var meId = CreateEmployee("MDL200");
        RegisterDevice(meId, RequestStatus.Open);

        (await IsRegisteredAsync(meId)).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Refuse_A_Device_Still_In_Draft()
    {
        var meId = CreateEmployee("MDL210");
        RegisterDevice(meId, RequestStatus.Draft);

        (await IsRegisteredAsync(meId)).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Refuse_A_Rejected_Device()
    {
        var meId = CreateEmployee("MDL220");
        RegisterDevice(meId, RequestStatus.Rejected);

        (await IsRegisteredAsync(meId)).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Refuse_A_Deactivated_Device()
    {
        // The row survives deactivation because it is the audit trail for punches already accepted from the
        // handset. It must stop authorising new ones the moment it is switched off.
        var meId = CreateEmployee("MDL230");
        RegisterDevice(meId, RequestStatus.Approved, isActive: false);

        (await IsRegisteredAsync(meId)).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Refuse_A_Device_That_Is_Not_Effective_Yet()
    {
        // Approved but future-dated. Ignoring EffectiveDate would make every registration effective the
        // moment it was signed off, which is not what the employee or the approver agreed to.
        var meId = CreateEmployee("MDL240");
        RegisterDevice(meId, RequestStatus.Approved, effectiveDate: Today.AddDays(1));

        (await IsRegisteredAsync(meId)).ShouldBeFalse();

        // ...and starts answering yes on the day it takes effect.
        (await IsRegisteredAsync(meId, asOf: Today.AddDays(1))).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_Another_Employees_Device()
    {
        // The check that makes this an authorisation rather than a device inventory: the handset is
        // registered and approved, just not to the employee whose punch is being offered.
        var meId = CreateEmployee("MDL250");
        var otherId = CreateEmployee("MDL250OTHER");
        RegisterDevice(otherId, RequestStatus.Approved);

        (await IsRegisteredAsync(meId)).ShouldBeFalse();
        (await IsRegisteredAsync(otherId)).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_An_Unknown_Device_Id()
    {
        var meId = CreateEmployee("MDL260");
        RegisterDevice(meId, RequestStatus.Approved);

        (await IsRegisteredAsync(meId, deviceId: "SOME-OTHER-HANDSET")).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Refuse_A_Missing_Device_Id_Rather_Than_Matching_Anything()
    {
        // An app that sends no device id must get a no, not a match against whatever row happens to have a
        // null or blank DeviceId -- which is precisely the row legacy's `== ""` check would have allowed to
        // be created.
        var meId = CreateEmployee("MDL270");
        RegisterDevice(meId, RequestStatus.Approved);

        (await IsRegisteredAsync(meId, deviceId: null)).ShouldBeFalse();
        (await IsRegisteredAsync(meId, deviceId: "")).ShouldBeFalse();
        (await IsRegisteredAsync(meId, deviceId: "   ")).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Ignore_Surrounding_Whitespace_On_The_Device_Id()
    {
        // The registration stores a trimmed id, so the lookup has to trim too or a mobile client that pads
        // the value would be refused for a device that is in fact registered.
        var meId = CreateEmployee("MDL280");
        RegisterDevice(meId, RequestStatus.Approved);

        (await IsRegisteredAsync(meId, deviceId: "  " + TheDeviceId + "  ")).ShouldBeTrue();
    }

    // ---------------------------------------------------------------- the rules, at the domain level

    private async Task EnsureAllowedAsync(int employeeId, string deviceId, string deviceType)
    {
        using var uow = _unitOfWorkManager.Begin();
        await _registrations.EnsureRegistrationIsAllowedAsync(employeeId, deviceId, deviceType);
        await uow.CompleteAsync();
    }

    [Fact]
    public async Task Should_Refuse_A_Null_Device_Id_At_The_Domain_Rule()
    {
        // THE legacy bug, asserted where the rule actually lives rather than at the DTO annotation that also
        // happens to catch it. Legacy's `if (this.DeviceId == "")` is false for null.
        var meId = CreateEmployee("MDL300");

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            EnsureAllowedAsync(meId, null, "iPhone 14"));
    }

    [Fact]
    public async Task Should_Refuse_A_Blank_Or_Whitespace_Device_Id_At_The_Domain_Rule()
    {
        var meId = CreateEmployee("MDL310");

        await Assert.ThrowsAsync<UserFriendlyException>(() => EnsureAllowedAsync(meId, "", "iPhone 14"));
        await Assert.ThrowsAsync<UserFriendlyException>(() => EnsureAllowedAsync(meId, "   ", "iPhone 14"));
    }

    [Fact]
    public async Task Should_Refuse_A_Null_Or_Blank_Device_Type_At_The_Domain_Rule()
    {
        var meId = CreateEmployee("MDL320");

        await Assert.ThrowsAsync<UserFriendlyException>(() => EnsureAllowedAsync(meId, TheDeviceId, null));
        await Assert.ThrowsAsync<UserFriendlyException>(() => EnsureAllowedAsync(meId, TheDeviceId, ""));
        await Assert.ThrowsAsync<UserFriendlyException>(() => EnsureAllowedAsync(meId, TheDeviceId, " "));
    }

    [Fact]
    public async Task Should_Refuse_A_Duplicate_For_The_Same_Employee()
    {
        var meId = CreateEmployee("MDL330");
        RegisterDevice(meId, RequestStatus.Approved);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            EnsureAllowedAsync(meId, TheDeviceId, "iPhone 14"));
    }

    [Fact]
    public async Task Should_Refuse_A_Device_Live_For_Another_Employee()
    {
        var meId = CreateEmployee("MDL340");
        var otherId = CreateEmployee("MDL340OTHER");
        RegisterDevice(otherId, RequestStatus.Approved);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            EnsureAllowedAsync(meId, TheDeviceId, "iPhone 14"));
    }

    [Fact]
    public async Task Should_Allow_A_Device_Whose_Previous_Registration_Was_Rejected()
    {
        // A refusal is not a claim on the device id. If it blocked, an employee whose first attempt was
        // rejected for a typo elsewhere on the form could never register that handset again.
        var meId = CreateEmployee("MDL350");
        RegisterDevice(meId, RequestStatus.Rejected);

        await EnsureAllowedAsync(meId, TheDeviceId, "iPhone 14");
    }

    [Fact]
    public async Task Should_Not_Let_A_Registration_Compete_With_Itself()
    {
        // What the exclude-id parameter is for: re-running the check against a row already saved (as the
        // workflow handler does at submit) must not refuse it for duplicating itself.
        var meId = CreateEmployee("MDL360");
        var deviceRowId = RegisterDevice(meId, RequestStatus.Open);

        using var uow = _unitOfWorkManager.Begin();
        await _registrations.EnsureRegistrationIsAllowedAsync(
            meId, TheDeviceId, "iPhone 14", excludeDeviceRegistrationId: deviceRowId);
        await uow.CompleteAsync();
    }
}
