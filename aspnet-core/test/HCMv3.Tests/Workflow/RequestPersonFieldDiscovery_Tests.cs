using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Which fields of a transaction a "Person on Request" step may route to, found from the entity itself.
/// </summary>
// ReSharper disable once InconsistentNaming
public class RequestPersonFieldDiscovery_Tests : AppTestBase
{
    [Fact]
    public void Should_Find_An_Employee_Field_Through_Its_Employee_Link()
    {
        RequestPersonFieldDiscovery.For(typeof(EmployeeDocumentMvmt)).ShouldContain(nameof(EmployeeDocumentMvmt.GuarantorId));
    }

    [Fact]
    public void Should_Find_A_Plain_Employee_Id_Tagged_As_A_Request_Person()
    {
        RequestPersonFieldDiscovery.For(typeof(EmployeeAbsence)).ShouldContain(nameof(EmployeeAbsence.PrimaryDelegateId));
    }

    [Fact]
    public void Should_Never_Offer_The_Requester()
    {
        RequestPersonFieldDiscovery.For(typeof(EmployeeDocumentMvmt)).ShouldNotContain(nameof(EmployeeDocumentMvmt.EmployeeId));
        RequestPersonFieldDiscovery.For(typeof(EmployeeAbsence)).ShouldNotContain(nameof(EmployeeAbsence.EmployeeId));
    }

    [Fact]
    public void Should_Not_Offer_An_Id_That_Is_Not_An_Employee()
    {
        RequestPersonFieldDiscovery.For(typeof(EmployeeDocumentMvmt)).ShouldNotContain(nameof(EmployeeDocumentMvmt.EmployeeDocumentId));
    }

    [Fact]
    public void Should_Read_The_Guarantor_From_A_Withdrawal()
    {
        var withdrawal = new EmployeeDocumentMvmt { EmployeeId = 7, GuarantorId = 42 };

        RequestPersonFieldDiscovery.Read(typeof(EmployeeDocumentMvmt), withdrawal, nameof(EmployeeDocumentMvmt.GuarantorId))
            .ShouldBe(42);
    }

    [Fact]
    public void Should_Read_Nothing_When_The_Field_Is_Empty_Or_Not_Offered()
    {
        var withdrawal = new EmployeeDocumentMvmt { EmployeeId = 7, GuarantorId = null };

        RequestPersonFieldDiscovery.Read(typeof(EmployeeDocumentMvmt), withdrawal, nameof(EmployeeDocumentMvmt.GuarantorId))
            .ShouldBeNull();
        RequestPersonFieldDiscovery.Read(typeof(EmployeeDocumentMvmt), withdrawal, nameof(EmployeeDocumentMvmt.EmployeeId))
            .ShouldBeNull();
    }

    [Fact]
    public void Should_Offer_The_Guarantor_On_The_Document_Withdrawal_Handler()
    {
        Resolve<PassportWithdrawalWorkflowTransactionHandler>().RequestPersonFields
            .ShouldContain(nameof(EmployeeDocumentMvmt.GuarantorId));
    }

    [Theory]
    [InlineData("EmployeeDocumentMvmt:GuarantorId", "EmployeeDocumentMvmt", "GuarantorId")]
    [InlineData("EmployeeAbsence:PrimaryDelegateId", "EmployeeAbsence", "PrimaryDelegateId")]
    public void Should_Split_A_Stored_Code(string code, string requestType, string propertyName)
    {
        WorkflowRequestPersonFields.TryParse(code, out var type, out var property).ShouldBeTrue();
        type.ShouldBe(requestType);
        property.ShouldBe(propertyName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("GuarantorId")]
    [InlineData(":GuarantorId")]
    [InlineData("EmployeeDocumentMvmt:")]
    public void Should_Reject_A_Malformed_Code(string code)
    {
        WorkflowRequestPersonFields.TryParse(code, out _, out _).ShouldBeFalse();
    }
}
