using System.Linq;
using System.Threading.Tasks;
using HCMv3.AiAgent.Tools;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AiAgent;

/// <summary>
/// Wage type matching against names taken from the real dev database, including the two cases that failed
/// there: "traffic violation deduction" not finding "Traffic Fines Ded", and "bonus" finding four wage types
/// because the payroll-generated Arrear twins were offered too.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AiPayLookup_Tests : AppTestBase
{
    private readonly int _carAllowance;
    private readonly int _carAllowanceNp;
    private readonly int _trafficFines;
    private readonly int _bonusPay;
    private readonly int _agmBonus;
    private readonly int _housing;

    public AiPayLookup_Tests()
    {
        LoginAsDefaultTenantAdmin();

        _carAllowance = WageType("1029", "Car Allowance", "بدل سيارة", BillingType.Earning, inBasicPay: true);
        _carAllowanceNp = WageType("1030", "Car Allowance(NP)", "بدل سيارة (غير مدفوع)", BillingType.Earning, inBasicPay: true);
        WageType("7029", "Car Allowance Arrear", "فرق بدل سيارة", BillingType.Earning, inBasicPay: false, arrearOf: _carAllowance);
        _housing = WageType("1010", "Housing Alw", "بدل سكن", BillingType.Earning, inBasicPay: true);

        _trafficFines = WageType("2014", "Traffic Fines Ded", "خصم مخالفات مرورية", BillingType.Deduction, inBasicPay: false);
        WageType("8014", "Traffic Fines Ded Arrear", "فرق خصم مخالفات مرورية", BillingType.Deduction, inBasicPay: false, arrearOf: _trafficFines);
        WageType("2020", "Loan Ded", "خصم قرض", BillingType.Deduction, inBasicPay: false);
        WageType("2030", "Absence Ded", "خصم غياب", BillingType.Deduction, inBasicPay: false);

        _bonusPay = WageType("1053", "Bonus Pay", "مكافأة", BillingType.Earning, inBasicPay: false);
        WageType("7053", "Bonus Pay Arrear", "فرق مكافأة", BillingType.Earning, inBasicPay: false, arrearOf: _bonusPay);
        _agmBonus = WageType("2047", "AGM Bonus", "مكافأة الجمعية العمومية", BillingType.Earning, inBasicPay: false);
        WageType("8047", "AGM Bonus Arrear", "فرق مكافأة الجمعية العمومية", BillingType.Earning, inBasicPay: false, arrearOf: _agmBonus);
    }

    private Task<WageTypeMatch> Resolve(string text, AiWageTypeUsage usage) =>
        Resolve<AiPayLookup>().ResolveWageTypeAsync(text, usage);

    [Fact]
    public async Task Arrear_Wage_Types_Should_Never_Be_Offered()
    {
        var oneTime = await Resolve<AiPayLookup>().GetAllowedWageTypesAsync(AiWageTypeUsage.OneTimePayment);

        oneTime.ShouldNotContain(w => w.NameEn.EndsWith("Arrear"));
        oneTime.Count.ShouldBe(8);
    }

    [Fact]
    public async Task Traffic_Violation_Deduction_Should_Find_Traffic_Fines_Ded()
    {
        var match = await Resolve("traffic violation deduction", AiWageTypeUsage.OneTimePayment);

        match.Match.ShouldNotBeNull();
        match.Match.Id.ShouldBe(_trafficFines);
    }

    [Fact]
    public async Task Bonus_Should_Ask_Between_The_Two_Real_Bonuses_Only()
    {
        var match = await Resolve("bonus", AiWageTypeUsage.OneTimePayment);

        match.Match.ShouldBeNull();
        match.IsAmbiguous.ShouldBeTrue();
        match.Candidates.Select(w => w.Id).ShouldBe(new[] { _bonusPay, _agmBonus }, ignoreOrder: true);
    }

    [Fact]
    public async Task A_Generic_Word_Alone_Should_Not_Pick_A_Wage_Type()
    {
        // Every deduction shares "deduction"; picking one would be a guess.
        var match = await Resolve("the deduction", AiWageTypeUsage.OneTimePayment);

        match.Match.ShouldBeNull();
    }

    [Fact]
    public async Task The_Exact_Name_Should_Win_Over_A_Longer_Similar_Name()
    {
        (await Resolve("car allowance", AiWageTypeUsage.BasicPay)).Match.Id.ShouldBe(_carAllowance);
        (await Resolve("Car Allowance(NP)", AiWageTypeUsage.BasicPay)).Match.Id.ShouldBe(_carAllowanceNp);
    }

    [Theory]
    [InlineData("car_allowance")]
    [InlineData("Car-Allowance")]
    public async Task A_Name_Written_Like_An_Identifier_Should_Still_Be_Exact(string text)
    {
        // Qwen3-4B on the dev laptop sent "car_allowance" for "car allowance".
        (await Resolve(text, AiWageTypeUsage.BasicPay)).Match.Id.ShouldBe(_carAllowance);
    }

    [Fact]
    public async Task A_Short_Form_In_The_Name_Should_Still_Match()
    {
        // "Housing Alw" is how the name is actually stored.
        (await Resolve("housing allowance", AiWageTypeUsage.BasicPay)).Match.Id.ShouldBe(_housing);
    }

    [Fact]
    public async Task An_Arabic_Name_With_The_Definite_Article_Should_Match()
    {
        (await Resolve("بدل السكن", AiWageTypeUsage.BasicPay)).Match.Id.ShouldBe(_housing);
    }

    [Fact]
    public async Task Asking_For_An_Arrear_By_Name_Should_Say_It_Is_Not_Allowed()
    {
        var match = await Resolve("Bonus Pay Arrear", AiWageTypeUsage.OneTimePayment);

        match.Match.ShouldBeNull();
        match.NotAllowed.ShouldNotBeNull();
        match.NotAllowed.NameEn.ShouldBe("Bonus Pay Arrear");
    }

    [Theory]
    [InlineData("traffic violation deduction", "Traffic Fines Ded", 1.25)]
    [InlineData("deduction", "Traffic Fines Ded", 0.25)]
    [InlineData("car", "Car Allowance(NP)", 1)]
    [InlineData("salary", "Car Allowance", 0)]
    public void Word_Score_Should_Weigh_Distinctive_Words_Above_Generic_Ones(string typed, string name, double expected)
    {
        AiPayLookup.WordScore(typed, name).ShouldBe(expected);
    }

    private int WageType(string code, string nameEn, string nameAr, BillingType billingType, bool inBasicPay, int? arrearOf = null)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = nameEn,
                NameAr = nameAr,
                BillingType = billingType,
                IncludeInBasicPay = inBasicPay,
                Status = true,
                ArrearSourceWageTypeId = arrearOf
            }).Entity;
        });
        return wageType.Id;
    }
}
