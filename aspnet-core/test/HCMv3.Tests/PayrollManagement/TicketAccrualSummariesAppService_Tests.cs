using System;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class TicketAccrualSummariesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private TicketAccrualSummariesAppService CreateAppService()
    {
        return CreateService(() => new TicketAccrualSummariesAppService(FakeRepository.For<TicketAccrualSummary>(Context)));
    }

    private int CreateEmployee()
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
            JoiningDate = new DateTime(2020, 1, 1)
        };
        Context.Employees.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_TicketAccrualSummary_With_Fractional_Values()
    {
        // BUG FIX parity: legacy TicketAccrued/TicketUsed were int; ported as double.
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTicketAccrualSummaryDto
        {
            EmployeeId = employeeId,
            Year = 2026,
            Month = 1,
            TicketAccrued = 0.5,
            TicketUsed = 0.25
        });

        var result = await service.GetAll(new GetAllTicketAccrualSummariesInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].TicketAccrued.ShouldBe(0.5);
        result.Items[0].TicketUsed.ShouldBe(0.25);
    }
}
