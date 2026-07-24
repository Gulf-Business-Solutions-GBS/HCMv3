using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeDocumentsAppService_Tests : AppTestBase
{
    private readonly IEmployeeDocumentsAppService _appService;

    public EmployeeDocumentsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeDocumentsAppService>();
    }

    private Employee CreateTestEmployee(string code)
    {
        Nationality nationality = null;
        Employee employee = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });
        return employee;
    }

    private DocumentType CreateTestDocumentType(string code)
    {
        DocumentType documentType = null;
        UsingDbContext(context =>
        {
            documentType = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return documentType;
    }

    [Fact]
    public async Task Should_Create_EmployeeDocument()
    {
        //Arrange
        var employee = CreateTestEmployee("ED100");
        var documentType = CreateTestDocumentType("DT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeDocumentDto
        {
            EmployeeId = employee.Id,
            DocumentTypeId = documentType.Id,
            DocumentNum = "DOC-100",
            IssueDate = new DateTime(2020, 1, 1),
            ExpiryDate = new DateTime(2030, 1, 1)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeDocumentsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].DocumentNum.ShouldBe("DOC-100");
    }

    [Fact]
    public async Task Should_Default_ExpiryDate_To_Infinite_When_Not_Provided()
    {
        //Arrange
        var employee = CreateTestEmployee("ED150");
        var documentType = CreateTestDocumentType("DT150");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeDocumentDto
        {
            EmployeeId = employee.Id,
            DocumentTypeId = documentType.Id,
            DocumentNum = "DOC-150",
            IssueDate = new DateTime(2020, 1, 1)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeDocumentsInput { EmployeeId = employee.Id });
        result.Items[0].ExpiryDate.ShouldBe(new DateTime(9999, 12, 31));
    }

    [Fact]
    public async Task Should_Get_EmployeeDocument_For_Edit()
    {
        //Arrange
        var employee = CreateTestEmployee("ED200");
        var documentType = CreateTestDocumentType("DT200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDocumentDto
        {
            EmployeeId = employee.Id,
            DocumentTypeId = documentType.Id,
            DocumentNum = "DOC-200",
            IssueDate = new DateTime(2020, 1, 1),
            ExpiryDate = new DateTime(2030, 1, 1)
        });

        var created = await _appService.GetAll(new GetAllEmployeeDocumentsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        var result = await _appService.GetEmployeeDocumentForEdit(new EntityDto(id));

        //Assert
        result.EmployeeDocument.ShouldNotBeNull();
        result.EmployeeDocument.DocumentNum.ShouldBe("DOC-200");
    }

    [Fact]
    public async Task Should_Update_EmployeeDocument()
    {
        //Arrange
        var employee = CreateTestEmployee("ED300");
        var documentType = CreateTestDocumentType("DT300");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDocumentDto
        {
            EmployeeId = employee.Id,
            DocumentTypeId = documentType.Id,
            DocumentNum = "DOC-300",
            IssueDate = new DateTime(2020, 1, 1),
            ExpiryDate = new DateTime(2030, 1, 1)
        });

        var created = await _appService.GetAll(new GetAllEmployeeDocumentsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeDocumentDto
        {
            Id = id,
            EmployeeId = employee.Id,
            DocumentTypeId = documentType.Id,
            DocumentNum = "DOC-300-UPDATED",
            IssueDate = new DateTime(2020, 1, 1),
            ExpiryDate = new DateTime(2031, 1, 1)
        });

        //Assert
        var result = await _appService.GetEmployeeDocumentForEdit(new EntityDto(id));
        result.EmployeeDocument.DocumentNum.ShouldBe("DOC-300-UPDATED");
        result.EmployeeDocument.ExpiryDate.ShouldBe(new DateTime(2031, 1, 1));
    }

    [Fact]
    public async Task Should_Delete_EmployeeDocument()
    {
        //Arrange
        var employee = CreateTestEmployee("ED400");
        var documentType = CreateTestDocumentType("DT400");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDocumentDto
        {
            EmployeeId = employee.Id,
            DocumentTypeId = documentType.Id,
            DocumentNum = "DOC-400",
            IssueDate = new DateTime(2020, 1, 1),
            ExpiryDate = new DateTime(2030, 1, 1)
        });

        var created = await _appService.GetAll(new GetAllEmployeeDocumentsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeDocumentsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Compute_IsActive_As_Active_For_Current_Document()
    {
        //Arrange
        var employee = CreateTestEmployee("ED500");
        var documentType = CreateTestDocumentType("DT500");
        var today = DateTime.Now.Date;

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeDocumentDto
        {
            EmployeeId = employee.Id,
            DocumentTypeId = documentType.Id,
            DocumentNum = "DOC-500",
            IssueDate = today.AddDays(-10),
            ExpiryDate = today.AddDays(10)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeDocumentsInput { EmployeeId = employee.Id });
        result.Items[0].IsActive.ShouldBe(DocumentStatus.Active);
    }

    [Fact]
    public async Task Should_Compute_IsActive_As_Inactive_When_Newer_Document_Of_Same_Type_Exists()
    {
        //Arrange
        var employee = CreateTestEmployee("ED600");
        var documentType = CreateTestDocumentType("DT600");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDocumentDto
        {
            EmployeeId = employee.Id,
            DocumentTypeId = documentType.Id,
            DocumentNum = "DOC-600-OLD",
            IssueDate = new DateTime(2018, 1, 1),
            ExpiryDate = new DateTime(2030, 1, 1)
        });

        //Act - newer document of the same type for the same employee
        await _appService.CreateOrEdit(new CreateOrEditEmployeeDocumentDto
        {
            EmployeeId = employee.Id,
            DocumentTypeId = documentType.Id,
            DocumentNum = "DOC-600-NEW",
            IssueDate = new DateTime(2022, 1, 1),
            ExpiryDate = new DateTime(2032, 1, 1)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeDocumentsInput { EmployeeId = employee.Id });
        var oldDoc = result.Items.Single(d => d.DocumentNum == "DOC-600-OLD");
        oldDoc.IsActive.ShouldBe(DocumentStatus.Inactive);
    }
}
