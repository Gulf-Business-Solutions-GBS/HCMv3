using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using HCMv3.PerformanceManagement.Review;
using HCMv3.PersonnelAdministration.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

/// <summary>
/// The admin reviewer grid now applies the employee's peer allowance through the shared reviewer rules,
/// and -- as legacy did -- records the signed-in employee as the one adding a reviewer when nobody is named.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PAEmployeeReviewersAppService_PeerAllowance_Tests : PerformanceManagementAppServiceTestBase
{
    private readonly IAppraisalReviewerManager _reviewerManager = Substitute.For<IAppraisalReviewerManager>();
    private readonly IAppraisalStepSettingResolver _stepSettingResolver = Substitute.For<IAppraisalStepSettingResolver>();
    private readonly IEmployeeScopeProvider _scope = Substitute.For<IEmployeeScopeProvider>();

    private PAEmployeeReviewersAppService CreateAppService()
    {
        var service = CreateService(() => new PAEmployeeReviewersAppService(
            FakeRepository.For<PAEmployeeReviewer>(Context),
            FakeRepository.For<PAEmployee>(Context),
            FakeRepository.For<FormType>(Context),
            FakeRepository.For<PAEmployeeDetail>(Context),
            FakeRepository.For<PAEmployeeRating>(Context)));

        service.ReviewerManager = _reviewerManager;
        service.StepSettingResolver = _stepSettingResolver;
        service.EmployeeScopeProvider = _scope;

        return service;
    }

    [Fact]
    public async Task A_New_Reviewer_Should_Be_Recorded_As_Added_By_The_Signed_In_Employee()
    {
        var employeeId = CreateEmployee();
        var peerId = CreateEmployee("P", "Q");
        var paEmployeeId = CreatePAEmployee(employeeId, CreatePACycle("C1"), CreateFormType());
        _scope.GetCurrentEmployeeIdAsync().Returns(Task.FromResult((int?)employeeId));

        await CreateAppService().CreateOrEdit(new CreateOrEditPAEmployeeReviewerDto { PAEmployeeId = paEmployeeId, EmployeeId = peerId, IsPeer = true });

        Context.PAEmployeeReviewers.Single().PeerAddedById.ShouldBe(employeeId);
    }

    [Fact]
    public async Task The_Shared_Rules_Should_Be_Able_To_Refuse_The_Save()
    {
        var employeeId = CreateEmployee();
        var paEmployeeId = CreatePAEmployee(employeeId, CreatePACycle("C1"), CreateFormType());
        _reviewerManager.ValidateAsync(Arg.Any<PAEmployeeReviewer>(), Arg.Any<PAEmployee>(), Arg.Any<AppraisalStepSetting>())
            .ThrowsAsync(new UserFriendlyException("peer allowance"));

        var exception = await Should.ThrowAsync<UserFriendlyException>(() => CreateAppService().CreateOrEdit(
            new CreateOrEditPAEmployeeReviewerDto { PAEmployeeId = paEmployeeId, EmployeeId = CreateEmployee("P", "Q"), IsPeer = true }));

        exception.Message.ShouldBe("peer allowance");
        Context.PAEmployeeReviewers.Count().ShouldBe(0);
    }
}
