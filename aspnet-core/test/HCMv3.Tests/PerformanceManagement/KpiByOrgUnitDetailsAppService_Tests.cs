using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class KpiByOrgUnitDetailsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private KpiByOrgUnitDetailsAppService CreateAppService()
    {
        return CreateService(() => new KpiByOrgUnitDetailsAppService(FakeRepository.For<KpiByOrgUnitDetail>(Context)));
    }

    private int CreateHeader()
    {
        var orgUnitId = CreateOrganizationUnit("Org1");
        var entity = new KpiByOrgUnit
        {
            TenantId = AbpSession.TenantId ?? 1,
            OrganizationUnitId = orgUnitId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(9999, 12, 31)
        };
        Context.KpiByOrgUnits.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Detail()
    {
        var headerId = CreateHeader();
        var kpiInfoId = CreateKpiInfo("K1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiByOrgUnitDetailDto
        {
            KpiByOrgUnitId = headerId,
            KpiInfoId = kpiInfoId,
            Weight = 50,
            Target = "T"
        });

        var result = await service.GetAll(new GetAllKpiByOrgUnitDetailsInput { KpiByOrgUnitId = headerId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Weight.ShouldBe(50);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_KpiInfo_For_Header()
    {
        var headerId = CreateHeader();
        var kpiInfoId = CreateKpiInfo("K1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiByOrgUnitDetailDto { KpiByOrgUnitId = headerId, KpiInfoId = kpiInfoId, Weight = 30 });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditKpiByOrgUnitDetailDto { KpiByOrgUnitId = headerId, KpiInfoId = kpiInfoId, Weight = 20 }));
    }

    [Fact]
    public async Task Should_Throw_When_TotalWeight_Exceeds_100()
    {
        var headerId = CreateHeader();
        var kpiInfo1 = CreateKpiInfo("K1");
        var kpiInfo2 = CreateKpiInfo("K2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiByOrgUnitDetailDto { KpiByOrgUnitId = headerId, KpiInfoId = kpiInfo1, Weight = 70 });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditKpiByOrgUnitDetailDto { KpiByOrgUnitId = headerId, KpiInfoId = kpiInfo2, Weight = 40 }));
    }
}
