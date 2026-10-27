using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Contract analysis grid report (legacy
/// <c>uscRptEmployeeContract.ascx</c>). Legacy exposes exactly the four common filters and nothing
/// report-specific, so this class adds no fields of its own beyond the default sort.
/// </summary>
public class GetEmployeeContractReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy applied no default sort (<c>ASPxGridView.AutoGenerateColumns</c> with sorting allowed but
    /// unset -- uscRptEmployeeContract.ascx.cs:349-354). Employee code ascending is used here so paging
    /// is deterministic, which server-side paging requires and the legacy in-memory grid did not.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeContract.ascx.cs:461-555</c> column-for-column:
/// OID, Code, Name, ContractType, StartDate, EndDate, Country.
///
/// Legacy swapped both the column CAPTIONS and the Name/ContractType/Country VALUES to Arabic when the
/// XAF aspect was <c>ar-KW</c> (lines 476-485, 524-525, 543). In this port the captions are Angular
/// localization keys and only the VALUES are resolved server-side from the request culture.
/// </summary>
public class EmployeeContractReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "Code" column -- <c>rec.Employee.Code</c>.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Name" column -- <c>Employee.FullNameAr</c> when Arabic, else <c>FullNameEn</c>.</summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "ContractType" column -- empty string (not null) when the contract has no type.</summary>
    public string ContractTypeName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>
    /// Legacy "Country" column -- country of the PayrollArea on the employee's org assignment that is
    /// current as of the FROM date. Null when there is no such assignment or it has no country.
    /// </summary>
    public string CountryName { get; set; }
}
