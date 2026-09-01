using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// An employee's PROPOSED changes to their own personnel file (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.Leave.PersonalDataUpdateRequest</c>).
///
/// <para>
/// <b>Why this is a separate table when almost every other ESS type is not.</b> The rest of the wave follows
/// the single-row rule -- the transaction row IS the request, hidden by the <c>ApprovedOnly</c> filter until
/// approved. That rule cannot apply here, because the "transaction row" would be <see cref="Employee"/>
/// itself, and an <see cref="Employee"/> carrying a non-null <see cref="IWorkflowEnabledEntity.WorkflowStatus"/>
/// would be hidden from the filter the moment somebody edited their phone number: payroll, quota, attendance
/// and every report would stop seeing a person who still works here. Making Employee workflow-enabled trades
/// a duplicate table for a payroll outage, so it is not done. This row holds the proposal; approval copies it
/// onto the master record and nothing else in the system ever reads it.
/// </para>
///
/// <para>
/// <b>Everything is nullable, deliberately.</b> A partial update is the normal case -- somebody changing only
/// their mobile number should not have to restate their identity for the change to be accepted. <c>null</c>
/// therefore means "leave the employee's existing value alone", NOT "blank it out"; see
/// <c>PersonalDataChangeWorkflowTransactionHandler.OnApprovedAsync</c>, which is the single place that reads
/// these values.
/// </para>
///
/// <para>
/// <b>No Photo column, by product decision.</b> Legacy carried one on the request and the copy line was
/// commented out "for performance issue", so an employee could upload a photo through self service that
/// silently never reached their profile -- a change that appears to succeed and does nothing is worse than
/// one that is refused. The photo is not on this form at all; it stays an HR-managed field on
/// <see cref="Employee.Photo"/>.
/// </para>
/// </summary>
public class EmployeePersonalDataChange : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    /// <summary>Whose personnel file this proposes to change -- and, on approval, the row that is written.</summary>
    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    #region Proposed name (English)

    [StringLength(128)]
    public virtual string FirstNameEn { get; set; }

    [StringLength(128)]
    public virtual string SecondNameEn { get; set; }

    [StringLength(128)]
    public virtual string ThirdNameEn { get; set; }

    [StringLength(128)]
    public virtual string LastNameEn { get; set; }

    #endregion

    #region Proposed name (Arabic)

    [StringLength(128)]
    public virtual string FirstNameAr { get; set; }

    [StringLength(128)]
    public virtual string SecondNameAr { get; set; }

    [StringLength(128)]
    public virtual string ThirdNameAr { get; set; }

    [StringLength(128)]
    public virtual string LastNameAr { get; set; }

    #endregion

    #region Proposed identity

    public virtual int? TitleId { get; set; }

    public virtual Title Title { get; set; }

    public virtual DateTime? BirthDate { get; set; }

    /// <summary>
    /// Nullable here even though <see cref="Employee.NationalityId"/> is required, for the same reason as
    /// everything else on this row: not supplying it means "do not change my nationality", and a required
    /// column would force every partial edit to restate it.
    /// </summary>
    public virtual int? NationalityId { get; set; }

    public virtual Nationality Nationality { get; set; }

    public virtual int? ReligionId { get; set; }

    public virtual Religion Religion { get; set; }

    public virtual Gender? Gender { get; set; }

    public virtual int? MaritalStatusId { get; set; }

    public virtual MaritalStatus MaritalStatus { get; set; }

    public virtual int? BirthPlaceId { get; set; }

    public virtual City BirthPlace { get; set; }

    #endregion

    #region Proposed address

    [StringLength(32)]
    public virtual string FlatNumn { get; set; }

    [StringLength(32)]
    public virtual string FloorNum { get; set; }

    [StringLength(32)]
    public virtual string BuildingNum { get; set; }

    [StringLength(32)]
    public virtual string StreetNum { get; set; }

    [StringLength(32)]
    public virtual string BlockNum { get; set; }

    [StringLength(32)]
    public virtual string ZipCode { get; set; }

    [StringLength(32)]
    public virtual string PACINumber { get; set; }

    /// <summary>Free-text, matching <see cref="Employee.City"/> -- legacy stores the city as a string here,
    /// not as a FK to the City master (which is only used for <see cref="BirthPlace"/>).</summary>
    [StringLength(128)]
    public virtual string City { get; set; }

    public virtual int? GovernorateId { get; set; }

    /// <summary>Legacy spelled this <c>Governate</c>; HCMv3 spells it correctly. Same field.</summary>
    public virtual Governorate Governorate { get; set; }

    public virtual int? CountryId { get; set; }

    public virtual Country Country { get; set; }

    [StringLength(1000)]
    public virtual string CurrentAddress { get; set; }

    [StringLength(1000)]
    public virtual string PermanentAddress { get; set; }

    #endregion

    #region Proposed contact

    /// <summary>Nullable, unlike <see cref="Employee.Telephone"/>, so that "I did not touch this" is
    /// distinguishable from "my number is 0".</summary>
    public virtual long? Telephone { get; set; }

    public virtual long? Mobile { get; set; }

    [StringLength(256)]
    public virtual string Email { get; set; }

    [StringLength(256)]
    public virtual string AlternateEmail { get; set; }

    #endregion

    #region Proposed family

    [StringLength(128)]
    public virtual string FatherNameEn { get; set; }

    [StringLength(128)]
    public virtual string FatherNameAr { get; set; }

    [StringLength(128)]
    public virtual string MotherNameEn { get; set; }

    [StringLength(128)]
    public virtual string MotherNameAr { get; set; }

    #endregion

    #region Proposed medical / emergency

    [StringLength(32)]
    public virtual string BloodGroup { get; set; }

    [StringLength(1000)]
    public virtual string CriticalIllness { get; set; }

    [StringLength(256)]
    public virtual string EmergencyContact { get; set; }

    [StringLength(64)]
    public virtual string EmergencyNumber { get; set; }

    [StringLength(128)]
    public virtual string EmergencyRelation { get; set; }

    #endregion

    /// <summary>What the employee wants the approver to know -- why the change is being asked for.</summary>
    [StringLength(1000)]
    public virtual string Comments { get; set; }

    /// <summary>The approval request that governs this proposal.</summary>
    public virtual int? WorkflowRequestId { get; set; }

    /// <summary>
    /// Null only for a row created with workflow switched off for this type. Read by the ApprovedOnly
    /// filter, which is what keeps an undecided proposal out of the rest of the system -- note that here the
    /// filter hides the PROPOSAL, never the employee.
    /// </summary>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
