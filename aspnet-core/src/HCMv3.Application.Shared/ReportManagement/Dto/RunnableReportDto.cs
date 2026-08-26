namespace HCMv3.ReportManagement.Dto;

/// <summary>
/// One entry in the "Print Reports" launcher list: a <see cref="PrintReport"/> catalog row the
/// current user may run, plus whether a report class actually exists for it in this release.
///
/// The field names are the contract the Angular launcher screen is written against; NSwag emits
/// them camelCased (id, code, nameEn, nameAr, reportName, reportCategory, isAvailable).
/// </summary>
public class RunnableReportDto
{
    /// <summary><see cref="PrintReport"/> primary key.</summary>
    public int Id { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    /// <summary>
    /// The DevExpress report identifier. This is what the client passes as <c>reportUrl</c> to
    /// /DXXRD (designer) and /DXXRDV (viewer).
    /// </summary>
    public string ReportName { get; set; }

    /// <summary>
    /// <see cref="ReportCategory"/> enum NAME (not the numeric value) -- deliberately a string so
    /// the client renders a stable token instead of a magic number.
    /// </summary>
    public string ReportCategory { get; set; }

    /// <summary>
    /// True only when a ported report class exists for <see cref="ReportName"/>. False means the
    /// catalog row is real but the report layout has not been ported yet: show it, do not let the
    /// user open it.
    /// </summary>
    public bool IsAvailable { get; set; }
}
