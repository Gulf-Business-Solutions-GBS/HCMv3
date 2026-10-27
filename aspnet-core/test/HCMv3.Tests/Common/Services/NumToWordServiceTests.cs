using HCMv3.Common.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Common.Services;

public class NumToWordServiceTests
{
    [Fact]
    public void Should_Convert_Zero_To_Zero()
    {
        NumToWordService.NumberToWordsEnglish(0).ShouldBe("Zero");
    }

    [Fact]
    public void Should_Convert_Negative_Number_With_Minus_Prefix()
    {
        NumToWordService.NumberToWordsEnglish(-5).ShouldBe("minus Five");
    }

    [Fact]
    public void Should_Convert_Simple_Number_To_English_Words()
    {
        NumToWordService.NumberToWordsEnglish(21).ShouldBe("Twenty-One");
    }

    [Fact]
    public void Should_Convert_Numerals_To_Arabic_Digits()
    {
        NumToWordService.ConvertNumeralsToArabic("123").ShouldBe("۱۲۳");
    }

    [Fact]
    public void Should_Convert_Amount_To_English_Words_With_Kuwaiti_Dinar()
    {
        var currency = new CurrencyInfo(CurrencyInfo.Currencies.Kuwait);
        var converter = new NumToWordService(125.500m, currency);

        var result = converter.ConvertToEnglish();

        result.ShouldContain("Hundred");
        result.ShouldContain("Kuwaiti Dinar");
    }

    [Fact]
    public void Should_Convert_Amount_To_Arabic_Words_With_Kuwaiti_Dinar()
    {
        var currency = new CurrencyInfo(CurrencyInfo.Currencies.Kuwait);
        var converter = new NumToWordService(3m, currency);

        var result = converter.ConvertToArabic();

        result.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Should_Return_Zero_Word_For_Zero_Amount()
    {
        var currency = new CurrencyInfo(CurrencyInfo.Currencies.Kuwait);
        var converter = new NumToWordService(0m, currency);

        converter.ConvertToEnglish().ShouldBe("Zero");
        converter.ConvertToArabic().ShouldBe("صفر");
    }
}
