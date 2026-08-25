using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.QuotaProvision;
using HCMv3.GridReports.QuotaProvision.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class LeaveQuotaBalanceReportAppService_Tests : AppTestBase
{
    private readonly ILeaveQuotaBalanceReportAppService _appService;

    public LeaveQuotaBalanceReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ILeaveQuotaBalanceReportAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F" + code,
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Full " + code,
                FullNameAr = "عربي " + code,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2018, 1, 1),
                ProbationEndDate = new DateTime(2018, 4, 1),
                NationalityId = nationality.Id,
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    private AbsenceType CreateAbsenceType(string code, bool haveQuotaAccrual, double totalDaysAllowed = 30)
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar",
                IsActive = true,
                HaveQuotaAccrual = haveQuotaAccrual,
                TotalDaysAllowed = totalDaysAllowed
            }).Entity;
        });

        return absenceType;
    }

    [Fact]
    public async Task Should_Return_Empty_When_No_Leave_Type_Is_Selected()
    {
        CreateEmployee("LQB-NONE");

        var result = await _appService.GetLeaveQuotaBalanceReport(new GetLeaveQuotaBalanceReportInput
        {
            MaxResultCount = 100
        });

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Reproduce_The_Legacy_Ledger_Balance_Formula_For_Accrual_Leave_Types()
    {
        var absenceType = CreateAbsenceType("LQB-1", haveQuotaAccrual: true);
        var employee = CreateEmployee("LQB-EMP1");

        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                AbsenceTypeId = absenceType.Id,
                EntitlementFrom = new DateTime(2024, 1, 1),
                EntitlementTo = new DateTime(2024, 12, 31),
                DeductionFrom = new DateTime(2024, 1, 1),
                DeductionTo = new DateTime(2024, 12, 31),
                QuotaAccrued = 30,
                QuotaUsed = 5,
                QuotaAdjusted = 2,
                QuotaForfeited = 1
            });
        });

        var result = await _appService.GetLeaveQuotaBalanceReport(new GetLeaveQuotaBalanceReportInput
        {
            LeaveTypeCode = "LQB-1",
            EmployeeCode = "LQB-EMP1",
            MaxResultCount = 100
        });

        var row = result.Items.Single();
        row.BalanceQuota.ShouldBe(22); // 30 - (5 + 2 + 1), the EmployeeAbsenceQuota ledger formula.
    }
}
