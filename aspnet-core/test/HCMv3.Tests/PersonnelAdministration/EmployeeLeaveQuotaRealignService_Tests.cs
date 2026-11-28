using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeLeaveQuotaRealignService_Tests : AppTestBase
{
    private static readonly DateTime NewJoiningDate = new(2023, 7, 15);

    private readonly EmployeeLeaveQuotaRealignService _service;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public EmployeeLeaveQuotaRealignService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();

        var settings = Substitute.For<ISysSettingsAccessor>();
        settings.GetTimeSettingsAsync().Returns(Task.FromResult(new ManageTimeSettings { LeaveDeductionStartMonth = 6 }));

        _service = new EmployeeLeaveQuotaRealignService(
            Resolve<IRepository<EmployeeAbsenceQuota>>(),
            Resolve<IRepository<AbsenceType>>(),
            settings)
        {
            UnitOfWorkManager = _unitOfWorkManager
        };
    }

    [Fact]
    public async Task Should_Move_Annual_Leave_Entitlement_To_Joining_Date_And_Deduction_By_Start_Month()
    {
        //Arrange
        var employeeId = CreateEmployee("RQ100");
        var typeId = CreateLeaveType(EmployeeLeaveQuotaRealignService.AnnualLeaveCode, AccrualPeriod.WorkYear);
        var firstId = CreateQuota(employeeId, typeId, new DateTime(2020, 1, 1));
        var laterId = CreateQuota(employeeId, typeId, new DateTime(2021, 1, 1));

        //Act
        await RealignAsync(employeeId);

        //Assert
        var first = Quota(firstId);
        first.EntitlementFrom.ShouldBe(NewJoiningDate);
        first.DeductionFrom.ShouldBe(NewJoiningDate.AddMonths(6));
        Quota(laterId).EntitlementFrom.ShouldBe(new DateTime(2021, 1, 1));
    }

    [Fact]
    public async Task Should_Start_Fiscal_Year_Leave_On_April_First_With_Deduction_Same_Day()
    {
        //Arrange
        var employeeId = CreateEmployee("RQ200");
        var typeId = CreateLeaveType("RQF" + Guid.NewGuid().ToString("N").Substring(0, 4), AccrualPeriod.FiscalYear);
        var quotaId = CreateQuota(employeeId, typeId, new DateTime(2020, 1, 1));

        //Act
        await RealignAsync(employeeId);

        //Assert
        var quota = Quota(quotaId);
        quota.EntitlementFrom.ShouldBe(new DateTime(2023, 4, 1));
        quota.DeductionFrom.ShouldBe(new DateTime(2023, 4, 1));
    }

    [Fact]
    public async Task Should_Start_Custom_Leave_On_Its_Custom_Start_Month()
    {
        //Arrange
        var employeeId = CreateEmployee("RQ300");
        var typeId = CreateLeaveType("RQC" + Guid.NewGuid().ToString("N").Substring(0, 4), AccrualPeriod.Custom, CustomStartMonth.September);
        var quotaId = CreateQuota(employeeId, typeId, new DateTime(2020, 1, 1));

        //Act
        await RealignAsync(employeeId);

        //Assert
        Quota(quotaId).EntitlementFrom.ShouldBe(new DateTime(2023, 9, 1));
    }

    [Fact]
    public async Task Should_Not_Change_Inactive_Or_NoAccrual_Leave_Types()
    {
        //Arrange
        var employeeId = CreateEmployee("RQ400");
        var inactiveId = CreateLeaveType("RQI" + Guid.NewGuid().ToString("N").Substring(0, 4), AccrualPeriod.WorkYear, isActive: false);
        var perEmploymentId = CreateLeaveType("RQP" + Guid.NewGuid().ToString("N").Substring(0, 4), AccrualPeriod.PerEmployment);
        var inactiveQuota = CreateQuota(employeeId, inactiveId, new DateTime(2020, 1, 1));
        var perEmploymentQuota = CreateQuota(employeeId, perEmploymentId, new DateTime(2020, 1, 1));

        //Act
        await RealignAsync(employeeId);

        //Assert
        Quota(inactiveQuota).EntitlementFrom.ShouldBe(new DateTime(2020, 1, 1));
        Quota(perEmploymentQuota).EntitlementFrom.ShouldBe(new DateTime(2020, 1, 1));
    }

    private async Task RealignAsync(int employeeId)
    {
        using var uow = _unitOfWorkManager.Begin();
        await _service.RealignAsync(employeeId, NewJoiningDate);
        await uow.CompleteAsync();
    }

    private EmployeeAbsenceQuota Quota(int id) =>
        UsingDbContext(context => context.Set<EmployeeAbsenceQuota>().Single(q => q.Id == id));

    private int CreateEmployee(string code)
    {
        var tenantId = AbpSession.TenantId ?? 1;
        return UsingDbContext(context =>
        {
            var nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = tenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
            context.SaveChanges();

            var employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = tenantId,
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
            context.SaveChanges();
            return employee.Id;
        });
    }

    private int CreateLeaveType(string code, AccrualPeriod period, CustomStartMonth customStartMonth = CustomStartMonth.None, bool isActive = true)
    {
        var tenantId = AbpSession.TenantId ?? 1;
        return UsingDbContext(context =>
        {
            var type = context.Set<AbsenceType>().FirstOrDefault(t => t.Code == code);
            if (type == null)
            {
                type = context.Set<AbsenceType>().Add(new AbsenceType { TenantId = tenantId, Code = code, NameEn = code, NameAr = code }).Entity;
            }

            type.HaveQuotaAccrual = true;
            type.IsActive = isActive;
            type.AccrualPeriod = period;
            type.CustomStartMonth = customStartMonth;
            context.SaveChanges();
            return type.Id;
        });
    }

    private int CreateQuota(int employeeId, int absenceTypeId, DateTime entitlementFrom)
    {
        var tenantId = AbpSession.TenantId ?? 1;
        return UsingDbContext(context =>
        {
            var quota = context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = tenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                EntitlementFrom = entitlementFrom,
                EntitlementTo = entitlementFrom.AddYears(1).AddDays(-1),
                DeductionFrom = entitlementFrom,
                DeductionTo = entitlementFrom.AddYears(1).AddDays(-1),
                QuotaAccrued = 10
            }).Entity;
            context.SaveChanges();
            return quota.Id;
        });
    }
}
