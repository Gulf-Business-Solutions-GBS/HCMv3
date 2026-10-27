using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeWageAccGroupsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private EmployeeWageAccGroupsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeWageAccGroupsAppService(FakeRepository.For<EmployeeWageAccGroup>(Context)));
    }

    private int CreateWageAccGroup(string code)
    {
        var entity = new WageAccGroup { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.WageAccGroups.Add(entity);
        Context.SaveChanges();
        return entity.Id;
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

    [Fact]
    public async Task Should_Create_EmployeeWageAccGroup_With_Infinite_EndDate()
    {
        var wageAccGroupId = CreateWageAccGroup("EWAG1");
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeWageAccGroupDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2025, 1, 1),
            WageAccGroupId = wageAccGroupId
        });

        var result = await service.GetAll(new GetAllEmployeeWageAccGroupsInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EndDate.ShouldBe(InfiniteEndDate);
    }

    [Fact]
    public async Task Should_Auto_Chain_EndDate_When_New_Record_Added()
    {
        var wageAccGroupId1 = CreateWageAccGroup("EWAG2");
        var wageAccGroupId2 = CreateWageAccGroup("EWAG3");
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeWageAccGroupDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2025, 1, 1),
            WageAccGroupId = wageAccGroupId1
        });

        await service.CreateOrEdit(new CreateOrEditEmployeeWageAccGroupDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2025, 6, 1),
            WageAccGroupId = wageAccGroupId2
        });

        var records = Context.EmployeeWageAccGroups
            .Where(g => g.EmployeeId == employeeId)
            .OrderBy(g => g.StartDate)
            .ToList();

        records.Count.ShouldBe(2);
        records[0].EndDate.ShouldBe(new DateTime(2025, 5, 31));
        records[1].EndDate.ShouldBe(InfiniteEndDate);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Triple()
    {
        var wageAccGroupId = CreateWageAccGroup("EWAG4");
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeWageAccGroupDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2025, 1, 1),
            WageAccGroupId = wageAccGroupId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeWageAccGroupDto
            {
                EmployeeId = employeeId,
                StartDate = new DateTime(2025, 1, 1),
                WageAccGroupId = wageAccGroupId
            }));
    }
}
