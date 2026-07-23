using System;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class LeaveAccrualSummariesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private LeaveAccrualSummariesAppService CreateAppService()
    {
        return CreateService(() => new LeaveAccrualSummariesAppService(FakeRepository.For<LeaveAccrualSummary>(Context)));
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

    private int CreateAbsenceType(string code)
    {
        var entity = new AbsenceType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.AbsenceTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_LeaveAccrualSummary_With_Fractional_Values()
    {
        // BUG FIX parity: legacy UsedDays/CumulativeAccruedDays were int; ported as double for consistency
        // with AccrualDays (already a double in the legacy source).
        var employeeId = CreateEmployee();
        var absenceTypeId = CreateAbsenceType("LAS_AT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditLeaveAccrualSummaryDto
        {
            EmployeeId = employeeId,
            AbsenceTypeId = absenceTypeId,
            Year = 2026,
            Month = 1,
            AccrualDays = 2.5,
            UsedDays = 1.25,
            CumulativeAccruedDays = 3.75
        });

        var result = await service.GetAll(new GetAllLeaveAccrualSummariesInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].UsedDays.ShouldBe(1.25);
        result.Items[0].CumulativeAccruedDays.ShouldBe(3.75);
    }
}
