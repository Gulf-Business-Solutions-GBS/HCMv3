using System;

namespace HCMv3.Reports
{
    /// <summary>
    /// Arabic amount-in-words, ported VERBATIM from the legacy
    /// <c>HCM10.Module.Reports.MyXtraReport.AmountInArabicWords</c> / <c>ProcessArabicGroup</c> /
    /// <c>GetDigitFeminineStatus</c> and the arabicOnes/arabicTens/... lookup tables that sat beside
    /// them, plus the Kuwait branch of <c>HCM10.Module.BusinessObjects.Services.CurrencyInfo</c>.
    ///
    /// The payslip port deliberately skipped this ("port them with the first Arabic report that
    /// needs them" -- HcmReportBase class remarks). rptBankFile is that report: it prints the
    /// transferred total in Arabic words in the covering paragraph of a letter to the bank.
    ///
    /// LIVES IN Bank/ ONLY BECAUSE Bank/ IS THE FIRST CONSUMER. The namespace is HCMv3.Reports (not
    /// .Bank) so the next Arabic report can use it without a Bank reference; move the file to the
    /// project root when a second folder starts calling it.
    ///
    /// ==== TWO LEGACY DEFECTS REPRODUCED ON PURPOSE ====================================================
    /// This text is printed on a bank transfer instruction. Its exact wording is what the bank has
    /// been receiving for years, so it is ported bug-for-bug rather than "fixed".
    ///
    /// 1. <c>_intergerValue</c> is a FIELD that <c>AmountInArabicWords</c> never assigns -- it is only
    ///    ever read. It is therefore always 0, so the whole "decline the currency name by the last two
    ///    digits" block (Arabic1CurrencyName / Arabic2CurrencyName / Arabic310CurrencyName /
    ///    Arabic1199CurrencyName) is DEAD CODE and the currency name is the hard-coded literal
    ///    "دينار كويتي " appended unconditionally just above it. Ported as the same unconditional
    ///    literal, with the dead branch kept as a comment so the intent is not lost.
    ///    Its other reader, the "special case for the number 2" test inside ProcessArabicGroup, is
    ///    likewise always false, so the appended-form arabicAppendedTwos[groupLevel] never fires.
    ///
    /// 2. <c>ArabicPrefixText</c> / <c>ArabicSuffixText</c> are auto-properties that nothing sets, so
    ///    they are null -- and <c>null != String.Empty</c> is TRUE, so both branches run and
    ///    <c>String.Format("{0} ", null)</c> contributes a bare space. The returned string therefore
    ///    has a leading space and a trailing space. Preserved.
    /// ==================================================================================================
    /// </summary>
    public static class ArabicAmountInWords
    {
        #region Kuwait branch of the legacy CurrencyInfo

        private const bool IsCurrencyNameFeminine = false;
        private const bool IsCurrencyPartNameFeminine = false;

        private const string Arabic1CurrencyPartName = "فلس";
        private const string Arabic2CurrencyPartName = "فلسان";
        private const string Arabic310CurrencyPartName = "فلوس";
        private const string Arabic1199CurrencyPartName = "فلساً";

        // Kept for the record; unreachable, see defect 1.
        // private const string Arabic1CurrencyName    = "دينار كويتي";
        // private const string Arabic2CurrencyName    = "دينار كويتيان";
        // private const string Arabic310CurrencyName  = "دينار كويتية";
        // private const string Arabic1199CurrencyName = "دينار كويتياً";

        #endregion

        #region Lookup tables, verbatim from MyXtraReport

        private static readonly string[] ArabicOnes =
        {
            string.Empty, "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة",
            "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر",
            "ثمانية عشر", "تسعة عشر"
        };

