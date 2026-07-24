using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeDocumentMvmtsAppService_Tests : AppTestBase
{
    private readonly IEmployeeDocumentMvmtsAppService _appService;

    public EmployeeDocumentMvmtsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeDocumentMvmtsAppService>();
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

    private EmployeeDocument CreateTestEmployeeDocument(string code)
    {
        var employee = CreateTestEmployee(code);
        DocumentType documentType = null;
        EmployeeDocument document = null;

        UsingDbContext(context =>
        {
            documentType = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "DT" + code,
                NameEn = "DT" + code,
                NameAr = "DT" + code
            }).Entity;
        });

        UsingDbContext(context =>
        {
            document = context.Set<EmployeeDocument>().Add(new EmployeeDocument
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employee.Id,
                DocumentTypeId = documentType.Id,
                DocumentNum = "DOC-" + code,
                IssueDate = new DateTime(2020, 1, 1),
                ExpiryDate = new DateTime(2030, 1, 1)
            }).Entity;
        });
        return document;
    }

    [Fact]
    public async Task Should_Create_EmployeeDocumentMvmt()
    {
        //Arrange
        var document = CreateTestEmployeeDocument("M100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeDocumentMvmtDto
        {
            EmployeeDocumentId = document.Id,
            WithdrawDate = new DateTime(2021, 1, 1),
            ReturnDate = new DateTime(2021, 2, 1)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeDocumentMvmtsInput { EmployeeDocumentId = document.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Get_EmployeeDocumentMvmt_For_Edit()
    {
        //Arrange
        var document = CreateTestEmployeeDocument("M200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDocumentMvmtDto
        {
            EmployeeDocumentId = document.Id,
            WithdrawDate = new DateTime(2021, 1, 1),
            ReturnDate = new DateTime(2021, 2, 1),
            Comments = "Test"
        });

        var created = await _appService.GetAll(new GetAllEmployeeDocumentMvmtsInput { EmployeeDocumentId = document.Id });
        var id = created.Items[0].Id;

        //Act
        var result = await _appService.GetEmployeeDocumentMvmtForEdit(new EntityDto(id));

        //Assert
        result.EmployeeDocumentMvmt.ShouldNotBeNull();
        result.EmployeeDocumentMvmt.Comments.ShouldBe("Test");
    }

    [Fact]
    public async Task Should_Update_EmployeeDocumentMvmt()
    {
        //Arrange
        var document = CreateTestEmployeeDocument("M300");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDocumentMvmtDto
        {
            EmployeeDocumentId = document.Id,
            WithdrawDate = new DateTime(2021, 1, 1),
            ReturnDate = new DateTime(2021, 2, 1)
        });

        var created = await _appService.GetAll(new GetAllEmployeeDocumentMvmtsInput { EmployeeDocumentId = document.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeDocumentMvmtDto
        {
            Id = id,
            EmployeeDocumentId = document.Id,
            WithdrawDate = new DateTime(2021, 1, 1),
            ReturnDate = new DateTime(2021, 2, 1),
            ActualReturnDate = new DateTime(2021, 1, 25)
        });

        //Assert
        var result = await _appService.GetEmployeeDocumentMvmtForEdit(new EntityDto(id));
        result.EmployeeDocumentMvmt.ActualReturnDate.ShouldBe(new DateTime(2021, 1, 25));
    }

    [Fact]
    public async Task Should_Delete_EmployeeDocumentMvmt()
    {
        //Arrange
        var document = CreateTestEmployeeDocument("M400");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDocumentMvmtDto
        {
            EmployeeDocumentId = document.Id,
            WithdrawDate = new DateTime(2021, 1, 1),
            ReturnDate = new DateTime(2021, 2, 1)
        });

        var created = await _appService.GetAll(new GetAllEmployeeDocumentMvmtsInput { EmployeeDocumentId = document.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeDocumentMvmtsInput { EmployeeDocumentId = document.Id });
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_On_Missing_WithdrawDate()
    {
        //Arrange
        var document = CreateTestEmployeeDocument("M500");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeDocumentMvmtDto
            {
                EmployeeDocumentId = document.Id,
                WithdrawDate = default,
                ReturnDate = new DateTime(2021, 2, 1)
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Missing_ReturnDate()
    {
        //Arrange
        var document = CreateTestEmployeeDocument("M600");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeDocumentMvmtDto
            {
                EmployeeDocumentId = document.Id,
                WithdrawDate = new DateTime(2021, 1, 1),
                ReturnDate = default
            }));
    }
}
