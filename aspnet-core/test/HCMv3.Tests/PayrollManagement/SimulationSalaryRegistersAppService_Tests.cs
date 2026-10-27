using System;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class SimulationSalaryRegistersAppService_Tests : PayrollManagementAppServiceTestBase
{
    private SimulationSalaryRegistersAppService CreateAppService()
    {
        return CreateService(() => new SimulationSalaryRegistersAppService(FakeRepository.For<SimulationSalaryRegister>(Context)));
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

    [Fact]
    public async Task Should_Create_SimulationSalaryRegister_With_No_Locked_Field()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SSR_RT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimulationSalaryRegisterDto
        {
            EmployeeId = employeeId,
            ForPeriod = new DateTime(2026, 1, 1),
            InPeriod = new DateTime(2026, 1, 1),
            PayrollReasonTypeId = reasonTypeId
        });

        var result = await service.GetAll(new GetAllSimulationSalaryRegistersInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Allow_Update_And_Delete_With_No_Lock_Guard()
    {
        // Simulation family has NO Locked field at all — a "locked-equivalent" scenario should never throw.
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SSR_RT2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimulationSalaryRegisterDto
        {
            EmployeeId = employeeId,
            ForPeriod = new DateTime(2026, 1, 1),
            InPeriod = new DateTime(2026, 1, 1),
            PayrollReasonTypeId = reasonTypeId
        });

        var created = await service.GetAll(new GetAllSimulationSalaryRegistersInput { EmployeeId = employeeId });
        var id = created.Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditSimulationSalaryRegisterDto
        {
            Id = id,
            EmployeeId = employeeId,
            ForPeriod = new DateTime(2026, 2, 1),
            InPeriod = new DateTime(2026, 2, 1),
            PayrollReasonTypeId = reasonTypeId
        });

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllSimulationSalaryRegistersInput { EmployeeId = employeeId });
        afterDelete.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_One_Register_With_Its_Reason_Type_Name()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SSR_RT3");
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditSimulationSalaryRegisterDto
        {
            EmployeeId = employeeId,
            ForPeriod = new DateTime(2026, 3, 1),
            InPeriod = new DateTime(2026, 3, 1),
            PayrollReasonTypeId = reasonTypeId
        });
        var id = (await service.GetAll(new GetAllSimulationSalaryRegistersInput { EmployeeId = employeeId })).Items[0].Id;

        var register = await service.GetSimulationSalaryRegister(new Abp.Application.Services.Dto.EntityDto(id));

        register.Id.ShouldBe(id);
        register.EmployeeId.ShouldBe(employeeId);
        register.PayrollReasonTypeName.ShouldBe("SSR_RT3");
    }

    [Fact]
    public async Task Should_Throw_When_The_Register_Does_Not_Exist()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<Abp.Domain.Entities.EntityNotFoundException>(
            () => service.GetSimulationSalaryRegister(new Abp.Application.Services.Dto.EntityDto(999999)));
    }
}
