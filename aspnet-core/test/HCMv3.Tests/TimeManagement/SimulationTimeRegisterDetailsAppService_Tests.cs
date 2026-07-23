using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class SimulationTimeRegisterDetailsAppService_Tests : TimeManagementAppServiceTestBase
{
    private SimulationTimeRegisterDetailsAppService CreateAppService()
    {
        return CreateService(() => new SimulationTimeRegisterDetailsAppService(FakeRepository.For<SimulationTimeRegisterDetail>(Context)));
    }

    private int CreateNationality()
    {
        var entity = new HCMv3.GeneralMasterInfo.Nationality
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Local",
            NameAr = "محلي"
        };
        Context.Nationalities.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateEmployee(string firstName)
    {
        var entity = new Employee
        {
            TenantId = AbpSession.TenantId ?? 1,
            FirstNameEn = firstName,
            LastNameEn = "Doe",
            FirstNameAr = firstName,
            LastNameAr = "Doe",
            FullNameEn = firstName,
            FullNameAr = firstName,
            NationalityId = CreateNationality()
        };
        Context.Employees.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateSimulationTimeRegister(int employeeId, DateTime dayDate)
    {
        var entity = new SimulationTimeRegister
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            DayDate = dayDate
        };
        Context.SimulationTimeRegisters.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateTimeType(string code)
    {
        var entity = new TimeType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.TimeTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_SimulationTimeRegisterDetail()
    {
        var employeeId = CreateEmployee("John");
        var simulationTimeRegisterId = CreateSimulationTimeRegister(employeeId, new DateTime(2026, 1, 1));
        var timeTypeId = CreateTimeType("1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimulationTimeRegisterDetailDto
        {
            SimulationTimeRegisterId = simulationTimeRegisterId,
            TimeTypeId = timeTypeId,
            TimeValue = "1"
        });

        var result = await service.GetAll(new GetAllSimulationTimeRegisterDetailsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].TimeValue.ShouldBe("1");
    }

    [Fact]
    public async Task Should_Throw_When_TimeType_Is_Blank()
    {
        var employeeId = CreateEmployee("Jane");
        var simulationTimeRegisterId = CreateSimulationTimeRegister(employeeId, new DateTime(2026, 2, 1));
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSimulationTimeRegisterDetailDto
            {
                SimulationTimeRegisterId = simulationTimeRegisterId,
                TimeTypeId = 0,
                TimeValue = "1"
            }));
    }
}
