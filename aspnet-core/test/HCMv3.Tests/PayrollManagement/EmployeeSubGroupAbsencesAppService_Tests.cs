using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeSubGroupAbsencesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private EmployeeSubGroupAbsencesAppService CreateAppService()
    {
        return CreateService(() => new EmployeeSubGroupAbsencesAppService(
            FakeRepository.For<EmployeeSubGroupAbsence>(Context),
            FakeRepository.For<Employee>(Context)));
    }

    private int CreateEmployeeSubGroup(string code)
    {
        var group = new EmployeeGroup { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.EmployeeGroups.Add(group);
        Context.SaveChanges();

        var subGroup = new EmployeeSubGroup
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeGroupId = group.Id,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.EmployeeSubGroups.Add(subGroup);
        Context.SaveChanges();
        return subGroup.Id;
    }

    private int CreateAbsenceType(string code)
    {
        var entity = new AbsenceType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.AbsenceTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateEmployee(DateTime joiningDate)
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
            JoiningDate = joiningDate
        };
        Context.Employees.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_EmployeeSubGroupAbsence()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG1");
        var absenceTypeId = CreateAbsenceType("AT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupAbsenceDto
        {
            EmployeeSubGroupId = subGroupId,
            AbsenceTypeId = absenceTypeId,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 12, 31),
            ChangeYear = 1,
            TotalDays = 21
        });

        var result = await service.GetAll(new GetAllEmployeeSubGroupAbsencesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].TotalDays.ShouldBe(21);
    }

    [Fact]
    public async Task Should_Throw_On_Overlap()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG2");
        var absenceTypeId = CreateAbsenceType("AT2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupAbsenceDto
        {
            EmployeeSubGroupId = subGroupId,
            AbsenceTypeId = absenceTypeId,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 12, 31),
            ChangeYear = 1
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupAbsenceDto
            {
                EmployeeSubGroupId = subGroupId,
                AbsenceTypeId = absenceTypeId,
                StartDate = new DateTime(2020, 6, 1),
                EndDate = new DateTime(2021, 6, 1),
                ChangeYear = 2
            }));
    }

    [Fact]
    public async Task GetEmployeeSubGroupLeave_Should_Resolve_Tier_By_YearsOfService()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG3");
        var absenceTypeId = CreateAbsenceType("AT3");
        var joiningDate = new DateTime(2015, 1, 1);
        var employeeId = CreateEmployee(joiningDate);
        var dayDate = new DateTime(2022, 6, 1); // ~7.4 years of service

        var service = CreateAppService();

        // Tier 1: ChangeYear = 5 (employee has more than 5 years service)
        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupAbsenceDto
        {
            EmployeeSubGroupId = subGroupId,
            AbsenceTypeId = absenceTypeId,
            StartDate = new DateTime(2019, 1, 1),
            EndDate = new DateTime(2025, 1, 1),
            ChangeYear = 5,
            TotalDays = 30
        });

        var result = await service.GetEmployeeSubGroupLeave(subGroupId, employeeId, absenceTypeId, dayDate);

        result.ShouldNotBeNull();
        result.TotalDays.ShouldBe(30);
    }
}
