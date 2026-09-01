using System.Threading.Tasks;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Roles.Dto;
using HCMv3.Test.Base;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Authorization.Roles;

// ReSharper disable once InconsistentNaming
public class RoleAppService_Tests : AppTestBase
{
    private readonly IRoleAppService _roleAppService;

    public RoleAppService_Tests()
    {
        _roleAppService = Resolve<IRoleAppService>();
    }

    [MultiTenantFact]
    public async Task Should_Get_Roles_For_Host()
    {
        LoginAsHostAdmin();

        //Act
        var output = await _roleAppService.GetRoles(new GetRolesInput());

        //Assert
        output.Items.Count.ShouldBe(1);
    }

    /// <summary>
    /// Asserts WHICH roles a new tenant gets, not how many.
    ///
    /// <para>
    /// It used to assert a bare count of two, and that broke twice as the product grew -- once when the
    /// careers portal added Candidate, once when self-service added Employee. A count says nothing about
    /// what is missing when it fails, and tempts whoever sees it red into bumping the number without
    /// checking that the new role was meant to be there. Naming them means a genuinely unexpected role
    /// fails the test and an expected one reads as intent.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Should_Get_Roles_For_Tenant()
    {
        //Act
        var output = await _roleAppService.GetRoles(new GetRolesInput());

        //Assert
        output.Items.Select(r => r.Name).ShouldBe(
            new[]
            {
                StaticRoleNames.Tenants.Admin,
                StaticRoleNames.Tenants.User,
                StaticRoleNames.Tenants.Candidate,
                StaticRoleNames.Tenants.Employee
            },
            ignoreOrder: true);
    }
}
