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
public class EmployeeTktAdjustmentsAppService_Tests : AppTestBase
{
    private readonly IEmployeeTktAdjustmentsAppService _appService;

    public EmployeeTktAdjustmentsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeTktAdjustmentsAppService>();
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
    public async Task Should_Create_Adjustment()
    {
        //Arrange
        var employee = CreateTestEmployee("ETA100");
        var ticketType = CreateTestTicketType("TAT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeTktAdjustmentDto
        {
            EmployeeId = employee.Id,
            TicketTypeId = ticketType.Id,
            AdjustmentType = AdjustmentType.Adjust,
            EffectiveDate = new DateTime(2024, 3, 1),
            QuotaAdjusted = 1.5,
            TicketPaymentType = TicketPaymentType.PayInAdvance
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeTktAdjustmentsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].QuotaAdjusted.ShouldBe(1.5);
        result.Items[0].TicketPaymentType.ShouldBe(TicketPaymentType.PayInAdvance);
    }
}
