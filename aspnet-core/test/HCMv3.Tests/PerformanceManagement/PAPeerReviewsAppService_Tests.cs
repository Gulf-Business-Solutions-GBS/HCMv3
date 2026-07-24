using System.Threading.Tasks;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PAPeerReviewsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private PAPeerReviewsAppService CreateAppService()
    {
        return CreateService(() => new PAPeerReviewsAppService(
            FakeRepository.For<PAPeerReview>(Context),
            FakeRepository.For<PAForm>(Context),
            FakeRepository.For<PAPeerNominee>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_PeerReview()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var peerId = CreateEmployee("P", "R");
        var paFormId = CreatePAForm(periodId, employeeId);
        var kpiInfoId = CreateKpiInfo("K1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAPeerReviewDto
        {
            PAFormId = paFormId,
            PeerId = peerId,
            KpiInfoId = kpiInfoId,
            Comments = "Good"
        });

        var result = await service.GetAll(new GetAllPAPeerReviewsInput { PAFormId = paFormId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].PeerId.ShouldBe(peerId);
    }

    [Fact]
    public async Task Should_Reflect_Submitted_From_Matching_PeerNominee()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var peerId = CreateEmployee("P", "R");
        var paFormId = CreatePAForm(periodId, employeeId);
        var kpiInfoId = CreateKpiInfo("K1");

        Context.PAPeerNominees.Add(new PAPeerNominee
        {
            TenantId = AbpSession.TenantId ?? 1,
            PAPeriodId = periodId,
            EmployeeId = employeeId,
            PeerId = peerId,
            Submitted = true
        });
        Context.SaveChanges();

        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditPAPeerReviewDto { PAFormId = paFormId, PeerId = peerId, KpiInfoId = kpiInfoId });

        var result = await service.GetAll(new GetAllPAPeerReviewsInput { PAFormId = paFormId });
        result.Items[0].Submitted.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Delete_PeerReview()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var peerId = CreateEmployee("P", "R");
        var paFormId = CreatePAForm(periodId, employeeId);
        var kpiInfoId = CreateKpiInfo("K1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAPeerReviewDto { PAFormId = paFormId, PeerId = peerId, KpiInfoId = kpiInfoId });

        var all = await service.GetAll(new GetAllPAPeerReviewsInput { PAFormId = paFormId });
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllPAPeerReviewsInput { PAFormId = paFormId });
        afterDelete.TotalCount.ShouldBe(0);
    }
}
