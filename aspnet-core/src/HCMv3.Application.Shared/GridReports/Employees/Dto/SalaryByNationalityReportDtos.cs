using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Salary By Nationality analysis grid report (legacy
/// <c>uscRptSalaryByNationality.ascx</c>).
///
/// This is an AGGREGATE report -- one row per nationality, not per employee -- but it still fits the
/// shared filter contract because legacy only ever exposed two filters on it: <c>cmbEmpStatus</c> and
/// the report-specific <c>cmbNationality</c> (uscRptSalaryByNationality.ascx.cs:483-489
/// <c>GetParameterStr</c> logs exactly those two and nothing else).
///
/// INHERITED-BUT-UNUSED: <see cref="GridReportInputBase.FromDate"/>/<see cref="GridReportInputBase.ToDate"/>
/// and <see cref="GridReportInputBase.EmployeeCode"/> are NOT used by this report. Legacy has no date or
/// employee-code inputs here at all -- <c>Table1()</c> pins the basic-pay rows to
/// <c>DateTime.Now</c> (lines 527-529). The Angular screen therefore switches those inputs off on the
/// shared filter panel rather than sending values the server would ignore.
/// </summary>
public class GetSalaryByNationalityReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbNationality</c>: filters on the Nationality CODE (line 545). Null or blank means
    /// "all nationalities".
    /// </summary>
    public string NationalityCode { get; set; }

    /// <summary>Legacy sorted the grouped result by nationality name (lines 556/569).</summary>
    protected override string DefaultSorting => "NationalityName ASC";
}

/// <summary>
/// One grid row -- ONE NATIONALITY, not one employee. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptSalaryByNationality.ascx.cs:499-598</c> column-for-column:
/// OID, Code, Nationality, Amount, Staff, "Percentage(%)".
/// </summary>
public class SalaryByNationalityReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "OID" column is the NATIONALITY's oid (line 551), not an employee's.</summary>
    /// <summary>Legacy "Code" column -- the nationality code (line 552).</summary>
    public string NationalityCode { get; set; }

    /// <summary>Legacy "Nationality" column -- NameAr when Arabic, else NameEn (line 553).</summary>
    public string NationalityName { get; set; }

    /// <summary>Legacy "Amount" column -- sum of current basic-pay amounts for that nationality (line 554).</summary>
    public double Amount { get; set; }

    /// <summary>
    /// Legacy "Staff" column -- <c>r.Count()</c> over the grouped basic-pay rows (line 555). Note this
    /// counts BASIC PAY ROWS current today, which is one per employee in practice but is not a
    /// DISTINCT employee count; reproduced as-is.
    /// </summary>
    public int Staff { get; set; }

    /// <summary>
    /// Legacy "Percentage(%)" column (line 566): the nationality's amount as a percentage of
    /// <c>TotalAmount</c>, rounded to 3 decimals. IMPORTANT -- <c>TotalAmount</c> is computed at line 531
    /// BEFORE the status, nationality and row-level-security filters are applied, so the percentages of a
    /// filtered result deliberately do NOT add up to 100. That is legacy behaviour and is reproduced.
    /// </summary>
    public double Percentage { get; set; }
}
