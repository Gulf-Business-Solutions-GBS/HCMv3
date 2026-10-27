using System;
using System.Text;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeSignaturesAppService_Tests : AppTestBase
{
    private readonly IEmployeeSignaturesAppService _appService;

    public EmployeeSignaturesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeSignaturesAppService>();
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

    [Fact]
    public async Task Should_Create_EmployeeSignature()
    {
        //Arrange
        var employee = CreateTestEmployee("SG100");
        var signature = Encoding.UTF8.GetBytes("sig-bytes");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeSignatureDto
        {
            EmployeeId = employee.Id,
            Signature = signature
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeSignaturesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Signature.ShouldBe(signature);
    }

    [Fact]
    public async Task Should_Get_EmployeeSignature_For_Edit()
    {
        //Arrange
        var employee = CreateTestEmployee("SG200");
        var signature = Encoding.UTF8.GetBytes("sig-bytes-2");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeSignatureDto
        {
            EmployeeId = employee.Id,
            Signature = signature
        });

        var created = await _appService.GetAll(new GetAllEmployeeSignaturesInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        var result = await _appService.GetEmployeeSignatureForEdit(new EntityDto(id));

        //Assert
        result.EmployeeSignature.ShouldNotBeNull();
        result.EmployeeSignature.EmployeeId.ShouldBe(employee.Id);
        result.EmployeeSignature.Signature.ShouldBe(signature);
    }

    [Fact]
    public async Task Should_Update_EmployeeSignature()
    {
        //Arrange
        var employee = CreateTestEmployee("SG300");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeSignatureDto
        {
            EmployeeId = employee.Id,
            Signature = Encoding.UTF8.GetBytes("original")
        });

        var created = await _appService.GetAll(new GetAllEmployeeSignaturesInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;
        var updatedSignature = Encoding.UTF8.GetBytes("updated");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeSignatureDto
        {
            Id = id,
            EmployeeId = employee.Id,
            Signature = updatedSignature
        });

        //Assert
        var result = await _appService.GetEmployeeSignatureForEdit(new EntityDto(id));
        result.EmployeeSignature.Signature.ShouldBe(updatedSignature);
    }

    [Fact]
    public async Task Should_Delete_EmployeeSignature()
    {
        //Arrange
        var employee = CreateTestEmployee("SG400");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeSignatureDto
        {
            EmployeeId = employee.Id,
            Signature = Encoding.UTF8.GetBytes("to-delete")
        });

        var created = await _appService.GetAll(new GetAllEmployeeSignaturesInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeSignaturesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Get_EmployeeSignatures_Filtered_By_Employee()
    {
        //Arrange
        var employee1 = CreateTestEmployee("SG500");
        var employee2 = CreateTestEmployee("SG501");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeSignatureDto
        {
            EmployeeId = employee1.Id,
            Signature = Encoding.UTF8.GetBytes("e1")
        });
        await _appService.CreateOrEdit(new CreateOrEditEmployeeSignatureDto
        {
            EmployeeId = employee2.Id,
            Signature = Encoding.UTF8.GetBytes("e2")
        });

        //Act
        var result = await _appService.GetAll(new GetAllEmployeeSignaturesInput { EmployeeId = employee1.Id });

        //Assert
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employee1.Id);
    }
}
