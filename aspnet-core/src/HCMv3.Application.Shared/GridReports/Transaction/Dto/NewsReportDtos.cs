using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Transaction.Dto;

/// <summary>
/// Filter input for the News analysis grid report (legacy <c>uscNews.ascx</c>).
///
/// Legacy had NO filter panel at all: <c>BindNewsDocument</c> (uscNews.ascx.cs:157-204) always queried
/// "today" only (<c>r.IssueDate.Date &lt;= DateTime.Now.Date &amp;&amp; r.ExpiryDate.Date &gt;= DateTime.Now.Date</c>)
/// and never touched Employee code/status. This port generalizes the hard-coded "today" check into the
/// shared From/To window (identical result on first load, since both default to today), for consistency
/// with the rest of the grid-report family; <see cref="GridReportInputBase.EmployeeCode"/> and
/// <see cref="GridReportInputBase.EmployeeStatus"/> are inherited but intentionally UNUSED here (legacy
/// never filtered News by employee) and are hidden on the Angular screen.
/// </summary>
public class GetNewsReportInput : GridReportInputBase
{
    /// <summary>Legacy showed the newest item first (<c>OrderByDescending(x =&gt; x.Oid)</c>, uscNews.ascx.cs:170).</summary>
    protected override string DefaultSorting => "Id DESC";
}

/// <summary>
/// One grid row. Mirrors the legacy anonymous projection (uscNews.ascx.cs:164-170): Oid, Description,
/// DocumentHtml, IssueDate. Legacy source is actually <c>EmployeeDocument</c> rows whose
/// <c>DocumentType.NameEn == "NEWS"</c> -- there is a separate legacy <c>News</c> business object but the
/// uscNews control does not query it.
/// </summary>
public class NewsReportRowDto : EntityDto<int>
{
    public string Description { get; set; }

    public string DocumentHtml { get; set; }

    public DateTime IssueDate { get; set; }
}
