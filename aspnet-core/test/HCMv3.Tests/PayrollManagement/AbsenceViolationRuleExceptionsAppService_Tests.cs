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
public class AbsenceViolationRuleExceptionsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private AbsenceViolationRuleExceptionsAppService CreateAppService()
    {
        return CreateService(() => new AbsenceViolationRuleExceptionsAppService(FakeRepository.For<AbsenceViolationRuleException>(Context)));
    }

    private int CreateAbsenceViolationRule(string code)
    {
        var entity = new AbsenceViolationRule
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.AbsenceViolationRules.Add(entity);
        Context.SaveChanges();
        return entity.Id;
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
    public async Task Should_Create_AbsenceViolationRuleException()
    {
        var ruleId = CreateAbsenceViolationRule("AVR1");
        var employeeId = CreateEmployee("John");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceViolationRuleExceptionDto
        {
            AbsenceViolationRuleId = ruleId,
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31)
        });

        var result = await service.GetAll(new GetAllAbsenceViolationRuleExceptionsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var ruleId = CreateAbsenceViolationRule("AVR2");
        var employeeId = CreateEmployee("Jane");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAbsenceViolationRuleExceptionDto
            {
                AbsenceViolationRuleId = ruleId,
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 1, 31),
                EndDate = new DateTime(2026, 1, 1)
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Date_Range_For_Same_Employee()
    {
        var ruleId = CreateAbsenceViolationRule("AVR3");
        var employeeId = CreateEmployee("Sam");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceViolationRuleExceptionDto
        {
            AbsenceViolationRuleId = ruleId,
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAbsenceViolationRuleExceptionDto
            {
                AbsenceViolationRuleId = ruleId,
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 1, 15),
                EndDate = new DateTime(2026, 2, 15)
            }));
    }
}
