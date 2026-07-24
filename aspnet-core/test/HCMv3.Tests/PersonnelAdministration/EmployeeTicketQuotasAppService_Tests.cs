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
public class EmployeeTicketQuotasAppService_Tests : AppTestBase
{
    private readonly IEmployeeTicketQuotasAppService _appService;

    public EmployeeTicketQuotasAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeTicketQuotasAppService>();
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
    public async Task Should_Create_Quota_And_Compute_Balance()
    {
        //Arrange
        var employee = CreateTestEmployee("ETQ100");
        var ticketType = CreateTestTicketType("TQT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeTicketQuotaDto
        {
            EmployeeId = employee.Id,
            TicketTypeId = ticketType.Id,
            EntitlementFrom = new DateTime(2024, 1, 1),
            EntitlementTo = new DateTime(2024, 12, 31),
            DeductionFrom = new DateTime(2024, 1, 1),
            DeductionTo = new DateTime(2024, 12, 31),
            QuotaAccrued = 2,
            QuotaUsed = 1,
            QuotaAdjusted = 0.5
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeTicketQuotasInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].QuotaBalance.ShouldBe(0.5);
    }
}
