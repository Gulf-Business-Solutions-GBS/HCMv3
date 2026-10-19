using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Licensing;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Licensing;

/// <summary>The Max Active Employees edition feature (legacy licence line "Active Employees"), a hard block.</summary>
// ReSharper disable once InconsistentNaming
public class ActiveEmployeeLimit_Tests : LicensingTestBase
{
    // Transactional, as in production: the Creating event fires after the INSERT inside SaveChanges, so a refusal
    // is undone only by the rollback. The harness itself runs non-transactional (HCMv3TestBaseModule).
    private async Task InsertThroughRepositoryAsync(Employee employee)
    {
        using var uow = Resolve<IUnitOfWorkManager>().Begin(new UnitOfWorkOptions { IsTransactional = true });
        await Resolve<IRepository<Employee>>().InsertAsync(employee);
        await uow.CompleteAsync();
    }

    [Fact]
    public async Task Should_Allow_A_New_Active_Employee_Below_The_Limit()
    {
        await SetTenantFeatureAsync(HcmFeatures.MaxActiveEmployees, (CountActiveEmployees() + 1).ToString());

        await InsertThroughRepositoryAsync(NewEmployee(EmployeeStatus.Active));
    }

    [Fact]
    public async Task Should_Refuse_A_New_Active_Employee_At_The_Limit()
    {
        var before = CountActiveEmployees();
        await SetTenantFeatureAsync(HcmFeatures.MaxActiveEmployees, (before + 1).ToString());
        await InsertThroughRepositoryAsync(NewEmployee(EmployeeStatus.Active));

        await Should.ThrowAsync<UserFriendlyException>(() => InsertThroughRepositoryAsync(NewEmployee(EmployeeStatus.Active)));

        CountActiveEmployees().ShouldBe(before + 1);
    }

    [Fact]
    public async Task Should_Allow_A_New_Inactive_Employee_At_The_Limit()
    {
        await SetTenantFeatureAsync(HcmFeatures.MaxActiveEmployees, (CountActiveEmployees() + 1).ToString());
        await InsertThroughRepositoryAsync(NewEmployee(EmployeeStatus.Active));

        await InsertThroughRepositoryAsync(NewEmployee(EmployeeStatus.Inactive));
    }

    [Fact]
    public async Task Should_Not_Limit_When_The_Feature_Is_Zero()
    {
        await SetTenantFeatureAsync(HcmFeatures.MaxActiveEmployees, "0");

        await InsertThroughRepositoryAsync(NewEmployee(EmployeeStatus.Active));
        await InsertThroughRepositoryAsync(NewEmployee(EmployeeStatus.Active));
    }

    [Fact]
    public async Task Should_Refuse_To_Reactivate_An_Employee_At_The_Limit()
    {
        // At least one active employee, so the limit below is never 0 (which means unlimited).
        await InsertThroughRepositoryAsync(NewEmployee(EmployeeStatus.Active));
        var inactive = NewEmployee(EmployeeStatus.Inactive);
        await InsertThroughRepositoryAsync(inactive);
        await SetTenantFeatureAsync(HcmFeatures.MaxActiveEmployees, CountActiveEmployees().ToString());

        await Should.ThrowAsync<UserFriendlyException>(() =>
            WithUnitOfWorkAsync(() => Resolve<IActiveEmployeeLimitChecker>().EnsureCanActivateAsync(TenantId, inactive.Id)));
    }

    [Fact]
    public async Task Should_Not_Count_The_Employee_Being_Activated_Twice()
    {
        var active = NewEmployee(EmployeeStatus.Active);
        await InsertThroughRepositoryAsync(active);
        await SetTenantFeatureAsync(HcmFeatures.MaxActiveEmployees, CountActiveEmployees().ToString());

        // Exactly at the limit, and the one asked about is already one of the active ones.
        await WithUnitOfWorkAsync(() => Resolve<IActiveEmployeeLimitChecker>().EnsureCanActivateAsync(TenantId, active.Id));
    }
}
