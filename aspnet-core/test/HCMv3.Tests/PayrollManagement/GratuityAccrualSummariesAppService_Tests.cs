using System;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class GratuityAccrualSummariesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private GratuityAccrualSummariesAppService CreateAppService()
    {
        return CreateService(() => new GratuityAccrualSummariesAppService(FakeRepository.For<GratuityAccrualSummary>(Context)));
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
    public async Task Should_Create_And_Retrieve_GratuityAccrualSummary_With_Fractional_Values()
    {
        // BUG FIX parity: legacy fields were int; ported as double to preserve fractional precision.
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditGratuityAccrualSummaryDto
        {
            EmployeeId = employeeId,
            Year = 2026,
            Month = 1,
            GratuityDaysAccrued = 2.5,
            GratuityDaysBalance = 10.75,
            GratuityAmount = 1234.56,
            GratuityAmountPaid = 0.5
        });

        var result = await service.GetAll(new GetAllGratuityAccrualSummariesInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].GratuityDaysAccrued.ShouldBe(2.5);
        result.Items[0].GratuityAmount.ShouldBe(1234.56);
    }
}
