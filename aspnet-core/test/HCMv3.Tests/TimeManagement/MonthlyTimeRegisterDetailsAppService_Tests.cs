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
public class MonthlyTimeRegisterDetailsAppService_Tests : TimeManagementAppServiceTestBase
{
    private MonthlyTimeRegisterDetailsAppService CreateAppService()
    {
        return CreateService(() => new MonthlyTimeRegisterDetailsAppService(FakeRepository.For<MonthlyTimeRegisterDetail>(Context)));
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

    private int CreateMonthlyTimeRegister(int employeeId, DateTime month)
    {
        var entity = new MonthlyTimeRegister
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            Month = month
        };
        Context.MonthlyTimeRegisters.Add(entity);
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
    public async Task Should_Create_MonthlyTimeRegisterDetail()
    {
        var employeeId = CreateEmployee("John");
        var monthlyTimeRegisterId = CreateMonthlyTimeRegister(employeeId, new DateTime(2026, 1, 1));
        var timeTypeId = CreateTimeType("1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditMonthlyTimeRegisterDetailDto
        {
            MonthlyTimeRegisterId = monthlyTimeRegisterId,
            TimeTypeId = timeTypeId,
            TimeValue = "2.5"
        });

        var result = await service.GetAll(new GetAllMonthlyTimeRegisterDetailsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].TimeValue.ShouldBe("2.5");
    }

    [Fact]
    public async Task Should_Throw_When_TimeType_Is_Blank()
    {
        var employeeId = CreateEmployee("Jane");
        var monthlyTimeRegisterId = CreateMonthlyTimeRegister(employeeId, new DateTime(2026, 2, 1));
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditMonthlyTimeRegisterDetailDto
            {
                MonthlyTimeRegisterId = monthlyTimeRegisterId,
                TimeTypeId = 0,
                TimeValue = "1"
            }));
    }
}
