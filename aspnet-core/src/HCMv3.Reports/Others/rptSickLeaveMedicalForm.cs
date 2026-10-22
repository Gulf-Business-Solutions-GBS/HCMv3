using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Others.rptSickLeaveMedicalForm</c>.
    ///
    /// The medical-referral letter that accompanies a sick-leave case: employee header block, a
    /// company logo, and two small fixed graphics.
    ///
    /// PORTABILITY. The legacy class had a second constructor taking <c>object[] args</c> plus an
    /// <c>isUnifiedLeaveRequest</c> flag, but its entire body was commented out -- the only live
    /// statement was <c>PrmFormDate.Value = DateTime.Now</c>, identical to the default constructor.
    /// The report's QUERY reads Employees / EmployeeOrgAssignments only and never touches the legacy
    /// self-service <c>LeaveRequest</c> object, so it is ported in full. The dead overload is
    /// replaced by a typed (employeeCode, formDate) one.
    ///
    /// XAF CONSTRUCTS REPLACED:
    ///
    /// | Legacy                                                       | Replacement |
    /// |--------------------------------------------------------------|-------------|
    /// | <c>rptSickLeaveMedicalForm(object[] args, bool isUnifiedLeaveRequest)</c> with a commented-out <c>WorkflowRequest</c>/<c>LeaveRequest</c> body | <see cref="rptSickLeaveMedicalForm(string, DateTime)"/>. |
    /// | <c>xrPictureBox2.Image</c> / <c>xrPictureBox3.Image</c> from the report .resx | embedded PNG / JPEG attached in the constructor; see the loader below. |
    ///
    /// <c>xrPictureBox1</c> is NOT one of those: it is data-bound to <c>[Companylogo]</c>, the employee's
    /// company logo, which HcmReportBase loads from the file store from <c>CompanylogoOrgUnitId</c>
    /// (OthersLeaveSql rewrite note 2).
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/>
    /// before <c>CreateDocument()</c> or the report returns nothing.
    /// </summary>
    public partial class rptSickLeaveMedicalForm : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "SickLeaveMedicalForm";

        // The two fixed graphics, shipped as embedded files rather than .resx entries for the reason
        // given on rptSickLeaveForm.FormScanResourceName. Byte-identical to the legacy resources;
        // the legacy entries happened to be one PNG and one JPEG, and the extensions record that.
        private const string PictureBox2ResourceName =
            "HCMv3.Reports.Others.rptSickLeaveMedicalForm.xrPictureBox2.png";

        private const string PictureBox3ResourceName =
            "HCMv3.Reports.Others.rptSickLeaveMedicalForm.xrPictureBox3.jpg";

        public rptSickLeaveMedicalForm()
        {
            InitializeComponent();

            PrmEmpCode.Value = string.Empty;
            PrmFormDate.Value = DateTime.Now;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            // SIDE EFFECT of moving the graphics out of the .resx: the Visual Studio report designer
            // shows empty picture boxes. The rendered document is unaffected.
            xrPictureBox2.Image = LoadEmbeddedImage(PictureBox2ResourceName);
            xrPictureBox3.Image = LoadEmbeddedImage(PictureBox3ResourceName);
        }

        /// <summary>Replaces the legacy (dead) <c>object[] args</c> constructor. See the class summary.</summary>
        public rptSickLeaveMedicalForm(string employeeCode, DateTime formDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmFormDate.Value = formDate;
        }

        /// <summary>
        /// Returns the embedded image, or <c>null</c> if it cannot be loaded. Never throws: a
        /// missing graphic is a cosmetic defect and must not fail the document.
        /// </summary>
        private static Image LoadEmbeddedImage(string resourceName)
        {
            try
            {
                var assembly = typeof(rptSickLeaveMedicalForm).GetTypeInfo().Assembly;
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
