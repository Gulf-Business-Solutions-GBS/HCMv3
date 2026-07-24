using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PAEmployeeReviewersAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private PAEmployeeReviewersAppService CreateAppService()
    {
        return CreateService(() => new PAEmployeeReviewersAppService(
            FakeRepository.For<PAEmployeeReviewer>(Context),
            FakeRepository.For<PAEmployee>(Context),
            FakeRepository.For<FormType>(Context),
            FakeRepository.For<PAEmployeeDetail>(Context),
            FakeRepository.For<PAEmployeeRating>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Reviewer()
    {
        var employeeId = CreateEmployee();
        var reviewerId = CreateEmployee("R", "V");
        var cycleId = CreatePACycle("C1");
        var formTypeId = CreateFormType();
        var paEmployeeId = CreatePAEmployee(employeeId, cycleId, formTypeId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAEmployeeReviewerDto
        {
            PAEmployeeId = paEmployeeId,
            EmployeeId = reviewerId,
            IsPeer = true
        });

        var result = await service.GetAll(new GetAllPAEmployeeReviewersInput { PAEmployeeId = paEmployeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(reviewerId);
    }

    [Fact]
    public async Task Should_Throw_When_Person_Already_Reviewer()
    {
        var employeeId = CreateEmployee();
        var reviewerId = CreateEmployee("R", "V");
        var cycleId = CreatePACycle("C1");
        var formTypeId = CreateFormType();
        var paEmployeeId = CreatePAEmployee(employeeId, cycleId, formTypeId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAEmployeeReviewerDto { PAEmployeeId = paEmployeeId, EmployeeId = reviewerId });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPAEmployeeReviewerDto { PAEmployeeId = paEmployeeId, EmployeeId = reviewerId }));
    }

    [Fact]
    public async Task Should_Throw_When_MaxReviewerLimit_Reached()
    {
        var employeeId = CreateEmployee();
        var reviewer1 = CreateEmployee("R1", "V1");
        var reviewer2 = CreateEmployee("R2", "V2");
        var cycleId = CreatePACycle("C1");
        var formTypeId = CreateFormType(maxReviewerLimit: 1);
        var paEmployeeId = CreatePAEmployee(employeeId, cycleId, formTypeId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAEmployeeReviewerDto { PAEmployeeId = paEmployeeId, EmployeeId = reviewer1 });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPAEmployeeReviewerDto { PAEmployeeId = paEmployeeId, EmployeeId = reviewer2 }));
    }

    [Fact]
    public async Task Should_AutoCreate_Ratings_For_Existing_Details_On_Create()
    {
        var employeeId = CreateEmployee();
        var reviewerId = CreateEmployee("R", "V");
        var cycleId = CreatePACycle("C1");
        var formTypeId = CreateFormType();
        var paEmployeeId = CreatePAEmployee(employeeId, cycleId, formTypeId);
        var kpiInfoId = CreateKpiInfo("K1");

        var detail = new PAEmployeeDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            PAEmployeeId = paEmployeeId,
            KpiInfoId = kpiInfoId,
            Weight = 100
        };
        Context.PAEmployeeDetails.Add(detail);
        Context.SaveChanges();

        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditPAEmployeeReviewerDto { PAEmployeeId = paEmployeeId, EmployeeId = reviewerId });

        var reviewers = await service.GetAll(new GetAllPAEmployeeReviewersInput { PAEmployeeId = paEmployeeId });
        var reviewerRecordId = reviewers.Items[0].Id;

        Context.PAEmployeeRatings.Any(r => r.PAEmployeeReviewerId == reviewerRecordId && r.PAEmployeeDetailId == detail.Id)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Throw_When_Deleting_NonPeer_Reviewer()
    {
        var employeeId = CreateEmployee();
        var reviewerId = CreateEmployee("R", "V");
        var cycleId = CreatePACycle("C1");
        var formTypeId = CreateFormType();
        var paEmployeeId = CreatePAEmployee(employeeId, cycleId, formTypeId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAEmployeeReviewerDto { PAEmployeeId = paEmployeeId, EmployeeId = reviewerId, IsPeer = false });

        var all = await service.GetAll(new GetAllPAEmployeeReviewersInput { PAEmployeeId = paEmployeeId });
        var id = all.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.Delete(new Abp.Application.Services.Dto.EntityDto(id)));
    }

    [Fact]
    public async Task Should_Delete_Peer_Reviewer()
    {
        var employeeId = CreateEmployee();
        var reviewerId = CreateEmployee("R", "V");
        var cycleId = CreatePACycle("C1");
        var formTypeId = CreateFormType();
        var paEmployeeId = CreatePAEmployee(employeeId, cycleId, formTypeId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAEmployeeReviewerDto { PAEmployeeId = paEmployeeId, EmployeeId = reviewerId, IsPeer = true });

        var all = await service.GetAll(new GetAllPAEmployeeReviewersInput { PAEmployeeId = paEmployeeId });
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllPAEmployeeReviewersInput { PAEmployeeId = paEmployeeId });
        afterDelete.TotalCount.ShouldBe(0);
    }
}
