using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;

namespace HCMv3.TimeManagement;

/// <summary>
/// A phone (or tablet) an employee has registered with HR so the mobile app will accept attendance punches
/// from it -- the port of legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.Time.MobileAppDeviceRequest</c>.
///
/// <para>
/// <b>One class. The registration IS the request.</b> Legacy's <c>MobileAppDeviceRequest</c> was a
/// <c>WorkflowRequest</c> subclass and there was no registration table at all -- the request was the only
/// row that ever existed, which is why nothing downstream could ever read it back cleanly. Here the row
/// carries the workflow columns itself and is kept behind the <c>ApprovedOnly</c> filter until it is
/// granted, so a device that has been asked for and a device that has been allowed are two different states
/// of one row rather than two tables that can disagree.
/// </para>
///
/// <para>
/// <b>The dead code that must not come back.</b> Legacy's entire approval side effect was commented out, and
/// the commented block was copy-pasted from the ASSET request: it referred to <c>EmployeeAsset</c>,
/// <c>this.Asset</c> and <c>this.StartDate</c>, none of which exist on a device request. It could never have
/// compiled, let alone run. The practical consequence was that in legacy, approving a device registration
/// did literally nothing -- there was no place for an approved device to land, and nothing anywhere read
/// <c>DeviceId</c> back. Under the single-table design that gap closes by construction: approval is what
/// makes this row visible to <see cref="Services.IMobileDeviceRegistrationService"/> and therefore what
/// makes the device usable. There is nothing to resurrect; anyone reinstating an asset-shaped
/// "create the real record on approval" block here would be recreating a bug, not restoring a feature.
/// </para>
///
/// <para>
/// Filed under TimeManagement rather than legacy's <c>Workflow/Time/</c> folder because its only real
/// consumer is attendance: the question this row exists to answer is "may I accept a punch from this
/// handset", which is asked on the punch path, not by the approval engine.
/// </para>
///
/// <para>
/// <b>Not wired into the punch path by this change.</b> <see cref="Services.IMobileDeviceRegistrationService"/>
/// exists so a future mobile punch endpoint can ask the question; nothing in <see cref="TimeEvent"/> or the
/// existing attendance path calls it yet. Registering a device today therefore changes no existing
/// behaviour -- deliberately, because silently starting to reject punches from unregistered handsets would
/// lock every current mobile user out on the day this shipped.
/// </para>
/// </summary>
public class EmployeeMobileDevice : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    /// <summary>
    /// Whose handset this is. Taken from the session when the employee registers it themselves, never from
    /// the form: a device registration is an authorisation to punch AS somebody, so a form that could name
    /// its own employee would be a form that could authorise punching as anybody.
    /// </summary>
    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>
    /// The handset's own identifier as the mobile app reports it (Android id, iOS vendor id, or whatever the
    /// app is built to send).
    ///
    /// <para>
    /// Bounded at 200 characters; legacy left it unbounded, which on SQL Server means <c>nvarchar(max)</c> --
    /// a column that cannot be indexed, for the one value in this table that every lookup filters on. No real
    /// device identifier comes close to 200.
    /// </para>
    /// </summary>
    [Required]
    [StringLength(200)]
    public virtual string DeviceId { get; set; }

    /// <summary>Free text describing the handset ("iPhone 14", "Samsung A54"), for HR to recognise it by.</summary>
    [Required]
    [StringLength(100)]
    public virtual string DeviceType { get; set; }

    /// <summary>
    /// The date from which punches from this device may be accepted. A registration is not usable before it,
    /// even once approved -- see <see cref="Services.IMobileDeviceRegistrationService.IsDeviceRegisteredAsync"/>.
    /// </summary>
    public virtual DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Whether the registration is still live. Kept as a flag rather than deleting the row, because the row
    /// is the audit trail for every punch that was accepted from this handset while it was active; deleting
    /// it would leave those punches unexplained.
    /// </summary>
    public virtual bool IsActive { get; set; }

    [StringLength(1000)]
    public virtual string Comments { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowRequestId"/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowStatus"/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
