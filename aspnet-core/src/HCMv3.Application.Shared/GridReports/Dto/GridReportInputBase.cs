using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GridReports.Dto;

/// <summary>
/// SHARED FILTER CONTRACT for the legacy "Analysis" grid-report family (~140 legacy
/// <c>usc*.ascx</c> controls, each a filter panel + <c>ASPxGridView</c> + <c>ASPxGridViewExporter</c>).
///
/// An inventory of the family shows the same three filters over and over:
/// <c>dtFrom</c>/<c>dtTo</c> (24 of 25 sampled), <c>txtEmpCode</c> (21) and <c>cmbEmpStatus</c> (20),
/// plus one or two report-specific fields. Those three live here; each concrete report input derives
/// from this class and adds only its own fields. The Angular
/// <c>&lt;grid-report-filter&gt;</c> component is the exact UI mirror of this class.
///
/// Legacy default: when <c>dtFrom</c>/<c>dtTo</c> were left blank the code-behind stamped them with
/// <c>DateTime.Now</c> (uscRptEmployeeContract.ascx.cs:128-139), so a null here is resolved to "today"
/// server-side by <see cref="ResolveFromDate"/>/<see cref="ResolveToDate"/> rather than meaning
/// "unbounded".
/// </summary>
public abstract class GridReportInputBase : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Legacy <c>dtFrom</c>. Null is resolved to today (legacy blank-date behaviour).</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Legacy <c>dtTo</c>. Null is resolved to today (legacy blank-date behaviour).</summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Legacy <c>txtEmpCode</c>: a free-text box holding one code or a comma-separated list of codes
    /// (legacy did <c>_Empcode.Split(',')</c> then <c>employees.Contains(r.Employee.Code)</c>).
    /// </summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy <c>cmbEmpStatus</c>. Null/unselected means "all statuses".</summary>
    public EmployeeStatus? EmployeeStatus { get; set; }

    /// <summary>
    /// Default sort applied when the client sends none. PrimeNG sends <c>sorting: ""</c> (not null) on
    /// first load, so every override must guard with <c>IsNullOrWhiteSpace()</c>, never <c>??</c>.
    /// </summary>
    protected abstract string DefaultSorting { get; }

    public virtual void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = DefaultSorting;
        }
    }

    public DateTime ResolveFromDate(DateTime today)
    {
        return (FromDate ?? today).Date;
    }

    public DateTime ResolveToDate(DateTime today)
    {
        return (ToDate ?? today).Date;
    }

    /// <summary>
    /// Splits <see cref="EmployeeCode"/> on commas into a distinct, trimmed, non-empty list.
    /// Returns an empty list when no codes were supplied (meaning "do not filter by code").
    /// </summary>
    public List<string> ResolveEmployeeCodes()
    {
        if (EmployeeCode.IsNullOrWhiteSpace())
        {
            return new List<string>();
        }

        return EmployeeCode
            .Split(',')
            .Select(c => c.Trim())
            .Where(c => !c.IsNullOrWhiteSpace())
            .Distinct()
            .ToList();
    }
}
