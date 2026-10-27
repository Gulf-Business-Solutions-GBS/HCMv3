using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class KpiByOrgUnitsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private KpiByOrgUnitsAppService CreateAppService()
    {
        return CreateService(() => new KpiByOrgUnitsAppService(
            FakeRepository.For<KpiByOrgUnit>(Context),
            FakeRepository.For<KpiByOrgUnitDetail>(Context)));
    }

    [Fact]
    public async Task Should_Create_First_Record_With_Infinite_EndDate()
    {
        var orgUnitId = CreateOrganizationUnit("Org1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiByOrgUnitDto
        {
            OrganizationUnitId = orgUnitId,
            StartDate = new DateTime(2026, 1, 1)
        });

        var result = await service.GetAll(new GetAllKpiByOrgUnitsInput { OrganizationUnitId = orgUnitId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EndDate.ShouldBe(InfiniteEndDate);
    }

    [Fact]
    public async Task Should_Chain_EndDates_When_Second_Record_Added()
    {
        var orgUnitId = CreateOrganizationUnit("Org1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiByOrgUnitDto { OrganizationUnitId = orgUnitId, StartDate = new DateTime(2026, 1, 1) });
        await service.CreateOrEdit(new CreateOrEditKpiByOrgUnitDto { OrganizationUnitId = orgUnitId, StartDate = new DateTime(2026, 6, 1) });

        var result = await service.GetAll(new GetAllKpiByOrgUnitsInput { OrganizationUnitId = orgUnitId });
        result.TotalCount.ShouldBe(2);

        result.Items[0].EndDate.ShouldBe(new DateTime(2026, 5, 31));
        result.Items[1].EndDate.ShouldBe(InfiniteEndDate);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_StartDate_For_Same_OrgUnit()
    {
        var orgUnitId = CreateOrganizationUnit("Org1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiByOrgUnitDto { OrganizationUnitId = orgUnitId, StartDate = new DateTime(2026, 1, 1) });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditKpiByOrgUnitDto { OrganizationUnitId = orgUnitId, StartDate = new DateTime(2026, 1, 1) }));
    }
}
