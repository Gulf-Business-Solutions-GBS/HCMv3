using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee master-data hub (ported from legacy XAF <c>HCM10.Module.BusinessObjects.PersonnelAdministration.Employee</c>).
/// Wave A scope only: core scalar fields. Legacy has ~45 <c>[Association]</c> child collections
/// (OrgAssignment, Contract, Manager, Sponsor, PayrollStatus, WageAccount, Insurance, Training, ...) --
/// none of those are declared here; each satellite entity (this wave or Wave B) carries its own FK to
/// Employee instead. <c>CurrentOrgAssignment</c>/<c>CurrentManager</c>/<c>CurrentSponsor</c>/<c>CurrentContract</c>/
/// <c>CurrentCompany</c>/<c>CurrentDepartment</c>/etc. computed properties are Wave B (their backing
/// entities don't exist yet) and are intentionally omitted. <c>IExcelImport</c> is an XAF UI concept and
/// is not ported. The legacy file has three abandoned/commented-out <c>FileSysAttachment</c>
/// implementations of Photo; only the active simple <c>byte[]</c> version is ported.
/// </summary>
public class Employee : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    /// <summary>
    /// Business code. Auto-generated (max numeric + 1) if left blank -- see
    /// <c>Services.EmployeeCodeGenerator</c> (same pattern as <c>CitiesAppService</c>).
    /// </summary>
    [StringLength(64)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(128)]
    public virtual string FirstNameEn { get; set; }

    [StringLength(128)]
    public virtual string SecondNameEn { get; set; }

    [StringLength(128)]
    public virtual string ThirdNameEn { get; set; }

    [Required]
    [StringLength(128)]
    public virtual string LastNameEn { get; set; }

    [Required]
    [StringLength(128)]
    public virtual string FirstNameAr { get; set; }

    [StringLength(128)]
    public virtual string SecondNameAr { get; set; }

    [StringLength(128)]
    public virtual string ThirdNameAr { get; set; }

    [Required]
    [StringLength(128)]
    public virtual string LastNameAr { get; set; }

    /// <summary>
    /// Concatenation of FirstNameEn/SecondNameEn/ThirdNameEn/LastNameEn, recomputed on every save
    /// (legacy <c>PerformValidation</c> parity) -- not user-editable.
    /// </summary>
    [StringLength(512)]
    public virtual string FullNameEn { get; set; }

    [StringLength(512)]
    public virtual string FullNameAr { get; set; }

    public virtual int? TitleId { get; set; }

    public virtual Title Title { get; set; }

    public virtual EmployeeStatus Status { get; set; }

    public virtual DateTime BirthDate { get; set; }

    public virtual Gender Gender { get; set; }

    public virtual int? MaritalStatusId { get; set; }

    public virtual MaritalStatus MaritalStatus { get; set; }

    public virtual int NationalityId { get; set; }

    public virtual Nationality Nationality { get; set; }

    public virtual int? ReligionId { get; set; }

    public virtual Religion Religion { get; set; }

    #region Address information

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
    public virtual string LaneNumber { get; set; }

    [StringLength(32)]
    public virtual string PACINumber { get; set; }

    [StringLength(32)]
    public virtual string ZipCode { get; set; }

    /// <summary>
    /// Free-text city name (legacy stores this as a plain string, not a FK to the City master).
    /// </summary>
    [StringLength(128)]
    public virtual string City { get; set; }

    public virtual int? GovernorateId { get; set; }

    public virtual Governorate Governorate { get; set; }

    public virtual int? CountryId { get; set; }

    public virtual Country Country { get; set; }

    public virtual long Telephone { get; set; }

    public virtual long Mobile { get; set; }

    [StringLength(1000)]
    public virtual string CurrentAddress { get; set; }

    [StringLength(1000)]
    public virtual string PermanentAddress { get; set; }

    public virtual long PermanentTelephone { get; set; }

    [StringLength(128)]
    public virtual string FatherNameEn { get; set; }

    [StringLength(128)]
    public virtual string FatherNameAr { get; set; }

    [StringLength(128)]
    public virtual string MotherNameEn { get; set; }

    [StringLength(128)]
    public virtual string MotherNameAr { get; set; }

    #endregion

    public virtual int? QualificationId { get; set; }

    public virtual Qualification Qualification { get; set; }

    [StringLength(32)]
    public virtual string BloodGroup { get; set; }

    public virtual long CivilID { get; set; }

    /// <summary>
    /// Link to this employee's ABP <c>User</c> account -- the identity anchor for Employee Self Service.
    /// Until this is set, the signed-in user cannot be resolved to an employee and every ESS scope
    /// (<c>IEmployeeScopeProvider</c>) resolves to "sees nothing".
    ///
    /// Supersedes the legacy string match on <see cref="LoginID"/> == <c>User.UserName</c>, which
    /// <c>EmployeeSessionService</c> still falls back to for rows not yet linked. That match was never safe
    /// as a primary key of identity: it silently rebinds when a username is renamed, has no uniqueness
    /// guarantee (two employees may carry the same LoginID), and is unindexed.
    ///
    /// Deliberately NOT an EF navigation property / FK constraint to <c>AbpUsers</c>, following the same
    /// reasoning as <c>Recruitment.Applicant.UserId</c>: both Employee and User are soft-deleted, so a real
    /// FK would couple delete ordering between this module and ABP identity for no integrity gain beyond
    /// what the index already provides. Uniqueness is enforced per tenant by a filtered unique index in
    /// <c>HCMv3DbContext.OnModelCreating</c>.
    /// </summary>
    public virtual long? UserId { get; set; }

    [StringLength(128)]
    public virtual string LoginID { get; set; }

    [StringLength(128)]
    public virtual string AccessID { get; set; }

    [StringLength(256)]
    public virtual string Email { get; set; }

    [StringLength(256)]
    public virtual string AlternateEmail { get; set; }

    public virtual int? BirthPlaceId { get; set; }

    public virtual City BirthPlace { get; set; }

    [StringLength(64)]
    public virtual string PreviousEmpCode { get; set; }

    [StringLength(64)]
    public virtual string ReferenceCode { get; set; }

    #region Emergency contact

    [StringLength(256)]
    public virtual string EmergencyContact { get; set; }

    [StringLength(64)]
    public virtual string EmergencyNumber { get; set; }

    [StringLength(128)]
    public virtual string EmergencyRelation { get; set; }

    [StringLength(1000)]
    public virtual string EmergencyContactAddress { get; set; }

    #endregion

    [StringLength(1000)]
    public virtual string CriticalIllness { get; set; }

    public virtual EmploymentType EmploymentType { get; set; }

    [StringLength(64)]
    public virtual string BusinessLandline { get; set; }

    [StringLength(32)]
    public virtual string BusinessExtension { get; set; }

    public virtual int? RecruitmentTypeId { get; set; }

    public virtual RecruitmentType RecruitmentType { get; set; }

    public virtual byte[] Photo { get; set; }

    public virtual DateTime JoiningDate { get; set; }

    public virtual int ProbationDays { get; set; }

    /// <summary>
    /// Stored, recomputed on every save as <c>JoiningDate.AddDays(ProbationDays)</c> (legacy
    /// <c>GetProbationEndDate</c> "CalendarDays" branch parity). The legacy "Working Days" branch depends
    /// on DutyPeriod/HolidayCalendar/TimeRegister, which are Wave B/TimeManagement -- not available yet,
    /// so only the calendar-days calculation is ported here.
    /// </summary>
    public virtual DateTime ProbationEndDate { get; set; }

    /// <summary>
    /// Rebuilds <see cref="FullNameEn"/>/<see cref="FullNameAr"/> from the four name parts, skipping blank
    /// ones (legacy <c>PerformValidation</c> parity, minus its English title-casing). Legacy ran this in
    /// <c>OnSaving</c> for every write; here each path that writes names calls it -- the employee service,
    /// the recruitment Joined hand-off, the personal-data-change workflow and the go-live loader.
    /// </summary>
    public void ApplyFullNames()
    {
        FullNameEn = JoinNames(FirstNameEn, SecondNameEn, ThirdNameEn, LastNameEn);
        FullNameAr = JoinNames(FirstNameAr, SecondNameAr, ThirdNameAr, LastNameAr);
    }

    private static string JoinNames(params string[] parts) =>
        string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
}
