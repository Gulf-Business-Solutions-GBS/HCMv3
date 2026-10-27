using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Others.rptEmploymentContractAr023</c>.
    ///
    /// The Arabic employment contract. Despite the "023" suffix this is a BASE report in the legacy
    /// tree, not a <c>_CustomNNN</c> customer variant, so it is ported alongside
    /// <see cref="rptEmploymentContract"/> and <see cref="rptEmploymentContractEn023"/>. The three
    /// share <see cref="OthersHrFormsSql.EmploymentContractWageQuery"/>.
    ///
    /// XAF CONSTRUCTS REPLACED:
    ///
    /// | Legacy                                                        | Replacement |
    /// |---------------------------------------------------------------|-------------|
    /// | <c>MyXtraReport</c> base                                      | <see cref="HcmReportBase"/>. |
    /// | <c>SqlDataSource.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL in the report <c>.resx</c>                                | <see cref="OthersHrFormsSql.EmploymentContractAr023Query"/> and <see cref="OthersHrFormsSql.EmploymentContractWageQuery"/>. |
    /// | four <c>xrLabelNN.Text</c> entries and <c>xrLabel31.ExpressionBindings</c> in the <c>.resx</c> | inlined verbatim as string literals in the designer file. |
    /// | <c>SecuritySystem.CurrentUserName</c> stamped onto <c>lblUserlogin</c> / <c>xrLabel35</c> in the ctor | the <see cref="HcmReportBase.LoggedUserNameParameter"/> report parameter, applied by the caller through <see cref="HcmReportBase.ApplyLoggedUserName"/> and stamped in <see cref="Detail_BeforePrint"/>. |
    /// | <c>new XPObjectSpaceProvider(...)</c> + <c>GetObjectsQuery&lt;Employee&gt;()</c> inside <c>BeforePrint</c>, to read the wage-type '1000' amount of the basic-pay header covering PrmFormDate | removed; the same lookup is now an OUTER APPLY projecting <c>Current_Salary</c>. See the query summary. |
    /// | <c>MyXtraReport.AmountInArabicWords</c> (+ <c>ProcessArabicGroup</c>, <c>GetDigitFeminineStatus</c> and eleven lookup tables) | ported into this file as private members. <see cref="HcmReportBase"/> deliberately did not carry them over -- its own header says "port them with the first Arabic report that needs them", and this is that report. They live here rather than in the shared base because the base class is off-limits to this batch; the first other Arabic report to need them should promote this region to <see cref="HcmReportBase"/> verbatim. |
    /// | report-level <c>BeforePrint</c>                               | moved to <see cref="Detail_BeforePrint"/>. Report-level BeforePrint fires once, before any data row is current. |
    /// | <c>PrintEventHandler(PrintEventArgs)</c>                      | <c>BeforePrintEventHandler(CancelEventArgs)</c> (v17.2 -> v23.2). |
    /// | <c>rptEmploymentContractAr023(object[] args)</c> ctor          | dropped; it did nothing but <c>InitializeComponent()</c> plus the PrmFormDate assignment. |
    ///
    /// MULTI-TENANCY: both queries carry <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptEmploymentContractAr023 : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "EmploymentContractAr023";

        public rptEmploymentContractAr023()
        {
            InitializeComponent();

            PrmFormDate.Value = DateTime.Now;
            PrmEmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. "true" also adds PrmLoggedUserName, which the two footer stamps need.
            EnsureHcmParameters(withLoggedUserName: true);
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptEmploymentContractAr023(string employeeCode, DateTime formDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmFormDate.Value = formDate;
        }

        /// <summary>
        /// Composes the Arabic basic-salary-in-words sentence and the two "printed by" footer stamps.
        ///
        /// Legacy, verbatim:
        /// <code>
        /// var basicSalaryInWordsAr = "(فقط " + AmountInArabicWords((decimal)amount) + " ).";
        /// lblBasicSalaryInWords.Text = Regex.Replace(basicSalaryInWordsAr, @"\s+", " ");
        /// </code>
        /// including the whitespace collapse, which exists because
        /// <see cref="AmountInArabicWords"/> emits stray spaces (see its own remarks).
        /// </summary>
        private void Detail_BeforePrint(object sender, CancelEventArgs e)
        {
            lblBasicSalaryInWords.Text = string.Empty;

            if (TryGetColumn("Current_Salary", out var currentSalary))
            {
                var amount = Convert.ToDecimal(currentSalary);
                var basicSalaryInWordsAr = "(فقط " + AmountInArabicWords(amount) + " ).";
                lblBasicSalaryInWords.Text = Regex.Replace(basicSalaryInWordsAr, @"\s+", " ");
            }

            // Legacy: CurrentUserName + " " + DateTime.Now.ToString("dd/MM/yyyy hh:mm tt").
            var stamp = Convert.ToString(Parameters[LoggedUserNameParameter].Value)
                        + " " + DateTime.Now.ToString("dd/MM/yyyy hh:mm tt");

            lblUserlogin.Text = stamp;
            xrLabel35.Text = stamp;
        }

        /// <summary>
        /// Reads a column of the row currently being printed, tolerating a missing column or a
        /// DBNull. NOTE: GetCurrentColumnValue returns <c>DBNull.Value</c>, never null.
        /// </summary>
        private bool TryGetColumn(string columnName, out object value)
        {
            value = null;

            try
            {
                value = GetCurrentColumnValue(columnName);
            }
            catch (Exception)
            {
                return false;
            }

            return value != null && value != DBNull.Value;
        }

        #region Arabic number-to-words, ported from MyXtraReport

        // ---------------------------------------------------------------------------------------
        // Ported from HCM10.Module.Reports.MyXtraReport (the "<< Arabic Number To Word >>" region),
        // which HcmReportBase deliberately left out until an Arabic report needed it.
        //
        // TWO LEGACY DEPENDENCIES DROPPED, both provably inert:
        //
        //  1. CurrencyInfo. The legacy GetDigitFeminineStatus built a
        //     List<CurrencyInfo> containing exactly one entry -- CurrencyInfo.Currencies.Kuwait --
        //     and assigned it to the Currency property on EVERY call. For Kuwait,
        //     NumToWordService.cs sets IsCurrencyNameFeminine = false and
        //     IsCurrencyPartNameFeminine = false, so both branches of that method returned
        //     arabicOnes[digit] and the arabicFeminineOnes table was never reachable. The table and
        //     the CurrencyInfo dependency are therefore dropped and the method collapses to an
        //     array index. If a non-Kuwaiti currency is ever needed, restore both.
        //
        //  2. The currency-name and decimal-part suffix blocks at the end of AmountInArabicWords.
        //     *** LEGACY BUG, REPRODUCED DELIBERATELY. *** Both blocks are guarded by the FIELDS
        //     _intergerValue and _decimalValue. _intergerValue was never assigned anywhere in the
        //     legacy class, and AmountInArabicWords shadowed _decimalValue with a LOCAL of the same
        //     name -- so both fields were always 0, both blocks were dead, and the printed sentence
        //     never carried a دينار/فلس agreement suffix nor the fractional part. The output is
        //     "<words> دينار كويتي" plus the stray leading/trailing spaces that the caller's
        //     Regex.Replace(@"\s+", " ") then collapses. Reproducing the bug keeps the printed
        //     contract identical to the one the customer has been signing; fixing it is a product
        //     decision, and is recorded under "Defects carried over" in the porting notes.
        //
        //     _intergerValue is kept as a const 0 below rather than deleted, because
        //     ProcessArabicGroup genuinely reads it in its "2000 / 2000000 / ..." special case and
        //     the reader needs to see why that branch is unreachable.
        // ---------------------------------------------------------------------------------------

        /// <summary>Always 0 in the legacy class -- see the region remarks. Kept so the dead branch in
        /// <see cref="ProcessArabicGroup"/> reads the same way it did.</summary>
        private const long LegacyIntegerValue = 0;

        private static readonly string[] ArabicOnes =
        {
            string.Empty, "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة",
            "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر",
            "ثمانية عشر", "تسعة عشر"
        };

        private static readonly string[] ArabicTens =
        {
            "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون"
        };

        private static readonly string[] ArabicHundreds =
        {
            "", "مائة", "مئتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة"
        };

        private static readonly string[] ArabicAppendedTwos =
        {
            "مئتا", "ألفا", "مليونا", "مليارا", "تريليونا", "كوادريليونا", "كوينتليونا", "سكستيليونا"
        };

        private static readonly string[] ArabicTwos =
        {
            "مئتان", "ألفان", "مليونان", "ملياران", "تريليونان", "كوادريليونان", "كوينتليونان", "سكستيليونان"
        };

        private static readonly string[] ArabicGroup =
        {
            "مائة", "ألف", "مليون", "مليار", "تريليون", "كوادريليون", "كوينتليون", "سكستيليون"
        };

        private static readonly string[] ArabicAppendedGroup =
        {
            "", "ألفاً", "مليوناً", "ملياراً", "تريليوناً", "كوادريليوناً", "كوينتليوناً", "سكستيليوناً"
        };

        private static readonly string[] ArabicPluralGroups =
        {
            "", "آلاف", "ملايين", "مليارات", "تريليونات", "كوادريليونات", "كوينتليونات", "سكستيليونات"
        };

        /// <summary>
        /// Legacy <c>GetDigitFeminineStatus</c>, collapsed: for the hard-coded Kuwaiti currency both
        /// feminine flags are false, so every branch returned <see cref="ArabicOnes"/>. See the
        /// region remarks.
        /// </summary>
        private static string GetDigitFeminineStatus(int digit)
        {
            return ArabicOnes[digit];
        }

        /// <summary>Legacy <c>ProcessArabicGroup</c>: renders one group of three digits.</summary>
        private static string ProcessArabicGroup(int groupNumber, int groupLevel, decimal remainingNumber)
        {
            var tens = groupNumber % 100;
            var hundreds = groupNumber / 100;

            var retVal = string.Empty;

            if (hundreds > 0)
            {
                retVal = tens == 0 && hundreds == 2
                    ? ArabicAppendedTwos[0]   // المضاف
                    : ArabicHundreds[hundreds];
            }

            if (tens > 0)
            {
                if (tens < 20)
                {
                    if (tens == 2 && hundreds == 0 && groupLevel > 0)
                    {
                        // Special case for a bare 2 in the group. The LegacyIntegerValue comparison
                        // is always false -- see the region remarks -- so ArabicTwos always wins.
                        retVal = LegacyIntegerValue == 2000
                                 || LegacyIntegerValue == 2000000
                                 || LegacyIntegerValue == 2000000000
                                 || LegacyIntegerValue == 2000000000000
                                 || LegacyIntegerValue == 2000000000000000
                                 || LegacyIntegerValue == 2000000000000000000
                            ? ArabicAppendedTwos[groupLevel]
                            : ArabicTwos[groupLevel];
                    }
                    else
                    {
                        if (retVal != string.Empty)
                        {
                            retVal += " و ";
                        }

                        if (tens == 1 && groupLevel > 0 && hundreds == 0)
                        {
                            retVal += " ";
                        }
                        else if ((tens == 1 || tens == 2)
                                 && (groupLevel == 0 || groupLevel == -1)
                                 && hundreds == 0
                                 && remainingNumber == 0)
                        {
                            retVal += string.Empty;
                        }
                        else
                        {
                            retVal += GetDigitFeminineStatus(tens);
                        }
                    }
                }
                else
                {
                    var ones = tens % 10;
                    tens = (tens / 10) - 2;   // 20's offset

                    if (ones > 0)
                    {
                        if (retVal != string.Empty)
                        {
                            retVal += " و ";
                        }

                        retVal += GetDigitFeminineStatus(ones);
                    }

                    if (retVal != string.Empty)
                    {
                        retVal += " و ";
                    }

                    retVal += ArabicTens[tens];
                }
            }

            return retVal;
        }

        /// <summary>
        /// Legacy <c>AmountInArabicWords</c>. Returns the number in Arabic words followed by
        /// "دينار كويتي", with the leading and trailing spaces the legacy method emitted (its
        /// prefix/suffix text properties were never set, and <c>null != string.Empty</c> is true, so
        /// both <c>String.Format</c> calls ran and contributed a bare space).
        ///
        /// The fractional part and the grammatical currency-agreement suffix are NOT emitted --
        /// see the region remarks for why that is faithful rather than an omission.
        /// </summary>
        private static string AmountInArabicWords(decimal number)
        {
            var tempNumber = number;

            if (tempNumber == 0)
            {
                return "صفر";
            }

            // Legacy computes this and feeds it to ProcessArabicGroup, then discards the result
            // because the guard below reads the always-zero FIELD of the same name. Kept so the
            // call sequence -- and its cost -- is identical.
            var decimalValue = (int)(tempNumber % 1 * 1000);
            ProcessArabicGroup(decimalValue, -1, 0);

            var retVal = string.Empty;
            byte group = 0;

            while (tempNumber >= 1)
            {
                var numberToProcess = (int)(tempNumber % 1000);
                tempNumber = tempNumber / 1000;

                var groupDescription = ProcessArabicGroup(numberToProcess, group, Math.Floor(tempNumber));

                if (groupDescription != string.Empty)
                {
                    if (group > 0)
                    {
                        if (retVal != string.Empty)
                        {
                            retVal = string.Format("{0} {1}", "و", retVal);
                        }

                        if (numberToProcess != 2)
                        {
                            if (numberToProcess % 100 != 1)
                            {
                                if (numberToProcess >= 3 && numberToProcess <= 10)
                                {
                                    // 3..9 take the plural group name
                                    retVal = string.Format("{0} {1}", ArabicPluralGroups[group], retVal);
                                }
                                else if (retVal != string.Empty)
                                {
                                    retVal = string.Format("{0} {1}", ArabicAppendedGroup[group], retVal);
                                }
                                else
                                {
                                    retVal = string.Format("{0} {1}", ArabicGroup[group], retVal);
                                }
                            }
                            else
                            {
                                retVal = string.Format("{0} {1}", ArabicGroup[group], retVal);
                            }
                        }
                    }

                    retVal = string.Format("{0} {1}", groupDescription, retVal);
                }

                group++;
            }

            // Legacy: ArabicPrefixText / ArabicSuffixText were never assigned, and the guard was
            // "!= String.Empty", which is TRUE for null -- so both contributed a bare space.
            var formattedNumber = " ";
            formattedNumber += retVal;
            formattedNumber += "دينار كويتي ";
            formattedNumber += " ";

            return formattedNumber;
        }

        #endregion
    }
}
