using System;

namespace HCMv3.Common.Services;

/// <summary>
/// Number-to-words converter (English + Arabic), used for payslip amount-in-words (ported near-verbatim
/// from legacy XAF <c>HCM10.Module.BusinessObjects.Services.NumToWordService</c>). Fully self-contained
/// algorithm, no DB/session dependency -- kept as a plain class (not an <c>IDomainService</c>).
///
/// <see cref="CurrencyInfo"/> is the legacy's own bespoke currency-naming lookup used only by this
/// converter (English/Arabic singular/dual/plural currency names + decimal-part precision) -- it is
/// distinct from, and not backed by, the real ported <c>HCMv3.GeneralMasterInfo.Currency</c> entity;
/// callers must map from a real <c>Currency</c> row to the appropriate <see cref="CurrencyInfo"/> preset
/// (or extend this class with additional presets) themselves.
/// </summary>
public class NumToWordService
{
    public NumToWordService(decimal number, CurrencyInfo currency)
        : this(number, currency, string.Empty, "only.", "فقط", "لا غير.")
    {
    }

    public NumToWordService(decimal number, CurrencyInfo currency, string englishPrefixText, string englishSuffixText, string arabicPrefixText, string arabicSuffixText)
    {
        Number = number;
        Currency = currency;
        EnglishPrefixText = englishPrefixText;
        EnglishSuffixText = englishSuffixText;
        ArabicPrefixText = arabicPrefixText;
        ArabicSuffixText = arabicSuffixText;

        ExtractIntegerAndDecimalParts();
    }

    /// Group Levels: 987,654,321.234
    /// 234 : Group Level -1
    /// 321 : Group Level 0
    /// 654 : Group Level 1
    /// 987 : Group Level 2

    private long _integerValue;
    private int _decimalValue;

    public decimal Number { get; set; }
    public CurrencyInfo Currency { get; set; }
    public string EnglishPrefixText { get; set; }
    public string EnglishSuffixText { get; set; }
    public string ArabicPrefixText { get; set; }
    public string ArabicSuffixText { get; set; }

