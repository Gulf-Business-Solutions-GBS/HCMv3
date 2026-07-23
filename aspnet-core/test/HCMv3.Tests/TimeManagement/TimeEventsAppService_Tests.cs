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
public class TimeEventsAppService_Tests : TimeManagementAppServiceTestBase
{
    private TimeEventsAppService CreateAppService()
    {
        return CreateService(() => new TimeEventsAppService(FakeRepository.For<TimeEvent>(Context)));
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

    private int CreateTransType(string code)
    {
        var entity = new TransType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.TransTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_TimeEvent()
    {
        var employeeId = CreateEmployee("John");
        var transTypeId = CreateTransType("IN");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTimeEventDto
        {
            EmployeeId = employeeId,
            TransTypeId = transTypeId,
            TransDate = new DateTime(2026, 1, 1),
            TransTime = new DateTime(2026, 1, 1, 8, 0, 0),
            TransSource = TransSource.Manual
        });

        var result = await service.GetAll(new GetAllTimeEventsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
        result.Items[0].TransTypeId.ShouldBe(transTypeId);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Employee_TransType_TransDate_TransTime()
    {
        var employeeId = CreateEmployee("Jane");
        var transTypeId = CreateTransType("OUT");
        var service = CreateAppService();

        var transDate = new DateTime(2026, 1, 1);
        var transTime = new DateTime(2026, 1, 1, 17, 0, 0);

        await service.CreateOrEdit(new CreateOrEditTimeEventDto
        {
            EmployeeId = employeeId,
            TransTypeId = transTypeId,
            TransDate = transDate,
            TransTime = transTime,
            TransSource = TransSource.Manual
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditTimeEventDto
            {
                EmployeeId = employeeId,
                TransTypeId = transTypeId,
                TransDate = transDate,
                TransTime = transTime,
                TransSource = TransSource.Manual
            }));
    }

    [Fact]
    public async Task Should_Throw_When_Machine_Sourced_RecordSource_Is_Blank()
    {
        var employeeId = CreateEmployee("Alex");
        var transTypeId = CreateTransType("IN2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditTimeEventDto
            {
                EmployeeId = employeeId,
                TransTypeId = transTypeId,
                TransDate = new DateTime(2026, 1, 2),
                TransTime = new DateTime(2026, 1, 2, 8, 0, 0),
                TransSource = TransSource.Machine,
                RecordSource = 0
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Machine_RecordSource()
    {
        var employeeId = CreateEmployee("Sam");
        var transTypeId = CreateTransType("IN3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTimeEventDto
        {
            EmployeeId = employeeId,
            TransTypeId = transTypeId,
            TransDate = new DateTime(2026, 1, 3),
            TransTime = new DateTime(2026, 1, 3, 8, 0, 0),
            TransSource = TransSource.Machine,
            RecordSource = 12345
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditTimeEventDto
            {
                EmployeeId = employeeId,
                TransTypeId = transTypeId,
                TransDate = new DateTime(2026, 1, 3),
                TransTime = new DateTime(2026, 1, 3, 9, 0, 0),
                TransSource = TransSource.Machine,
                RecordSource = 12345
            }));
    }
}
