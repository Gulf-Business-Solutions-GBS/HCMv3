using System;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GridReports.Status.Dto;

/// <summary>
/// Filter input for the Employee New Joiner analysis grid report (legacy
/// <c>uscRptEmployeeNewJoiner.ascx</c>). Legacy exposes exactly the four common filters (filtered on
/// <c>Employee.JoiningDate</c>) and nothing report-specific.
/// </summary>
public class GetEmployeeNewJoinerReportInput : GridReportInputBase
{
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeNewJoiner.ascx.cs:460-706</c> (<c>Table1()</c>): OID, Code, Name, JobTitle, Country,
/// JoiningDate, Salary, Currency, [one column per wage type], OrgLevel1..7, WorkLocation, Gender,
/// Nationality, Email, Mobile, ContractType, ContractStartDate, ContractEndDate, CostCenter,
/// CostCenterGroup.
///
/// SCOPE REDUCTION (not a data-loss defect -- flagged for visibility, not silently dropped): legacy built
/// ONE COLUMN PER DISTINCT WAGE TYPE CODE present in the current basic-pay details (a data-driven pivot,
/// lines 548-552/667-675), then also summed all of them into <see cref="Salary"/>. The pivot columns are
/// NOT reproduced here -- unlike a fixed report shape, a per-tenant dynamic column set cannot be exposed
/// through a strongly-typed paged grid/export contract without a bespoke dynamic-columns UI, which is out
/// of scope for this wave. <see cref="Salary"/> (the sum, legacy's own aggregate) and
/// <see cref="CurrencyName"/> ARE ported, so the total is not lost -- only the per-wage-type breakdown.
/// </summary>
public class EmployeeNewJoinerReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitleName { get; set; }

    /// <summary>Legacy "Country" column -- <c>Employee.Country</c> (the personal/address country, not PayrollArea's).</summary>
    public string CountryName { get; set; }

    public DateTime JoiningDate { get; set; }

    /// <summary>Legacy "Salary" column: sum of every current basic-pay-detail amount. See class remarks.</summary>
    public double Salary { get; set; }

    /// <summary>Legacy "Currency" column: the currency of the first wage-type detail found, or empty.</summary>
    public string CurrencyName { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    public long? OrganizationUnitId { get; set; }

    public string WorkLocationName { get; set; }

    public Gender Gender { get; set; }

    public string NationalityName { get; set; }

    public string Email { get; set; }

    public long Mobile { get; set; }

    /// <summary>Legacy line 636: the most recent contract (by StartDate) regardless of date window.</summary>
    public string ContractTypeName { get; set; }

    public DateTime? ContractStartDate { get; set; }

    public DateTime? ContractEndDate { get; set; }

    public string CostCenterName { get; set; }

    public string CostCenterGroupName { get; set; }
}
