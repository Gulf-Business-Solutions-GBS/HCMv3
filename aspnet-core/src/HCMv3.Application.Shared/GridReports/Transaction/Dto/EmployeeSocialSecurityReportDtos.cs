using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Transaction.Dto;

/// <summary>
/// Filter input for the Employee Social Security analysis grid report (legacy
/// <c>uscRptEmployeeSocialSecurity.ascx</c>).
///
/// DEVIATION FROM THE SHARED FROM/TO CONTRACT: legacy's own filter panel has a SINGLE <c>dtDate</c> picker
/// ("effective as of" date), not a range. <see cref="AsOfDate"/> carries that; the base
/// <c>FromDate</c>/<c>ToDate</c> are unused by this report and hidden client-side.
/// </summary>
public class GetEmployeeSocialSecurityReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>dtDate</c>. Null defaults to today, same as the base class's blank-date rule.</summary>
    public DateTime? AsOfDate { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";

    public DateTime ResolveAsOfDate(DateTime today) => (AsOfDate ?? today).Date;
}

/// <summary>
/// One grid row: an employee's PIFSS social-security contribution calculation as of the requested date.
/// Mirrors the summary columns of legacy <c>Table1()</c> (uscRptEmployeeSocialSecurity.ascx.cs:437-807).
///
/// SCOPE REDUCTION: legacy adds ONE DYNAMIC COLUMN PER <c>Education</c> MASTER ROW, each holding that
/// education's allowance amount for the employee (a data-driven pivot with no fixed shape). This port
/// keeps the EducationAllowance TOTAL (which legacy also computed) and drops the per-education breakdown
/// columns -- not a data-loss defect, the total is exact, only the per-education detail is folded away.
/// </summary>
public class EmployeeSocialSecurityReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public DateTime EffectiveDate { get; set; }

    public string Gender { get; set; }

    public string MaritalStatusName { get; set; }

    public double BasicSalary { get; set; }

    public double ContractSalary { get; set; }

    public string EducationBranchName { get; set; }

    public string EducationLevelName { get; set; }

    public double MaritalAllowance { get; set; }

    public double ChildAllowance { get; set; }

    public double SpecialChildAllowance { get; set; }

    public double EducationAllowance { get; set; }

    public double GrossAmount { get; set; }

    public double TotalAmount { get; set; }

    /// <summary>Legacy "PIFSS Salary Cap 1" (the lower earnings-tier boundary applied).</summary>
    public double Caps { get; set; }

    /// <summary>Legacy "Extra PIFSS Cap 2" (the upper earnings-tier boundary applied, if any).</summary>
    public double ExtraCaps { get; set; }

    public double CompanySharePer { get; set; }

    public double EmployeeSharePer { get; set; }

    public double CompanyShareAmount { get; set; }

    public double EmployeeShareAmount { get; set; }
}
