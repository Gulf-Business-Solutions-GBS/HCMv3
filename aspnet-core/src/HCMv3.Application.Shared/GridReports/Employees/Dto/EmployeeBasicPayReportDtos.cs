using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Basic Pay analysis grid report (legacy
/// <c>uscRptEmployeeBasicPay.ascx</c>).
///
/// ATYPICAL for this family: the legacy panel renders <c>dtFrom</c>/<c>dtTo</c>, but
/// <c>Table1()</c> NEVER READS THEM -- the only date predicate in the method is commented out
/// (uscRptEmployeeBasicPay.ascx.cs:552-554) and <c>GetParameterStr()</c> (lines 444-454) logs only the
/// status and the employee code. The two date pickers are dead UI. The inherited
/// <see cref="GridReportInputBase.FromDate"/>/<see cref="GridReportInputBase.ToDate"/> are therefore
/// deliberately NOT applied, and the Angular screen hides the date range rather than showing controls
/// that do nothing.
/// </summary>
public class GetEmployeeBasicPayReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy left the grid unsorted and then grouped by employee code to compute the increment
    /// (line 682). Code ascending, then start date descending, matches the order the increment is
    /// computed in and keeps server-side paging deterministic.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, StartDate DESC";
}

/// <summary>
/// One grid row = one <c>EmployeeBasicPay</c> record. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeBasicPay.ascx.cs:462-706</c>: OID, Code, Name, JoiningDate, EmployeeStatus,
/// StartDate, EndDate, JobTitle, WorkLocation, OrgLevel1..7, Status, Currency, ONE COLUMN PER WAGE TYPE,
/// TotalAllowance, "Total Amount", "Increment Amount".
///
/// DEVIATIONS:
/// <list type="bullet">
/// <item><description>The legacy per-wage-type columns are DYNAMIC (one DataColumn per wage type found
/// in the data, ordered by <c>WageType.PayrollOrder</c> -- lines 481/540-545). A typed DTO cannot have
/// dynamic properties, so they are carried as the <see cref="WageAmounts"/> list and the Angular grid
/// and the Excel export both expand that list into columns.</description></item>
/// <item><description>The legacy OrgLevel1..7 columns are the single <see cref="OrganizationUnitName"/>
/// (HCMv3 collapsed those seven fields into one OrganizationUnit FK).</description></item>
/// </list>
/// </summary>
public class EmployeeBasicPayReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Name" column -- FullNameAr when Arabic, else FullNameEn.</summary>
    public string EmployeeName { get; set; }

    public DateTime JoiningDate { get; set; }

    /// <summary>Legacy "EmployeeStatus" column -- <c>Employee.Status.ToString()</c>.</summary>
    public string EmployeeStatusName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>Legacy "JobTitle" column, from the org assignment current TODAY (line 570).</summary>
    public string JobTitleName { get; set; }

    /// <summary>Legacy "WorkLocation" column, from the org assignment current TODAY.</summary>
    public string WorkLocationName { get; set; }

    /// <summary>Stands in for the legacy OrgLevel1..7 columns, from the assignment current TODAY.</summary>
    public string OrganizationUnitName { get; set; }

    /// <summary>
    /// Legacy "Status" column (line 662): "Current" when the pay record covers today, else "History".
    /// Distinct from <see cref="EmployeeStatusName"/>, which is the employee's own status.
    /// </summary>
    public string RecordStatus { get; set; }

    /// <summary>
    /// Legacy "Currency" column (lines 655-663): the currency of the FIRST wage line on the record --
    /// legacy took whichever line the enumerator yielded first, so it is only meaningful when all the
    /// lines share a currency.
    /// </summary>
    public string Currency { get; set; }

    /// <summary>
    /// The legacy dynamic per-wage-type columns. See the class remarks.
    /// </summary>
    public List<EmployeeBasicPayReportWageAmountDto> WageAmounts { get; set; }
        = new List<EmployeeBasicPayReportWageAmountDto>();

    /// <summary>
    /// Legacy "TotalAllowance" column (line 626): the sum of the record's wage lines EXCLUDING wage type
    /// "1000" (basic pay) and including only wage types flagged <c>IncludeInBasicPay</c>.
    /// </summary>
    public double TotalAllowance { get; set; }

    /// <summary>Legacy "Total Amount" column -- the record's own <c>Amount</c>, not a sum of the lines.</summary>
    public double TotalAmount { get; set; }

    /// <summary>
    /// Legacy "Increment Amount" column (lines 680-701): within one employee, ordering the records by
    /// StartDate DESCENDING, this row's TotalAmount minus the NEXT row's in that order (i.e. the
    /// previous pay record). Zero when there is no previous record or its amount is zero.
    ///
    /// This is why the report cannot be paged in the database: the value depends on the employee's other
    /// rows, so the whole filtered set is built first and paged afterwards.
    /// </summary>
    public double IncrementAmount { get; set; }
}

/// <summary>
/// One wage-type cell of a <see cref="EmployeeBasicPayReportRowDto"/>, standing in for one of the
/// legacy dynamic wage-type columns.
/// </summary>
public class EmployeeBasicPayReportWageAmountDto
{
    public int WageTypeId { get; set; }

    public string WageTypeCode { get; set; }

    /// <summary>Wage type name in the request culture -- legacy used the Ar/En name per aspect (line 649).</summary>
    public string WageTypeName { get; set; }

    /// <summary>Legacy ordered the generated columns by this (line 481).</summary>
    public int PayrollOrder { get; set; }

    public double Amount { get; set; }

    /// <summary>
    /// Currency short form of this wage line. Legacy's drill-down grid (<c>Table2()</c>, lines 912-988)
    /// was nothing but the wage lines of the selected pay record -- that data is already carried here,
    /// so the drill-down becomes an expandable row rather than a second server round trip.
    /// </summary>
    public string CurrencyShortForm { get; set; }
}
