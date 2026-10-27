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
public class EmployeeAbsAdjustmentsAppService_Tests : AppTestBase
{
    private readonly IEmployeeAbsAdjustmentsAppService _appService;

    public EmployeeAbsAdjustmentsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeAbsAdjustmentsAppService>();
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

    private AbsenceType CreateTestAbsenceType(string code)
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return absenceType;
    }

    [Fact]
    public async Task Should_Create_Adjustment()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA100");
        var absenceType = CreateTestAbsenceType("AAT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsAdjustmentDto
        {
            EmployeeId = employee.Id,
            AbsenceTypeId = absenceType.Id,
            AdjustmentType = AdjustmentType.Adjust,
            EffectiveDate = new DateTime(2024, 3, 1),
            QuotaAdjusted = 2.5,
            AdjustmentPaymentType = AdjustmentPaymentType.PayWithPayroll
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeAbsAdjustmentsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].QuotaAdjusted.ShouldBe(2.5);
        result.Items[0].AdjustmentType.ShouldBe(AdjustmentType.Adjust);
    }

    [Fact]
    public async Task Should_Update_Adjustment()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA200");
        var absenceType = CreateTestAbsenceType("AAT200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsAdjustmentDto
        {
            EmployeeId = employee.Id,
            AbsenceTypeId = absenceType.Id,
            AdjustmentType = AdjustmentType.Encashment,
            EffectiveDate = new DateTime(2024, 3, 1),
            QuotaAdjusted = 1
        });

        var created = await _appService.GetAll(new GetAllEmployeeAbsAdjustmentsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsAdjustmentDto
        {
            Id = id,
            EmployeeId = employee.Id,
            AbsenceTypeId = absenceType.Id,
            AdjustmentType = AdjustmentType.Encashment,
            EffectiveDate = new DateTime(2024, 3, 1),
            QuotaAdjusted = 3
        });

        //Assert
        var updated = await _appService.GetEmployeeAbsAdjustmentForEdit(new Abp.Application.Services.Dto.EntityDto(id));
        updated.EmployeeAbsAdjustment.QuotaAdjusted.ShouldBe(3);
    }
}
