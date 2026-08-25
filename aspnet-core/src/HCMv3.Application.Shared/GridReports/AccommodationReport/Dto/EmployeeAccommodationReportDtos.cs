using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.AccommodationReport.Dto;

/// <summary>
/// Filter input for the Employee Accommodation analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/AccommodationReport/uscRptEmployeeAccommodation.ascx</c>).
///
/// Legacy's <c>dtFrom</c>/<c>dtTo</c> filter accommodation records whose date range OVERLAPS the window
/// (<c>!(r.StartDate.Date &gt; dtTo.Date || r.EndDate.Date &lt; dtFrom.Date)</c>), the same "overlap" shape
/// <see cref="GridReportInputBase"/> already gives every report. <c>AccommodationTypeCode</c> and
/// <c>ServiceTypeCode</c> are report-specific, ported from legacy's <c>cmbAccommodationType</c>/
/// <c>cmbServiceType</c>.
/// </summary>
public class GetEmployeeAccommodationReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbAccommodationType</c> (matched by Code).</summary>
    public string AccommodationTypeCode { get; set; }

    /// <summary>
    /// Legacy <c>cmbServiceType</c>: an accommodation qualifies when ANY of its service rows (legacy
    /// <c>r.Services.Any(...)</c>) matches this service-type code.
    /// </summary>
    public string ServiceTypeCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Legacy produced one row per (EmployeeAccommodation, AccommodationServices) pair via a LEFT
/// JOIN (<c>servsGroup.DefaultIfEmpty()</c>), so an accommodation with no service rows still appears once,
/// with the service columns blank, and an accommodation with N service rows appears N times.
/// </summary>
public class EmployeeAccommodationReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string AccommodationCode { get; set; }

    public string AccommodationTypeName { get; set; }

    public string LocationName { get; set; }

    public string LocationComments { get; set; }

    public string BuildingName { get; set; }

    public string BuildingComments { get; set; }

    public string FloorName { get; set; }

    public string FloorComments { get; set; }

    public string FlatName { get; set; }

    public string FlatComments { get; set; }

    public string RoomName { get; set; }

    public string RoomComments { get; set; }

    public string BedName { get; set; }

    public string BedComments { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string JobTitle { get; set; }

    public string WorkLocation { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>Leaf org unit the seven levels above are resolved from (as of today, legacy line 700-701).</summary>
    public long? OrganizationUnitId { get; set; }

    public string ServiceTypeName { get; set; }

    public DateTime? ServiceStartDate { get; set; }

    public DateTime? ServiceEndDate { get; set; }

    public string Comments { get; set; }
}
