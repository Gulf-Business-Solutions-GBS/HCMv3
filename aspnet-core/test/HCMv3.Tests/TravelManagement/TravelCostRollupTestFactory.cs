using System;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using HCMv3.GeneralMasterInfo.Services;
using HCMv3.TravelManagement;
using NSubstitute;

namespace HCMv3.Tests.TravelManagement;

/// <summary>
/// Builds a real <see cref="TravelCostRollupService"/> over the private travel test context. Currency
/// rounding (legacy <c>CurrencyService.Rounding</c>, ported as <see cref="ICurrencyService.RoundAsync"/>) is
/// substituted: <paramref name="decimals"/> plays the employee's payroll-area currency decimals (3 = KWD),
/// and <c>null</c> plays "no currency found", where legacy returns the value unrounded.
/// </summary>
internal static class TravelCostRollupTestFactory
{
    public static TravelCostRollupService Create(TravelManagementTestDbContext context, int? decimals = 3)
    {
        var currencyService = Substitute.For<ICurrencyService>();
        currencyService
            .RoundAsync(Arg.Any<int>(), Arg.Any<double>())
            .Returns(ci => Task.FromResult(decimals.HasValue
                ? Math.Round(ci.ArgAt<double>(1), decimals.Value)
                : ci.ArgAt<double>(1)));

        return new TravelCostRollupService(
            FakeRepository.For<Travel>(context),
            FakeRepository.For<TravelDetail>(context),
            FakeRepository.For<TravelDetailCost>(context),
            currencyService)
        {
            UnitOfWorkManager = Substitute.For<IUnitOfWorkManager>()
        };
    }
}
