using System.Threading.Tasks;
using HCMv3.Common;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Common;

public class RawSqlExecutorTests : AppTestBase
{
    private readonly IRawSqlExecutor _rawSqlExecutor;

    public RawSqlExecutorTests()
    {
        LoginAsDefaultTenantAdmin();
        _rawSqlExecutor = Resolve<IRawSqlExecutor>();
    }

    [Fact]
    public async Task Should_ExecuteNonQuery_And_Then_GetData_Reflect_The_Change()
    {
        await _rawSqlExecutor.ExecuteNonQueryAsync(
            "CREATE TABLE IF NOT EXISTS RawSqlExecutorTest (Id INTEGER PRIMARY KEY, Name TEXT)");

        await _rawSqlExecutor.ExecuteNonQueryAsync(
            "INSERT INTO RawSqlExecutorTest (Id, Name) VALUES (1, 'Alpha')");

        var dataTable = await _rawSqlExecutor.GetDataAsync("SELECT Id, Name FROM RawSqlExecutorTest");

        dataTable.Rows.Count.ShouldBe(1);
        dataTable.Rows[0]["Id"].ShouldBe(1L);
        dataTable.Rows[0]["Name"].ShouldBe("Alpha");
    }

    [Fact]
    public async Task GetDataAsync_Should_Return_Empty_DataTable_On_Invalid_Sql()
    {
        var dataTable = await _rawSqlExecutor.GetDataAsync("SELECT * FROM ThisTableDoesNotExist");

        dataTable.Rows.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_Should_Not_Throw_On_Invalid_Sql()
    {
        // Matches legacy SysService.ExecuteNonQuery: exceptions are swallowed, never surfaced to the caller.
        await Should.NotThrowAsync(async () =>
            await _rawSqlExecutor.ExecuteNonQueryAsync("THIS IS NOT VALID SQL"));
    }

    [Fact]
    public async Task Should_NoOp_On_Empty_Sql()
    {
        await Should.NotThrowAsync(async () => await _rawSqlExecutor.ExecuteNonQueryAsync(""));

        var dataTable = await _rawSqlExecutor.GetDataAsync("");
        dataTable.Rows.Count.ShouldBe(0);
    }
}
