using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

/// <summary>
/// The self-service switches on a document type are saved, read back and changed through the admin screen's
/// service, since they are the only thing that decides what an employee may do with the type.
/// </summary>
// ReSharper disable once InconsistentNaming
public class DocumentTypesAppService_Tests : AppTestBase
{
    private readonly IDocumentTypesAppService _documentTypes;

    public DocumentTypesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _documentTypes = Resolve<IDocumentTypesAppService>();
    }

    private DocumentType Stored(string nameEn) =>
        UsingDbContext(context => context.DocumentTypes.Single(t => t.NameEn == nameEn));

    [Fact]
    public async Task Should_Save_The_Self_Service_Switches_On_Create()
    {
        await _documentTypes.CreateOrEdit(new CreateOrEditDocumentTypeDto
        {
            NameEn = "Switch test passport",
            NameAr = "Switch test passport ar",
            IsWithdrawable = true,
            IncludeInWorkflow = true,
            IsHiddenFromEmployee = true
        });

        var stored = Stored("Switch test passport");

        stored.IsWithdrawable.ShouldBeTrue();
        stored.IncludeInWorkflow.ShouldBeTrue();
        stored.IsHiddenFromEmployee.ShouldBeTrue();
        stored.IsLetterType.ShouldBeFalse();
    }

    [Fact]
    public async Task A_New_Type_Should_Be_Offered_To_Employees_Unless_Told_Otherwise()
    {
        // The input a caller sends when it knows nothing of the switch.
        await _documentTypes.CreateOrEdit(new CreateOrEditDocumentTypeDto
        {
            NameEn = "Switch test default",
            NameAr = "Switch test default ar"
        });

        var stored = Stored("Switch test default");

        stored.IncludeInWorkflow.ShouldBeTrue();
        stored.IsWithdrawable.ShouldBeFalse();
        stored.IsHiddenFromEmployee.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Change_The_Self_Service_Switches_On_Edit()
    {
        await _documentTypes.CreateOrEdit(new CreateOrEditDocumentTypeDto
        {
            NameEn = "Switch test edit",
            NameAr = "Switch test edit ar",
            IsWithdrawable = true
        });

        var id = Stored("Switch test edit").Id;
        var edit = (await _documentTypes.GetDocumentTypeForEdit(new EntityDto(id))).DocumentType;

        edit.IsWithdrawable.ShouldBeTrue();
        edit.IncludeInWorkflow.ShouldBeTrue();

        edit.IsWithdrawable = false;
        edit.IncludeInWorkflow = false;
        edit.IsLetterType = true;

        await _documentTypes.CreateOrEdit(edit);

        var stored = Stored("Switch test edit");

        stored.IsWithdrawable.ShouldBeFalse();
        stored.IncludeInWorkflow.ShouldBeFalse();
        stored.IsLetterType.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Save_And_Change_The_Date_Switches()
    {
        await _documentTypes.CreateOrEdit(new CreateOrEditDocumentTypeDto
        {
            NameEn = "Switch test dates",
            NameAr = "Switch test dates ar",
            IsExpiryDateRequired = true
        });

        var stored = Stored("Switch test dates");

        stored.IsExpiryDateRequired.ShouldBeTrue();

        // As legacy defaults it: a new type does not allow overlapping documents until somebody says so.
        stored.AllowDateOverlap.ShouldBeFalse();

        var edit = (await _documentTypes.GetDocumentTypeForEdit(new EntityDto(stored.Id))).DocumentType;
        edit.IsExpiryDateRequired.ShouldBeTrue();

        edit.IsExpiryDateRequired = false;
        edit.AllowDateOverlap = true;

        await _documentTypes.CreateOrEdit(edit);

        stored = Stored("Switch test dates");

        stored.IsExpiryDateRequired.ShouldBeFalse();
        stored.AllowDateOverlap.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Return_The_Switches_In_The_List()
    {
        await _documentTypes.CreateOrEdit(new CreateOrEditDocumentTypeDto
        {
            NameEn = "Switch test list",
            NameAr = "Switch test list ar",
            IsWithdrawable = true,
            IsHiddenFromEmployee = true
        });

        var listed = (await _documentTypes.GetAll(new GetAllDocumentTypesInput { Filter = "Switch test list", MaxResultCount = 10 }))
            .Items.Single();

        listed.IsWithdrawable.ShouldBeTrue();
        listed.IncludeInWorkflow.ShouldBeTrue();
        listed.IsHiddenFromEmployee.ShouldBeTrue();
    }
}
