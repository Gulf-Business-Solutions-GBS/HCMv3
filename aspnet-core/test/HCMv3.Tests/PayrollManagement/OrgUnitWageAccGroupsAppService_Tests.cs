using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class OrgUnitWageAccGroupsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private OrgUnitWageAccGroupsAppService CreateAppService()
    {
        return CreateService(() => new OrgUnitWageAccGroupsAppService(FakeRepository.For<OrgUnitWageAccGroup>(Context)));
    }

    private int CreateWageAccGroup(string code)
    {
        var entity = new WageAccGroup { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.WageAccGroups.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private long CreateOrganizationUnit(string displayName)
    {
        var entity = new Abp.Organizations.OrganizationUnit(AbpSession.TenantId, displayName, null)
        {
            Code = displayName
        };
        Context.OrganizationUnits.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_OrgUnitWageAccGroup_With_Infinite_EndDate()
    {
        var wageAccGroupId = CreateWageAccGroup("WAG10");
        var organizationUnitId = CreateOrganizationUnit("Org1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditOrgUnitWageAccGroupDto
        {
            OrganizationUnitId = organizationUnitId,
            StartDate = new DateTime(2025, 1, 1),
            WageAccGroupId = wageAccGroupId
        });

        var result = await service.GetAll(new GetAllOrgUnitWageAccGroupsInput { OrganizationUnitId = organizationUnitId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EndDate.ShouldBe(InfiniteEndDate);
    }

    [Fact]
    public async Task Should_Auto_Chain_EndDate_When_New_Record_Added()
    {
        var wageAccGroupId1 = CreateWageAccGroup("WAG11");
        var wageAccGroupId2 = CreateWageAccGroup("WAG12");
        var organizationUnitId = CreateOrganizationUnit("Org2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditOrgUnitWageAccGroupDto
        {
            OrganizationUnitId = organizationUnitId,
            StartDate = new DateTime(2025, 1, 1),
            WageAccGroupId = wageAccGroupId1
        });

        await service.CreateOrEdit(new CreateOrEditOrgUnitWageAccGroupDto
        {
            OrganizationUnitId = organizationUnitId,
            StartDate = new DateTime(2025, 6, 1),
            WageAccGroupId = wageAccGroupId2
        });

        var records = Context.OrgUnitWageAccGroups
            .Where(g => g.OrganizationUnitId == organizationUnitId)
            .OrderBy(g => g.StartDate)
            .ToList();

        records.Count.ShouldBe(2);
        records[0].EndDate.ShouldBe(new DateTime(2025, 5, 31));
        records[1].EndDate.ShouldBe(InfiniteEndDate);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Triple()
    {
        var wageAccGroupId = CreateWageAccGroup("WAG13");
        var organizationUnitId = CreateOrganizationUnit("Org3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditOrgUnitWageAccGroupDto
        {
            OrganizationUnitId = organizationUnitId,
            StartDate = new DateTime(2025, 1, 1),
            WageAccGroupId = wageAccGroupId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditOrgUnitWageAccGroupDto
            {
                OrganizationUnitId = organizationUnitId,
                StartDate = new DateTime(2025, 1, 1),
                WageAccGroupId = wageAccGroupId
            }));
    }
}
