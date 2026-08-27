using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// Loads the full-page A4 letterhead bitmap that both salary certificates print behind their
    /// text (<c>xrPictureBox1</c>).
    ///
    /// WHY THIS EXISTS. In the legacy reports the bitmap lived in the report's <c>.resx</c> and the
    /// designer file pulled it out with
    /// <c>(System.Drawing.Image)resources.GetObject("xrPictureBox1.Image")</c>. That was not carried
    /// over: HCMv3.Reports multi-targets net8.0 as well as net8.0-windows, and a .resx entry typed
    /// <c>System.Drawing.Bitmap</c> forces the resource compiler to bind a Windows-only type on both
    /// flavours. The bytes are instead shipped verbatim as an embedded JPEG (byte-identical to what
    /// was base64-encoded in the legacy .resx -- the legacy entry used the
    /// <c>bytearray.base64</c> mimetype, i.e. the raw file, not a serialised object) and attached to
    /// the picture box in the report constructor.
    ///
    /// SIDE EFFECT: the Visual Studio report designer shows an empty picture box rather than the
    /// letterhead, because the image is no longer assigned inside InitializeComponent. The rendered
    /// document is unaffected.
    /// </summary>
    internal static class SalaryCertificateBackground
    {
        internal const string EnglishResourceName = "HCMv3.Reports.Others.SalaryCertificateEnBackground.jpg";
        internal const string ArabicResourceName = "HCMv3.Reports.Others.SalaryCertificateArBackground.jpg";

        /// <summary>
        /// Returns the embedded letterhead, or <c>null</c> if it cannot be loaded. Never throws: a
        /// missing background is a cosmetic defect, not a reason to fail a payroll document.
        /// </summary>
        internal static Image Load(string resourceName)
        {
            try
            {
                var assembly = typeof(SalaryCertificateBackground).GetTypeInfo().Assembly;
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
