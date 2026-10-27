using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Bonus analysis grid report (legacy <c>uscRptEmployeeBonus.ascx</c>).
///
/// Legacy HID both date pickers (<c>uscRptEmployeeBonus.ascx:19-22</c>, <c>Visible="false"</c>) and
/// <c>GetParameterStr</c> (lines 434-446) logs only Status and Code, so this report has NO date range at
/// all: the year columns always span every bonus transaction in the system. <see cref="FromDate"/>/
/// <see cref="ToDate"/> are inherited from the base but deliberately unused, and the Angular screen hides
/// them.
/// </summary>
public class GetEmployeeBonusReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy applied no sort. Employee code ascending, as in the reference port, so server-side paging is
    /// deterministic. Only <c>EmployeeCode</c>/<c>EmployeeName</c> are sortable -- the year, average and
    /// monthly-average values are computed after materialization (see the app service remarks).
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>One year column of the legacy pivot (legacy added one DataColumn per year, lines 486-495).</summary>
public class EmployeeBonusYearAmountDto
{
    public int Year { get; set; }

    public double Amount { get; set; }
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (uscRptEmployeeBonus.ascx.cs:449-563).
///
/// ATYPICAL: this report is a PIVOT -- legacy generated one DataColumn per calendar year between the
/// MIN and MAX <c>PaymentDate.Year</c> of every wage-type-1053 transaction in the database (lines 458-459,
/// 486-495), then Average and MonthlyAverage. The dynamic year set is returned once per response on
/// <see cref="EmployeeBonusReportOutput.Years"/>; the per-row values are in <see cref="YearlyAmounts"/>,
/// in the same order.
/// </summary>
public class EmployeeBonusReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "Code" column -- <c>rec.Code</c> (the Employee, not the transaction).</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Name" column -- <c>FullNameAr</c> when Arabic, else <c>FullNameEn</c> (line 522).</summary>
    public string EmployeeName { get; set; }

    /// <summary>One entry per year in <see cref="EmployeeBonusReportOutput.Years"/>, same order.</summary>
    public List<EmployeeBonusYearAmountDto> YearlyAmounts { get; set; } = new();

    /// <summary>
    /// Legacy "AverageBonusAmount" (line 555): total bonus divided by the NUMBER OF YEARS THAT WERE
    /// NON-ZERO (not the number of year columns), or 0 when the employee has no bonus at all.
    /// </summary>
    public double AverageBonusAmount { get; set; }

    /// <summary>Legacy "MonthlyAverageBonusAmount" (line 556): <see cref="AverageBonusAmount"/> / 12.</summary>
    public double MonthlyAverageBonusAmount { get; set; }
}

/// <summary>
/// Paged result plus the dynamic year column set. A plain <c>PagedResultDto</c> cannot carry the pivot's
/// column list, and the client needs it to render the header.
/// </summary>
public class EmployeeBonusReportOutput : PagedResultDto<EmployeeBonusReportRowDto>
{
    /// <summary>
    /// The generated year columns, ascending. Empty when there is not a single wage-type-1053 transaction
    /// in the database (legacy's <c>minYear == 0</c> branch, line 487, which added no year columns).
    /// </summary>
    public List<int> Years { get; set; } = new();
}
