using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PAFormsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _settings =
        NSubstitute.Substitute.For<HCMv3.SystemSettings.Services.ISysSettingsAccessor>();

    private PAFormsAppService CreateAppService(bool peerReview = false, int peerMin = 0, int peerMax = 0)
    {
        NSubstitute.SubstituteExtensions.Returns(_settings.GetAppraisalSettingsAsync(),
            Task.FromResult(new HCMv3.SystemSettings.ManageAppraisalSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                EnablePAPeerReview = peerReview,
                SetPeerMinLimit = peerMin,
                SetPeerMaxLimit = peerMax
            }));

        return CreateService(() => new PAFormsAppService(
            FakeRepository.For<PAForm>(Context),
            FakeRepository.For<PAPeerNominee>(Context),
            _settings));
    }

    private void NominatePeer(int periodId, int employeeId, int peerId)
    {
        Context.PAPeerNominees.Add(new PAPeerNominee
        {
            TenantId = AbpSession.TenantId ?? 1,
            PAPeriodId = periodId,
            EmployeeId = employeeId,
            PeerId = peerId
        });
        Context.SaveChanges();
    }

    /// <summary>Legacy PAForm.PerformValidation: with peer review on, the nominee count must sit within the limits.</summary>
    [Fact]
    public async Task With_Peer_Review_On_Should_Require_The_Configured_Number_Of_Peer_Nominees()
    {
        var periodId = CreatePAPeriod("2027");
        var employeeId = CreateEmployee();
        var peerId = CreateEmployee("P", "Q");
        var service = CreateAppService(peerReview: true, peerMin: 1, peerMax: 2);

        var exception = await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPAFormDto { PAPeriodId = periodId, EmployeeId = employeeId }));
        exception.Message.ShouldContain("between 1 and 2");

        NominatePeer(periodId, employeeId, peerId);
        await service.CreateOrEdit(new CreateOrEditPAFormDto { PAPeriodId = periodId, EmployeeId = employeeId });

        (await service.GetAll(new GetAllPAFormsInput())).TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task With_Peer_Review_Off_Should_Not_Look_At_Peer_Nominees()
    {
        var periodId = CreatePAPeriod("2028");
        var employeeId = CreateEmployee();
        var service = CreateAppService(peerReview: false, peerMin: 3, peerMax: 5);

        await service.CreateOrEdit(new CreateOrEditPAFormDto { PAPeriodId = periodId, EmployeeId = employeeId });

        (await service.GetAll(new GetAllPAFormsInput())).TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetAppraisalFormSettings_Should_Return_The_Tenant_Appraisal_Settings()
    {
        var service = CreateAppService(peerReview: true, peerMin: 2, peerMax: 4);

        var settings = await service.GetAppraisalFormSettings();

        settings.EnablePAPeerReview.ShouldBeTrue();
        settings.SetPeerMinLimit.ShouldBe(2);
        settings.SetPeerMaxLimit.ShouldBe(4);
        settings.AppraisalCycle.ShouldBe(HCMv3.SystemSettings.AppraisalCycle.Quarterly);
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_PAForm()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAFormDto
        {
            PAPeriodId = periodId,
            EmployeeId = employeeId,
            Comments = "C"
        });

        var result = await service.GetAll(new GetAllPAFormsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].PAPeriodId.ShouldBe(periodId);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Period_And_Employee()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAFormDto { PAPeriodId = periodId, EmployeeId = employeeId });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPAFormDto { PAPeriodId = periodId, EmployeeId = employeeId }));
    }

    [Fact]
    public async Task Should_Delete_PAForm()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAFormDto { PAPeriodId = periodId, EmployeeId = employeeId });

        var all = await service.GetAll(new GetAllPAFormsInput());
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllPAFormsInput());
        afterDelete.TotalCount.ShouldBe(0);
    }
}
