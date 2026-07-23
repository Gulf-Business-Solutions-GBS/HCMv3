using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeDutyPeriodsAppService_Tests : TimeManagementAppServiceTestBase
{
    private EmployeeDutyPeriodsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeDutyPeriodsAppService(FakeRepository.For<EmployeeDutyPeriod>(Context)));
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

    private int CreateDutyPeriod(string code)
    {
        var entity = new DutyPeriod
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(9999, 12, 31)
        };
        Context.DutyPeriods.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_EmployeeDutyPeriod()
    {
        var employeeId = CreateEmployee("John");
        var dutyPeriodId = CreateDutyPeriod("DP1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeDutyPeriodDto
        {
            EmployeeId = employeeId,
            DutyPeriodId = dutyPeriodId,
            StartDate = new DateTime(2026, 1, 1)
        });

        var result = await service.GetAll(new GetAllEmployeeDutyPeriodsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
        result.Items[0].EndDate.ShouldBe(new DateTime(9999, 12, 31));
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_StartDate_For_Same_Employee()
    {
        var employeeId = CreateEmployee("Jane");
        var dutyPeriodId = CreateDutyPeriod("DP2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeDutyPeriodDto
        {
            EmployeeId = employeeId,
            DutyPeriodId = dutyPeriodId,
            StartDate = new DateTime(2026, 1, 1)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeDutyPeriodDto
            {
                EmployeeId = employeeId,
                DutyPeriodId = dutyPeriodId,
                StartDate = new DateTime(2026, 1, 1)
            }));
    }

    [Fact]
    public async Task Should_Auto_Chain_EndDate_When_New_Record_Starts()
    {
        var employeeId = CreateEmployee("Alex");
        var dutyPeriodId = CreateDutyPeriod("DP3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeDutyPeriodDto
        {
            EmployeeId = employeeId,
            DutyPeriodId = dutyPeriodId,
            StartDate = new DateTime(2020, 1, 1)
        });

        await service.CreateOrEdit(new CreateOrEditEmployeeDutyPeriodDto
        {
            EmployeeId = employeeId,
            DutyPeriodId = dutyPeriodId,
            StartDate = new DateTime(2021, 1, 1)
        });

        var records = Context.EmployeeDutyPeriods
            .Where(e => e.EmployeeId == employeeId)
            .OrderBy(e => e.StartDate)
            .ToList();

        records.Count.ShouldBe(2);
        records[0].EndDate.ShouldBe(new DateTime(2020, 12, 31));
        records[1].EndDate.ShouldBe(new DateTime(9999, 12, 31));
    }

    [Fact]
    public async Task Should_Restore_Infinite_EndDate_On_Delete_Of_Latest_Record()
    {
        var employeeId = CreateEmployee("Sam");
        var dutyPeriodId = CreateDutyPeriod("DP4");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeDutyPeriodDto
        {
            EmployeeId = employeeId,
            DutyPeriodId = dutyPeriodId,
            StartDate = new DateTime(2020, 1, 1)
        });

        await service.CreateOrEdit(new CreateOrEditEmployeeDutyPeriodDto
        {
            EmployeeId = employeeId,
            DutyPeriodId = dutyPeriodId,
            StartDate = new DateTime(2021, 1, 1)
        });

        var secondRecordId = Context.EmployeeDutyPeriods
            .Single(e => e.EmployeeId == employeeId && e.StartDate == new DateTime(2021, 1, 1)).Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(secondRecordId));

        var remaining = Context.EmployeeDutyPeriods.Single(e => e.EmployeeId == employeeId);
        remaining.EndDate.ShouldBe(new DateTime(9999, 12, 31));
    }
}
