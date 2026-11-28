using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeIndemnityQuotaSyncService_Tests : AppTestBase
{
    private readonly IAccrualService _accrualService = Substitute.For<IAccrualService>();
    private readonly EmployeeIndemnityQuotaSyncService _service;

    public EmployeeIndemnityQuotaSyncService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _service = new EmployeeIndemnityQuotaSyncService(
            Resolve<IRepository<EmployeeGratuityQuota>>(),
            Resolve<IRepository<SeparationReason>>(),
            Resolve<IRepository<EmployeeOrgAssignment>>(),
            _accrualService);
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private async Task WithUnitOfWorkAsync(Func<Task> action)
    {
        using var uow = Resolve<IUnitOfWorkManager>().Begin();
        await action();
        await uow.CompleteAsync();
    }

    private async Task<T> WithUnitOfWorkAsync<T>(Func<Task<T>> action)
    {
        using var uow = Resolve<IUnitOfWorkManager>().Begin();
        var result = await action();
        await uow.CompleteAsync();
        return result;
    }

    private int CreateEmployee()
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
            context.SaveChanges();

            var employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
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
            id = employee.Id;
        });
        return id;
    }

    private int CreateQuota(int employeeId, DateTime from, double accrued = 0)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var quota = context.Set<EmployeeGratuityQuota>().Add(new EmployeeGratuityQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                EntitlementFrom = from,
                EntitlementTo = new DateTime(9999, 12, 31),
                DeductionFrom = from,
                DeductionTo = new DateTime(9999, 12, 31),
                QuotaAccrued = accrued
            }).Entity;
            context.SaveChanges();
            id = quota.Id;
        });
        return id;
    }

    private int CreateReason(bool allowAdvanceEoS)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var reason = context.Set<SeparationReason>().Add(new SeparationReason
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Reason",
                NameAr = "Reason",
                AllowAdvanceEoS = allowAdvanceEoS
            }).Entity;
            context.SaveChanges();
            id = reason.Id;
        });
        return id;
    }

    private EmployeeGratuityQuota GetQuota(int id)
        => UsingDbContext(context => context.Set<EmployeeGratuityQuota>().Single(q => q.Id == id));

    [Fact]
    public async Task Should_Move_Earliest_Quota_Start_To_New_Joining_Date()
    {
        // Arrange
        var employeeId = CreateEmployee();
        var later = CreateQuota(employeeId, new DateTime(2022, 1, 1));
        var earliest = CreateQuota(employeeId, new DateTime(2020, 1, 1));

        // Act
        await WithUnitOfWorkAsync(() => _service.AlignToJoiningDateAsync(employeeId, new DateTime(2019, 6, 15, 10, 0, 0)));

        // Assert
        var moved = GetQuota(earliest);
        moved.EntitlementFrom.ShouldBe(new DateTime(2019, 6, 15));
        moved.DeductionFrom.ShouldBe(new DateTime(2019, 6, 15));
        GetQuota(later).EntitlementFrom.ShouldBe(new DateTime(2022, 1, 1));
    }

    [Fact]
    public async Task Should_Do_Nothing_On_Joining_Date_Change_When_Employee_Has_No_Quota()
    {
        // Arrange
        var employeeId = CreateEmployee();

        // Act (an exception here fails the test)
        await WithUnitOfWorkAsync(() => _service.AlignToJoiningDateAsync(employeeId, new DateTime(2019, 6, 15)));

        // Assert
        UsingDbContext(context => context.Set<EmployeeGratuityQuota>().Count(q => q.EmployeeId == employeeId)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Accrue_Quota_To_Last_Day_In_Service_When_Reason_Allows_Advance_EoS()
    {
        // Arrange
        var employeeId = CreateEmployee();
        var quotaId = CreateQuota(employeeId, new DateTime(2020, 1, 1), accrued: 10);
        var reasonId = CreateReason(allowAdvanceEoS: true);
        var lastDay = new DateTime(2025, 6, 30);
        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(9999, 12, 31)
            });
            context.SaveChanges();
        });
        _accrualService.GetTotalIndemnityDaysAsync(employeeId, lastDay, Arg.Any<int?>(), Arg.Any<int?>(), null)
            .Returns(Task.FromResult(123.456));

        // Act
        var changed = await WithUnitOfWorkAsync(() => _service.ApplyAdvanceEndOfServiceAsync(employeeId, reasonId, lastDay));

        // Assert
        changed.ShouldBeTrue();
        GetQuota(quotaId).QuotaAccrued.ShouldBe(123.456);
    }

    [Fact]
    public async Task Should_Leave_Quota_Alone_When_Reason_Does_Not_Allow_Advance_EoS()
    {
        var employeeId = CreateEmployee();
        var quotaId = CreateQuota(employeeId, new DateTime(2020, 1, 1), accrued: 10);
        var reasonId = CreateReason(allowAdvanceEoS: false);

        var changed = await WithUnitOfWorkAsync(() => _service.ApplyAdvanceEndOfServiceAsync(employeeId, reasonId, new DateTime(2025, 6, 30)));

        changed.ShouldBeFalse();
        GetQuota(quotaId).QuotaAccrued.ShouldBe(10);
        await _accrualService.DidNotReceiveWithAnyArgs().GetTotalIndemnityDaysAsync(default, default, default, default, default);
    }

    [Fact]
    public async Task Should_Return_False_On_Advance_EoS_When_Employee_Has_No_Quota()
    {
        var employeeId = CreateEmployee();
        var reasonId = CreateReason(allowAdvanceEoS: true);

        var changed = await WithUnitOfWorkAsync(() => _service.ApplyAdvanceEndOfServiceAsync(employeeId, reasonId, new DateTime(2025, 6, 30)));

        changed.ShouldBeFalse();
    }
}
