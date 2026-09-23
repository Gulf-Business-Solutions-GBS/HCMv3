using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using HCMv3.Workflow.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// The tenant-wide request number.
///
/// <para>
/// Two things are being protected here, and they are separate. The first is the shape of the sequence: one
/// counter for the whole tenant, starting at 1, so that a leave request filed after two exit permits is
/// number 3 and not number 1 all over again. The second is that the counter is safe when two people submit
/// at the same moment -- the old implementation read the highest number in the table and added one, which
/// hands the same number to both and leaves nothing in the schema to notice.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class RequestNumberGenerator_Tests : AppTestBase
{
    private readonly IRequestNumberGenerator _generator;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public RequestNumberGenerator_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _generator = Resolve<IRequestNumberGenerator>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    /// <summary>Runs one claim in its own completed unit of work, the way a real submission would.</summary>
    private async Task<string> NextAsync(string prefix)
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            var number = await _generator.NextAsync(prefix);
            await uow.CompleteAsync();

            return number;
        }
    }

    private static long DigitsOf(string number)
    {
        return long.Parse(number.Substring(number.IndexOf('-') + 1));
    }

    [Fact]
    public async Task The_Sequence_Should_Start_At_One()
    {
        // A fresh tenant with no counter row yet. The generator has to create it and hand out 1 rather than
        // fall over or start somewhere arbitrary, because this is what every tenant's first request does.
        var first = await NextAsync("EXP");

        DigitsOf(first).ShouldBe(1);
        first.ShouldBe("EXP-000001");
    }

    [Fact]
    public async Task Numbers_Should_Be_Unique_Across_Different_Transaction_Types()
    {
        // THE point of the change. Under the old per-type generator these four calls produced EXP-...-00001,
        // EXP-...-00002, LEV-...-00001, LOAN-...-00001 -- three requests all called "1". One sequence means
        // the digits identify the request outright.
        var numbers = new List<string>
        {
            await NextAsync("EXP"),
            await NextAsync("EXP"),
            await NextAsync("LEV"),
            await NextAsync("LOAN")
        };

        numbers.Select(DigitsOf).ShouldBe(new long[] { 1, 2, 3, 4 });

        numbers.Select(DigitsOf).Distinct().Count()
            .ShouldBe(numbers.Count, "Two request types were issued the same number.");
    }

    [Fact]
    public async Task The_Type_Tag_Should_Stay_On_The_Front_Of_The_Number()
    {
        // The prefix survives as a display hint -- a bare "000042" says nothing about what it refers to.
        // It takes no part in uniqueness, which is what the test above establishes.
        var number = await NextAsync("LEV");

        number.ShouldStartWith("LEV-");
    }

    [Fact]
    public async Task A_Caller_With_No_Type_Tag_Should_Still_Get_A_Number()
    {
        // Nothing should be able to produce a number that begins with a dash, or throw, merely because a
        // caller had no tag to offer.
        var number = await NextAsync(null);

        number.ShouldBe("REQ-000001");

        var blank = await NextAsync("   ");

        blank.ShouldBe("REQ-000002");
    }

    [Fact]
    public async Task Claims_Made_Before_Any_Of_Them_Is_Committed_Should_Not_Duplicate()
    {
        // The concurrency case, reproduced in the one way a single-connection test database allows: several
        // claims inside ONE unit of work, so nothing any of them wrote has been committed when the next one
        // runs.
        //
        // This is precisely the state two simultaneous submissions are in, and precisely what the old
        // implementation got wrong -- it asked the database for the highest request number in existence,
        // which does not change until somebody commits, so every claim in this window returned the same
        // answer. The new one increments the counter row in the database itself, so each claim moves it on
        // whether or not anything has been committed.
        List<string> numbers;

        using (var uow = _unitOfWorkManager.Begin())
        {
            numbers = new List<string>();

            for (var i = 0; i < 10; i++)
            {
                numbers.Add(await _generator.NextAsync("EXP"));
            }

            await uow.CompleteAsync();
        }

        numbers.Distinct().Count().ShouldBe(10, "Two claims in flight at the same time were given the " +
                                                "same request number.");

        numbers.Select(DigitsOf).ShouldBe(new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
    }

    [Fact]
    public async Task Concurrent_Claims_Should_Not_Duplicate()
    {
        // The same property approached from the other side: claims started together rather than in a known
        // order. What comes back may be in any order -- that is the nature of it -- so the assertion is on
        // the set, which is the only thing that actually matters: ten claims, ten different numbers.
        var claims = Enumerable.Range(0, 10).Select(_ => NextAsync("EXP")).ToArray();

        var numbers = await Task.WhenAll(claims);

        numbers.Distinct().Count().ShouldBe(10, "Concurrent claims were given the same request number.");

        numbers.Select(DigitsOf).OrderBy(n => n).ShouldBe(new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
    }
}
