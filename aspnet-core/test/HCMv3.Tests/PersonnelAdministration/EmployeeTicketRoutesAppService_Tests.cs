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
public class EmployeeTicketRoutesAppService_Tests : AppTestBase
{
    private readonly IEmployeeTicketRoutesAppService _appService;

    public EmployeeTicketRoutesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeTicketRoutesAppService>();
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

    private TicketRoute CreateTestTicketRoute(string code)
    {
        TicketRoute ticketRoute = null;
        UsingDbContext(context =>
        {
            ticketRoute = context.Set<TicketRoute>().Add(new TicketRoute
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return ticketRoute;
    }

    [Fact]
    public async Task Should_Create_EmployeeTicketRoute()
    {
        //Arrange
        var employee = CreateTestEmployee("ETR100");
        var ticketRoute = CreateTestTicketRoute("TR100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeTicketRouteDto
        {
            EmployeeId = employee.Id,
            TicketRouteId = ticketRoute.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeTicketRoutesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Date_Range()
    {
        //Arrange
        var employee = CreateTestEmployee("ETR200");
        var ticketRoute = CreateTestTicketRoute("TR200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeTicketRouteDto
        {
            EmployeeId = employee.Id,
            TicketRouteId = ticketRoute.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31)
        });

        //Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeTicketRouteDto
            {
                EmployeeId = employee.Id,
                TicketRouteId = ticketRoute.Id,
                StartDate = new DateTime(2024, 6, 1),
                EndDate = new DateTime(2025, 6, 1)
            }));
    }
}
