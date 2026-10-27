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
public class YearlyTimeRegistersAppService_Tests : TimeManagementAppServiceTestBase
{
    private YearlyTimeRegistersAppService CreateAppService()
    {
        return CreateService(() => new YearlyTimeRegistersAppService(FakeRepository.For<YearlyTimeRegister>(Context)));
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

    [Fact]
    public async Task Should_Create_YearlyTimeRegister()
    {
        var employeeId = CreateEmployee("John");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditYearlyTimeRegisterDto
        {
            EmployeeId = employeeId,
            Year = new DateTime(2026, 1, 1),
            ActualWorkingDays = 300,
            TicketAccrualDays = 12
        });

        var result = await service.GetAll(new GetAllYearlyTimeRegistersInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
        result.Items[0].TicketAccrualDays.ShouldBe(12);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Employee_Year()
    {
        var employeeId = CreateEmployee("Jane");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditYearlyTimeRegisterDto
        {
            EmployeeId = employeeId,
            Year = new DateTime(2026, 1, 1)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditYearlyTimeRegisterDto
            {
                EmployeeId = employeeId,
                Year = new DateTime(2026, 1, 1)
            }));
    }
}
