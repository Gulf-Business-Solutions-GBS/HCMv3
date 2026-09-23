using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PAPeerNomineesAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _settings =
        NSubstitute.Substitute.For<HCMv3.SystemSettings.Services.ISysSettingsAccessor>();

    private PAPeerNomineesAppService CreateAppService(bool peerReview = false, int peerMax = 0)
    {
        NSubstitute.SubstituteExtensions.Returns(_settings.GetAppraisalSettingsAsync(),
            Task.FromResult(new HCMv3.SystemSettings.ManageAppraisalSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                EnablePAPeerReview = peerReview,
                SetPeerMaxLimit = peerMax
            }));

        return CreateService(() => new PAPeerNomineesAppService(FakeRepository.For<PAPeerNominee>(Context), _settings));
    }

    /// <summary>Legacy ESS HasReachedPeerReviewerLimit: no more peers than SetPeerMaxLimit per employee and period.</summary>
    [Fact]
    public async Task With_Peer_Review_On_Should_Refuse_A_Nominee_Beyond_The_Maximum()
    {
        var periodId = CreatePAPeriod("2029");
        var employeeId = CreateEmployee();
        var firstPeer = CreateEmployee("P1", "R");
        var secondPeer = CreateEmployee("P2", "R");
        var service = CreateAppService(peerReview: true, peerMax: 1);

        await service.CreateOrEdit(new CreateOrEditPAPeerNomineeDto { PAPeriodId = periodId, EmployeeId = employeeId, PeerId = firstPeer });

        var exception = await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPAPeerNomineeDto { PAPeriodId = periodId, EmployeeId = employeeId, PeerId = secondPeer }));
        exception.Message.ShouldContain("maximum");
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Nominee()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var peerId = CreateEmployee("P", "R");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAPeerNomineeDto
        {
            PAPeriodId = periodId,
            EmployeeId = employeeId,
            PeerId = peerId
        });

        var result = await service.GetAll(new GetAllPAPeerNomineesInput { PAPeriodId = periodId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].PeerId.ShouldBe(peerId);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Nominee()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var peerId = CreateEmployee("P", "R");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAPeerNomineeDto { PAPeriodId = periodId, EmployeeId = employeeId, PeerId = peerId });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPAPeerNomineeDto { PAPeriodId = periodId, EmployeeId = employeeId, PeerId = peerId }));
    }

    [Fact]
    public async Task Should_Delete_Nominee()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var peerId = CreateEmployee("P", "R");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAPeerNomineeDto { PAPeriodId = periodId, EmployeeId = employeeId, PeerId = peerId });

        var all = await service.GetAll(new GetAllPAPeerNomineesInput { PAPeriodId = periodId });
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllPAPeerNomineesInput { PAPeriodId = periodId });
        afterDelete.TotalCount.ShouldBe(0);
    }
}
