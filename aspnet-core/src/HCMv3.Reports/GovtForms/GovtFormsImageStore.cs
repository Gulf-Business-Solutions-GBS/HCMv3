using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace HCMv3.Reports.GovtForms
{
    /// <summary>
    /// Loads the full-page scanned government form that each GovtForms report prints behind its
    /// labels (<c>xrPictureBox1</c>, and a second page image on the two-page Shoun forms).
    ///
    /// WHY THIS EXISTS. In the legacy reports the scan lived in the report's <c>.resx</c> and the
    /// designer file pulled it out with
    /// <c>(System.Drawing.Image)resources.GetObject("xrPictureBox1.Image")</c>. That does not carry
    /// over: HCMv3.Reports multi-targets net8.0 as well as net8.0-windows, and a .resx entry typed
    /// <c>System.Drawing.Bitmap</c> forces the resource compiler to bind a Windows-only type on both
    /// flavours. The bytes ship instead as embedded JPEGs under <c>GovtForms/Assets</c>, extracted
    /// verbatim from the legacy .resx (the legacy entries used the <c>bytearray.base64</c> mimetype,
    /// i.e. the raw JPEG file, not a serialised object), and are attached in each report ctor.
    ///
    /// FIDELITY: the bytes are byte-identical to the legacy resource. These are photographs of
    /// official paper forms whose field positions the labels are aligned against, so they are NOT
    /// recompressed or resized -- rptPayRequestShounRenewalEn's two pages alone are ~10.7 MB.
    ///
    /// SIDE EFFECT: the Visual Studio report designer shows an empty picture box rather than the
    /// form, because the image is no longer assigned inside InitializeComponent. The rendered
    /// document is unaffected.
    /// </summary>
    internal static class GovtFormsImageStore
    {
        private const string ResourcePrefix = "HCMv3.Reports.GovtForms.Assets.";

        /// <summary>
        /// Returns the embedded scan for <paramref name="reportName"/>'s picture box, or
        /// <c>null</c> if it cannot be loaded. Never throws: a missing background is a cosmetic
        /// defect and must not fail the document.
        /// </summary>
        /// <param name="reportName">e.g. <c>rptAbsenceReportCancellation</c>.</param>
        /// <param name="pictureBoxName">e.g. <c>xrPictureBox1</c>.</param>
        internal static Image Load(string reportName, string pictureBoxName)
        {
            return LoadByResourceName(ResourcePrefix + reportName + "." + pictureBoxName + ".jpg");
        }

        internal static Image LoadByResourceName(string resourceName)
        {
            try
            {
                var assembly = typeof(GovtFormsImageStore).GetTypeInfo().Assembly;
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
