using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filter input for the Service Bonus grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptServiceBonus.ascx</c>).
///
/// LEGACY QUIRK REPRODUCED: <c>dtFrom</c>/<c>dtTo</c> (base <see cref="GridReportInputBase.FromDate"/> /
/// <see cref="GridReportInputBase.ToDate"/>) are shown on the panel and logged in <c>GetParameterStr</c>
/// but <c>Table1()</c> (lines 497-710) never reads them -- only Employee Status and Employee Code filter
/// the data. Kept here only so the logged parameter string matches legacy.
/// </summary>
public class GetServiceBonusReportInput : GridReportInputBase
{
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c>: the wage-type-35/36 "Service Bonus" salary-register line for
/// each employee's most recent Regular in-period register, plus the derived years-of-service.
/// </summary>
public class ServiceBonusReportRowDto
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string EmployeeNameAr { get; set; }

    public string JobTitle { get; set; }

    public string Grade { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    public System.DateTime JoiningDate { get; set; }

    /// <summary>The salary register period the bonus (or its arrear) was posted in.</summary>
    public System.DateTime? ServiceDueDate { get; set; }

    /// <summary>
    /// Legacy lines 670-679: whole years between <see cref="JoiningDate"/> and
    /// <see cref="ServiceDueDate"/> + 1 day.
    /// </summary>
    public double? ServicePeriod { get; set; }

    /// <summary>Wage type 35 (Service Bonus) amount.</summary>
    public double Amount { get; set; }

    /// <summary>Wage type 36 (Service Bonus Arrear) amount.</summary>
    public double ArrearAmount { get; set; }
}
