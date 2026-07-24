using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class KpiByJobTitleDetailsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private KpiByJobTitleDetailsAppService CreateAppService()
    {
        return CreateService(() => new KpiByJobTitleDetailsAppService(FakeRepository.For<KpiByJobTitleDetail>(Context)));
    }

    private int CreateHeader()
    {
        var jobTitleId = CreateJobTitle("JT1");
        var entity = new KpiByJobTitle
        {
            TenantId = AbpSession.TenantId ?? 1,
            JobTitleId = jobTitleId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(9999, 12, 31)
        };
        Context.KpiByJobTitles.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Detail()
    {
        var headerId = CreateHeader();
        var kpiInfoId = CreateKpiInfo("K1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiByJobTitleDetailDto
        {
            KpiByJobTitleId = headerId,
            KpiInfoId = kpiInfoId,
            Weight = 50,
            Target = "T"
        });

        var result = await service.GetAll(new GetAllKpiByJobTitleDetailsInput { KpiByJobTitleId = headerId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Weight.ShouldBe(50);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_KpiInfo_For_Header()
    {
        var headerId = CreateHeader();
        var kpiInfoId = CreateKpiInfo("K1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiByJobTitleDetailDto { KpiByJobTitleId = headerId, KpiInfoId = kpiInfoId, Weight = 30 });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditKpiByJobTitleDetailDto { KpiByJobTitleId = headerId, KpiInfoId = kpiInfoId, Weight = 20 }));
    }

    [Fact]
    public async Task Should_Throw_When_TotalWeight_Exceeds_100()
    {
        var headerId = CreateHeader();
        var kpiInfo1 = CreateKpiInfo("K1");
        var kpiInfo2 = CreateKpiInfo("K2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiByJobTitleDetailDto { KpiByJobTitleId = headerId, KpiInfoId = kpiInfo1, Weight = 70 });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditKpiByJobTitleDetailDto { KpiByJobTitleId = headerId, KpiInfoId = kpiInfo2, Weight = 40 }));
    }

    [Fact]
    public async Task Should_Allow_TotalWeight_Equal_100()
    {
        var headerId = CreateHeader();
        var kpiInfo1 = CreateKpiInfo("K1");
        var kpiInfo2 = CreateKpiInfo("K2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiByJobTitleDetailDto { KpiByJobTitleId = headerId, KpiInfoId = kpiInfo1, Weight = 60 });
        await service.CreateOrEdit(new CreateOrEditKpiByJobTitleDetailDto { KpiByJobTitleId = headerId, KpiInfoId = kpiInfo2, Weight = 40 });

        var result = await service.GetAll(new GetAllKpiByJobTitleDetailsInput { KpiByJobTitleId = headerId });
        result.TotalCount.ShouldBe(2);
    }
}
