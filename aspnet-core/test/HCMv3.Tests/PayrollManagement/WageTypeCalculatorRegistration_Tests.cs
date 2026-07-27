using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using HCMv3.PayrollManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P1 -- DI regression guard for <see cref="IWageTypeCalculator"/>.
///
/// <para><b>Why this file exists.</b> <see cref="WageTypeCalculatorResolver"/> receives its rules as an
/// injected <c>IEnumerable&lt;IWageTypeCalculator&gt;</c>. ABP's conventional registrar registers a class
/// under <c>WithService.DefaultInterfaces()</c>, and Castle's default-interface strategy matches an interface
/// to a class BY NAME. If a wave P4-P6 rule is named such that the strategy does not match
/// <see cref="IWageTypeCalculator"/>, the class still registers (under its own concrete type) and nothing
/// fails -- the injected enumerable is simply MISSING that rule, and every dispatch for its wage code falls
/// through the resolver's "unclaimed wage code" arm, which is a legitimate, silent, no-error outcome. The
/// result is a wage type that silently stops being paid.</para>
///
/// <para>The sibling TimeEngine wave hit exactly this failure with <c>IPunchResolutionStrategy</c> and added
/// <c>All_Four_Strategies_Should_Be_Resolvable_As_IPunchResolutionStrategy</c>
/// (<c>test/HCMv3.Tests/TimeManagement/PunchResolutionStrategies_Tests.cs</c>). This is the payroll
/// equivalent.</para>
///
/// <para><b>Design note -- there are no <see cref="IWageTypeCalculator"/> implementations yet</b> (they land
/// in waves P4-P6), so a "count the registrations" assertion would be vacuous today AND vacuous forever if
/// hardcoded to zero. Two complementary tests are used instead:</para>
/// <list type="number">
/// <item><description><see cref="Every_IWageTypeCalculator_In_The_Core_Assembly_Should_Resolve_From_The_Container"/>
/// discovers implementations by REFLECTION over the Core assembly and asserts the container hands back one of
/// each. It passes trivially today (0 == 0) and starts doing real work the moment the first rule lands, with
/// no edit required.</description></item>
/// <item><description><see cref="Castle_Default_Interface_Convention_Should_Match_The_WageTypeCalculator_Naming"/>
/// exercises the exact Castle registration shape ABP uses, against sample rules in this test assembly, so the
/// naming requirement is proven and documented NOW rather than discovered in production.</description></item>
/// </list>
/// </summary>
// ReSharper disable once InconsistentNaming
public class WageTypeCalculatorRegistration_Tests : AppTestBase
{
    #region << Samples used only by the Castle-convention proof >>

    /// <summary>Correctly named: ends with the interface name, so <c>DefaultInterfaces</c> matches.</summary>
    public class LoanPayWageTypeCalculator : IWageTypeCalculator
    {
        public string WageCode => "1006";

        public Task CalculateAsync(PayrollCalculationContext context) => Task.CompletedTask;
    }

    /// <summary>Correctly named: ends with the interface name.</summary>
    public class NetPayWageTypeCalculator : IWageTypeCalculator
    {
        public string WageCode => "9000";

        public Task CalculateAsync(PayrollCalculationContext context) => Task.CompletedTask;
    }

    #endregion

    [Fact]
    public void The_Resolver_Itself_Should_Be_Resolvable_From_The_Container()
    {
        // Note: the container hands back a Castle [UnitOfWork] interception proxy, not the raw
        // WageTypeCalculatorResolver instance -- so assert on behaviour, never with ShouldBeOfType<T>.
        var resolver = LocalIocManager.Resolve<IWageTypeCalculatorResolver>();

        resolver.ShouldNotBeNull();

        // Wave P6 landed NetPayWageTypeCalculator, so 9000 now resolves. (Before P4-P6 this asserted null.)
        resolver.Resolve("9000").ShouldNotBeNull();
        resolver.Resolve("9000").WageCode.ShouldBe("9000");

        // A code no wave claims still resolves to null -- that is a normal, expected outcome.
        resolver.Resolve("__no_such_wage_code__").ShouldBeNull();
    }

    [Fact]
    public void Resolving_The_Calculator_Enumerable_Should_Never_Throw()
    {
        // Castle throws on ResolveAll only if the service was never registered in a way it can enumerate.
        // The resolver's constructor depends on this enumerable, so a throw here would break every payroll
        // run at container-resolution time.
        IReadOnlyList<IWageTypeCalculator> calculators = null;

        Should.NotThrow(() => calculators = LocalIocManager.ResolveAll<IWageTypeCalculator>());

        calculators.ShouldNotBeNull();
    }

    /// <summary>
    /// The self-maintaining regression guard: every concrete <see cref="IWageTypeCalculator"/> in
    /// <c>HCMv3.Core</c> must come back from <c>ResolveAll</c>. Trivially true while waves P4-P6 are
    /// outstanding; automatically meaningful the moment they land.
    /// </summary>
    [Fact]
    public void Every_IWageTypeCalculator_In_The_Core_Assembly_Should_Resolve_From_The_Container()
    {
        var coreImplementations = typeof(IWageTypeCalculator).GetTypeInfo().Assembly
            .GetTypes()
            .Where(t => t.IsClass
                        && !t.IsAbstract
                        && typeof(IWageTypeCalculator).IsAssignableFrom(t))
            .ToList();

        var resolved = LocalIocManager.ResolveAll<IWageTypeCalculator>();

        resolved.Length.ShouldBe(
            coreImplementations.Count,
            "Some IWageTypeCalculator implementations are not registered under IWageTypeCalculator. "
            + "This does NOT fail loudly at runtime -- the resolver just never sees them and every wage code "
            + "they claim silently stops being calculated. Registered: "
            + string.Join(", ", resolved.Select(r => r.GetType().FullName))
            + " | Expected: "
            + string.Join(", ", coreImplementations.Select(t => t.FullName)));

        // Each declared wage code must be unique, or the resolver refuses to pick between them.
        resolved.Select(c => c.WageCode).Distinct().Count().ShouldBe(resolved.Length);
    }

    /// <summary>
    /// Proves the naming requirement using the exact Castle registration shape ABP's
    /// <c>BasicConventionalRegistrar</c> uses (<c>.WithService.Self().WithService.DefaultInterfaces()</c>),
    /// restricted to this test assembly so nothing else is affected.
    /// </summary>
    [Fact]
    public void Castle_Default_Interface_Convention_Should_Match_The_WageTypeCalculator_Naming()
    {
        using (var container = new WindsorContainer())
        {
            container.Register(
                Classes.FromAssembly(typeof(WageTypeCalculatorRegistration_Tests).GetTypeInfo().Assembly)
                    .BasedOn<IWageTypeCalculator>()
                    .WithService.Self()
                    .WithService.DefaultInterfaces()
                    .LifestyleTransient());

            var resolved = container.ResolveAll<IWageTypeCalculator>();

            resolved.Select(c => c.WageCode)
                .ShouldContain("1006", "LoanPayWageTypeCalculator must register under IWageTypeCalculator.");
            resolved.Select(c => c.WageCode)
                .ShouldContain("9000", "NetPayWageTypeCalculator must register under IWageTypeCalculator.");
        }
    }
}
