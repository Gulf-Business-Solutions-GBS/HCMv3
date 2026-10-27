using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement.Review;

/// <summary>
/// The appraisal handler's side of "approvers assigned": it reads the step's settings and adds that step's
/// reviewers -- at assignment, not at approval, as legacy did.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AppraisalWorkflowTransactionHandler_OnApproversAssigned_Tests : AppraisalReviewTestBase
{
    private AppraisalWorkflowTransactionHandler CreateHandler()
    {
        var unitOfWork = Substitute.For<IUnitOfWorkManager>();
        unitOfWork.Current.Returns(Substitute.For<IActiveUnitOfWork>());

        return Wire(new AppraisalWorkflowTransactionHandler(
            ReviewFakeRepository.For<PAEmployee>(Context),
            ReviewFakeRepository.For<PACycle>(Context),
            ReviewFakeRepository.For<FormType>(Context),
            CreateStepSettingResolver(),
            CreateReviewerManager(),
            unitOfWork));
    }

    [Fact]
    public async Task Should_Add_The_Steps_Reviewers_When_Its_Approvers_Are_Assigned()
    {
        var employee = CreateEmployee("E1");
        var approver = CreateEmployee("A1");
        var appraisal = CreateAppraisal(employee);
        var master = CreateRequestMaster(1, 2);
        CreateStepSetting(master, 2, s =>
        {
            s.CanEmployeeSelfRate = true;
            s.CanAddRatings = true;
        });
        var request = Route(appraisal, step: 2);

        await CreateHandler().OnApproversAssignedAsync(new WorkflowTransactionContext(request, employee), 2, new[] { approver });

        ReviewersOf(appraisal).Select(r => r.EmployeeId).ShouldBe(new[] { employee, approver });
    }

    [Fact]
    public async Task Should_Read_The_Settings_Of_The_Step_Being_Assigned()
    {
        // The step-1 settings would add the employee; the step being assigned is 2, which adds nobody.
        var employee = CreateEmployee("E1");
        var appraisal = CreateAppraisal(employee);
        var master = CreateRequestMaster(1, 2);
        CreateStepSetting(master, 1, s => s.CanEmployeeSelfRate = true);
        CreateStepSetting(master, 2);
        var request = Route(appraisal, step: 2);

        await CreateHandler().OnApproversAssignedAsync(new WorkflowTransactionContext(request, employee), 2, new[] { CreateEmployee("A1") });

        ReviewersOf(appraisal).ShouldBeEmpty();
    }

    [Fact]
    public async Task A_Step_With_No_Settings_Should_Add_Nobody()
    {
        var employee = CreateEmployee("E1");
        var appraisal = CreateAppraisal(employee);
        CreateRequestMaster(1, 2);
        var request = Route(appraisal, step: 2);

        await CreateHandler().OnApproversAssignedAsync(new WorkflowTransactionContext(request, employee), 2, new[] { CreateEmployee("A1") });

        ReviewersOf(appraisal).ShouldBeEmpty();
    }

    [Fact]
    public async Task An_Amendment_Should_Add_Nobody()
    {
        var employee = CreateEmployee("E1");
        var appraisal = CreateAppraisal(employee);
        var master = CreateRequestMaster(1);
        CreateStepSetting(master, 1, s => s.CanEmployeeSelfRate = true);
        var request = Route(appraisal, step: 1);
        request.IsAmendment = true;

        await CreateHandler().OnApproversAssignedAsync(new WorkflowTransactionContext(request, employee), 1, new[] { CreateEmployee("A1") });

        ReviewersOf(appraisal).ShouldBeEmpty();
    }
}
