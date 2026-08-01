using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="Applicant"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditApplicantDto
{
    public int? Id { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    public int? TitleId { get; set; }

    // UserName/Password are GONE from this DTO on purpose. Careers-portal credentials are ABP user
    // credentials now (Applicant.UserId -> AbpUsers.Id); a recruiter editing an applicant record must never
    // be able to set, read or overwrite them. See Applicant.UserId.

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string FirstNameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string FirstNameAr { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string SecondNameEn { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string SecondNameAr { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string ThirdNameEn { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string ThirdNameAr { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string LastNameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string LastNameAr { get; set; }

    [Required]
    public int NationalityId { get; set; }

    public int? JobTitleId { get; set; }

    [Required]
    public Gender Gender { get; set; }

    public int? MaritalStatusId { get; set; }

    [Required]
    public DateTime BirthDate { get; set; }

    public DateTime JoiningDate { get; set; }

    public DateTime ProbationEndDate { get; set; }

    public byte[] Photo { get; set; }

    public long Telephone { get; set; }

    public long Mobile { get; set; }

    public string CurrentAddress { get; set; }

    public string PermanentAddress { get; set; }

    public int DrivingLicense { get; set; }

    public int? ReligionId { get; set; }

    public int? CountryId { get; set; }

    public int? QualificationId { get; set; }

    [StringLength(50)]
    public string BloodGroup { get; set; }

    [Required]
    public long CivilID { get; set; }

    [StringLength(256)]
    public string Email { get; set; }

    [StringLength(100)]
    public string PassportNum { get; set; }

    [StringLength(256)]
    public string PlaceOfBirth { get; set; }

    public string Address1 { get; set; }

    public string Address2 { get; set; }

    public string Comments { get; set; }

    public int CompletionPoints { get; set; }

    public bool IsOnbdStarted { get; set; }

    public bool IsAgency { get; set; }

    public bool IsOnbdInit { get; set; }

    public int? OnbCoordinatorId { get; set; }

    public int? OnbBuddyId { get; set; }
}
