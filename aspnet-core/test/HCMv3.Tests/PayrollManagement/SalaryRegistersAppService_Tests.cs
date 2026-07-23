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
public class SalaryRegistersAppService_Tests : PayrollManagementAppServiceTestBase
{
    private SalaryRegistersAppService CreateAppService()
    {
        return CreateService(() => new SalaryRegistersAppService(FakeRepository.For<SalaryRegister>(Context)));
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

    private int CreatePayrollReasonType(string code)
    {
        var entity = new PayrollReasonType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.PayrollReasonTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateSalaryRegister(int employeeId, int payrollReasonTypeId, bool locked)
    {
        var entity = new SalaryRegister
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            ForPeriod = new DateTime(2026, 1, 1),
            InPeriod = new DateTime(2026, 1, 1),
            PayrollRunType = PayrollRunType.Regular,
            PayrollResultType = PayrollResultType.Current,
            PayrollReasonTypeId = payrollReasonTypeId,
            Locked = locked
        };
        Context.SalaryRegisters.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_SalaryRegister()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SR_RT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSalaryRegisterDto
        {
            EmployeeId = employeeId,
            ForPeriod = new DateTime(2026, 1, 1),
            InPeriod = new DateTime(2026, 1, 1),
            PayrollReasonTypeId = reasonTypeId,
            Locked = false
        });

        var result = await service.GetAll(new GetAllSalaryRegistersInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Locked.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Throw_When_Updating_Register_That_Remains_Locked()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SR_RT2");
        var registerId = CreateSalaryRegister(employeeId, reasonTypeId, locked: true);
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSalaryRegisterDto
            {
                Id = registerId,
                EmployeeId = employeeId,
                ForPeriod = new DateTime(2026, 1, 1),
                InPeriod = new DateTime(2026, 1, 1),
                PayrollReasonTypeId = reasonTypeId,
                Locked = true
            }));
    }

    [Fact]
    public async Task Should_Allow_Unlocking_A_Locked_Register()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SR_RT3");
        var registerId = CreateSalaryRegister(employeeId, reasonTypeId, locked: true);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSalaryRegisterDto
        {
            Id = registerId,
            EmployeeId = employeeId,
            ForPeriod = new DateTime(2026, 1, 1),
            InPeriod = new DateTime(2026, 1, 1),
            PayrollReasonTypeId = reasonTypeId,
            Locked = false
        });

        var result = await service.GetAll(new GetAllSalaryRegistersInput { EmployeeId = employeeId });
        result.Items[0].Locked.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Throw_When_Deleting_Locked_Register()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SR_RT4");
        var registerId = CreateSalaryRegister(employeeId, reasonTypeId, locked: true);
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.Delete(new Abp.Application.Services.Dto.EntityDto(registerId)));
    }
}
