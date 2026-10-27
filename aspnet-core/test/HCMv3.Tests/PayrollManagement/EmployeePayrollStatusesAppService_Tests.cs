using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class EmployeePayrollStatusesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private EmployeePayrollStatusesAppService CreateAppService()
    {
        return CreateService(() => new EmployeePayrollStatusesAppService(FakeRepository.For<EmployeePayrollStatus>(Context)));
    }

    private int CreateEmployee()
    {
        var nationality = new HCMv3.GeneralMasterInfo.Nationality
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "N",
            NameAr = "N"
        };
        Context.Nationalities.Add(nationality);
        Context.SaveChanges();

        var entity = new Employee
        {
            TenantId = AbpSession.TenantId ?? 1,
            FirstNameEn = "F",
            LastNameEn = "L",
            FirstNameAr = "ف",
            LastNameAr = "ل",
            NationalityId = nationality.Id,
            JoiningDate = new DateTime(2020, 1, 1)
        };
        Context.Employees.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private static CreateOrEditEmployeePayrollStatusDto BuildDto(int employeeId)
    {
        return new CreateOrEditEmployeePayrollStatusDto
        {
            EmployeeId = employeeId,
            EarliestRetroAccountingDate = new DateTime(2025, 1, 1),
            AccountedTo = new DateTime(2025, 1, 31),
            RunPayrollUpto = new DateTime(2025, 1, 31),
            EarliestMasterDataChange = new DateTime(2025, 1, 1),
            DoNoAccountAfter = new DateTime(2025, 12, 31),
            PersonLocked = false,
            PayrollCorrection = false,
            EarliestRecalculationDate = new DateTime(2025, 1, 1),
            PersonalCalendarFrom = new DateTime(2025, 1, 1),
            NextTimeEvaluationFrom = new DateTime(2025, 2, 1)
        };
    }

    [Fact]
    public async Task Should_Create_EmployeePayrollStatus()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(BuildDto(employeeId));

        var result = await service.GetAll(new GetAllEmployeePayrollStatusesInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_Creating_Second_Row_For_Same_Employee()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(BuildDto(employeeId));

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(BuildDto(employeeId)));
    }

    [Fact]
    public async Task Should_Allow_Updating_Existing_Row_Without_Triggering_Uniqueness_Check()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(BuildDto(employeeId));

        var existing = (await service.GetAll(new GetAllEmployeePayrollStatusesInput { EmployeeId = employeeId })).Items[0];

        var updateDto = BuildDto(employeeId);
        updateDto.Id = existing.Id;
        updateDto.PersonLocked = true;

        await service.CreateOrEdit(updateDto);

        var result = await service.GetAll(new GetAllEmployeePayrollStatusesInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].PersonLocked.ShouldBeTrue();
    }
}
