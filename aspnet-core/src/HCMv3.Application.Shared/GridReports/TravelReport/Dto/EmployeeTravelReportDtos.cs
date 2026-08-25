using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.TravelReport.Dto;

/// <summary>
/// Filter input for the Employee Travel analysis grid report (legacy
/// <c>TravelReport/uscRptEmployeeTravel.ascx</c>).
/// </summary>
public class GetEmployeeTravelReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbTravelType</c> (<see cref="HCMv3.TravelManagement.TravelType"/> code).</summary>
    public string TravelTypeCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors legacy <c>Table1()</c>'s column set (uscRptEmployeeTravel.ascx.cs:529-709).
///
/// DEVIATION (non-data-loss): legacy's <c>Travel</c> entity had an either-or Employee/Applicant party FK
/// (a Travel could belong to a job Applicant from the never-ported Recruitment module); HCMv3's
/// <see cref="HCMv3.TravelManagement.Travel"/> dropped the Applicant side entirely (see that entity's
/// class remarks) so every row is Employee-owned and the legacy "Applicant" column is always blank here.
/// </summary>
public class EmployeeTravelReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    /// <summary>Always blank -- see class remarks (legacy Applicant party dropped from Travel).</summary>
    public string Applicant { get; set; }

    public string TravelCode { get; set; }

    public string TravelType { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string JobTitle { get; set; }

    public string WorkLocation { get; set; }

    public long? OrganizationUnitId { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }

    public double TotalTravelCost { get; set; }

    public double ClaimAmount { get; set; }
}
