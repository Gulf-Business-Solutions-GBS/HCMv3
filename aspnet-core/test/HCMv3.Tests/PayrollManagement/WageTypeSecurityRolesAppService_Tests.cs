using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Authorization.Roles;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class WageTypeSecurityRolesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private WageTypeSecurityRolesAppService CreateAppService()
    {
        return CreateService(() => new WageTypeSecurityRolesAppService(FakeRepository.For<WageTypeSecurityRole>(Context)));
    }

    private int CreateWageType(string code)
    {
        var entity = new WageType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            PayrollOrder = 1
        };
        Context.WageTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateRole(string name)
    {
        var entity = new Role(AbpSession.TenantId, name, name);
        Context.Roles.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_WageTypeSecurityRole()
    {
        var wageTypeId = CreateWageType("WTS1");
        var roleId = CreateRole("PayrollAdmin1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWageTypeSecurityRoleDto
        {
            WageTypeId = wageTypeId,
            RoleId = roleId
        });

        var result = await service.GetAll(new GetAllWageTypeSecurityRolesInput { WageTypeId = wageTypeId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Pair()
    {
        var wageTypeId = CreateWageType("WTS2");
        var roleId = CreateRole("PayrollAdmin2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWageTypeSecurityRoleDto
        {
            WageTypeId = wageTypeId,
            RoleId = roleId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditWageTypeSecurityRoleDto
            {
                WageTypeId = wageTypeId,
                RoleId = roleId
            }));
    }

    [Fact]
    public async Task Should_Filter_Security_Roles_By_Role_Name()
    {
        var wageTypeId = CreateWageType("WTS3");
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditWageTypeSecurityRoleDto { WageTypeId = wageTypeId, RoleId = CreateRole("PayrollClerk3") });
        await service.CreateOrEdit(new CreateOrEditWageTypeSecurityRoleDto { WageTypeId = wageTypeId, RoleId = CreateRole("HrManager3") });

        var result = await service.GetAll(new GetAllWageTypeSecurityRolesInput { WageTypeId = wageTypeId, Filter = "Clerk" });

        result.TotalCount.ShouldBe(1);
    }
}
