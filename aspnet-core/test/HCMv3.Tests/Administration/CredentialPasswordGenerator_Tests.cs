using System.Linq;
using System.Threading.Tasks;
using HCMv3.Administration.Tools;
using HCMv3.Security;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Administration;

/// <summary>
/// The temporary-password generator behind Reset User Passwords: follows the tenant's complexity settings
/// (read through Zero's <see cref="IPasswordComplexitySettingStore"/>) and never repeats itself in a loop.
/// </summary>
// ReSharper disable once InconsistentNaming
public class CredentialPasswordGenerator_Tests
{
    private static CredentialPasswordGenerator Create(PasswordComplexitySetting settings)
    {
        var store = Substitute.For<IPasswordComplexitySettingStore>();
        store.GetSettingsAsync().Returns(Task.FromResult(settings));
        return new CredentialPasswordGenerator(store);
    }

    [Fact]
    public async Task Should_Contain_Every_Required_Kind_Of_Character()
    {
        var generator = Create(new PasswordComplexitySetting
        {
            RequireDigit = true,
            RequireLowercase = true,
            RequireUppercase = true,
            RequireNonAlphanumeric = true,
            RequiredLength = 6
        });

        for (var i = 0; i < 50; i++)
        {
            var password = await generator.CreateAsync();

            password.Any(char.IsDigit).ShouldBeTrue();
            password.Any(char.IsLower).ShouldBeTrue();
            password.Any(char.IsUpper).ShouldBeTrue();
            password.Any(c => !char.IsLetterOrDigit(c)).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Should_Honour_A_Longer_Tenant_Minimum_And_Never_Go_Below_Its_Own()
    {
        var longPolicy = Create(new PasswordComplexitySetting { RequiredLength = 20 });
        var shortPolicy = Create(new PasswordComplexitySetting { RequiredLength = 3 });

        (await longPolicy.CreateAsync()).Length.ShouldBe(20);
        (await shortPolicy.CreateAsync()).Length.ShouldBe(CredentialPasswordGenerator.MinimumLength);
    }

    [Fact]
    public async Task Should_Not_Repeat_A_Password_When_Called_In_A_Tight_Loop()
    {
        // Zero's CreateRandomPassword seeds System.Random from the clock tick, so calls inside one tick repeat.
        var generator = Create(new PasswordComplexitySetting { RequireDigit = true, RequiredLength = 8 });

        var passwords = await Task.WhenAll(Enumerable.Range(0, 200).Select(_ => generator.CreateAsync()));

        passwords.Distinct().Count().ShouldBe(200);
    }
}
