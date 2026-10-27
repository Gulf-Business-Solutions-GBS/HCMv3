using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Workflow;

namespace HCMv3.TimeManagement.Services;

/// <inheritdoc cref="IMobileDeviceRegistrationService"/>
///
/// <remarks>
/// <para>
/// <b>The duplicate call, and why it goes further than asked.</b> Legacy applied no duplicate rule of any
/// kind: the same handset could be registered for twenty employees, and one employee could register the same
/// handset over and over. Two rules replace that.
/// </para>
/// <para>
/// The first is uncontroversial -- the same employee may not hold two live registrations of the same device
/// id. A second row grants nothing the first does not, and it makes deactivating the device a job of finding
/// every copy rather than one row.
/// </para>
/// <para>
/// The second is the interesting one: the same device id may NOT be live for two different employees, and
/// this is refused rather than allowed. The argument for allowing it is a shared tablet on a wall that
/// several people punch from, which is a real thing in this domain. The argument against is what this
/// particular row authorises: it says "accept an attendance punch AS this employee from this handset", and
/// with two employees sharing one device id, a punch arriving from that handset cannot be attributed --
/// buddy-punching becomes untraceable, which is the exact failure attendance controls exist to prevent. A
/// shared wall tablet is a fixed attendance TERMINAL and belongs in the device/terminal configuration that
/// the attendance integration already reads; it is not one employee's personal handset, which is what this
/// table is for. Refusing is also the reversible direction: a tenant that genuinely needs sharing can have
/// the rule relaxed, whereas data recorded under an ambiguous device id cannot be disambiguated afterwards.
/// The refusal deliberately does not name the other employee -- an employee should not be able to enumerate
/// colleagues by guessing device ids.
/// </para>
///
/// <para>
/// <b>Deliberately not carried over from legacy:</b> the <c>catch (Exception ex) { throw ex; }</c> wrapper
/// around every validation, which reset the stack trace to the rethrow line and made the original failure
/// point unrecoverable from a log; and the raw <c>throw new Exception("DeviceId Cannot be blank.")</c>
/// English strings, which reach the user as a 500 rather than as a message. Both are replaced by
/// <see cref="UserFriendlyException"/> with localized keys and no catch at all.
/// </para>
/// </remarks>
public class MobileDeviceRegistrationService : HCMv3DomainServiceBase, IMobileDeviceRegistrationService, ITransientDependency
{
    private readonly IRepository<EmployeeMobileDevice> _deviceRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public MobileDeviceRegistrationService(
        IRepository<EmployeeMobileDevice> deviceRepository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _deviceRepository = deviceRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public Task<bool> IsDeviceRegisteredAsync(int employeeId, string deviceId, DateTime asOf)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            // No handset identifier at all cannot match a registration. Answered here rather than left to
            // the query, so "the app sent nothing" and "the app sent something unknown" both read as no.
            return Task.FromResult(false);
        }

        var normalized = deviceId.Trim();
        var on = asOf.Date;

        // The filter comes off and the status is asserted explicitly instead -- see the interface remarks.
        // An authorisation answer must not depend on whether some caller further up happened to disable it.
        using (DisableApprovedOnly())
        {
            var registered = _deviceRepository.GetAll()
                .Any(d => d.EmployeeId == employeeId
                          && d.DeviceId == normalized
                          && d.IsActive
                          && d.EffectiveDate <= on
                          && (d.WorkflowStatus == null
                              || d.WorkflowStatus == RequestStatus.Approved
                              || d.WorkflowStatus == RequestStatus.Closed));

            return Task.FromResult(registered);
        }
    }

    public Task EnsureRegistrationIsAllowedAsync(
        int employeeId,
        string deviceId,
        string deviceType,
        int? excludeDeviceRegistrationId = null)
    {
        // Legacy compared against "" -- which is false for null, so a null device id sailed through and a
        // registration for no device at all was recorded. Trimmed emptiness catches null, "" and "   ".
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new UserFriendlyException(L("MobileDeviceIdIsRequired"));
        }

        if (string.IsNullOrWhiteSpace(deviceType))
        {
            throw new UserFriendlyException(L("MobileDeviceTypeIsRequired"));
        }

        var normalized = deviceId.Trim();

        using (DisableApprovedOnly())
        {
            // A pending registration is still a claim on the device id, so Open counts as live here even
            // though it is invisible everywhere else. Draft and Rejected do not: a draft has not been asked
            // for and a rejected one was refused, and neither should stop the employee trying again.
            var live = _deviceRepository.GetAll()
                .Where(d => d.DeviceId == normalized
                            && d.IsActive
                            && (!excludeDeviceRegistrationId.HasValue || d.Id != excludeDeviceRegistrationId.Value)
                            && (d.WorkflowStatus == null
                                || d.WorkflowStatus == RequestStatus.Approved
                                || d.WorkflowStatus == RequestStatus.Closed
                                || d.WorkflowStatus == RequestStatus.Open))
                .Select(d => new { d.Id, d.EmployeeId })
                .ToList();

            if (live.Any(d => d.EmployeeId == employeeId))
            {
                throw new UserFriendlyException(L("ThisDeviceIsAlreadyRegisteredForYou"));
            }

            if (live.Any())
            {
                // Whose it is stays unsaid on purpose -- otherwise this endpoint reads out which colleague
                // owns any device id the caller cares to try.
                throw new UserFriendlyException(L("ThisDeviceIsRegisteredToAnotherEmployee"));
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Lifts <c>ApprovedOnly</c> for the duration of a query. Null-safe because a domain service can be
    /// called outside a unit of work in tests.
    /// </summary>
    private IDisposable DisableApprovedOnly()
    {
        return _unitOfWorkManager.Current?.DisableFilter(HCMv3DataFilters.ApprovedOnly) ?? NullDisposable.Instance;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new NullDisposable();

        public void Dispose()
        {
        }
    }
}
