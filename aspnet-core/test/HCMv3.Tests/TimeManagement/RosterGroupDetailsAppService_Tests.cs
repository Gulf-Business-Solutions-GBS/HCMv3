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
public class RosterGroupDetailsAppService_Tests : TimeManagementAppServiceTestBase
{
    private RosterGroupDetailsAppService CreateAppService()
    {
        return CreateService(() => new RosterGroupDetailsAppService(FakeRepository.For<RosterGroupDetail>(Context)));
    }

    private int CreateRosterGroup(string code)
    {
        var entity = new RosterGroup
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.RosterGroups.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateNationality()
    {
        var nationality = new HCMv3.GeneralMasterInfo.Nationality
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Local",
            NameAr = "محلي"
        };
        Context.Nationalities.Add(nationality);
        Context.SaveChanges();
        return nationality.Id;
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
    public async Task Should_Create_RosterGroupDetail()
    {
        var rosterGroupId = CreateRosterGroup("RG1");
        var employeeId = CreateEmployee("John");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRosterGroupDetailDto
        {
            RosterGroupId = rosterGroupId,
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 6, 30)
        });

        var result = await service.GetAll(new GetAllRosterGroupDetailsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Date_Range_For_Same_Employee()
    {
        var rosterGroupId = CreateRosterGroup("RG2");
        var employeeId = CreateEmployee("Jane");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRosterGroupDetailDto
        {
            RosterGroupId = rosterGroupId,
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 6, 30)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditRosterGroupDetailDto
            {
                RosterGroupId = rosterGroupId,
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 12, 31)
            }));
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var rosterGroupId = CreateRosterGroup("RG3");
        var employeeId = CreateEmployee("Alex");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditRosterGroupDetailDto
            {
                RosterGroupId = rosterGroupId,
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 6, 30),
                EndDate = new DateTime(2026, 1, 1)
            }));
    }
}
