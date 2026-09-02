using System;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration.Dto;

/// <summary>
/// An employee's own particulars, READ-SHAPED: every lookup arrives as a resolved name rather than an
/// id.
///
/// WHY NOT REUSE <see cref="CreateOrEditEmployeeDto"/>. That DTO is edit-shaped -- it carries
/// <c>NationalityId</c>, <c>ReligionId</c>, <c>QualificationId</c> and six more as bare integers,
/// because a form binds them to dropdowns that already hold the names. A read screen has no dropdowns,
/// so rendering from it would either show the reader "Nationality: 42" or force the client to fetch
/// nine lookup lists to translate nine numbers -- nine round trips to display one record.
///
/// Resolving them here costs one query with nine joins, which is what the list screen already does for
/// two of them.
/// </summary>
public class EmployeeProfileDto
{
    public int Id { get; set; }

    public string Code { get; set; }

    // ---- names -----------------------------------------------------------------------------------

    public string FullNameEn { get; set; }

    public string FullNameAr { get; set; }

    public string FirstNameEn { get; set; }

    public string SecondNameEn { get; set; }

    public string ThirdNameEn { get; set; }

    public string LastNameEn { get; set; }

    public string FirstNameAr { get; set; }

    public string SecondNameAr { get; set; }

    public string ThirdNameAr { get; set; }

    public string LastNameAr { get; set; }

    public string TitleName { get; set; }

    // ---- personal --------------------------------------------------------------------------------

    public EmployeeStatus Status { get; set; }

    public EmploymentType EmploymentType { get; set; }

    public Gender Gender { get; set; }

    public DateTime BirthDate { get; set; }

    public string BirthPlaceName { get; set; }

    public string NationalityName { get; set; }

    public string ReligionName { get; set; }

    public string MaritalStatusName { get; set; }

    public string QualificationName { get; set; }

    public string BloodGroup { get; set; }

    public long CivilID { get; set; }

    public string PACINumber { get; set; }

    public string FatherNameEn { get; set; }

    public string MotherNameEn { get; set; }

    public string CriticalIllness { get; set; }

    // ---- employment ------------------------------------------------------------------------------

    public DateTime JoiningDate { get; set; }

    public int ProbationDays { get; set; }

    public DateTime ProbationEndDate { get; set; }

    public string RecruitmentTypeName { get; set; }

    public string PreviousEmpCode { get; set; }

    public string ReferenceCode { get; set; }

    public string LoginID { get; set; }

    public string AccessID { get; set; }

    // ---- contact ---------------------------------------------------------------------------------

    public string Email { get; set; }

    public string AlternateEmail { get; set; }

    public long Mobile { get; set; }

    public long Telephone { get; set; }

    public string BusinessLandline { get; set; }

    public string BusinessExtension { get; set; }

    // ---- address ---------------------------------------------------------------------------------

    public string CurrentAddress { get; set; }

    public string PermanentAddress { get; set; }

    public long PermanentTelephone { get; set; }

    public string FlatNumn { get; set; }

    public string FloorNum { get; set; }

    public string BuildingNum { get; set; }

    public string StreetNum { get; set; }

    public string BlockNum { get; set; }

    public string LaneNumber { get; set; }

    public string City { get; set; }

    public string GovernorateName { get; set; }

    public string CountryName { get; set; }

    public string ZipCode { get; set; }

    // ---- emergency -------------------------------------------------------------------------------

    public string EmergencyContact { get; set; }

    public string EmergencyNumber { get; set; }

    public string EmergencyRelation { get; set; }

    public string EmergencyContactAddress { get; set; }
}
