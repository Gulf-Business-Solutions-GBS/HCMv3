using System.Threading.Tasks;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class SysUserOrgUnitDataAppService_Tests : SystemSecurityAppServiceTestBase
{
    private SysUserOrgUnitDataAppService CreateAppService()
    {
        return CreateService(() => new SysUserOrgUnitDataAppService(FakeRepository.For<SysUserOrgUnitData>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_SysUserOrgUnitData()
    {
        var roleId = CreateRole();
        var orgLevelId = CreateOrganizationUnitType();
        var orgUnitId = CreateOrganizationUnit();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSysUserOrgUnitDataDto
        {
            RoleId = roleId,
            OrgLevelId = orgLevelId,
            OrganizationUnitId = orgUnitId,
            ShowUnlocked = true
        });

        var result = await service.GetAll(new GetAllSysUserOrgUnitDataInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].RoleId.ShouldBe(roleId);
        result.Items[0].OrgLevelId.ShouldBe(orgLevelId);
        result.Items[0].OrganizationUnitId.ShouldBe(orgUnitId);
        result.Items[0].ShowUnlocked.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Update_SysUserOrgUnitData()
    {
        var roleId = CreateRole();
        var orgLevelId = CreateOrganizationUnitType();
        var orgUnitId = CreateOrganizationUnit();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSysUserOrgUnitDataDto
        {
            RoleId = roleId,
            OrgLevelId = orgLevelId,
            OrganizationUnitId = orgUnitId
        });

        var created = await service.GetAll(new GetAllSysUserOrgUnitDataInput());
        var id = created.Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditSysUserOrgUnitDataDto
        {
            Id = id,
            RoleId = roleId,
            OrgLevelId = orgLevelId,
            OrganizationUnitId = orgUnitId,
            ShowUnlocked = true
        });

        var edited = await service.GetSysUserOrgUnitDataForEdit(new Abp.Application.Services.Dto.EntityDto(id));
        edited.SysUserOrgUnitData.ShowUnlocked.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Delete_SysUserOrgUnitData()
    {
        var roleId = CreateRole();
        var orgLevelId = CreateOrganizationUnitType();
        var orgUnitId = CreateOrganizationUnit();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSysUserOrgUnitDataDto { RoleId = roleId, OrgLevelId = orgLevelId, OrganizationUnitId = orgUnitId });
        var created = await service.GetAll(new GetAllSysUserOrgUnitDataInput());
        var id = created.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var result = await service.GetAll(new GetAllSysUserOrgUnitDataInput());
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Filter_By_RoleId()
    {
        var roleId1 = CreateRole("Role1");
        var roleId2 = CreateRole("Role2");
        var orgLevelId = CreateOrganizationUnitType();
        var orgUnitId1 = CreateOrganizationUnit("Unit1");
        var orgUnitId2 = CreateOrganizationUnit("Unit2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSysUserOrgUnitDataDto { RoleId = roleId1, OrgLevelId = orgLevelId, OrganizationUnitId = orgUnitId1 });
        await service.CreateOrEdit(new CreateOrEditSysUserOrgUnitDataDto { RoleId = roleId2, OrgLevelId = orgLevelId, OrganizationUnitId = orgUnitId2 });

        var result = await service.GetAll(new GetAllSysUserOrgUnitDataInput { RoleId = roleId2 });
        result.TotalCount.ShouldBe(1);
        result.Items[0].RoleId.ShouldBe(roleId2);
    }
}
