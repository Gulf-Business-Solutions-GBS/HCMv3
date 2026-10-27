using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Others.rptSickLeaveForm</c>.
    ///
    /// The Kuwait Ministry of Health sick-leave form: a full-page scan with the employee's details
    /// overlaid, plus <see cref="srptSickLeaveForm"/> listing their sick-leave history.
    ///
    /// PORTABILITY. The legacy class had three constructors, two of which took the self-service
    /// <c>HCM10.Module.BusinessObjects.Workflow.LeaveRequest</c>. HCMv3 has no LeaveRequest entity,
    /// so those overloads are dropped -- but they were only ever CONVENIENCE overloads: all they did
    /// was copy <c>Type1.Employee.Code</c> into <c>PrmEmpCode</c> and stamp <c>PrmFormDate</c> with
    /// <c>DateTime.Now</c>. The report's own QUERY never read a leave request. The report is
    /// therefore ported in full, driven by employee code + form date, which is exactly what the
    /// third legacy constructor (the "unified leave request" one) already did.
    ///
    /// XAF CONSTRUCTS REPLACED:
    ///
    /// | Legacy                                                       | Replacement |
    /// |--------------------------------------------------------------|-------------|
    /// | <c>rptSickLeaveForm(object[] args)</c> taking a <c>WorkflowRequest</c> cast to <c>LeaveRequest</c> | <see cref="rptSickLeaveForm(string, DateTime)"/>. The two values it extracted are the two parameters. |
    /// | <c>rptSickLeaveForm(object[] args, bool isUnifiedLeaveRequest)</c> -- an untyped (empCode, startDateTime) pair | the same typed constructor. The <c>isUnifiedLeaveRequest</c> flag was never read. |
    /// | client-side <c>FilterString = "[Code] In (?PrmEmpCode)"</c> | replaced by <c>emp.Code = @PrmEmpCode</c> in SQL, on this report AND on the subreport. See OthersLeaveSql rewrite note 3. |
    /// | <c>xrPictureBox1.Image</c> from the report .resx | an embedded PNG attached in the constructor; see the loader below. |
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/>
    /// before <c>CreateDocument()</c> or the report returns nothing -- it reaches the subreport too.
    /// </summary>
    public partial class rptSickLeaveForm : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "SickLeaveForm";

        /// <summary>
        /// The full-page scan of the official form. Shipped as an embedded PNG rather than a .resx
        /// entry: HCMv3.Reports multi-targets net8.0 as well as net8.0-windows, and a .resx entry
        /// typed <c>System.Drawing.Bitmap</c> forces the resource compiler to bind a Windows-only
        /// type on both flavours. Byte-identical to the legacy resource (the legacy entry used the
        /// <c>bytearray.base64</c> mimetype, i.e. the raw PNG file, not a serialised object) -- this
        /// is a photograph of an official paper form whose field positions the labels are aligned
        /// against, so it is NOT recompressed or resized.
        /// </summary>
        private const string FormScanResourceName =
            "HCMv3.Reports.Others.rptSickLeaveForm.xrPictureBox1.png";

        public rptSickLeaveForm()
        {
            InitializeComponent();

            PrmEmpCode.Value = string.Empty;
            PrmFormDate.Value = DateTime.Now;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            // SIDE EFFECT of moving the scan out of the .resx: the Visual Studio report designer
            // shows an empty picture box. The rendered document is unaffected.
            xrPictureBox1.Image = LoadEmbeddedImage(FormScanResourceName);
        }

        /// <summary>
        /// Replaces both legacy <c>object[] args</c> constructors. See the class summary.
        /// </summary>
        public rptSickLeaveForm(string employeeCode, DateTime formDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmFormDate.Value = formDate;
        }

        /// <summary>
        /// Returns the embedded image, or <c>null</c> if it cannot be loaded. Never throws: a
        /// missing background is a cosmetic defect and must not fail the document.
        /// </summary>
        private static Image LoadEmbeddedImage(string resourceName)
        {
            try
            {
                var assembly = typeof(rptSickLeaveForm).GetTypeInfo().Assembly;
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    // GDI+ keeps a reference to the backing stream for the lifetime of the Image, so
                    // the manifest stream is copied into a MemoryStream that is deliberately NOT
                    // disposed -- disposing it would make the image throw on first draw.
                    var buffer = new MemoryStream();
                    stream.CopyTo(buffer);
                    buffer.Position = 0;
                    return Image.FromStream(buffer, useEmbeddedColorManagement: false, validateImageData: false);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
