using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PAEmployeeDetailsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private PAEmployeeDetailsAppService CreateAppService()
    {
        return CreateService(() => new PAEmployeeDetailsAppService(
            FakeRepository.For<PAEmployeeDetail>(Context),
            FakeRepository.For<PAEmployeeReviewer>(Context),
            FakeRepository.For<PAEmployeeRating>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Detail()
    {
        var employeeId = CreateEmployee();
        var cycleId = CreatePACycle("C1");
        var formTypeId = CreateFormType();
        var paEmployeeId = CreatePAEmployee(employeeId, cycleId, formTypeId);
        var kpiInfoId = CreateKpiInfo("K1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAEmployeeDetailDto
        {
            PAEmployeeId = paEmployeeId,
            KpiInfoId = kpiInfoId,
            Weight = 50,
            Target = "T"
        });

        var result = await service.GetAll(new GetAllPAEmployeeDetailsInput { PAEmployeeId = paEmployeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Weight.ShouldBe(50);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_KpiInfo_For_PAEmployee()
    {
        var employeeId = CreateEmployee();
        var cycleId = CreatePACycle("C1");
        var formTypeId = CreateFormType();
        var paEmployeeId = CreatePAEmployee(employeeId, cycleId, formTypeId);
        var kpiInfoId = CreateKpiInfo("K1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAEmployeeDetailDto { PAEmployeeId = paEmployeeId, KpiInfoId = kpiInfoId, Weight = 30 });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPAEmployeeDetailDto { PAEmployeeId = paEmployeeId, KpiInfoId = kpiInfoId, Weight = 20 }));
    }

    [Fact]
    public async Task Should_AutoCreate_Ratings_For_Existing_Reviewers_On_Create()
    {
        var employeeId = CreateEmployee();
        var reviewerEmployeeId = CreateEmployee("R", "V");
        var cycleId = CreatePACycle("C1");
        var formTypeId = CreateFormType();
        var paEmployeeId = CreatePAEmployee(employeeId, cycleId, formTypeId);
        var kpiInfoId = CreateKpiInfo("K1");

        var reviewer = new PAEmployeeReviewer
        {
            TenantId = AbpSession.TenantId ?? 1,
            PAEmployeeId = paEmployeeId,
            EmployeeId = reviewerEmployeeId
        };
        Context.PAEmployeeReviewers.Add(reviewer);
        Context.SaveChanges();

        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAEmployeeDetailDto { PAEmployeeId = paEmployeeId, KpiInfoId = kpiInfoId, Weight = 40 });

        var ratings = Context.PAEmployeeRatings.Where(r => r.PAEmployeeReviewerId == reviewer.Id).ToList();
        ratings.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Delete_Detail_And_Related_Ratings()
    {
        var employeeId = CreateEmployee();
        var reviewerEmployeeId = CreateEmployee("R", "V");
        var cycleId = CreatePACycle("C1");
        var formTypeId = CreateFormType();
        var paEmployeeId = CreatePAEmployee(employeeId, cycleId, formTypeId);
        var kpiInfoId = CreateKpiInfo("K1");

        var reviewer = new PAEmployeeReviewer
        {
            TenantId = AbpSession.TenantId ?? 1,
            PAEmployeeId = paEmployeeId,
            EmployeeId = reviewerEmployeeId
        };
        Context.PAEmployeeReviewers.Add(reviewer);
        Context.SaveChanges();

        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditPAEmployeeDetailDto { PAEmployeeId = paEmployeeId, KpiInfoId = kpiInfoId, Weight = 40 });

        var all = await service.GetAll(new GetAllPAEmployeeDetailsInput { PAEmployeeId = paEmployeeId });
        var detailId = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(detailId));

        Context.PAEmployeeRatings.Any(r => r.PAEmployeeDetailId == detailId).ShouldBeFalse();
        var afterDelete = await service.GetAll(new GetAllPAEmployeeDetailsInput { PAEmployeeId = paEmployeeId });
        afterDelete.TotalCount.ShouldBe(0);
    }
}
