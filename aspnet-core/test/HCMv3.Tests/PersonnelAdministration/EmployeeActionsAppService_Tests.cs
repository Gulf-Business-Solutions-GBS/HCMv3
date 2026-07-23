using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeActionsAppService_Tests : AppTestBase
{
    private readonly IEmployeeActionsAppService _appService;

    public EmployeeActionsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeActionsAppService>();
    }

    private Employee CreateTestEmployee(string code)
    {
        Nationality nationality = null;
        Employee employee = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });
        return employee;
    }

    private PersonalAction CreateTestPersonalAction(string code)
    {
        PersonalAction personalAction = null;
        UsingDbContext(context =>
        {
            personalAction = context.Set<PersonalAction>().Add(new PersonalAction
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return personalAction;
    }

    private PersonalActionReason CreateTestPersonalActionReason(string code, int personalActionId)
    {
        PersonalActionReason reason = null;
        UsingDbContext(context =>
        {
            reason = context.Set<PersonalActionReason>().Add(new PersonalActionReason
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code,
                PersonalActionId = personalActionId
            }).Entity;
        });
        return reason;
    }

    [Fact]
    public async Task Should_Create_EmployeeAction()
    {
        //Arrange
        var employee = CreateTestEmployee("A100");
        var personalAction = CreateTestPersonalAction("PA100");
        var reason = CreateTestPersonalActionReason("PAR100", personalAction.Id);

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeActionDto
        {
            EmployeeId = employee.Id,
            PersonalActionId = personalAction.Id,
            ActionReasonId = reason.Id,
            StartDate = new DateTime(2020, 1, 1)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeActionsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Employee_PersonalAction_StartDate_Combo()
    {
        //Arrange
        var employee = CreateTestEmployee("A200");
        var personalAction = CreateTestPersonalAction("PA200");
        var reason = CreateTestPersonalActionReason("PAR200", personalAction.Id);

        await _appService.CreateOrEdit(new CreateOrEditEmployeeActionDto
        {
            EmployeeId = employee.Id,
            PersonalActionId = personalAction.Id,
            ActionReasonId = reason.Id,
            StartDate = new DateTime(2020, 1, 1)
        });

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeActionDto
            {
                EmployeeId = employee.Id,
                PersonalActionId = personalAction.Id,
                ActionReasonId = reason.Id,
                StartDate = new DateTime(2020, 1, 1)
            }));
    }
}
