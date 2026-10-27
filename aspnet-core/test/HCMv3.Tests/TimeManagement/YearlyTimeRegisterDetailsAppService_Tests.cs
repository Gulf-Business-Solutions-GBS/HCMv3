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
public class YearlyTimeRegisterDetailsAppService_Tests : TimeManagementAppServiceTestBase
{
    private YearlyTimeRegisterDetailsAppService CreateAppService()
    {
        return CreateService(() => new YearlyTimeRegisterDetailsAppService(FakeRepository.For<YearlyTimeRegisterDetail>(Context)));
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

    private int CreateYearlyTimeRegister(int employeeId, DateTime year)
    {
        var entity = new YearlyTimeRegister
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            Year = year
        };
        Context.YearlyTimeRegisters.Add(entity);
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
    public async Task Should_Create_YearlyTimeRegisterDetail()
    {
        var employeeId = CreateEmployee("John");
        var yearlyTimeRegisterId = CreateYearlyTimeRegister(employeeId, new DateTime(2026, 1, 1));
        var timeTypeId = CreateTimeType("1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditYearlyTimeRegisterDetailDto
        {
            YearlyTimeRegisterId = yearlyTimeRegisterId,
            TimeTypeId = timeTypeId,
            TimeValue = "30"
        });

        var result = await service.GetAll(new GetAllYearlyTimeRegisterDetailsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].TimeValue.ShouldBe("30");
    }

    [Fact]
    public async Task Should_Throw_When_YearlyTimeRegister_Is_Blank()
    {
        var timeTypeId = CreateTimeType("2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditYearlyTimeRegisterDetailDto
            {
                YearlyTimeRegisterId = 0,
                TimeTypeId = timeTypeId,
                TimeValue = "1"
            }));
    }
}
