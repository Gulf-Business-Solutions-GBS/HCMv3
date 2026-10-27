using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.Workflow.Dto;
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

    // ------------------------------------------------------------------ routing rules, the same for every type

    private int CreateChain(string code)
    {
        return UsingDbContext(context =>
        {
            var master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = AbpSession.TenantId!.Value, Code = code, NameEn = code, NameAr = code
            }).Entity;
            context.SaveChanges();
            return master.Id;
        });
    }

    private async Task<CreateOrEditWorkflowTransactionTypeDto> LoadAsync(string code)
    {
        return (await _service.GetWorkflowTransactionTypeForEdit(new EntityDto(TypeId(code)))).WorkflowTransactionType;
    }

    [Fact]
    public async Task Should_Save_And_Reload_Rules_For_Any_Type()
    {
        var chainId = CreateChain("DOC-CANCEL");
        var input = await LoadAsync("EmployeeDocument");
        input.Rules.Add(new WorkflowTransactionTypeRuleDto { RequestSubTypeId = 5, RequestAction = WorkflowRequestAction.Cancellation, RequestMasterId = chainId });

        await _service.CreateOrEdit(input);

        var rule = (await LoadAsync("EmployeeDocument")).Rules.ShouldHaveSingleItem();
        rule.RequestSubTypeId.ShouldBe(5);
        rule.RequestAction.ShouldBe(WorkflowRequestAction.Cancellation);
        rule.RequestMasterId.ShouldBe(chainId);
    }

    [Fact]
    public async Task Saving_Should_Replace_The_Rules_The_Type_Had()
    {
        var chainId = CreateChain("LEAVE-RET");
        var input = await LoadAsync("EmployeeAbsence");
        input.Rules.Add(new WorkflowTransactionTypeRuleDto { RequestAction = WorkflowRequestAction.Return, RequestMasterId = chainId });
        await _service.CreateOrEdit(input);

        var again = await LoadAsync("EmployeeAbsence");
        again.Rules.Clear();
        await _service.CreateOrEdit(again);

        (await LoadAsync("EmployeeAbsence")).Rules.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Refuse_Two_Rules_For_The_Same_Sub_Type_And_Action()
    {
        var first = CreateChain("A1");
        var second = CreateChain("A2");
        var input = await LoadAsync("EmployeeAbsence");
        input.Rules.Add(new WorkflowTransactionTypeRuleDto { RequestAction = WorkflowRequestAction.Cancellation, RequestMasterId = first });
        input.Rules.Add(new WorkflowTransactionTypeRuleDto { RequestAction = WorkflowRequestAction.Cancellation, RequestMasterId = second });

        await Should.ThrowAsync<UserFriendlyException>(() => _service.CreateOrEdit(input));
    }

    [Fact]
    public async Task Should_Refuse_A_Sub_Type_Rule_On_A_Type_Without_A_Sub_Type_Source()
    {
        var chainId = CreateChain("ASSET-X");
        var input = await LoadAsync("EmployeeAsset");
        input.SubTypeSource = null;
        input.Rules.Add(new WorkflowTransactionTypeRuleDto { RequestSubTypeId = 3, RequestMasterId = chainId });

        await Should.ThrowAsync<UserFriendlyException>(() => _service.CreateOrEdit(input));
    }

    [Fact]
    public async Task Should_List_The_Sub_Types_Of_A_Source_By_Name()
    {
        UsingDbContext(context =>
        {
            context.Set<AbsenceType>().Add(new AbsenceType { TenantId = AbpSession.TenantId!.Value, Code = "SICKX", NameEn = "Sick Leave X", NameAr = "مرضية" });
        });

        var options = await _service.GetSubTypeOptions(WorkflowSubTypeSources.AbsenceType);

        options.ShouldContain(o => o.Code == "SICKX" && o.NameEn == "Sick Leave X");
    }

    [Fact]
    public async Task Should_Return_No_Sub_Types_For_An_Unknown_Source()
    {
        (await _service.GetSubTypeOptions("NotATable")).ShouldBeEmpty();
    }

    [Fact]
    public void Should_Offer_The_Standard_Sub_Type_Sources()
    {
        _service.GetSubTypeSources().ShouldContain(WorkflowSubTypeSources.AbsenceType);
        _service.GetSubTypeSources().ShouldContain(WorkflowSubTypeSources.DocumentType);
    }
}
