using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeTimePoliciesAppService_Tests : TimeManagementAppServiceTestBase
{
    private EmployeeTimePoliciesAppService CreateAppService()
    {
        return CreateService(() => new EmployeeTimePoliciesAppService(FakeRepository.For<EmployeeTimePolicy>(Context)));
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

    private int CreateTimePolicy(string code)
    {
        var entity = new TimePolicy
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.TimePolicies.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_EmployeeTimePolicy()
    {
        var employeeId = CreateEmployee("John");
        var timePolicyId = CreateTimePolicy("TP1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeTimePolicyDto
        {
            EmployeeId = employeeId,
            TimePolicyId = timePolicyId,
            StartDate = new DateTime(2026, 1, 1)
        });

        var result = await service.GetAll(new GetAllEmployeeTimePoliciesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
        result.Items[0].EndDate.ShouldBe(new DateTime(9999, 12, 31));
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_StartDate_For_Same_Employee()
    {
        var employeeId = CreateEmployee("Jane");
        var timePolicyId = CreateTimePolicy("TP2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeTimePolicyDto
        {
            EmployeeId = employeeId,
            TimePolicyId = timePolicyId,
            StartDate = new DateTime(2026, 1, 1)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeTimePolicyDto
            {
                EmployeeId = employeeId,
                TimePolicyId = timePolicyId,
                StartDate = new DateTime(2026, 1, 1)
            }));
    }

    [Fact]
    public async Task Should_Auto_Chain_EndDate_When_New_Record_Starts()
    {
        var employeeId = CreateEmployee("Alex");
        var timePolicyId = CreateTimePolicy("TP3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeTimePolicyDto
        {
            EmployeeId = employeeId,
            TimePolicyId = timePolicyId,
            StartDate = new DateTime(2020, 1, 1)
        });

        await service.CreateOrEdit(new CreateOrEditEmployeeTimePolicyDto
        {
            EmployeeId = employeeId,
            TimePolicyId = timePolicyId,
            StartDate = new DateTime(2021, 1, 1)
        });

        var records = Context.EmployeeTimePolicies
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
        var timePolicyId = CreateTimePolicy("TP4");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeTimePolicyDto
        {
            EmployeeId = employeeId,
            TimePolicyId = timePolicyId,
            StartDate = new DateTime(2020, 1, 1)
        });

        await service.CreateOrEdit(new CreateOrEditEmployeeTimePolicyDto
        {
            EmployeeId = employeeId,
            TimePolicyId = timePolicyId,
            StartDate = new DateTime(2021, 1, 1)
        });

        var secondRecordId = Context.EmployeeTimePolicies
            .Single(e => e.EmployeeId == employeeId && e.StartDate == new DateTime(2021, 1, 1)).Id;

        await service.Delete(new EntityDto(secondRecordId));

        var remaining = Context.EmployeeTimePolicies.Single(e => e.EmployeeId == employeeId);
        remaining.EndDate.ShouldBe(new DateTime(9999, 12, 31));
    }
}
