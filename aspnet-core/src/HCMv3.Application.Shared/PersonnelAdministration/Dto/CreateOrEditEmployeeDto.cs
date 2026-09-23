using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PersonnelAdministration.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="Employee"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditEmployeeDto
{
    public int? Id { get; set; }

    /// <summary>
    /// Optional. When left blank the server auto-generates the next numeric code (legacy
    /// <c>NumberSeries.GetCode</c> parity, simplified to the same max-numeric+1 pattern as <c>City</c>).
    /// </summary>
    [StringLength(64)]
    public string Code { get; set; }

    [Required]
    [StringLength(128)]
    public string FirstNameEn { get; set; }

    [StringLength(128)]
    public string SecondNameEn { get; set; }

    [StringLength(128)]
    public string ThirdNameEn { get; set; }

    [Required]
    [StringLength(128)]
    public string LastNameEn { get; set; }

    [Required]
    [StringLength(128)]
    public string FirstNameAr { get; set; }

    [StringLength(128)]
    public string SecondNameAr { get; set; }

    [StringLength(128)]
    public string ThirdNameAr { get; set; }

    [Required]
    [StringLength(128)]
    public string LastNameAr { get; set; }

    public int? TitleId { get; set; }

    public EmployeeStatus Status { get; set; }

    [Required]
    public DateTime BirthDate { get; set; }

    public Gender Gender { get; set; }

    public int? MaritalStatusId { get; set; }

    [Required]
    public int NationalityId { get; set; }

    public int? ReligionId { get; set; }

    public string FlatNumn { get; set; }
    public string FloorNum { get; set; }
    public string BuildingNum { get; set; }
    public string StreetNum { get; set; }
    public string BlockNum { get; set; }
    public string LaneNumber { get; set; }
    public string PACINumber { get; set; }
    public string ZipCode { get; set; }
    public string City { get; set; }
    public int? GovernorateId { get; set; }
    public int? CountryId { get; set; }
    public long Telephone { get; set; }
    public long Mobile { get; set; }

    [StringLength(1000)]
    public string CurrentAddress { get; set; }

    [StringLength(1000)]
    public string PermanentAddress { get; set; }

    public long PermanentTelephone { get; set; }

    public string FatherNameEn { get; set; }
    public string FatherNameAr { get; set; }
    public string MotherNameEn { get; set; }
    public string MotherNameAr { get; set; }

    public int? QualificationId { get; set; }

    public string BloodGroup { get; set; }

    public long CivilID { get; set; }

    public string LoginID { get; set; }

    public string AccessID { get; set; }

    [StringLength(256)]
    [EmailAddress]
    public string Email { get; set; }

    [StringLength(256)]
    public string AlternateEmail { get; set; }

    public int? BirthPlaceId { get; set; }

    public string PreviousEmpCode { get; set; }

    public string ReferenceCode { get; set; }

    public string EmergencyContact { get; set; }
    public string EmergencyNumber { get; set; }
    public string EmergencyRelation { get; set; }
    public string EmergencyContactAddress { get; set; }

    public string CriticalIllness { get; set; }

    public EmploymentType EmploymentType { get; set; }

    public string BusinessLandline { get; set; }
    public string BusinessExtension { get; set; }

    public int? RecruitmentTypeId { get; set; }

    public byte[] Photo { get; set; }

    [Required]
    public DateTime JoiningDate { get; set; }

    [Range(0, int.MaxValue)]
    public int ProbationDays { get; set; }

    /// <summary>
    /// New employees only, and only with <c>ManagePersonnelSettings.AutoWageAssignOnHire</c> on: the employee
    /// whose open-ended wage accounts are copied to the new hire (legacy "Copy Wage Account" prompt,
    /// <c>Employee.AddAutoWageAccount</c>). Not stored.
    /// </summary>
    public int? CopyWageAccountsFromEmployeeId { get; set; }
}