        private static readonly string[] ArabicFeminineOnes =
        {
            string.Empty, "إحدى", "اثنتان", "ثلاث", "أربع", "خمس", "ست", "سبع", "ثمان", "تسع",
            "عشر", "إحدى عشرة", "اثنتا عشرة", "ثلاث عشرة", "أربع عشرة", "خمس عشرة", "ست عشرة",
            "سبع عشرة", "ثماني عشرة", "تسع عشرة"
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

        #endregion

        /// <summary>
        /// Legacy <c>MyXtraReport.AmountInArabicWords(decimal)</c>. Returns "صفر" for zero; otherwise a
        /// space-padded Arabic phrase (see defect 2).
        /// </summary>
        public static string Convert(decimal number)
        {
            var tempNumber = number;

            if (tempNumber == 0)
            {
                return "صفر";
            }

            // KWD has three decimal places, hence *1000 -- fils.
            var decimalValue = (int)(tempNumber % 1 * 1000);

            var decimalString = ProcessArabicGroup(decimalValue, -1, 0);

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
                                    // 3..9 take the plural form
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

            var formattedNumber = string.Empty;

            // Legacy: ArabicPrefixText is null and "null != String.Empty" is true -> a bare space. See defect 2.
            formattedNumber += " ";
            formattedNumber += retVal != string.Empty ? retVal : string.Empty;

            // Legacy appended this literal unconditionally, then guarded the declined forms behind
            // "if (_intergerValue != 0)" -- which is never true. See defect 1.
            formattedNumber += "دينار كويتي ";

            formattedNumber += decimalValue != 0 ? " و " : string.Empty;
            formattedNumber += decimalValue != 0 ? decimalString : string.Empty;

            if (decimalValue != 0)
            {
                formattedNumber += " ";

                var remaining100 = decimalValue % 100;

                if (remaining100 == 0)
                {
                    formattedNumber += Arabic1CurrencyPartName;
                }
                else if (remaining100 == 1)
                {
                    formattedNumber += Arabic1CurrencyPartName;
                }
                else if (remaining100 == 2)
                {
                    formattedNumber += Arabic2CurrencyPartName;
                }
                else if (remaining100 >= 3 && remaining100 <= 10)
                {
                    formattedNumber += Arabic310CurrencyPartName;
                }
                else if (remaining100 >= 11 && remaining100 <= 99)
                {
                    formattedNumber += Arabic1199CurrencyPartName;
                }
            }

            // Legacy: ArabicSuffixText is null -> another bare space. See defect 2.
            formattedNumber += " ";

            return formattedNumber;
        }

        /// <summary>Legacy <c>MyXtraReport.GetDigitFeminineStatus</c>.</summary>
        private static string GetDigitFeminineStatus(int digit, int groupLevel)
        {
            if (groupLevel == -1)
            {
                // decimal part
                return IsCurrencyPartNameFeminine ? ArabicFeminineOnes[digit] : ArabicOnes[digit];
            }

            if (groupLevel == 0)
            {
                return IsCurrencyNameFeminine ? ArabicFeminineOnes[digit] : ArabicOnes[digit];
            }

            return ArabicOnes[digit];
        }

        /// <summary>Legacy <c>MyXtraReport.ProcessArabicGroup</c> -- one group of three digits.</summary>
        private static string ProcessArabicGroup(int groupNumber, int groupLevel, decimal remainingNumber)
        {
            var tens = groupNumber % 100;
            var hundreds = groupNumber / 100;

            var retVal = string.Empty;

            if (hundreds > 0)
            {
                retVal = tens == 0 && hundreds == 2
                    ? string.Format("{0}", ArabicAppendedTwos[0])   // المضاف
                    : string.Format("{0}", ArabicHundreds[hundreds]);
            }

            if (tens > 0)
            {
                if (tens < 20)
                {
                    if (tens == 2 && hundreds == 0 && groupLevel > 0)
                    {
                        // Legacy tested "_intergerValue == 2000 || 2000000 || ..." here. _intergerValue
                        // is always 0 (defect 1), so the appended form is unreachable and the
                        // stand-alone form always wins. Collapsed to the reachable branch.
                        retVal = string.Format("{0}", ArabicTwos[groupLevel]);
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
                        else if ((tens == 1 || tens == 2) && (groupLevel == 0 || groupLevel == -1) &&
                                 hundreds == 0 && remainingNumber == 0)
                        {
                            retVal += string.Empty;
                        }
                        else
                        {
                            retVal += GetDigitFeminineStatus(tens, groupLevel);
                        }
                    }
                }
                else
                {
                    var ones = tens % 10;
                    tens = (tens / 10) - 2; // 20's offset

                    if (ones > 0)
                    {
                        if (retVal != string.Empty)
                        {
                            retVal += " و ";
                        }

                        retVal += GetDigitFeminineStatus(ones, groupLevel);
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
    }
}
