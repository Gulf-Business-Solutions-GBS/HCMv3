using System;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.TimeManagement.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.TimeManagement.Workflow;

/// <summary>
/// Workflow handler for a mobile device registration (<see cref="EmployeeMobileDevice"/>) -- the port of
/// legacy <c>HCM10.Module.BusinessObjects.Workflow.Time.MobileAppDeviceRequest</c>.
///
/// <para>
/// <b>Approval writes nothing, and that is the whole design.</b> There is no second table to copy the
/// registration into: the row IS the registration, hidden by the <c>ApprovedOnly</c> filter while it is in
/// flight and visible -- therefore usable by
/// <see cref="IMobileDeviceRegistrationService.IsDeviceRegisteredAsync"/> -- once it is granted. The base
/// class's status stamp is the entire side effect.
/// </para>
///
/// <para>
/// Legacy's approval block was commented out AND copy-pasted from the asset request, referring to
/// <c>EmployeeAsset</c>, <c>this.Asset</c> and <c>this.StartDate</c> -- none of which exist on a device
/// request. So in legacy, approving a device registration did nothing whatsoever. Nothing needs porting from
/// it; see the entity's remarks for why reinstating anything of that shape here would be recreating a bug.
/// </para>
///
/// <para>
/// The rules live in <see cref="IMobileDeviceRegistrationService"/> rather than in this class, because HR's
/// own screen has to reach the same verdict -- and because with workflow switched off for this type the
/// handler never runs at all.
/// </para>
/// </summary>
public class MobileDeviceWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeMobileDevice>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for mobile device registrations.</summary>
    public const string MobileDeviceRequestType = "EmployeeMobileDevice";

    private readonly IMobileDeviceRegistrationService _registrationService;

    public MobileDeviceWorkflowTransactionHandler(
        IRepository<EmployeeMobileDevice> repository,
        IUnitOfWorkManager unitOfWorkManager,
        IMobileDeviceRegistrationService registrationService)
        : base(repository, unitOfWorkManager)
    {
        _registrationService = registrationService;
    }

    public override string RequestType => MobileDeviceRequestType;

    /// <summary>
    /// Refuses a registration with no device id, no device type, no effective date, or one that duplicates a
    /// live registration of the same handset.
    ///
    /// <para>
    /// Every one of these is a legacy rule reinstated rather than invented, except that legacy's versions did
    /// not work: it compared <c>DeviceId</c> and <c>DeviceType</c> against <c>""</c>, which is false for
    /// null, so a request carrying neither passed validation. It also had no duplicate rule at all.
    /// </para>
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var device = await GetTargetAsync(context);
        if (device == null)
        {
            return;
        }

        if (device.EffectiveDate == default)
        {
            throw new UserFriendlyException(L("EffectiveDateIsRequired"));
        }

        // Excluded by id so the row being submitted never competes with itself -- it is Draft here and so
        // would not match today, but the check has to give the same answer if it is ever re-run against a
        // row already stamped Open.
        await _registrationService.EnsureRegistrationIsAllowedAsync(
            device.EmployeeId,
            device.DeviceId,
            device.DeviceType,
            excludeDeviceRegistrationId: device.Id);
    }

    /// <summary>
    /// The one-line inbox label, e.g. "Mobile device iPhone 14, from 01 Jan 2026".
    ///
    /// <para>
    /// The device TYPE is in it rather than the device id. What the approver is deciding is whether this
    /// employee's handset may punch, and "iPhone 14" is something they can check against the person in front
    /// of them; a 64-character opaque identifier is not, and putting it in an inbox list would only train
    /// people to approve without reading.
    /// </para>
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var device = await GetTargetAsync(context);
        if (device == null)
        {
            return null;
        }

        var from = device.EffectiveDate == default
            ? DateTime.Today.ToString("dd MMM yyyy")
            : device.EffectiveDate.ToString("dd MMM yyyy");

        var deviceType = string.IsNullOrWhiteSpace(device.DeviceType) ? "device" : device.DeviceType.Trim();

        return new WorkflowSummary(
            $"Mobile device {deviceType}, from {from}",
            $"جهاز محمول {deviceType}، اعتبارا من {from}");
    }
}
