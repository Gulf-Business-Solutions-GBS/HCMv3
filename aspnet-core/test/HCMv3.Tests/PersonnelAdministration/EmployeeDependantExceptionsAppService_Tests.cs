using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeDependantExceptionsAppService_Tests : AppTestBase
{
    private readonly IEmployeeDependantExceptionsAppService _appService;
    private readonly IEmployeeDependantsAppService _dependantsAppService;

    public EmployeeDependantExceptionsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeDependantExceptionsAppService>();
        _dependantsAppService = Resolve<IEmployeeDependantsAppService>();
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

    private DependantType CreateTestDependantType(string code)
    {
        DependantType dependantType = null;
        UsingDbContext(context =>
        {
            dependantType = context.Set<DependantType>().Add(new DependantType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return dependantType;
    }

    private TicketType CreateTestTicketType(string code)
    {
        TicketType ticketType = null;
        UsingDbContext(context =>
        {
            ticketType = context.Set<TicketType>().Add(new TicketType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return ticketType;
    }

    private async Task<int> CreateTestDependantAsync(int employeeId, int dependantTypeId, int ticketTypeId, DateTime startDate, DateTime endDate)
    {
        await _dependantsAppService.CreateOrEdit(new CreateOrEditEmployeeDependantDto
        {
            EmployeeId = employeeId,
            NameEn = "Dependant",
            NameAr = "معال",
            DependantTypeId = dependantTypeId,
            BirthDate = new DateTime(2015, 1, 1),
            TicketTypeId = ticketTypeId,
            StartDate = startDate,
            EndDate = endDate
        });

        var result = await _dependantsAppService.GetAll(new GetAllEmployeeDependantsInput { EmployeeId = employeeId });
        return result.Items[0].Id;
    }

    [Fact]
    public async Task Should_Create_Exception_Within_Dependant_Range()
    {
        //Arrange
        var employee = CreateTestEmployee("EDE100");
        var dependantType = CreateTestDependantType("DTE100");
        var ticketType = CreateTestTicketType("DTTE100");
        var dependantId = await CreateTestDependantAsync(employee.Id, dependantType.Id, ticketType.Id,
            new DateTime(2024, 1, 1), new DateTime(9999, 12, 31));

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeDependantExceptionDto
        {
            EmployeeDependantId = dependantId,
            ExceptionBenefitType = ExceptionBenefitType.Ticket,
            StartDate = new DateTime(2024, 2, 1),
            EndDate = new DateTime(2024, 12, 31),
            Amount = 500
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeDependantExceptionsInput { EmployeeDependantId = dependantId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Amount.ShouldBe(500);
    }

    [Fact]
    public async Task Should_Throw_When_Exception_Range_Outside_Dependant_Range()
    {
        //Arrange
        var employee = CreateTestEmployee("EDE200");
        var dependantType = CreateTestDependantType("DTE200");
        var ticketType = CreateTestTicketType("DTTE200");
        var dependantId = await CreateTestDependantAsync(employee.Id, dependantType.Id, ticketType.Id,
            new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));

        //Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeDependantExceptionDto
            {
                EmployeeDependantId = dependantId,
                ExceptionBenefitType = ExceptionBenefitType.Ticket,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 3, 1),
                Amount = 500
            }));
    }
}
