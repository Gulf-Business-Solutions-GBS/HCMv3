using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Administration.Tools;
using HCMv3.Administration.Tools.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.Tests.PayrollManagement;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Administration;

/// <summary>
/// Unit tests for the ported legacy uscPayrollConfigPanel utilities. Reuses the PayrollManagement bespoke
/// harness (private in-memory Sqlite + NSubstitute-backed fake repositories) because every entity these
/// tools touch already lives in <c>PayrollManagementTestDbContext</c>.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollAdminToolsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private readonly IAccrualService _accrualService = Substitute.For<IAccrualService>();
    private readonly ISysSettingsAccessor _sysSettingsAccessor = Substitute.For<ISysSettingsAccessor>();

    private PayrollAdminToolsAppService CreateAppService()
    {
        return CreateService(() => new PayrollAdminToolsAppService(
            FakeRepository.For<PayrollArea>(Context),
            FakeRepository.For<PayrollCalendar>(Context),
            FakeRepository.For<PayrollControlRecord>(Context),
            FakeRepository.For<SalaryRegister>(Context),
            FakeRepository.For<SalaryRegisterDetail>(Context),
            FakeRepository.For<Employee>(Context),
            FakeRepository.For<EmployeeOrgAssignment>(Context),
            FakeRepository.For<EmployeeAbsenceQuota>(Context),
            FakeRepository.For<AbsenceType>(Context),
            _accrualService,
            _sysSettingsAccessor));
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private PayrollArea CreatePayrollArea(string code, string payrollPeriodCode = "Jan-Dec")
    {
        var period = new PayrollPeriod { TenantId = TenantId, Code = payrollPeriodCode, NameEn = code, NameAr = code };
        Context.PayrollPeriods.Add(period);

        var currency = new HCMv3.GeneralMasterInfo.Currency { TenantId = TenantId, Code = code, NameEn = code, NameAr = code };
        Context.Currencies.Add(currency);
        Context.SaveChanges();

        var area = new PayrollArea
        {
            TenantId = TenantId,
            Code = code,
            Name = code,
            PayrollPeriodId = period.Id,
            CurrencyId = currency.Id
        };
        Context.PayrollAreas.Add(area);
        Context.SaveChanges();
        return area;
    }

    private void CreateControlRecord(int payrollAreaId)
    {
        Context.PayrollControlRecords.Add(new PayrollControlRecord
        {
            TenantId = TenantId,
            PayrollAreaId = payrollAreaId,
            PayrollMonth = 1,
            PayrollYear = 2025,
            CalendarMonthStart = new DateTime(2025, 1, 1),
            CalendarMonthEnd = new DateTime(2025, 1, 31)
        });
        Context.SaveChanges();
    }

    private int? _nationalityId;

    private int GetOrCreateNationalityId()
    {
        if (_nationalityId.HasValue)
        {
            return _nationalityId.Value;
        }

        var nationality = new HCMv3.GeneralMasterInfo.Nationality
        {
            TenantId = TenantId,
            Code = "N1",
            NameEn = "N1",
            NameAr = "N1"
        };
        Context.Nationalities.Add(nationality);
        Context.SaveChanges();

        _nationalityId = nationality.Id;
        return nationality.Id;
    }

    private Employee CreateEmployee(string code)
    {
        var employee = new Employee
        {
            TenantId = TenantId,
            NationalityId = GetOrCreateNationalityId(),
            Code = code,
            FirstNameEn = code,
            LastNameEn = code,
            FirstNameAr = code,
            LastNameAr = code,
            FullNameEn = code,
            FullNameAr = code,
            LoginID = code,
            Status = EmployeeStatus.Active,
            JoiningDate = new DateTime(2020, 1, 1)
        };
        Context.Employees.Add(employee);
        Context.SaveChanges();
        return employee;
    }

    private int? _payrollReasonTypeId;

    private int GetOrCreatePayrollReasonTypeId()
    {
        if (_payrollReasonTypeId.HasValue)
        {
            return _payrollReasonTypeId.Value;
        }

        var reasonType = new PayrollReasonType { TenantId = TenantId, Code = "R1", NameEn = "R1", NameAr = "R1" };
        Context.PayrollReasonTypes.Add(reasonType);
        Context.SaveChanges();

        _payrollReasonTypeId = reasonType.Id;
        return reasonType.Id;
    }

    private SalaryRegister CreateSalaryRegister(int employeeId, DateTime inPeriod, PayrollRunType runType, bool locked = false)
    {
        var register = new SalaryRegister
        {
            TenantId = TenantId,
            EmployeeId = employeeId,
            PayrollReasonTypeId = GetOrCreatePayrollReasonTypeId(),
            InPeriod = inPeriod,
            ForPeriod = inPeriod,
            PayrollRunType = runType,
            Locked = locked
        };
        Context.SalaryRegisters.Add(register);
        Context.SaveChanges();
        return register;
    }

    #region GeneratePayrollCalendar (legacy uscPayrollConfigPanel line 98)

    [Fact]
    public async Task Should_Generate_One_PayrollCalendar_Row_Per_Month_Inclusive()
    {
        var area = CreatePayrollArea("GC1");
        CreateControlRecord(area.Id);

        var result = await CreateAppService().GeneratePayrollCalendar(new GeneratePayrollCalendarInput
        {
            PayrollAreaId = area.Id,
            // Legacy normalises the start to the 1st of its month and includes the end date's month.
            StartDate = new DateTime(2025, 1, 15),
            EndDate = new DateTime(2025, 3, 2)
        });

        result.AffectedCount.ShouldBe(3);

        var rows = Context.PayrollCalendars.Where(r => r.PayrollAreaId == area.Id).OrderBy(r => r.PayrollMonth).ToList();
        rows.Count.ShouldBe(3);
        rows[0].PayrollMonth.ShouldBe(1);
        rows[0].StartDate.ShouldBe(new DateTime(2025, 1, 1));
        rows[0].EndDate.ShouldBe(new DateTime(2025, 1, 31));
        rows[2].PayrollMonth.ShouldBe(3);
        rows[2].EndDate.ShouldBe(new DateTime(2025, 3, 31));
    }

    [Fact]
    public async Task Should_Skip_Months_That_Already_Have_A_PayrollCalendar()
    {
        var area = CreatePayrollArea("GC2");
        CreateControlRecord(area.Id);

        Context.PayrollCalendars.Add(new PayrollCalendar
        {
            TenantId = TenantId,
            PayrollAreaId = area.Id,
            PayrollMonth = 2,
            PayrollYear = 2025,
            StartDate = new DateTime(2025, 2, 1),
            EndDate = new DateTime(2025, 2, 28)
        });
        Context.SaveChanges();

        var result = await CreateAppService().GeneratePayrollCalendar(new GeneratePayrollCalendarInput
        {
            PayrollAreaId = area.Id,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 3, 1)
        });

        result.AffectedCount.ShouldBe(2);
        result.SkippedCount.ShouldBe(1);
        Context.PayrollCalendars.Count(r => r.PayrollAreaId == area.Id).ShouldBe(3);
    }

    [Fact]
    public async Task Should_Apply_Fiscal_Shift_From_PayrollPeriod_Code()
    {
        // "Dec-Nov": payroll month 1 maps to calendar December of the SAME year (legacy comment
        // "01/15 is 01.12.2015" at PayrollControlRecord.cs line 763).
        var area = CreatePayrollArea("GC3", "Dec-Nov");
        CreateControlRecord(area.Id);

        await CreateAppService().GeneratePayrollCalendar(new GeneratePayrollCalendarInput
        {
            PayrollAreaId = area.Id,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 1)
        });

        var row = Context.PayrollCalendars.Single(r => r.PayrollAreaId == area.Id);
        row.StartDate.ShouldBe(new DateTime(2025, 12, 1));
        row.EndDate.ShouldBe(new DateTime(2025, 12, 31));
    }

    [Fact]
    public async Task Should_Refuse_To_Generate_When_PayrollArea_Has_No_ControlRecord()
    {
        // Legacy silently wrote null dates in this case (PayrollControlRecord.cs line 549); this port fails.
        var area = CreatePayrollArea("GC4");

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            CreateAppService().GeneratePayrollCalendar(new GeneratePayrollCalendarInput
            {
                PayrollAreaId = area.Id,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 1, 1)
            }));
    }

    #endregion

    #region DeleteSalaryRegisters (legacy uscPayrollConfigPanel line 1253)

    [Fact]
    public async Task Should_Delete_Salary_Registers_And_Their_Details()
    {
        var employee = CreateEmployee("D1");
        var inPeriod = new DateTime(2025, 1, 31);
        var register = CreateSalaryRegister(employee.Id, inPeriod, PayrollRunType.Regular);

        var wageType = new WageType { TenantId = TenantId, Code = "1000", NameEn = "Basic", NameAr = "Basic" };
        Context.WageTypes.Add(wageType);
        Context.SaveChanges();

        Context.SalaryRegisterDetails.Add(new SalaryRegisterDetail
        {
            TenantId = TenantId,
            SalaryRegisterId = register.Id,
            WageTypeId = wageType.Id,
            Amount = 100
        });
        Context.SaveChanges();

        var result = await CreateAppService().DeleteSalaryRegisters(new SalaryRegisterMaintenanceInput
        {
            Month = 1,
            Year = 2025,
            EmployeeCodes = new[] { "D1" },
            PayrollRunType = PayrollRunType.Regular
        });

        result.AffectedCount.ShouldBe(1);
        Context.SalaryRegisters.Count().ShouldBe(0);
        Context.SalaryRegisterDetails.Count().ShouldBe(0);
    }

    [Fact]
    public async Task Should_Refuse_Delete_When_A_Later_Register_Exists()
    {
        // Legacy lines 1311-1326: any later register blocks, regardless of its run type.
        var employee = CreateEmployee("D2");
        CreateSalaryRegister(employee.Id, new DateTime(2025, 1, 31), PayrollRunType.Regular);
        CreateSalaryRegister(employee.Id, new DateTime(2025, 2, 28), PayrollRunType.Regular);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            CreateAppService().DeleteSalaryRegisters(new SalaryRegisterMaintenanceInput
            {
                Month = 1,
                Year = 2025,
                EmployeeCodes = new[] { "D2" },
                PayrollRunType = PayrollRunType.Regular
            }));

        Context.SalaryRegisters.Count().ShouldBe(2);
    }

    [Fact]
    public async Task Should_Refuse_When_PayrollArea_And_EmployeeCodes_Are_Both_Blank()
    {
        // Legacy lines 1267-1270.
        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            CreateAppService().DeleteSalaryRegisters(new SalaryRegisterMaintenanceInput
            {
                Month = 1,
                Year = 2025,
                PayrollRunType = PayrollRunType.Regular
            }));
    }

    #endregion

    #region UnlockSalaryRegisters (legacy uscPayrollConfigPanel line 1397)

    [Fact]
    public async Task Should_Unlock_Only_Locked_Registers_For_The_Selected_Period()
    {
        var employee = CreateEmployee("U1");
        var locked = CreateSalaryRegister(employee.Id, new DateTime(2025, 1, 31), PayrollRunType.Regular, locked: true);
        var alreadyUnlocked = CreateSalaryRegister(employee.Id, new DateTime(2025, 1, 31), PayrollRunType.Regular);

        var result = await CreateAppService().UnlockSalaryRegisters(new SalaryRegisterMaintenanceInput
        {
            Month = 1,
            Year = 2025,
            EmployeeCodes = new[] { "U1" },
            PayrollRunType = PayrollRunType.Regular
        });

        result.AffectedCount.ShouldBe(1);
        Context.SalaryRegisters.Single(r => r.Id == locked.Id).Locked.ShouldBeFalse();
        Context.SalaryRegisters.Single(r => r.Id == alreadyUnlocked.Id).Locked.ShouldBeFalse();
    }

    #endregion

    #region FixForfeitQuota (legacy uscPayrollConfigPanel line 1171)

    [Fact]
    public async Task Should_Do_Nothing_When_ForfeitRule_Is_Not_Monthly()
    {
        // Legacy line 1203.
        _sysSettingsAccessor.GetTimeSettingsAsync().Returns(Task.FromResult(new ManageTimeSettings
        {
            TenantId = TenantId,
            ForfeitRule = ForfeitRule.Yearly,
            EarliestEvaluationDate = new DateTime(2020, 1, 1)
        }));

        var result = await CreateAppService().FixForfeitQuota(new FixForfeitQuotaInput());

        result.AffectedCount.ShouldBe(0);
        await _accrualService.DidNotReceiveWithAnyArgs()
            .CalculateForfeitForLeaveAsync(0, null, default, default, null);
    }

    [Fact]
    public async Task Should_Recalculate_Forfeit_For_Employees_With_An_AnnualLeave_Quota()
    {
        _sysSettingsAccessor.GetTimeSettingsAsync().Returns(Task.FromResult(new ManageTimeSettings
        {
            TenantId = TenantId,
            ForfeitRule = ForfeitRule.Monthly,
            EarliestEvaluationDate = new DateTime(2024, 1, 1)
        }));

        var annualLeave = new AbsenceType { TenantId = TenantId, Code = "1", NameEn = "Annual", NameAr = "Annual" };
        Context.AbsenceTypes.Add(annualLeave);
        Context.SaveChanges();

        var withQuota = CreateEmployee("F1");
        var withoutQuota = CreateEmployee("F2");

        Context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
        {
            TenantId = TenantId,
            EmployeeId = withQuota.Id,
            AbsenceTypeId = annualLeave.Id,
            EntitlementFrom = new DateTime(2024, 1, 1),
            EntitlementTo = new DateTime(2026, 12, 31)
        });
        Context.SaveChanges();

        _accrualService
            .CalculateForfeitForLeaveAsync(Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int?>())
            .Returns(Task.FromResult(new ForfeitCalculationResult { TotalForfeitDays = 2 }));

        var result = await CreateAppService().FixForfeitQuota(new FixForfeitQuotaInput());

        result.AffectedCount.ShouldBe(1);
        result.SkippedCount.ShouldBe(1);
        await _accrualService.Received(1)
            .CalculateForfeitForLeaveAsync(
                Arg.Is(withQuota.Id), Arg.Any<int?>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Is((int?)null));
    }

    #endregion
}
