using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GoLiveSetup.ReferenceData;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetup;

// ReSharper disable once InconsistentNaming
public class ReferenceDataSeeder_Tests : AppTestBase
{
    private readonly IReferenceDataSeeder _seeder;

    public ReferenceDataSeeder_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _seeder = Resolve<IReferenceDataSeeder>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- packs load

    [Fact]
    public void Should_Load_The_Universal_Pack_From_Embedded_Resources()
    {
        var pack = _seeder.GetPack("universal");

        pack.Id.ShouldBe("universal");
        pack.Lookups.ShouldContainKey(nameof(Country));
        pack.Lookups.ShouldContainKey(nameof(Nationality));
        pack.Currencies.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Every wage type is paid on fixed 26 days a month, never on the month's calendar days: a full month
    /// is 26 days whatever its length, as the legacy payroll customers compare against pays it.
    /// </summary>
    [Fact]
    public void Should_Pay_Every_Wage_Type_On_Fixed_26_Days()
    {
        var wageTypes = _seeder.GetPack("payroll").WageTypes;

        wageTypes.ShouldNotBeEmpty();
        wageTypes.Where(w => w.BillingMethod != HCMv3.PayrollManagement.BillingMethod.FixedDaysPerMonth)
            .Select(w => w.Code).ShouldBeEmpty();
        wageTypes.Where(w => w.FixedDays != 26).Select(w => w.Code).ShouldBeEmpty();
    }

    /// <summary>Every arrear wage type names the wage type it is the arrear of, and that wage type is in the pack.</summary>
    [Fact]
    public void Should_Link_Every_Arrear_Wage_Type_To_Its_Source()
    {
        var wageTypes = _seeder.GetPack("payroll").WageTypes;
        var codes = wageTypes.Select(w => w.Code).ToHashSet();

        wageTypes.Where(w => w.IncludeInArrearPay && (string.IsNullOrWhiteSpace(w.ArrearSourceCode) || !codes.Contains(w.ArrearSourceCode)))
            .Select(w => w.Code).ShouldBeEmpty();
    }

    /// <summary>Seeded, the arrear wage types point at their sources in the database too.</summary>
    [Fact]
    public async Task Should_Seed_The_Arrear_Source_Links()
    {
        await _seeder.SeedPackAsync("payroll", TenantId);

        UsingDbContext(context => context.WageTypes
                .Where(w => w.TenantId == TenantId && w.IncludeInArrearPay && w.ArrearSourceWageTypeId == null)
                .Select(w => w.Code).ToList())
            .ShouldBeEmpty();
        UsingDbContext(context => context.WageTypes.Where(w => w.TenantId == TenantId && w.Code == "1041").Select(w => w.ArrearSourceWageType.Code).Single())
            .ShouldBe("1040");
    }

    /// <summary>Basic Salary, the wage type the day counts are taken from, reaches a new tenant on fixed 26 days.</summary>
    [Fact]
    public async Task Should_Seed_Basic_Salary_On_Fixed_26_Days()
    {
        await _seeder.SeedPackAsync("payroll", TenantId);

        var basic = UsingDbContext(context => context.WageTypes.Single(w => w.TenantId == TenantId && w.Code == "1000"));
        basic.BillingMethod.ShouldBe(HCMv3.PayrollManagement.BillingMethod.FixedDaysPerMonth);
        basic.FixedDays.ShouldBe(26);
    }

    [Fact]
    public void Should_Load_The_Kuwait_Pack_From_Embedded_Resources()
    {
        var pack = _seeder.GetPack("kuwait");

        pack.Id.ShouldBe("kuwait");
        pack.Lookups.ShouldContainKey(nameof(Bank));
        pack.Lookups.ShouldContainKey(nameof(Governorate));
    }

    [Fact]
    public void Should_Throw_For_An_Unknown_Pack()
    {
        Should.Throw<UserFriendlyException>(() => _seeder.GetPack("atlantis"));
    }

    [Fact]
    public void Should_Ship_Every_Default_Pack()
    {
        _seeder.GetAvailablePackIds().ShouldBe(new[] { "universal", "kuwait", "payroll" });
    }

    /// <summary>
    /// Every key in a pack must resolve to a repository. Without this, a typo in the JSON would only
    /// surface as a runtime exception during a real tenant creation.
    /// </summary>
    [Fact]
    public async Task Should_Recognise_Every_Lookup_Named_In_Every_Pack()
    {
        foreach (var packId in _seeder.GetAvailablePackIds())
        {
            await Should.NotThrowAsync(() => _seeder.PreviewPackAsync(packId));
        }
    }

    // ---------------------------------------------------------------- content sanity

    [Fact]
    public void Should_Give_Every_Row_A_Code_And_Both_Names()
    {
        foreach (var packId in _seeder.GetAvailablePackIds())
        {
            var pack = _seeder.GetPack(packId);

            foreach (var (lookup, rows) in pack.Lookups)
            {
                foreach (var row in rows)
                {
                    row.Code.ShouldNotBeNullOrWhiteSpace($"{packId}/{lookup} has a row with no code");
                    row.NameEn.ShouldNotBeNullOrWhiteSpace($"{packId}/{lookup} has a row with no English name");
                    row.NameAr.ShouldNotBeNullOrWhiteSpace($"{packId}/{lookup} has a row with no Arabic name");
                }
            }
        }
    }

    [Fact]
    public void Should_Not_Repeat_An_English_Name_Within_A_Lookup()
    {
        foreach (var packId in _seeder.GetAvailablePackIds())
        {
            var pack = _seeder.GetPack(packId);

            foreach (var (lookup, rows) in pack.Lookups)
            {
                var names = rows.Select(r => r.NameEn.Trim().ToLowerInvariant()).ToList();
                names.Distinct().Count().ShouldBe(names.Count, $"{packId}/{lookup} repeats an English name");
            }
        }
    }

    /// <summary>Arabic must actually be Arabic -- copying the English across is the classic failure.</summary>
    [Fact]
    public void Should_Give_Arabic_Names_In_Arabic_Script()
    {
        foreach (var packId in _seeder.GetAvailablePackIds())
        {
            var pack = _seeder.GetPack(packId);

            foreach (var (lookup, rows) in pack.Lookups)
            {
                foreach (var row in rows)
                {
                    row.NameAr.Any(c => c >= 0x0600 && c <= 0x06FF)
                        .ShouldBeTrue($"{packId}/{lookup} row '{row.NameEn}' has no Arabic characters");
                }
            }
        }
    }

    [Fact]
    public void Should_Give_The_Gulf_Dinars_Three_Decimal_Places()
    {
        var currencies = _seeder.GetPack("universal").Currencies;

        currencies.Single(c => c.Code == "KWD").DecimalCount.ShouldBe(3);
        currencies.Single(c => c.Code == "BHD").DecimalCount.ShouldBe(3);
        currencies.Single(c => c.Code == "OMR").DecimalCount.ShouldBe(3);
        currencies.Single(c => c.Code == "USD").DecimalCount.ShouldBe(2);
    }

    // ---------------------------------------------------------------- seeding

    [Fact]
    public async Task Should_Seed_The_Universal_Pack_Into_An_Empty_Tenant()
    {
        var result = await _seeder.SeedPackAsync("universal", TenantId);

        result.TotalInserted.ShouldBeGreaterThan(400);
        result.TotalSkipped.ShouldBe(0);

        UsingDbContext(context =>
        {
            context.Countries.Count(c => c.TenantId == TenantId).ShouldBeGreaterThan(150);
            context.Nationalities.Count(n => n.TenantId == TenantId).ShouldBeGreaterThan(200);
            context.Currencies.Count(c => c.TenantId == TenantId).ShouldBeGreaterThan(10);
        });
    }

    [Fact]
    public async Task Should_Stamp_The_Tenant_On_Every_Seeded_Row()
    {
        await _seeder.SeedPackAsync("kuwait", TenantId);

        UsingDbContext(context =>
        {
            context.Banks.Where(b => b.TenantId != TenantId).ShouldBeEmpty();
            context.Governorates.Where(g => g.TenantId != TenantId).ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task Should_Carry_Currency_Decimal_Places_Into_The_Database()
    {
        await _seeder.SeedPackAsync("universal", TenantId);

        UsingDbContext(context =>
        {
            var kwd = context.Currencies.Single(c => c.TenantId == TenantId && c.NameEn == "Kuwaiti Dinar");
            kwd.DecimalCount.ShouldBe(3);
            kwd.Code.ShouldBe("KWD");
            kwd.NameAr.ShouldNotBeNullOrWhiteSpace();
        });
    }

    // ---------------------------------------------------------------- idempotency

    [Fact]
    public async Task Should_Insert_Nothing_On_A_Second_Run()
    {
        var first = await _seeder.SeedPackAsync("kuwait", TenantId);
        var second = await _seeder.SeedPackAsync("kuwait", TenantId);

        first.TotalInserted.ShouldBeGreaterThan(0);
        second.TotalInserted.ShouldBe(0);
        second.TotalSkipped.ShouldBe(first.TotalInserted);
    }

    [Fact]
    public async Task Should_Not_Duplicate_Rows_On_A_Second_Run()
    {
        await _seeder.SeedPackAsync("kuwait", TenantId);
        await _seeder.SeedPackAsync("kuwait", TenantId);

        UsingDbContext(context =>
        {
            var names = context.Banks.Where(b => b.TenantId == TenantId).Select(b => b.NameEn).ToList();
            names.Distinct().Count().ShouldBe(names.Count);
        });
    }

    /// <summary>The bank-file generator picks a paying bank's layouts by its code, so the banks carry those codes.</summary>
    [Theory]
    [InlineData("National Bank of Kuwait", "NBK")]
    [InlineData("Commercial Bank of Kuwait", "CBK")]
    [InlineData("Al-Ahli Bank of Kuwait", "ABK")]
    [InlineData("Gulf Bank", "GB")]
    [InlineData("Burgan Bank", "BUR")]
    [InlineData("Kuwait Finance House", "KFH")]
    [InlineData("Kuwait International Bank", "KIB")]
    [InlineData("Boubyan Bank", "BB")]
    [InlineData("Warba Bank", "WRB")]
    public async Task Should_Seed_The_Kuwait_Banks_With_The_Codes_Bank_Files_Know_Them_By(string name, string code)
    {
        await _seeder.SeedPackAsync("kuwait", TenantId);

        UsingDbContext(context => context.Banks.Single(b => b.TenantId == TenantId && b.NameEn == name).Code.ShouldBe(code));
    }

    [Fact]
    public async Task Should_Leave_A_Row_The_Tenant_Already_Has_Untouched()
    {
        UsingDbContext(context =>
        {
            context.Banks.Add(new Bank
            {
                TenantId = TenantId,
                Code = "HOUSE",
                // Same name as the pack, different case, to prove matching ignores case.
                NameEn = "gulf bank",
                NameAr = "اسم من العميل",
            });
        });

        await _seeder.SeedPackAsync("kuwait", TenantId);

        UsingDbContext(context =>
        {
            var banks = context.Banks.Where(b => b.TenantId == TenantId && b.NameEn.ToLower() == "gulf bank").ToList();
            banks.Count.ShouldBe(1);
            banks[0].Code.ShouldBe("HOUSE");
            banks[0].NameAr.ShouldBe("اسم من العميل");
        });
    }

    // ---------------------------------------------------------------- preview

    [Fact]
    public async Task Should_Preview_Without_Writing_Anything()
    {
        var preview = await _seeder.PreviewPackAsync("kuwait");

        preview.TotalInserted.ShouldBeGreaterThan(0);

        UsingDbContext(context => { context.Banks.Count(b => b.TenantId == TenantId).ShouldBe(0); });
    }

    [Fact]
    public async Task Should_Report_Nothing_Left_To_Do_After_Seeding()
    {
        await _seeder.SeedPackAsync("kuwait", TenantId);

        var preview = await _seeder.PreviewPackAsync("kuwait");

        preview.TotalInserted.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Seed_Every_Default_Pack()
    {
        var results = await _seeder.SeedDefaultsAsync(TenantId);

        results.Select(r => r.PackId).ShouldBe(new[] { "universal", "kuwait", "payroll" });
        results.Sum(r => r.TotalInserted).ShouldBeGreaterThan(450);
    }
}
