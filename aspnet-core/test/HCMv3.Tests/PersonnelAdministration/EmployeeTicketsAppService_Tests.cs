using System;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeTicketsAppService_Tests : AppTestBase
{
    private readonly IEmployeeTicketsAppService _appService;

    public EmployeeTicketsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeTicketsAppService>();
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

    [Fact]
    public async Task Should_Create_EmployeeTicket()
    {
        //Arrange
        var employee = CreateTestEmployee("ETK100");
        var ticketType = CreateTestTicketType("TT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeTicketDto
        {
            EmployeeId = employee.Id,
            TicketTypeId = ticketType.Id,
            TicketDate = new DateTime(2024, 5, 1),
            PaymentDate = new DateTime(2024, 5, 1),
            Compensate = 1,
            Amount = 250
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeTicketsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Amount.ShouldBe(250);
    }

    [Fact]
    public async Task Should_Update_EmployeeTicket()
    {
        //Arrange
        var employee = CreateTestEmployee("ETK200");
        var ticketType = CreateTestTicketType("TT200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeTicketDto
        {
            EmployeeId = employee.Id,
            TicketTypeId = ticketType.Id,
            TicketDate = new DateTime(2024, 5, 1),
            PaymentDate = new DateTime(2024, 5, 1),
            Compensate = 1,
            Amount = 250
        });

        var created = await _appService.GetAll(new GetAllEmployeeTicketsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeTicketDto
        {
            Id = id,
            EmployeeId = employee.Id,
            TicketTypeId = ticketType.Id,
            TicketDate = new DateTime(2024, 5, 1),
            PaymentDate = new DateTime(2024, 5, 1),
            Compensate = 1,
            Amount = 300
        });

        //Assert
        var updated = await _appService.GetEmployeeTicketForEdit(new Abp.Application.Services.Dto.EntityDto(id));
        updated.EmployeeTicket.Amount.ShouldBe(300);
    }
}
