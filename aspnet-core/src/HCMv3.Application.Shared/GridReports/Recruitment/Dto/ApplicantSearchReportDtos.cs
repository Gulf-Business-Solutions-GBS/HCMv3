using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GridReports.Recruitment.Dto;

/// <summary>
/// Filter input for the Applicant Search analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Recruitment/uscRptApplicantSearch.ascx</c>).
///
/// Legacy's filter panel is entirely applicant-specific (Nationality/MaritalStatus/Gender/Age/Name) --
/// there is no employee-code box and no employee-status combo. The two inherited filters
/// (<see cref="GridReportInputBase.EmployeeCode"/>/<see cref="GridReportInputBase.EmployeeStatus"/>) are
/// simply unused, and <see cref="GridReportInputBase.FromDate"/>/<see cref="GridReportInputBase.ToDate"/>
/// likewise -- same convention as <c>GetEmployeeSalaryChangeReportInput</c>.
/// </summary>
public class GetApplicantSearchReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbNationality</c>.</summary>
    public int? NationalityId { get; set; }

    /// <summary>Legacy <c>cmbMaritalStatus</c>.</summary>
    public int? MaritalStatusId { get; set; }

    /// <summary>Legacy <c>cmbGender</c>.</summary>
    public Gender? Gender { get; set; }

    /// <summary>Legacy <c>txtAgeFrom</c>. Negative values are clamped to 0, same as legacy.</summary>
    public int? AgeFrom { get; set; }

    /// <summary>Legacy <c>txtAgeTo</c>. Zero/negative values are treated as "no upper bound" (legacy default 100).</summary>
    public int? AgeTo { get; set; }

    /// <summary>Legacy <c>txtFirstName</c>: substring match.</summary>
    public string FirstName { get; set; }

    /// <summary>Legacy <c>txtLastName</c>: substring match.</summary>
    public string LastName { get; set; }

    protected override string DefaultSorting => "Code ASC";
}

/// <summary>One grid row. Ports legacy <c>Table1()</c> (uscRptApplicantSearch.ascx.cs:633-767).</summary>
public class ApplicantSearchReportRowDto : EntityDto<int>
{
    public string Code { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Email { get; set; }

    public string NationalityName { get; set; }

    public string MaritalStatusName { get; set; }

    public string Gender { get; set; }

    /// <summary>Legacy "Age": <c>DateTime.Now.Year - BirthDate.Year</c> (a birthday-naive year subtraction, preserved as-is).</summary>
    public int Age { get; set; }

    public long Mobile { get; set; }
}
