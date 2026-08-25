using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GridReports.Status.Dto;

/// <summary>
/// Filter input for the Employee Full &amp; Final Payment analysis grid report (legacy
/// <c>uscRptEmployeeFnFPayment.ascx</c>).
///
/// DEVIATION: legacy's <c>cmbEmpStatus</c> box is LABELLED "Employee Status" but is actually bound to
/// the <see cref="PersonnelAdministration.SeparationType"/> enum (lines 60/67/191 -- "lblLeaveType"/
/// "Separation Type") and filtered against <c>EmployeeSeparation.SeparationType</c>, not
/// <c>Employee.Status</c>. The shared base's <see cref="GridReportInputBase.EmployeeStatus"/> is the
/// wrong type for that, so this report does not use it (never bound in the Angular filter panel) and
/// adds its own correctly-typed <see cref="SeparationType"/> filter instead.
/// </summary>
public class GetEmployeeFnFPaymentReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbEmpStatus</c> (mislabelled -- see class remarks): actually a separation-type filter.</summary>
    public SeparationType? SeparationType { get; set; }

    /// <summary>Legacy applied no default sort; employee code ascending keeps paging deterministic.</summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeFnFPayment.ascx.cs:459-662</c> (<c>Table1()</c>): OID, Code, Name, JobTitle, Salary,
/// JoiningDate, CompensationDate, WorkLocation, ResidencyType, PHBalance, PayStatus, Country.
/// NoticePeriodDays/LastDayInService/FnFPaymentType/FnFPaymentReason are declared as legacy
/// <c>DataColumn</c>s but every line that would fill them is commented out (lines 473-474/510/513-514),
/// so legacy always rendered them empty -- porting four permanently-blank columns would be porting a bug.
/// OrgLevel1..7 are likewise commented out for this report (lines 564-570) and are correctly absent here.
/// </summary>
public class EmployeeFnFPaymentReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitleName { get; set; }

    /// <summary>Legacy "Salary" column: the basic-pay HEADER amount current TODAY (line 547-554).</summary>
    public double Salary { get; set; }

    public DateTime JoiningDate { get; set; }

    public DateTime CompensationDate { get; set; }

    public string WorkLocationName { get; set; }

    /// <summary>
    /// Legacy line 611-613: the sponsor record whose window covers the CompensationDate (not "today").
    /// Legacy assigned the raw enum to an untyped DataTable cell (rendered via <c>ToString()</c> on
    /// export); when no sponsor record covers the date this is <c>default(ResidencyType)</c> ("Company"),
    /// exactly as legacy's <c>FirstOrDefault()</c> on a non-nullable enum sequence would yield -- pinned
    /// as a legacy quirk rather than "fixed" to null, see the test with the matching name.
    /// </summary>
    public ResidencyType ResidencyType { get; set; }

    /// <summary>Legacy "PHBalance" column -- <c>EmployeeAbsenceQuota.QuotaBalance</c> for AbsenceType code "9".</summary>
    public double PHBalance { get; set; }

    /// <summary>
    /// Legacy "PayStatus" column -- actually <c>EmployeeAbsenceQuota.QuotaUsed</c> (line 517/648), not a
    /// pay/approval status despite the name. Reproduced as-is; the naming is legacy's own.
    /// </summary>
    public double PayStatus { get; set; }

    public string CountryName { get; set; }
}
