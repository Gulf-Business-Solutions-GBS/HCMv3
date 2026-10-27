using System.Linq;
using HCMv3.Common;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Common;

public class IdRangeCompressorTests
{
    [Fact]
    public void Should_Return_AlwaysFalse_Fragment_For_Empty_List()
    {
        var fragment = IdRangeCompressor.CompressToSqlFragment("EmployeeId", System.Array.Empty<int>());

        fragment.ShouldBe("1 = 0");
    }

    [Fact]
    public void Should_Return_AlwaysFalse_Fragment_For_Null_List()
    {
        var fragment = IdRangeCompressor.CompressToSqlFragment("EmployeeId", null);

        fragment.ShouldBe("1 = 0");
    }

    [Fact]
    public void Should_Compress_Single_Value_As_In_Clause()
    {
        var (singles, ranges) = IdRangeCompressor.Compress(new[] { 5 });

        singles.ShouldBe(new[] { 5 });
        ranges.ShouldBeEmpty();

        var fragment = IdRangeCompressor.CompressToSqlFragment("EmployeeId", new[] { 5 });
        fragment.ShouldBe("EmployeeId IN (5)");
    }

    [Fact]
    public void Should_Compress_Contiguous_Run_As_Range()
    {
        var (singles, ranges) = IdRangeCompressor.Compress(new[] { 100, 101, 102, 103, 104, 105 });

        singles.ShouldBeEmpty();
        ranges.ShouldBe(new[] { (100, 105) });

        var fragment = IdRangeCompressor.CompressToSqlFragment("EmployeeId", new[] { 100, 101, 102, 103, 104, 105 });
        fragment.ShouldBe("EmployeeId BETWEEN 100 AND 105");
    }

    [Fact]
    public void Should_Compress_Mixed_Singles_And_Ranges()
    {
        var ids = new[] { 1, 2, 3, 50, 100, 101, 102, 150, 151, 200 };

        var (singles, ranges) = IdRangeCompressor.Compress(ids);

        singles.ShouldBe(new[] { 50, 200 });
        ranges.ShouldBe(new[] { (1, 3), (100, 102), (150, 151) });

        var fragment = IdRangeCompressor.CompressToSqlFragment("EmployeeId", ids);
        fragment.ShouldBe("EmployeeId IN (50,200) OR EmployeeId BETWEEN 1 AND 3 OR EmployeeId BETWEEN 100 AND 102 OR EmployeeId BETWEEN 150 AND 151");
    }

    [Fact]
    public void Should_Dedupe_And_Sort_Before_Compressing()
    {
        // Deduped + sorted: {1, 2, 3, 5}. The 1-2-3 run compresses to a range; the standalone 5 (no
        // contiguous neighbor after dedup) is a single, not a self-range -- matching legacy
        // RLSRangeStore.Build's start==prev "single value" branch.
        var ids = new[] { 5, 3, 5, 1, 2, 3 };

        var (singles, ranges) = IdRangeCompressor.Compress(ids);

        singles.ShouldBe(new[] { 5 });
        ranges.ShouldBe(new[] { (1, 3) });
    }

    [Fact]
    public void Should_Handle_Large_Contiguous_And_Scattered_List()
    {
        var contiguous = Enumerable.Range(1000, 500).ToList(); // 1000..1499
        var scattered = new[] { 1, 3, 5, 7, 9 };
        var ids = contiguous.Concat(scattered).ToList();

        var (singles, ranges) = IdRangeCompressor.Compress(ids);

        singles.ShouldBe(new[] { 1, 3, 5, 7, 9 });
        ranges.ShouldBe(new[] { (1000, 1499) });
    }
}
