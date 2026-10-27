using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Sponsor analysis grid report (legacy
/// <c>uscRptEmployeeSponsor.ascx</c>).
///
/// This report is an AS-ON-DATE report, not a date-range one: the legacy markup hides <c>dtTo</c>
/// (<c>uscRptEmployeeSponsor.ascx:12,21 Visible="false"</c>) and the code-behind labels the remaining date
/// "As On Date" (<c>GetParameterStr</c>). Only <see cref="GridReportInputBase.FromDate"/> is used;
/// <see cref="GridReportInputBase.ToDate"/> is accepted (it is on the shared base) but IGNORED, exactly as
/// legacy ignored it.
/// </summary>
public class GetEmployeeSponsorReportInput : GridReportInputBase
{
    /// <summary>Legacy sorted the rows by employee code (<c>newds.OrderBy(r =&gt; r.Code)</c>, line 596).</summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeSponsor.ascx.cs:459-650</c> column-for-column: OID, Code, Name, EmployeeStatus,
/// SponsorStatus, Sponsor, GovtJobTitle, GovtJobTitleAr, GovtJobTitleCategory, StartDate, EndDate,
/// ContractSalary, SalaryCurrency, EmployeeSponsorRelation, Country, Nationality, NationalityGroup,
/// ContractType, ContractStartDate, ContractEndDate, ResidencyType.
///
/// Three different "as of" dates are in play and they are legacy's, not a simplification:
/// the SPONSOR block is as of the As-On (From) date (line 551-553), the CONTRACT block is as of
/// <c>DateTime.Now</c> (line 574-575), and Country comes from the employee's CURRENT payroll area.
/// </summary>
public class EmployeeSponsorReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    /// <summary>Legacy "EmployeeStatus" column -- the raw <c>Employee.Status</c> enum value.</summary>
    public EmployeeStatus EmployeeStatus { get; set; }

    /// <summary>
    /// Legacy "SponsorStatus" column (line 618): the literal string "Active" when a sponsor record is
    /// current on the As-On date and "Expired" when none is. Not localized in legacy; kept verbatim so
    /// saved filters and downstream sheets keep matching.
    /// </summary>
    public string SponsorStatus { get; set; }

    public string SponsorName { get; set; }

    /// <summary>Legacy "GovtJobTitle" -- ALWAYS the English name, even in Arabic (line 556).</summary>
    public string GovtJobTitleEn { get; set; }

    /// <summary>Legacy "GovtJobTitleAr" -- the Arabic name, shown as its own column alongside the English one.</summary>
    public string GovtJobTitleAr { get; set; }

    public string GovtJobTitleCategoryName { get; set; }

    /// <summary>Sponsor record start date. Null when no sponsor record is current on the As-On date.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Sponsor record end date. Null when no sponsor record is current on the As-On date.</summary>
    public DateTime? EndDate { get; set; }

    public double? ContractSalary { get; set; }

    public string SalaryCurrencyName { get; set; }

    /// <summary>Legacy "EmployeeSponsorRelation" -- HCMv3 <c>EmployeeSponsor.EmpSponsorRelation</c>.</summary>
    public string EmployeeSponsorRelation { get; set; }

    /// <summary>
    /// Legacy "Country" column -- <c>Employee.CurrentPayrollArea.Country</c>. HCMv3 has no
    /// <c>CurrentPayrollArea</c> computed property, so it is resolved the same way the ported
    /// EmployeeContract report resolves it: the PayrollArea of the org assignment current TODAY
    /// ("current" is what the legacy computed property meant).
    /// </summary>
    public string CountryName { get; set; }

    public string NationalityName { get; set; }

    public string NationalityGroupName { get; set; }

    public string ContractTypeName { get; set; }

    public DateTime? ContractStartDate { get; set; }

    public DateTime? ContractEndDate { get; set; }

    /// <summary>Legacy "ResidencyType" column. Null when no sponsor record is current on the As-On date.</summary>
    public ResidencyType? ResidencyType { get; set; }
}
