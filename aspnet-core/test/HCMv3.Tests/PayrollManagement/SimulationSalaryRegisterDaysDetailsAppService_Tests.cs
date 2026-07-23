using System;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class SimulationSalaryRegisterDaysDetailsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private SimulationSalaryRegisterDaysDetailsAppService CreateAppService()
    {
        return CreateService(() => new SimulationSalaryRegisterDaysDetailsAppService(FakeRepository.For<SimulationSalaryRegisterDaysDetail>(Context)));
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

    private int CreateWageType(string code)
    {
        var entity = new WageType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code, PayrollOrder = 1 };
        Context.WageTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateSimulationSalaryRegister(int employeeId, int payrollReasonTypeId)
    {
        var entity = new SimulationSalaryRegister
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            ForPeriod = new DateTime(2026, 1, 1),
            InPeriod = new DateTime(2026, 1, 1),
            PayrollRunType = PayrollRunType.Regular,
            PayrollResultType = PayrollResultType.Current,
            PayrollReasonTypeId = payrollReasonTypeId
        };
        Context.SimulationSalaryRegisters.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateSimulationSalaryRegisterDetail(int registerId, int wageTypeId)
    {
        var entity = new SimulationSalaryRegisterDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            SimulationSalaryRegisterId = registerId,
            WageTypeId = wageTypeId,
            Rate = 10,
            Number = 5,
            Amount = 50
        };
        Context.SimulationSalaryRegisterDetails.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_SimulationSalaryRegisterDaysDetail()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SSRDD_RT1");
        var registerId = CreateSimulationSalaryRegister(employeeId, reasonTypeId);
        var wageTypeId = CreateWageType("SSRDD_WT1");
        var detailId = CreateSimulationSalaryRegisterDetail(registerId, wageTypeId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimulationSalaryRegisterDaysDetailDto
        {
            SimulationSalaryRegisterDetailId = detailId,
            Period = new DateTime(2026, 1, 1),
            Rate = 10,
            Number = 1,
            Amount = 10
        });

        var result = await service.GetAll(new GetAllSimulationSalaryRegisterDaysDetailsInput { SimulationSalaryRegisterDetailId = detailId });
        result.TotalCount.ShouldBe(1);
    }
}
