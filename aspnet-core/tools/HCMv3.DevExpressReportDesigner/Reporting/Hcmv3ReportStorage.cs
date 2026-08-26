using System.Collections.Generic;
using System.IO;
using DevExpress.XtraReports.UI;
using DevExpress.XtraReports.Web.Extensions;

namespace HCMv3.DevExpressReportDesigner.Reporting;

/// <summary>
/// Maps the single report name "PayslipEn" the Designer/Viewer widgets bind to, onto the real
/// rptPayslipEn report -- constructed from the exported legacy layout and rebound against the live
/// HCMv3 database (see <see cref="PayslipEnReportFactory"/>).
/// </summary>
public class Hcmv3ReportStorage : ReportStorageWebExtension
{
    public const string ReportUrl = "PayslipEn";

    public override bool CanSetData(string url) => false;

    public override bool IsValidUrl(string url) => url == ReportUrl;

    public override byte[] GetData(string url)
    {
        using var report = PayslipEnReportFactory.Build();
        using var stream = new MemoryStream();
        report.SaveLayoutToXml(stream);
        return stream.ToArray();
    }

    public override Dictionary<string, string> GetUrls() => new()
    {
        [ReportUrl] = "Payslip (English) -- rptPayslipEn",
    };

    public override void SetData(XtraReport report, string url)
    {
        // No-op: this is a read/render demo host, not a report authoring store. Designer edits are
        // not persisted back to Reports/rptPayslipEn.repx.
    }

    public override string SetNewData(XtraReport report, string defaultUrl) => defaultUrl;
}
