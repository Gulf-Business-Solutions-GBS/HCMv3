using System.Threading.Tasks;
using HCMv3.Schemas;
using Xunit;

namespace HCMv3.GraphQL.Tests.Roles;

// ReSharper disable once InconsistentNaming
public class RoleQuery_Tests : GraphQLTestBase<MainSchema>
{
    [Fact]
    public async Task Should_Get_Roles()
    {
        LoginAsDefaultTenantAdmin();

        const string query = @"
             query MyQuery {
                roles {
                  id
                  displayName
                }
             }";


        const string expectedResult = "{\"data\": { \"roles\": [ { \"id\": 2, \"displayName\": \"Admin\" }, { \"id\": 3, \"displayName\": \"User\" } ]}}";

        await AssertQuerySuccessAsync(query, expectedResult);
    }
}
