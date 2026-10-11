using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// The setup dialog for a transaction type offers an approval chain "for amendments", "for cancellations" and
/// "for returns" -- but only on a type that has such a thing. These pin which types say they do.
/// </summary>
// ReSharper disable once InconsistentNaming
public class WorkflowTransactionTypesAppService_Tests : AppTestBase
{
    private readonly IWorkflowTransactionTypesAppService _service;

    public WorkflowTransactionTypesAppService_Tests()
    {
        _service = Resolve<IWorkflowTransactionTypesAppService>();
    }

    [Fact]
    public async Task Should_Offer_Amendment_Cancellation_And_Return_Chains_For_Leave()
    {
        var output = await _service.GetWorkflowTransactionTypeForEdit(new EntityDto(TypeId("EmployeeAbsence")));

        output.SupportedChangeActions.ShouldBe(
            new[] { WorkflowRequestAction.Amendment, WorkflowRequestAction.Cancellation, WorkflowRequestAction.Return },
            ignoreOrder: true);
    }

    [Fact]
    public async Task Should_Offer_Only_A_Return_Chain_For_A_Passport_Withdrawal()
    {
        // A document that went out can come back; it cannot be "amended" or "cancelled" once approved.
        var output = await _service.GetWorkflowTransactionTypeForEdit(new EntityDto(TypeId("EmployeeDocumentMvmt")));

        output.SupportedChangeActions.ShouldBe(new[] { WorkflowRequestAction.Return });
    }

    [Fact]
    public async Task Should_Offer_No_Change_Chains_For_A_Type_That_Cannot_Be_Changed_After_Approval()
    {
        // An asset request has no amendment, cancellation or return: showing those chains on it was wrong.
        var output = await _service.GetWorkflowTransactionTypeForEdit(new EntityDto(TypeId("EmployeeAsset")));

        output.SupportedChangeActions.ShouldBeEmpty();
    }

    private int TypeId(string code)
    {
        return UsingDbContext(context => context.Set<WorkflowTransactionType>().Single(t => t.Code == code && !t.IsDeleted).Id);
    }
}
