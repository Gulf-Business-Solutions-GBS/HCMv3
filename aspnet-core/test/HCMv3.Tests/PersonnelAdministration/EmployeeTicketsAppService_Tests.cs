using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
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

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string NewCode() => Guid.NewGuid().ToString("N").Substring(0, 8);

    /// <summary>
    /// An employee in a FixAmount sub-group (250 per ticket, 2 tickets a year) for the employee ticket type.
    /// Returns (employeeId, ticketTypeId).
    /// </summary>
    private (int EmployeeId, int TicketTypeId) CreateEmployeeWithTicketSetup()
    {
        var result = (0, 0);
        UsingDbContext(context =>
        {
            var nationality = context.Nationalities.Add(new Nationality { TenantId = TenantId, Code = NewCode(), NameEn = "N", NameAr = "N" }).Entity;
            var group = context.Set<EmployeeGroup>().Add(new EmployeeGroup { TenantId = TenantId, Code = NewCode(), NameEn = "G", NameAr = "G" }).Entity;
            var ticketType = context.Set<TicketType>().Add(new TicketType { TenantId = TenantId, Code = "1", NameEn = "Employee", NameAr = "E" }).Entity;
            context.SaveChanges();

            var employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId, Code = NewCode(),
                FirstNameEn = "F", LastNameEn = "L", FirstNameAr = "ف", LastNameAr = "ل",
                FullNameEn = "F L", FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1), JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1), NationalityId = nationality.Id
            }).Entity;
            var subGroup = context.Set<EmployeeSubGroup>().Add(new EmployeeSubGroup
            {
                TenantId = TenantId, EmployeeGroupId = group.Id, Code = NewCode(), NameEn = "SG", NameAr = "SG",
                AirTicketPayType = AirTicketPayType.FixAmount
            }).Entity;
            context.SaveChanges();

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId, EmployeeId = employee.Id,
                StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2100, 12, 31),
                EmployeeGroupId = group.Id, EmployeeSubGroupId = subGroup.Id
            });
            context.Set<EmployeeSubGroupTicket>().Add(new EmployeeSubGroupTicket
            {
                TenantId = TenantId, EmployeeSubGroupId = subGroup.Id, TicketTypeId = ticketType.Id,
                StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2100, 12, 31),
                TicketCount = 2, FixAmount = 250, AccrualPeriod = AccrualPeriod.CalendarYear
            });

            result = (employee.Id, ticketType.Id);
        });
        return result;
    }

    private static CreateOrEditEmployeeTicketDto NewTicket(int employeeId, int ticketTypeId) => new()
    {
        EmployeeId = employeeId,
        TicketTypeId = ticketTypeId,
        TicketDate = new DateTime(2024, 5, 1),
        PaymentDate = new DateTime(2024, 5, 1),
        Compensate = 1,
        Comments = "first"
    };

    [Fact]
    public async Task Should_Create_EmployeeTicket()
    {
        //Arrange
        var (employeeId, ticketTypeId) = CreateEmployeeWithTicketSetup();

        //Act
        await _appService.CreateOrEdit(NewTicket(employeeId, ticketTypeId));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeTicketsInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Amount.ShouldBe(250);
    }

    [Fact]
    public async Task Should_Update_EmployeeTicket()
    {
        //Arrange
        var (employeeId, ticketTypeId) = CreateEmployeeWithTicketSetup();
        await _appService.CreateOrEdit(NewTicket(employeeId, ticketTypeId));
        var id = (await _appService.GetAll(new GetAllEmployeeTicketsInput { EmployeeId = employeeId })).Items[0].Id;

        //Act
        var edit = NewTicket(employeeId, ticketTypeId);
        edit.Id = id;
        edit.Compensate = 2;
        edit.Comments = "changed";
        await _appService.CreateOrEdit(edit);

        //Assert
        var updated = await _appService.GetEmployeeTicketForEdit(new EntityDto(id));
        updated.EmployeeTicket.Comments.ShouldBe("changed");
        updated.EmployeeTicket.Amount.ShouldBe(500);
    }

    [Fact]
    public async Task Should_Calculate_Amount()
    {
        //Arrange
        var (employeeId, ticketTypeId) = CreateEmployeeWithTicketSetup();

        //Act
        var result = await _appService.CalculateAmount(new CalculateEmployeeTicketAmountInput
        {
            EmployeeId = employeeId, TicketTypeId = ticketTypeId, PaymentDate = new DateTime(2024, 5, 1), Compensate = 1
        });

        //Assert
        result.Amount.ShouldBe(250);
    }

    [Fact]
    public async Task Should_Return_Employees_For_Lookup()
    {
        //Arrange
        var (employeeId, _) = CreateEmployeeWithTicketSetup();

        //Act
        var result = await _appService.GetEmployeesForLookup(new GetEmployeeTicketEmployeesInput());

        //Assert
        result.Items.ShouldContain(e => e.Id == employeeId);
    }

    [Fact]
    public async Task Should_Filter_Tickets_By_Ticket_Type_Name()
    {
        //Arrange
        var (employeeId, ticketTypeId) = CreateEmployeeWithTicketSetup();
        await _appService.CreateOrEdit(NewTicket(employeeId, ticketTypeId));

        //Act
        var matching = await _appService.GetAll(new GetAllEmployeeTicketsInput { Filter = "Employee" });
        var notMatching = await _appService.GetAll(new GetAllEmployeeTicketsInput { Filter = "zzz-no-match" });

        //Assert
        matching.TotalCount.ShouldBe(1);
        notMatching.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Delete_EmployeeTicket()
    {
        //Arrange
        var (employeeId, ticketTypeId) = CreateEmployeeWithTicketSetup();
        await _appService.CreateOrEdit(NewTicket(employeeId, ticketTypeId));
        var id = (await _appService.GetAll(new GetAllEmployeeTicketsInput { EmployeeId = employeeId })).Items[0].Id;

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        (await _appService.GetAll(new GetAllEmployeeTicketsInput { EmployeeId = employeeId })).TotalCount.ShouldBe(0);
    }
}
