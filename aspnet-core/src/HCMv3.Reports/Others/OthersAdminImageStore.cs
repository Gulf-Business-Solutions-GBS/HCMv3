using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// Loads the fixed artwork that four OthersAdmin reports print behind or beside their labels:
    /// a company logo/letterhead strip on rptEmployeeProfile and rptBusinessTrip, and a full-page
    /// scanned form on rptPassportInOut and rptPassportRequestForm.
    ///
    /// WHY THIS EXISTS. In the legacy reports these lived inside the report's <c>.resx</c> and the
    /// designer file pulled them out with
    /// <c>(System.Drawing.Image)resources.GetObject("xrPictureBoxN.Image")</c>. That does not carry
    /// over: HCMv3.Reports multi-targets net8.0 as well as net8.0-windows, and a .resx entry typed
    /// <c>System.Drawing.Bitmap</c> forces the resource compiler to bind a Windows-only type on
    /// both flavours. Same finding, same fix, and the same file layout as
    /// <c>GovtForms/GovtFormsImageStore.cs</c> and <c>Others/SalaryCertificateBackground.cs</c>.
    ///
    /// FIDELITY: the bytes are byte-identical to the legacy resource -- the legacy entries used the
    /// <c>bytearray.base64</c> mimetype, i.e. the raw PNG/JPEG file rather than a serialised
    /// object, so they were extracted and written straight out. The two passport forms are
    /// photographs of official paper whose field positions the labels are aligned against, so they
    /// are deliberately NOT recompressed or resized.
    ///
    /// SIDE EFFECT: the Visual Studio report designer shows an empty picture box rather than the
    /// artwork, because the image is no longer assigned inside InitializeComponent. The rendered
    /// document is unaffected.
    /// </summary>
    internal static class OthersAdminImageStore
    {
        private const string ResourcePrefix = "HCMv3.Reports.Others.";

        internal const string EmployeeProfileLogo = ResourcePrefix + "rptEmployeeProfile.xrPictureBox2.png";
        internal const string BusinessTripLogo = ResourcePrefix + "rptBusinessTrip.xrPictureBox2.png";
        internal const string PassportInOutForm = ResourcePrefix + "rptPassportInOut.xrPictureBox1.jpg";
        internal const string PassportRequestForm = ResourcePrefix + "rptPassportRequestForm.xrPictureBox1.jpg";

        /// <summary>
        /// Returns the embedded artwork, or <c>null</c> if it cannot be loaded. Never throws: a
        /// missing background is a cosmetic defect and must not fail the document.
        /// </summary>
        internal static Image Load(string resourceName)
        {
            try
            {
                var assembly = typeof(OthersAdminImageStore).GetTypeInfo().Assembly;
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    // GDI+ keeps a reference to the backing stream for the lifetime of the Image,
                    // so the manifest stream is copied into a MemoryStream that is deliberately NOT
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
