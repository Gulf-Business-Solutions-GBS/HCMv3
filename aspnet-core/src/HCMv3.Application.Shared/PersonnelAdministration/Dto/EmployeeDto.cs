using System;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PersonnelAdministration.Dto;

/// <summary>
/// Read/list DTO for <see cref="Employee"/>.
/// </summary>
public class EmployeeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string FirstNameEn { get; set; }

    public string LastNameEn { get; set; }

    public string FirstNameAr { get; set; }

    public string LastNameAr { get; set; }

    public string FullNameEn { get; set; }

    public string FullNameAr { get; set; }

    public EmployeeStatus Status { get; set; }

    public DateTime BirthDate { get; set; }

    public DateTime JoiningDate { get; set; }

    public int ProbationDays { get; set; }

    public DateTime ProbationEndDate { get; set; }

    public long CivilID { get; set; }

    public string Email { get; set; }

    public EmploymentType EmploymentType { get; set; }

    public int? NationalityId { get; set; }

    public string NationalityName { get; set; }

    public int? TitleId { get; set; }

    public string TitleName { get; set; }
}
