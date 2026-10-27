using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PAQuarter3ReviewsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private PAQuarter3ReviewsAppService CreateAppService()
    {
        return CreateService(() => new PAQuarter3ReviewsAppService(FakeRepository.For<PAQuarter3Review>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Quarter3Review()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var paFormId = CreatePAForm(periodId, employeeId);
        var kpiInfoId = CreateKpiInfo("K1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAQuarter3ReviewDto
        {
            PAFormId = paFormId,
            KpiInfoId = kpiInfoId,
            Weight = 50,
            Target = "T"
        });

        var result = await service.GetAll(new GetAllPAQuarter3ReviewsInput { PAFormId = paFormId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Weight.ShouldBe(50);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_KpiInfo_For_PAForm()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var paFormId = CreatePAForm(periodId, employeeId);
        var kpiInfoId = CreateKpiInfo("K1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAQuarter3ReviewDto { PAFormId = paFormId, KpiInfoId = kpiInfoId, Weight = 30 });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPAQuarter3ReviewDto { PAFormId = paFormId, KpiInfoId = kpiInfoId, Weight = 20 }));
    }

    [Fact]
    public async Task Should_Delete_Quarter3Review()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var paFormId = CreatePAForm(periodId, employeeId);
        var kpiInfoId = CreateKpiInfo("K1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAQuarter3ReviewDto { PAFormId = paFormId, KpiInfoId = kpiInfoId, Weight = 30 });

        var all = await service.GetAll(new GetAllPAQuarter3ReviewsInput { PAFormId = paFormId });
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllPAQuarter3ReviewsInput { PAFormId = paFormId });
        afterDelete.TotalCount.ShouldBe(0);
    }
}