    public static string NumberToWordsEnglish(int number)
    {
        if (number == 0)
        {
            return "Zero";
        }

        if (number < 0)
        {
            return "minus " + NumberToWordsEnglish(Math.Abs(number));
        }

        var words = "";

        if (number / 1000000 > 0)
        {
            words += NumberToWordsEnglish(number / 1000000) + " Million ";
            number %= 1000000;
        }

        if (number / 1000 > 0)
        {
            words += NumberToWordsEnglish(number / 1000) + " Thousand ";
            number %= 1000;
        }

        if (number / 100 > 0)
        {
            words += NumberToWordsEnglish(number / 100) + " Hundred ";
            number %= 100;
        }

        if (number > 0)
        {
            if (words != "")
            {
                words += "and ";
            }

            var unitsMap = new[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            var tensMap = new[] { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            if (number < 20)
            {
                words += unitsMap[number];
            }
            else
            {
                words += tensMap[number / 10];
                if (number % 10 > 0)
                {
                    words += "-" + unitsMap[number % 10];
                }
            }
        }

        return words;
    }

    public static string ConvertNumeralsToArabic(string input)
    {
        return input
            .Replace('0', '٠')
            .Replace('1', '۱')
            .Replace('2', '۲')
            .Replace('3', '۳')
            .Replace('4', '٤')
            .Replace('5', '۵')
            .Replace('6', '٦')
            .Replace('7', '٧')
            .Replace('8', '٨')
            .Replace('9', '٩');
    }

    private string GetDecimalValue(string decimalPart)
    {
        string result;

        if (Currency.PartPrecision != decimalPart.Length)
        {
            var decimalPartLength = decimalPart.Length;

            for (int i = 0, loopTo = Currency.PartPrecision - decimalPartLength; i <= loopTo; i++)
            {
                decimalPart += "0"; // Fix for 1 number after decimal ( 10.5 , 1442.2 , 375.4 )
            }

            result = string.Format("{0}.{1}", decimalPart.Substring(0, Currency.PartPrecision), decimalPart.Substring(Currency.PartPrecision, decimalPart.Length - Currency.PartPrecision));
            result = Math.Round(Convert.ToDecimal(result)).ToString();
        }
        else
        {
            result = decimalPart;
        }

        for (int i = 0, loopTo1 = Currency.PartPrecision - result.Length - 1; i <= loopTo1; i++)
        {
            result += "0";
        }

        return result;
    }

    private void ExtractIntegerAndDecimalParts()
    {
        var splits = Number.ToString(System.Globalization.CultureInfo.InvariantCulture).Split('.');

        _integerValue = Convert.ToInt32(splits[0]);

        if (splits.Length > 1)
        {
            _decimalValue = Convert.ToInt32(GetDecimalValue(splits[1]));
        }
    }

    private static readonly string[] EnglishOnes = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
    private static readonly string[] EnglishTens = { "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
    private static readonly string[] EnglishGroup = { "Hundred", "Thousand", "Million", "Billion", "Trillion", "Quadrillion", "Quintillion", "Sextillian", "Septillion", "Octillion", "Nonillion", "Decillion", "Undecillion", "Duodecillion", "Tredecillion", "Quattuordecillion", "Quindecillion", "Sexdecillion", "Septendecillion", "Octodecillion", "Novemdecillion", "Vigintillion", "Unvigintillion", "Duovigintillion", "10^72", "10^75", "10^78", "10^81", "10^84", "10^87", "Vigintinonillion", "10^93", "10^96", "Duotrigintillion", "Trestrigintillion" };

    private string ProcessGroup(int groupNumber)
    {
        var tens = groupNumber % 100;
        var hundreds = groupNumber / 100;
        var retVal = string.Empty;

        if (hundreds > 0)
        {
            retVal = string.Format("{0} {1}", EnglishOnes[hundreds], EnglishGroup[0]);
        }

        if (tens > 0)
        {
            if (tens < 20)
            {
                retVal += (retVal != string.Empty ? " " : string.Empty) + EnglishOnes[tens];
            }
            else
            {
                var ones = tens % 10;
                tens = tens / 10 - 2;
                retVal += (retVal != string.Empty ? " " : string.Empty) + EnglishTens[tens];

                if (ones > 0)
                {
                    retVal += (retVal != string.Empty ? " " : string.Empty) + EnglishOnes[ones];
                }
            }
        }

        return retVal;
    }

    public string ConvertToEnglish()
    {
        var tempNumber = Number;

        if (tempNumber == 0)
        {
            return "Zero";
        }

        var decimalString = ProcessGroup(_decimalValue);

        var retVal = string.Empty;
        var group = 0;

        if (tempNumber < 1)
        {
            retVal = EnglishOnes[0];
        }
        else
        {
            while (tempNumber >= 1)
            {
                var numberToProcess = Convert.ToInt32(Math.Truncate(tempNumber % 1000));
                tempNumber /= 1000;

                var groupDescription = ProcessGroup(numberToProcess);

                if (groupDescription != string.Empty)
                {
                    if (group > 0)
                    {
                        retVal = string.Format("{0} {1}", EnglishGroup[group], retVal);
                    }

                    retVal = string.Format("{0} {1}", groupDescription, retVal);
                }

                group += 1;
            }
        }

        var formattedNumber = string.Empty;
        formattedNumber += EnglishPrefixText != string.Empty ? string.Format("{0} ", EnglishPrefixText) : string.Empty;
        formattedNumber += retVal != string.Empty ? retVal : string.Empty;
        formattedNumber += retVal != string.Empty ? (_integerValue == 1 ? Currency.EnglishCurrencyName : Currency.EnglishPluralCurrencyName) : string.Empty;
        formattedNumber += decimalString != string.Empty ? " and " : string.Empty;
        formattedNumber += decimalString != string.Empty ? decimalString : string.Empty;
        formattedNumber += decimalString != string.Empty ? " " + (_decimalValue == 1 ? Currency.EnglishCurrencyPartName : Currency.EnglishPluralCurrencyPartName) : string.Empty;
        formattedNumber += EnglishSuffixText != string.Empty ? string.Format(" {0}", EnglishSuffixText) : string.Empty;

        return formattedNumber;
    }

    private static readonly string[] ArabicOnes = { string.Empty, "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة", "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر" };
    private static readonly string[] ArabicFeminineOnes = { string.Empty, "إحدى", "اثنتان", "ثلاث", "أربع", "خمس", "ست", "سبع", "ثمان", "تسع", "عشر", "إحدى عشرة", "اثنتا عشرة", "ثلاث عشرة", "أربع عشرة", "خمس عشرة", "ست عشرة", "سبع عشرة", "ثماني عشرة", "تسع عشرة" };
    private static readonly string[] ArabicTens = { "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون" };
    private static readonly string[] ArabicHundreds = { "", "مائة", "مئتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة" };
    private static readonly string[] ArabicAppendedTwos = { "مئتا", "ألفا", "مليونا", "مليارا", "تريليونا", "كوادريليونا", "كوينتليونا", "سكستيليونا" };
    private static readonly string[] ArabicTwos = { "مئتان", "ألفان", "مليونان", "ملياران", "تريليونان", "كوادريليونان", "كوينتليونان", "سكستيليونان" };
    private static readonly string[] ArabicGroup = { "مائة", "ألف", "مليون", "مليار", "تريليون", "كوادريليون", "كوينتليون", "سكستيليون" };
    private static readonly string[] ArabicAppendedGroup = { "", "ألفاً", "مليوناً", "ملياراً", "تريليوناً", "كوادريليوناً", "كوينتليوناً", "سكستيليوناً" };
    private static readonly string[] ArabicPluralGroups = { "", "آلاف", "ملايين", "مليارات", "تريليونات", "كوادريليونات", "كوينتليونات", "سكستيليونات" };

    private string GetDigitFeminineStatus(int digit, int groupLevel)
    {
        if (groupLevel == -1)
        {
            return Currency.IsCurrencyPartNameFeminine ? ArabicFeminineOnes[digit] : ArabicOnes[digit];
        }

        if (groupLevel == 0)
        {
            return Currency.IsCurrencyNameFeminine ? ArabicFeminineOnes[digit] : ArabicOnes[digit];
        }

        return ArabicOnes[digit];
    }

    private string ProcessArabicGroup(int groupNumber, int groupLevel, decimal remainingNumber)
    {
        var tens = groupNumber % 100;
        var hundreds = groupNumber / 100;
        var retVal = string.Empty;

        if (hundreds > 0)
        {
            retVal = tens == 0 && hundreds == 2
                ? string.Format("{0}", ArabicAppendedTwos[0])
                : string.Format("{0}", ArabicHundreds[hundreds]);
        }

        if (tens > 0)
        {
            if (tens < 20)
            {
                if (tens == 2 && hundreds == 0 && groupLevel > 0)
                {
                    retVal = (_integerValue == 2000 || _integerValue == 2000000 || _integerValue == 2000000000 ||
                              _integerValue == 2000000000000L || _integerValue == 2000000000000000L || _integerValue == 2000000000000000000L)
                        ? string.Format("{0}", ArabicAppendedTwos[groupLevel])
                        : string.Format("{0}", ArabicTwos[groupLevel]);
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
                    else if ((tens == 1 || tens == 2) && (groupLevel == 0 || groupLevel == -1) && hundreds == 0 && remainingNumber == 0)
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
                tens = tens / 10 - 2;

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

    public string ConvertToArabic()
    {
        var tempNumber = Number;

        if (tempNumber == 0)
        {
            return "صفر";
        }

        var decimalString = ProcessArabicGroup(_decimalValue, -1, 0);

        var retVal = string.Empty;
        byte group = 0;
        while (tempNumber >= 1)
        {
            var numberToProcess = Convert.ToInt32(Math.Truncate(tempNumber % 1000));
            tempNumber /= 1000;

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

            group += 1;
        }

        var formattedNumber = string.Empty;
        formattedNumber += retVal != string.Empty ? retVal : string.Empty;
        formattedNumber += _decimalValue != 0 ? " و " : string.Empty;
        formattedNumber += _decimalValue != 0 ? decimalString : string.Empty;

        if (_decimalValue != 0)
        {
            formattedNumber += " ";

            var remaining100 = _decimalValue % 100;

            if (remaining100 == 0 || remaining100 == 1)
            {
                formattedNumber += Currency.Arabic1CurrencyPartName;
            }
            else if (remaining100 == 2)
            {
                formattedNumber += Currency.Arabic2CurrencyPartName;
            }
            else if (remaining100 >= 3 && remaining100 <= 10)
            {
                formattedNumber += Currency.Arabic310CurrencyPartName;
            }
            else if (remaining100 >= 11 && remaining100 <= 99)
            {
                formattedNumber += Currency.Arabic1199CurrencyPartName;
            }
        }

        return formattedNumber;
    }
}

/// <summary>
/// Bespoke currency-naming lookup used only by <see cref="NumToWordService"/> (English/Arabic
/// singular/dual/plural currency names + decimal-part precision), ported verbatim from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.CurrencyInfo</c>. Not backed by the real ported
/// <c>HCMv3.GeneralMasterInfo.Currency</c> entity -- see <see cref="NumToWordService"/> remarks.
/// </summary>
public class CurrencyInfo
{
    public enum Currencies
    {
        Syria = 0,
        UAE,
        SaudiArabia,
        Tunisia,
        Kuwait,
        Gold
    }

    public CurrencyInfo(Currencies currency)
    {
        switch (currency)
        {
            case Currencies.Kuwait:
                CurrencyID = 5;
                CurrencyCode = "KWD";
                IsCurrencyNameFeminine = false;
                EnglishCurrencyName = "Kuwaiti Dinar";
                EnglishPluralCurrencyName = "Kuwaiti Dinars";
                EnglishCurrencyPartName = "Fils";
                EnglishPluralCurrencyPartName = "Fils";
                Arabic1CurrencyName = "دينار كويتي";
                Arabic2CurrencyName = "دينار كويتيان";
                Arabic310CurrencyName = "دينار كويتية";
                Arabic1199CurrencyName = "دينار كويتياً";
                Arabic1CurrencyPartName = "فلس";
                Arabic2CurrencyPartName = "فلسان";
                Arabic310CurrencyPartName = "فلوس";
                Arabic1199CurrencyPartName = "فلساً";
                PartPrecision = 2;
                IsCurrencyPartNameFeminine = false;
                break;

            case Currencies.Syria:
                CurrencyID = 0;
                CurrencyCode = "SYP";
                IsCurrencyNameFeminine = true;
                EnglishCurrencyName = "Syrian Pound";
                EnglishPluralCurrencyName = "Syrian Pounds";
                EnglishCurrencyPartName = "Piaster";
                EnglishPluralCurrencyPartName = "Piasteres";
                Arabic1CurrencyName = "ليرة سورية";
                Arabic2CurrencyName = "ليرتان سوريتان";
                Arabic310CurrencyName = "ليرات سورية";
                Arabic1199CurrencyName = "ليرة سورية";
                Arabic1CurrencyPartName = "قرش";
                Arabic2CurrencyPartName = "قرشان";
                Arabic310CurrencyPartName = "قروش";
                Arabic1199CurrencyPartName = "قرشاً";
                PartPrecision = 2;
                IsCurrencyPartNameFeminine = false;
                break;

            case Currencies.UAE:
                CurrencyID = 1;
                CurrencyCode = "AED";
                IsCurrencyNameFeminine = false;
                EnglishCurrencyName = "UAE Dirham";
                EnglishPluralCurrencyName = "UAE Dirhams";
                EnglishCurrencyPartName = "Fils";
                EnglishPluralCurrencyPartName = "Fils";
                Arabic1CurrencyName = "درهم إماراتي";
                Arabic2CurrencyName = "درهمان إماراتيان";
                Arabic310CurrencyName = "دراهم إماراتية";
                Arabic1199CurrencyName = "درهماً إماراتياً";
                Arabic1CurrencyPartName = "فلس";
                Arabic2CurrencyPartName = "فلسان";
                Arabic310CurrencyPartName = "فلوس";
                Arabic1199CurrencyPartName = "فلساً";
                PartPrecision = 2;
                IsCurrencyPartNameFeminine = false;
                break;

            case Currencies.SaudiArabia:
                CurrencyID = 2;
                CurrencyCode = "SAR";
                IsCurrencyNameFeminine = false;
                EnglishCurrencyName = "Saudi Riyal";
                EnglishPluralCurrencyName = "Saudi Riyals";
                EnglishCurrencyPartName = "Halala";
                EnglishPluralCurrencyPartName = "Halalas";
                Arabic1CurrencyName = "ريال سعودي";
                Arabic2CurrencyName = "ريالان سعوديان";
                Arabic310CurrencyName = "ريالات سعودية";
                Arabic1199CurrencyName = "ريالاً سعودياً";
                Arabic1CurrencyPartName = "هللة";
                Arabic2CurrencyPartName = "هللتان";
                Arabic310CurrencyPartName = "هللات";
                Arabic1199CurrencyPartName = "هللة";
                PartPrecision = 2;
                IsCurrencyPartNameFeminine = true;
                break;

            case Currencies.Tunisia:
                CurrencyID = 3;
                CurrencyCode = "TND";
                IsCurrencyNameFeminine = false;
                EnglishCurrencyName = "Tunisian Dinar";
                EnglishPluralCurrencyName = "Tunisian Dinars";
                EnglishCurrencyPartName = "milim";
                EnglishPluralCurrencyPartName = "millimes";
                Arabic1CurrencyName = "دينار تونسي";
                Arabic2CurrencyName = "ديناران تونسيان";
                Arabic310CurrencyName = "دنانير تونسية";
                Arabic1199CurrencyName = "ديناراً تونسياً";
                Arabic1CurrencyPartName = "مليم";
                Arabic2CurrencyPartName = "مليمان";
                Arabic310CurrencyPartName = "ملاليم";
                Arabic1199CurrencyPartName = "مليماً";
                PartPrecision = 3;
                IsCurrencyPartNameFeminine = false;
                break;

            case Currencies.Gold:
                CurrencyID = 4;
                CurrencyCode = "XAU";
                IsCurrencyNameFeminine = false;
                EnglishCurrencyName = "Gram";
                EnglishPluralCurrencyName = "Grams";
                EnglishCurrencyPartName = "Milligram";
                EnglishPluralCurrencyPartName = "Milligrams";
                Arabic1CurrencyName = "جرام";
                Arabic2CurrencyName = "جرامان";
                Arabic310CurrencyName = "جرامات";
                Arabic1199CurrencyName = "جراماً";
                Arabic1CurrencyPartName = "ملجرام";
                Arabic2CurrencyPartName = "ملجرامان";
                Arabic310CurrencyPartName = "ملجرامات";
                Arabic1199CurrencyPartName = "ملجراماً";
                PartPrecision = 2;
                IsCurrencyPartNameFeminine = false;
                break;
        }
    }

    public int CurrencyID { get; set; }
    public string CurrencyCode { get; set; }
    public bool IsCurrencyNameFeminine { get; set; }
    public string EnglishCurrencyName { get; set; }
    public string EnglishPluralCurrencyName { get; set; }
    public string Arabic1CurrencyName { get; set; }
    public string Arabic2CurrencyName { get; set; }
    public string Arabic310CurrencyName { get; set; }
    public string Arabic1199CurrencyName { get; set; }
    public byte PartPrecision { get; set; }
    public bool IsCurrencyPartNameFeminine { get; set; }
    public string EnglishCurrencyPartName { get; set; }
    public string EnglishPluralCurrencyPartName { get; set; }
    public string Arabic1CurrencyPartName { get; set; }
    public string Arabic2CurrencyPartName { get; set; }
    public string Arabic310CurrencyPartName { get; set; }
    public string Arabic1199CurrencyPartName { get; set; }
}
