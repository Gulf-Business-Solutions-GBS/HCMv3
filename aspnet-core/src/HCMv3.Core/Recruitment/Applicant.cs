using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;

namespace HCMv3.Recruitment;

/// <summary>
/// Job applicant profile (ported from legacy XAF <c>HCM10.Module.BusinessObjects.Recruitment.Applicant</c>).
/// Legacy <c>PerformValidation</c> parity (auto Code, duplicate Code/Email checks, name/date rules) is
/// enforced in <c>ApplicantsAppService</c>, not here. Legacy computed <c>FullNameEn</c>/<c>FullNameAr</c>
/// (name concatenation) are not persisted -- computed in the DTO mapping instead.
/// </summary>
public class Applicant : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    public virtual int? TitleId { get; set; }
    public virtual Title Title { get; set; }

    /// <summary>
    /// Id of the ABP <c>User</c> (<c>AbpUsers.Id</c>) this applicant signs in as, or <c>null</c> for an
    /// applicant that was created by a recruiter and has never registered on the careers portal.
    ///
    /// This REPLACES the legacy <c>UserName</c>/<c>Password</c> columns, which stored the careers-portal
    /// credentials in plaintext and were compared with <c>a.UserName == x &amp;&amp; a.Password == y</c>
    /// (<c>HCM10.Recruiting/Controllers/AccountController.cs:105-106,168-169</c>) and mailed back to the
    /// candidate in cleartext on "forgot password" (<c>AccountController.cs:445</c>) and on first apply
    /// (<c>VacancyController.cs:685,779</c>). Candidate identity is now an ordinary ABP user in the
    /// <c>Candidate</c> role, so hashing, lockout, password policy, email confirmation and reset codes are
    /// all ASP.NET Zero's.
    ///
    /// Deliberately NOT an EF navigation property / FK constraint to <c>AbpUsers</c>: <c>Applicant</c> is
    /// soft-deleted (<see cref="FullAuditedEntity"/>) and <c>User</c> is too, so a real FK would add a
    /// delete-order coupling between the recruitment module and ABP identity for no integrity gain that the
    /// unique index below does not already provide. Uniqueness is enforced per tenant in
    /// <c>HCMv3DbContext.OnModelCreating</c>.
    /// </summary>
    public virtual long? UserId { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string FirstNameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string FirstNameAr { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string SecondNameEn { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string SecondNameAr { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string ThirdNameEn { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string ThirdNameAr { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string LastNameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string LastNameAr { get; set; }

    public virtual int? NationalityId { get; set; }
    public virtual Nationality Nationality { get; set; }

    public virtual int? JobTitleId { get; set; }
    public virtual JobTitle JobTitle { get; set; }

    public virtual Gender Gender { get; set; }

    public virtual int? MaritalStatusId { get; set; }
    public virtual MaritalStatus MaritalStatus { get; set; }

    public virtual DateTime BirthDate { get; set; }

    public virtual DateTime JoiningDate { get; set; }

    public virtual DateTime ProbationEndDate { get; set; }

    /// <summary>Where the photo is, relative to the file store; never kept in the database. Null means none.</summary>
    [StringLength(512)]
    public virtual string PhotoPath { get; set; }


    public virtual long Telephone { get; set; }

    public virtual long Mobile { get; set; }

    public virtual string CurrentAddress { get; set; }

    public virtual string PermanentAddress { get; set; }

    public virtual int DrivingLicense { get; set; }

    public virtual int? ReligionId { get; set; }
    public virtual Religion Religion { get; set; }

    public virtual int? CountryId { get; set; }
    public virtual Country Country { get; set; }

    public virtual int? QualificationId { get; set; }
    public virtual Qualification Qualification { get; set; }

    [StringLength(50)]
    public virtual string BloodGroup { get; set; }

    [Required]
    public virtual long CivilID { get; set; }

    [StringLength(256)]
    public virtual string Email { get; set; }

    [StringLength(100)]
    public virtual string PassportNum { get; set; }

    [StringLength(256)]
    public virtual string PlaceOfBirth { get; set; }

    public virtual string Address1 { get; set; }

    public virtual string Address2 { get; set; }

    public virtual string Comments { get; set; }

    /// <summary>
    /// Profile completion points, indicating how much of the applicant profile is completed compared to
    /// the total points (legacy comment: Shabbir Babji 12.09.2020).
    /// </summary>
    public virtual int CompletionPoints { get; set; }

    /// <summary>
    /// Whether onboarding should start on the user profile or not (initialized by admin).
    /// </summary>
    public virtual bool IsOnbdStarted { get; set; }

    /// <summary>
    /// Whether this profile is for an agency (used by the onboarding module).
    /// </summary>
    public virtual bool IsAgency { get; set; }

    /// <summary>
    /// Whether onboarding has been initiated.
    /// </summary>
    public virtual bool IsOnbdInit { get; set; }

    public virtual int? OnbCoordinatorId { get; set; }

    public virtual int? OnbBuddyId { get; set; }
}
