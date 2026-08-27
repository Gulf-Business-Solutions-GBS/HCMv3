using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// Images printed by the Others/HR-forms report batch: the three full-page scanned form
    /// backgrounds (clearance form, probationary report form, disciplinary form) and the single
    /// stamp mark the disciplinary form overlays on whichever penalty box applies.
    ///
    /// WHY THIS EXISTS. In the legacy reports these lived in each report's <c>.resx</c> and the
    /// designer file pulled them out with
    /// <c>(System.Drawing.Image)resources.GetObject("xrPictureBox1.Image")</c>. That does not carry
    /// over: HCMv3.Reports multi-targets net8.0 as well as net8.0-windows, and a .resx entry typed
    /// <c>System.Drawing.Bitmap</c> forces the resource compiler to bind a Windows-only type on both
    /// flavours. The bytes ship instead as embedded images extracted verbatim from the legacy .resx
    /// (the legacy entries used the <c>bytearray.base64</c> mimetype, i.e. the raw JPEG/PNG file and
    /// not a serialised object) and are attached in each report's constructor. Same pattern as
    /// <c>GovtForms/GovtFormsImageStore.cs</c> and <c>Others/SalaryCertificateBackground.cs</c>.
    ///
    /// FIDELITY: byte-identical to the legacy resources, deliberately not recompressed or resized --
    /// these are photographs of paper forms whose field positions the report labels are aligned
    /// against.
    ///
    /// DEDUPLICATION: rptDisciplinaryForm's .resx held EIGHT picture-box images (pbDED_04, pbDED_08,
    /// pbDED_16, pbDED_24, pbDED_Sep, pbDED_Warn, pblast, pbLast2) that are all the same 7,921-byte
    /// PNG -- verified by hash. One copy ships as <see cref="DisciplinaryMarkResourceName"/> and all
    /// eight boxes load it.
    ///
    /// SIDE EFFECT: the Visual Studio report designer shows empty picture boxes rather than the
    /// forms, because the images are no longer assigned inside InitializeComponent. The rendered
    /// document is unaffected.
    /// </summary>
    internal static class OthersHrFormsImageStore
    {
        /// <summary>rptClearanceForm :: xrPictureBox1 -- the full-page clearance form scan.</summary>
        internal const string ClearanceFormBackgroundResourceName =
            "HCMv3.Reports.Others.OthersHrFormsClearanceFormBackground.jpg";

        /// <summary>rptProbationaryReportForm :: xrPictureBox1 -- the full-page probationary form scan.</summary>
        internal const string ProbationaryFormBackgroundResourceName =
            "HCMv3.Reports.Others.OthersHrFormsProbationaryFormBackground.jpg";

        /// <summary>rptDisciplinaryForm :: xrPictureBox1 -- the full-page (Arabic) disciplinary form scan.</summary>
        internal const string DisciplinaryFormBackgroundResourceName =
            "HCMv3.Reports.Others.OthersHrFormsDisciplinaryFormBackground.jpg";

        /// <summary>
        /// rptDisciplinaryForm :: the stamp mark shared by all eight penalty/occurrence picture
        /// boxes. See the deduplication note on this class.
        /// </summary>
        internal const string DisciplinaryMarkResourceName =
            "HCMv3.Reports.Others.OthersHrFormsDisciplinaryMark.png";

        /// <summary>rptFinalClearance_EOS :: xrPictureBox2 -- the letterhead logo.</summary>
        internal const string FinalClearanceEosLogoResourceName =
            "HCMv3.Reports.Others.OthersHrFormsFinalClearanceEosLogo.png";

        /// <summary>
        /// Returns the embedded image, or <c>null</c> if it cannot be loaded. Never throws: a
        /// missing background is a cosmetic defect and must not fail the document.
        /// </summary>
        internal static Image Load(string resourceName)
        {
            try
            {
                var assembly = typeof(OthersHrFormsImageStore).GetTypeInfo().Assembly;
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
