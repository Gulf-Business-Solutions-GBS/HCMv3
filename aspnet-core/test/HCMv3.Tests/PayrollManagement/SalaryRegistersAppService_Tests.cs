using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class SalaryRegistersAppService_Tests : PayrollManagementAppServiceTestBase
{
    private SalaryRegistersAppService CreateAppService()
    {
        return CreateService(() => new SalaryRegistersAppService(FakeRepository.For<SalaryRegister>(Context)));
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

    [Fact]
    public async Task Should_Create_And_Retrieve_SalaryRegister()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SR_RT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSalaryRegisterDto
        {
            EmployeeId = employeeId,
            ForPeriod = new DateTime(2026, 1, 1),
            InPeriod = new DateTime(2026, 1, 1),
            PayrollReasonTypeId = reasonTypeId,
            Locked = false
        });

        var result = await service.GetAll(new GetAllSalaryRegistersInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Locked.ShouldBeFalse();
    }

    /// <summary>
    /// Legacy showed <c>Employee.CurrentPayrollArea</c> on the list: the area of the org assignment in force
    /// today, filterable. An employee in another area is left out by the filter.
    /// </summary>
    [Fact]
    public async Task Should_Show_And_Filter_By_The_Employees_Current_Payroll_Area()
    {
        var tenantId = AbpSession.TenantId ?? 1;
        var period = new PayrollPeriod { TenantId = tenantId, Code = "SR-PP", NameEn = "SR-PP", NameAr = "SR-PP" };
        var currency = new HCMv3.GeneralMasterInfo.Currency { TenantId = tenantId, Code = "KWD", NameEn = "KWD", NameAr = "KWD" };
        Context.PayrollPeriods.Add(period);
        Context.Currencies.Add(currency);
        Context.SaveChanges();

        PayrollArea Area(string code, string name) => new()
        {
            TenantId = tenantId, Code = code, Name = name, PayrollPeriodId = period.Id, CurrencyId = currency.Id
        };
        var inArea = Area("PA-IN", "Kuwait");
        var outArea = Area("PA-OUT", "Dubai");
        Context.PayrollAreas.AddRange(inArea, outArea);
        Context.SaveChanges();

        var reasonTypeId = CreatePayrollReasonType("SR_PA");
        var insideId = CreateEmployee();
        var outsideId = CreateEmployee();
        foreach (var (employeeId, area) in new[] { (insideId, inArea), (outsideId, outArea) })
        {
            Context.EmployeeOrgAssignments.Add(new EmployeeOrgAssignment
            {
                TenantId = tenantId,
                EmployeeId = employeeId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(9999, 12, 31),
                PayrollAreaId = area.Id
            });
            CreateSalaryRegister(employeeId, reasonTypeId, locked: false);
        }
        Context.SaveChanges();

        var service = CreateAppService();
        service.EmployeeOrgAssignmentRepository = FakeRepository.For<EmployeeOrgAssignment>(Context);

        var result = await service.GetAll(new GetAllSalaryRegistersInput { PayrollAreaId = inArea.Id });

        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(insideId);
        result.Items[0].PayrollAreaName.ShouldBe("Kuwait");
    }

    [Fact]
    public async Task Should_Throw_When_Updating_Register_That_Remains_Locked()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SR_RT2");
        var registerId = CreateSalaryRegister(employeeId, reasonTypeId, locked: true);
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSalaryRegisterDto
            {
                Id = registerId,
                EmployeeId = employeeId,
                ForPeriod = new DateTime(2026, 1, 1),
                InPeriod = new DateTime(2026, 1, 1),
                PayrollReasonTypeId = reasonTypeId,
                Locked = true
            }));
    }

    [Fact]
    public async Task Should_Allow_Unlocking_A_Locked_Register()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SR_RT3");
        var registerId = CreateSalaryRegister(employeeId, reasonTypeId, locked: true);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSalaryRegisterDto
        {
            Id = registerId,
            EmployeeId = employeeId,
            ForPeriod = new DateTime(2026, 1, 1),
            InPeriod = new DateTime(2026, 1, 1),
            PayrollReasonTypeId = reasonTypeId,
            Locked = false
        });

        var result = await service.GetAll(new GetAllSalaryRegistersInput { EmployeeId = employeeId });
        result.Items[0].Locked.ShouldBeFalse();
    }

    /// <summary>
    /// REGRESSION (defect: SalaryRegister.PayrollReasonTypeId was ported as a NOT NULL FK). Legacy leaves it
    /// NULL on 62,400 of 64,999 production registers because a REGULAR run never resolves a reason -- only an
    /// off-cycle run does. With the non-nullable port, the engine could not persist a regular register at all:
    /// SaveChanges failed on FK_SalaryRegisters_PayrollReasonTypes_PayrollReasonTypeId (Id 0 does not exist).
    /// The test DbContext is SQLite with FK enforcement on, so this insert genuinely exercises the constraint.
    /// </summary>
    [Fact]
    public async Task Should_Persist_Regular_Register_With_No_PayrollReasonType()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSalaryRegisterDto
        {
            EmployeeId = employeeId,
            ForPeriod = new DateTime(2026, 1, 1),
            InPeriod = new DateTime(2026, 1, 1),
            PayrollRunType = PayrollRunType.Regular,
            PayrollResultType = PayrollResultType.Current,
            PayrollReasonTypeId = null,
            Locked = false
        });

        var result = await service.GetAll(new GetAllSalaryRegistersInput { EmployeeId = employeeId });

        result.TotalCount.ShouldBe(1);
        result.Items[0].PayrollReasonTypeId.ShouldBeNull();
        result.Items[0].PayrollReasonTypeName.ShouldBeNull();

        // And the row really is on disk with a NULL FK -- no '(none)' sentinel row is required.
        var persisted = Context.SalaryRegisters.Single(r => r.EmployeeId == employeeId);
        persisted.PayrollReasonTypeId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Throw_When_Deleting_Locked_Register()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SR_RT4");
        var registerId = CreateSalaryRegister(employeeId, reasonTypeId, locked: true);
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.Delete(new Abp.Application.Services.Dto.EntityDto(registerId)));
    }
}
