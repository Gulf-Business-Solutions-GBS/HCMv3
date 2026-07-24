using System;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeePaymentsAppService_Tests : AppTestBase
{
    private readonly IEmployeePaymentsAppService _appService;

    public EmployeePaymentsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeePaymentsAppService>();
    }

    private Employee CreateTestEmployee(string code)
    {
        Nationality nationality = null;
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

        Employee employee = null;
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

    private WageType CreateTestWageType(string code)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code,
                PayrollOrder = 1
            }).Entity;
        });
        return wageType;
    }

    [Fact]
    public async Task Should_Create_EmployeePayment()
    {
        //Arrange
        var employee = CreateTestEmployee("EP100");
        var wageType = CreateTestWageType("WT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeePaymentDto
        {
            EmployeeId = employee.Id,
            WageTypeId = wageType.Id,
            Amount = 250,
            PaymentDate = new DateTime(2024, 3, 15),
            UnitType = UnitType.Amount
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeePaymentsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].WageTypeNameEn.ShouldBe("WT100");
    }
}
