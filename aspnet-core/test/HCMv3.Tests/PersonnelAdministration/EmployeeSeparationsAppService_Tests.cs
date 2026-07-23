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
public class EmployeeSeparationsAppService_Tests : AppTestBase
{
    private readonly IEmployeeSeparationsAppService _appService;

    public EmployeeSeparationsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeSeparationsAppService>();
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

    private SeparationReason CreateTestSeparationReason(string code)
    {
        SeparationReason reason = null;
        UsingDbContext(context =>
        {
            reason = context.Set<SeparationReason>().Add(new SeparationReason
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return reason;
    }

    [Fact]
    public async Task Should_Create_Separation()
    {
        //Arrange
        var employee = CreateTestEmployee("S100");
        var reason = CreateTestSeparationReason("SR100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeSeparationDto
        {
            EmployeeId = employee.Id,
            SubmissionDate = new DateTime(2023, 1, 1),
            LastDayInService = new DateTime(2023, 2, 1),
            SeparationType = SeparationType.Resignation,
            SeparationReasonId = reason.Id
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeSeparationsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_LastDayInService_Earlier_Than_SubmissionDate()
    {
        //Arrange
        var employee = CreateTestEmployee("S200");
        var reason = CreateTestSeparationReason("SR200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeSeparationDto
            {
                EmployeeId = employee.Id,
                SubmissionDate = new DateTime(2023, 2, 1),
                LastDayInService = new DateTime(2023, 1, 1),
                SeparationType = SeparationType.Resignation,
                SeparationReasonId = reason.Id
            }));
    }
}
