using System;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="Applicant"/>. Legacy computed <c>FullNameEn</c>/<c>FullNameAr</c> are
/// reproduced here from the four name parts.
/// </summary>
public class ApplicantDto : EntityDto<int>
{
    public string Code { get; set; }

    public int? TitleId { get; set; }

    public string FirstNameEn { get; set; }
    public string FirstNameAr { get; set; }
    public string SecondNameEn { get; set; }
    public string SecondNameAr { get; set; }
    public string ThirdNameEn { get; set; }
    public string ThirdNameAr { get; set; }
    public string LastNameEn { get; set; }
    public string LastNameAr { get; set; }

    public string FullNameEn { get; set; }
    public string FullNameAr { get; set; }

    public int? NationalityId { get; set; }
    public string NationalityNameEn { get; set; }

    public int? JobTitleId { get; set; }
    public string JobTitleNameEn { get; set; }

    public Gender Gender { get; set; }

    public int? MaritalStatusId { get; set; }

    public DateTime BirthDate { get; set; }

    public DateTime JoiningDate { get; set; }

    public DateTime ProbationEndDate { get; set; }

    public long Telephone { get; set; }

    public long Mobile { get; set; }

    public string CurrentAddress { get; set; }

    public string PermanentAddress { get; set; }

    public int DrivingLicense { get; set; }

    public int? ReligionId { get; set; }

    public int? CountryId { get; set; }

    public int? QualificationId { get; set; }

    public string BloodGroup { get; set; }

    public long CivilID { get; set; }

    public string Email { get; set; }

    public string PassportNum { get; set; }

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
