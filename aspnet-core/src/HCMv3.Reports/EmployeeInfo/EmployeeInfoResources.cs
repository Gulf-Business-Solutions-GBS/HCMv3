using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// Everything the legacy EmployeeInfo reports kept in their <c>.resx</c> other than SQL.
    ///
    /// WHY THIS EXISTS. HCMv3.Reports multi-targets net8.0 as well as net8.0-windows, and a .resx
    /// entry typed <c>System.Drawing.Bitmap</c> forces the resource compiler to bind a
    /// Windows-only type on BOTH flavours, so the .resx files are not carried over at all (the
    /// same decision <c>Others/SalaryCertificateBackground.cs</c> took for the salary
    /// certificates). Long static strings become constants here; bitmaps ship as embedded JPEGs.
    ///
    /// SIDE EFFECT of dropping the .resx: the Visual Studio report designer shows an empty picture
    /// box instead of the letterhead, because the image is no longer assigned inside
    /// InitializeComponent. The rendered document is unaffected.
    /// </summary>
    internal static class EmployeeInfoResources
    {
        internal const string StatusChangeLetterhead = "HCMv3.Reports.EmployeeInfo.StatusChangeLetterhead.jpg";
        internal const string FinalClearanceLetterhead = "HCMv3.Reports.EmployeeInfo.FinalClearanceLetterhead.jpg";
        internal const string JoiningFormLetterhead = "HCMv3.Reports.EmployeeInfo.JoiningFormLetterhead.jpg";
        internal const string TerminationLetterhead = "HCMv3.Reports.EmployeeInfo.TerminationLetterhead.jpg";

        /// <summary>
        /// Returns an embedded letterhead, or <c>null</c> if it cannot be loaded. Never throws: a
        /// missing background is a cosmetic defect, not a reason to fail an HR document.
        /// </summary>
        internal static Image LoadImage(string resourceName)
        {
            try
            {
                var assembly = typeof(EmployeeInfoResources).GetTypeInfo().Assembly;
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

        #region rptJobOffer / rptProbation_Letter static text

        /// <summary>
        /// rptProbation_Letter xrLabel7 expression (English body paragraph).
        /// </summary>
        internal const string ProbationBodyEnExpression =
            "'We hope this letter finds you well. This letter is to officially confirm the completion of your " +
            "probationary period at ' + [CompanyEn] + '. We are pleased to inform you that, based on your " +
            "performance and dedication, you have successfully completed the probationary period and are now " +
            "a regular employee of the company.'";

        /// <summary>
        /// Arabic counterpart of <see cref="ProbationBodyEnExpression"/>. NOTE this same Arabic
        /// PROBATION paragraph is bound to rptJobOffer's xrLabel1 in the legacy layout -- a
        /// copy-paste artefact in the legacy report, where a job OFFER prints probation-completion
        /// text in Arabic. Ported verbatim rather than silently corrected; flag it to the product
        /// owner.
        /// </summary>
        internal const string ProbationBodyArExpression =
            "'نتمنى أن يجدك هذا الخطاب " +
            "وأنت بخير. يهدف هذا الخطاب " +
            "إلى تأكيد انتهاء فترة الاختبار " +
            "الخاصة بك في ' + [CompanyAr] + '. يسرنا أن " +
            "نُبلغك بأنه استنادًا إلى " +
            "أدائك والتفاني الذي أظهرته، " +
            "قد اجتزت فترة الاختبار بنجاح " +
            "وأصبحت الآن موظفاً دائماً " +
            "في الشركة.'";

        /// <summary>
        /// rptJobOffer xrLabel7 expression. *** CUSTOMER-SPECIFIC TEXT BAKED INTO THE BASE REPORT. ***
        /// This is a literal string with no field references: it hard-codes both the job title
        /// ("Senior System Analyst, IS") and the employing company name of whoever last exported
        /// the legacy .repx. It is ported verbatim so the layout is a faithful copy, but it should
        /// be replaced with an expression over [JobtitleEn] / [CompanyEn] before this report is
        /// shown to any tenant.
        /// </summary>
        internal const string JobOfferBodyEnExpression =
            "'We are pleased to offer you \"Senior System Analyst, IS\" position with Al-Sayer International " +
            "Group For Gen. Trading & Contracting Co. WILL at State of Kuwait under the following terms and conditions:'";

        /// <summary>rptJobOffer xrLabel21 static text (clause 8 of the offer terms).</summary>
        internal const string JobOfferClause8 =
            "8.  The employee may not engage in or undertake any work of any nature whatsoever directly or " +
            "indirectly with any third party with or without pay. without the prior written and express " +
            "consent of the Employer.";

        /// <summary>rptProbation_Letter xrLabel11 static text.</summary>
        internal const string ProbationClosingParagraph =
            "We are confident that you will continue to excel in your role and contribute to the growth and " +
            "success of the company. As a valued member of our team, your dedication and hard work are " +
            "integral to achieving our shared goals.";

        /// <summary>rptProbation_Letter xrLabel12 static text.</summary>
        internal const string ProbationContactParagraph =
            "Please do not hesitate to reach out to your immediate supervisor or the HR department if you have " +
            "any questions regarding your employment status or any other matters related to your role within " +
            "the company.";

        #endregion

        #region rptSettlementAcknowledgement_DischargeForm rich text

        // XRRichText.SerializableRtfString is base64-encoded UTF-16 RTF. Carried over byte-for-byte
        // rather than re-authored, so the rendered block is identical to the legacy report.

        /// <summary>xrRichText1: the "Signature: : ______" block.</summary>
        internal const string SignatureRtf =
            "ewBcAHIAdABmADEAXABhAG4AcwBpAGMAcABnADEAMgA1ADIADQAKAHsADQAKAFwAZgBvAG4AdAB0AGIAbAANAAoAewBcAGYAMAAgAFQAaQBtAGUAcwAgAE4AZQB3ACAAUgBvAG0AYQBuADsAfQANAAoAewBcAGYAMQAgAFQAYQBoAG8AbQBhADsAfQANAAoAfQANAAoAewANAAoAXABjAG8AbABvAHIAdABiAGwADQAKADsADQAKAFwAcgBlAGQAMABcAGcAcgBlAGUAbgAwAFwAYgBsAHUAZQAyADUANQA7AA0ACgBcAHIAZQBkADAAXABnAHIAZQBlAG4AMABcAGIAbAB1AGUAMAA7AA0ACgB9AA0ACgBcAG4AbwB1AGkAYwBvAG0AcABhAHQAXABzAHAAbAB5AHQAdwBuAGkAbgBlAFwAaAB0AG0AYQB1AHQAcwBwAHsAXABwAGEAcgBkAFwAcABsAGEAaQBuAFwAcQBsAHsAXABiAFwAZgAxAFwAZgBzADIANABcAGMAZgAyACAAUwBpAGcAbgBhAHQAdQByAGUAOgAgADoAIABfAF8AXwBfAF8AXwBfAF8AXwBfAF8AXwBfAF8AXwBfAF8AfQBcAGIAXABmADEAXABmAHMAMgA0AFwAcABhAHIAXABwAGEAcgBkAFwAcABsAGEAaQBuAFwAcQBsAFwAYgBcAGYAMQBcAGYAcwAyADQAXABwAGEAcgBcAHAAYQByAGQAXABwAGwAYQBpAG4AXABxAGwAXABiAFwAZgAxAFwAZgBzADIANABcAHAAYQByAFwAcABhAHIAZABcAHAAbABhAGkAbgBcAHEAbABcAGYAcwAyADQAXABwAGEAcgB9AA0ACgB9AA0ACgA=";

        /// <summary>
        /// xrRichText2. Contains the literal placeholder text "xrRichText2" -- a designer leftover
        /// in the legacy report, not real content. Kept verbatim so the layout is unchanged;
        /// worth deleting once the product owner confirms it was never meant to print.
        /// </summary>
        internal const string Placeholder2Rtf =
            "ewBcAHIAdABmADEAXABhAG4AcwBpAGMAcABnADEAMgA1ADIADQAKAHsADQAKAFwAZgBvAG4AdAB0AGIAbAANAAoAewBcAGYAMAAgAFQAaQBtAGUAcwAgAE4AZQB3ACAAUgBvAG0AYQBuADsAfQANAAoAewBcAGYAMQAgAFQAYQBoAG8AbQBhADsAfQANAAoAfQANAAoAewANAAoAXABjAG8AbABvAHIAdABiAGwADQAKADsADQAKAFwAcgBlAGQAMABcAGcAcgBlAGUAbgAwAFwAYgBsAHUAZQAyADUANQA7AA0ACgBcAHIAZQBkADAAXABnAHIAZQBlAG4AMABcAGIAbAB1AGUAMAA7AA0ACgB9AA0ACgBcAG4AbwB1AGkAYwBvAG0AcABhAHQAXABzAHAAbAB5AHQAdwBuAGkAbgBlAFwAaAB0AG0AYQB1AHQAcwBwAHsAXABwAGEAcgBkAFwAcABsAGEAaQBuAFwAcQBsAHsAXABmADEAXABmAHMAMgAwAFwAYwBmADIAIAB4AHIAUgBpAGMAaABUAGUAeAB0ADIAfQBcAGYAcwAyADQAXABwAGEAcgB9AA0ACgB9AA0ACgA=";

        /// <summary>xrRichText3. Same designer-leftover placeholder as <see cref="Placeholder2Rtf"/>.</summary>
        internal const string Placeholder3Rtf =
            "ewBcAHIAdABmADEAXABhAG4AcwBpAGMAcABnADEAMgA1ADIADQAKAHsADQAKAFwAZgBvAG4AdAB0AGIAbAANAAoAewBcAGYAMAAgAFQAaQBtAGUAcwAgAE4AZQB3ACAAUgBvAG0AYQBuADsAfQANAAoAewBcAGYAMQAgAFQAYQBoAG8AbQBhADsAfQANAAoAfQANAAoAewANAAoAXABjAG8AbABvAHIAdABiAGwADQAKADsADQAKAFwAcgBlAGQAMABcAGcAcgBlAGUAbgAwAFwAYgBsAHUAZQAyADUANQA7AA0ACgBcAHIAZQBkADAAXABnAHIAZQBlAG4AMABcAGIAbAB1AGUAMAA7AA0ACgB9AA0ACgBcAG4AbwB1AGkAYwBvAG0AcABhAHQAXABzAHAAbAB5AHQAdwBuAGkAbgBlAFwAaAB0AG0AYQB1AHQAcwBwAHsAXABwAGEAcgBkAFwAcABsAGEAaQBuAFwAcQBsAHsAXABmADEAXABmAHMAMgAwAFwAYwBmADIAIAB4AHIAUgBpAGMAaABUAGUAeAB0ADMAfQBcAGYAcwAyADQAXABwAGEAcgB9AA0ACgB9AA0ACgA=";

        #endregion
    }
}
