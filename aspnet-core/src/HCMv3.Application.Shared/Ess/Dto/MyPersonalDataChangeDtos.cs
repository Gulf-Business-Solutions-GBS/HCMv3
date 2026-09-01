using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.Ess.Dto;

/// <summary>
/// The forty personnel-file fields an employee may propose to change about themselves, shared by the
/// pre-filled form, the submitted request and the list row.
///
/// <para>
/// <b>Every field is nullable and none is <c>[Required]</c>,</b> which is the opposite of what a form DTO
/// usually looks like and is deliberate. A partial update is the normal case, and on this DTO <c>null</c>
/// carries meaning: it says "leave my existing value alone". Marking the four name fields <c>[Required]</c>
/// here would be tempting -- they ARE required -- but the rule belongs in the workflow handler so it applies
/// to a request however it was raised, and the annotation would additionally forbid a legitimate
/// mobile-number-only request that never touched the name.
/// </para>
///
/// <para>
/// <b>There is no Photo field, by product decision.</b> Legacy carried one whose copy onto the employee was
/// commented out "for performance issue", so an employee could upload a picture that silently never reached
/// their profile. The photo stays HR-managed.
/// </para>
/// </summary>
public abstract class PersonalDataFieldsDtoBase
{
    [StringLength(128)]
    public string FirstNameEn { get; set; }

    [StringLength(128)]
    public string SecondNameEn { get; set; }

    [StringLength(128)]
    public string ThirdNameEn { get; set; }

    [StringLength(128)]
    public string LastNameEn { get; set; }

    [StringLength(128)]
    public string FirstNameAr { get; set; }

    [StringLength(128)]
    public string SecondNameAr { get; set; }

    [StringLength(128)]
    public string ThirdNameAr { get; set; }

    [StringLength(128)]
    public string LastNameAr { get; set; }

    public int? TitleId { get; set; }

    public DateTime? BirthDate { get; set; }

    public int? NationalityId { get; set; }

    public int? ReligionId { get; set; }

    public Gender? Gender { get; set; }

    public int? MaritalStatusId { get; set; }

    /// <summary>City master row -- the only lookup-backed city on this form; <see cref="City"/> itself is
    /// free text, matching the employee record.</summary>
    public int? BirthPlaceId { get; set; }

    [StringLength(32)]
    public string FlatNumn { get; set; }

    [StringLength(32)]
    public string FloorNum { get; set; }

    [StringLength(32)]
    public string BuildingNum { get; set; }

    [StringLength(32)]
    public string StreetNum { get; set; }

    [StringLength(32)]
    public string BlockNum { get; set; }

    [StringLength(32)]
    public string ZipCode { get; set; }

    [StringLength(32)]
    public string PACINumber { get; set; }

    [StringLength(128)]
    public string City { get; set; }

    /// <summary>Legacy called this <c>Governate</c>. Same field, spelled correctly.</summary>
    public int? GovernorateId { get; set; }

    public int? CountryId { get; set; }

    [StringLength(1000)]
    public string CurrentAddress { get; set; }

    [StringLength(1000)]
    public string PermanentAddress { get; set; }

    public long? Telephone { get; set; }

    public long? Mobile { get; set; }

    [StringLength(256)]
    [EmailAddress]
    public string Email { get; set; }

    [StringLength(256)]
    [EmailAddress]
    public string AlternateEmail { get; set; }

    [StringLength(128)]
    public string FatherNameEn { get; set; }

    [StringLength(128)]
    public string FatherNameAr { get; set; }

    [StringLength(128)]
    public string MotherNameEn { get; set; }

    [StringLength(128)]
    public string MotherNameAr { get; set; }

    [StringLength(32)]
    public string BloodGroup { get; set; }

    [StringLength(1000)]
    public string CriticalIllness { get; set; }

    [StringLength(256)]
    public string EmergencyContact { get; set; }

    [StringLength(64)]
    public string EmergencyNumber { get; set; }

    [StringLength(128)]
    public string EmergencyRelation { get; set; }
}

/// <summary>
/// The employee's own personnel-file update request, as their self-service list shows it.
/// </summary>
public class MyPersonalDataChangeDto : MyRequestDtoBase
{
    /// <summary>The proposed values, exactly as submitted.</summary>
    public PersonalDataChangeValuesDto Proposed { get; set; }

    public string Comments { get; set; }
}

/// <summary>A concrete carrier for the forty fields, used inside the list and edit DTOs.</summary>
public class PersonalDataChangeValuesDto : PersonalDataFieldsDtoBase
{
}

/// <summary>
/// The form an employee opens, PRE-FILLED with what their personnel file says today.
///
/// <para>
/// Pre-filling is not cosmetic. Without it the employee retypes forty fields to change one, and every field
/// they retype slightly differently becomes a change the approver has to check -- the screen would generate
/// exactly the corruption this feature has to avoid.
/// </para>
/// </summary>
public class MyPersonalDataForEditDto
{
    public int EmployeeId { get; set; }

    /// <summary>Their current values. What the employee edits.</summary>
    public PersonalDataChangeValuesDto Current { get; set; }

    /// <summary>
    /// True when the employee already has an undecided request in flight. The screen uses it to say so
    /// rather than letting them raise a second one and wonder which the approver saw.
    /// </summary>
    public bool HasPendingRequest { get; set; }
}

/// <summary>What the employee submits. Same forty fields, plus a note for the approver.</summary>
public class RequestPersonalDataChangeInput : PersonalDataFieldsDtoBase
{
    [StringLength(1000)]
    public string Comments { get; set; }
}
