using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class SalaryRegisterDaysDetailsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private SalaryRegisterDaysDetailsAppService CreateAppService()
    {
        return CreateService(() => new SalaryRegisterDaysDetailsAppService(
            FakeRepository.For<SalaryRegisterDaysDetail>(Context),
            FakeRepository.For<SalaryRegisterDetail>(Context),
            FakeRepository.For<SalaryRegister>(Context)));
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

    private int CreatePayrollReasonType(string code)
    {
        var entity = new PayrollReasonType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.PayrollReasonTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateWageType(string code)
    {
        var entity = new WageType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code, PayrollOrder = 1 };
        Context.WageTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateSalaryRegister(int employeeId, int payrollReasonTypeId, bool locked)
    {
        var entity = new SalaryRegister
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            ForPeriod = new DateTime(2026, 1, 1),
            InPeriod = new DateTime(2026, 1, 1),
            PayrollRunType = PayrollRunType.Regular,
            PayrollResultType = PayrollResultType.Current,
            PayrollReasonTypeId = payrollReasonTypeId,
            Locked = locked
        };
        Context.SalaryRegisters.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateSalaryRegisterDetail(int registerId, int wageTypeId)
    {
        var entity = new SalaryRegisterDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            SalaryRegisterId = registerId,
            WageTypeId = wageTypeId,
            Rate = 10,
            Number = 5,
            Amount = 50
        };
        Context.SalaryRegisterDetails.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_DaysDetail_When_Ultimate_Parent_Not_Locked()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SRDD_RT1");
        var registerId = CreateSalaryRegister(employeeId, reasonTypeId, locked: false);
        var wageTypeId = CreateWageType("SRDD_WT1");
        var detailId = CreateSalaryRegisterDetail(registerId, wageTypeId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSalaryRegisterDaysDetailDto
        {
            SalaryRegisterDetailId = detailId,
            Period = new DateTime(2026, 1, 1),
            Rate = 10,
            Number = 1,
            Amount = 10
        });

        var result = await service.GetAll(new GetAllSalaryRegisterDaysDetailsInput { SalaryRegisterDetailId = detailId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_Creating_DaysDetail_Under_Locked_Ultimate_Parent()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SRDD_RT2");
        var registerId = CreateSalaryRegister(employeeId, reasonTypeId, locked: true);
        var wageTypeId = CreateWageType("SRDD_WT2");
        var detailId = CreateSalaryRegisterDetail(registerId, wageTypeId);
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSalaryRegisterDaysDetailDto
            {
                SalaryRegisterDetailId = detailId,
                Period = new DateTime(2026, 1, 1),
                Rate = 10,
                Number = 1,
                Amount = 10
            }));
    }
}
