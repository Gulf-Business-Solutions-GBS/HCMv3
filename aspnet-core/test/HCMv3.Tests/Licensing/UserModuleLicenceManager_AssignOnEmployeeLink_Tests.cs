using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.Licensing;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Licensing;

/// <summary>Linking a user to an employee gives them ESS and Mobile licences when seats are free, as legacy did.</summary>
// ReSharper disable once InconsistentNaming
public class UserModuleLicenceManager_AssignOnEmployeeLink_Tests : LicensingTestBase
{
    private async Task<int> InsertEmployeeAsync()
    {
        var employee = NewEmployee(EmployeeStatus.Inactive);
        await WithUnitOfWorkAsync(async () => await Resolve<IRepository<Employee>>().InsertAsync(employee));
        return employee.Id;
    }

    private Task LinkAsync(int employeeId, long userId)
    {
        return Resolve<IEmployeeUserLinkAppService>().LinkUser(new LinkEmployeeUserInput
        {
            EmployeeId = employeeId,
            UserId = userId
        });
    }

    [Fact]
    public async Task Should_Assign_Ess_And_Mobile_When_A_User_Is_Linked()
    {
        var user = CreateUser();

        await LinkAsync(await InsertEmployeeAsync(), user.Id);

        CountLicences(user.Id, LicenceModule.Ess).ShouldBe(1);
        CountLicences(user.Id, LicenceModule.Mobile).ShouldBe(1);
        CountLicences(user.Id, LicenceModule.Admin).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Skip_A_Full_Module_And_Still_Link_The_User()
    {
        await SetTenantFeatureAsync(HcmFeatures.SeatsMobile, "1");
        GiveLicence(CreateUser().Id, LicenceModule.Mobile);
        var user = CreateUser();
        var employeeId = await InsertEmployeeAsync();

        await LinkAsync(employeeId, user.Id);

        CountLicences(user.Id, LicenceModule.Ess).ShouldBe(1);
        CountLicences(user.Id, LicenceModule.Mobile).ShouldBe(0);
        UsingDbContext(context => context.Set<Employee>().Find(employeeId).UserId).ShouldBe(user.Id);
    }

    [Fact]
    public async Task Should_Skip_A_Module_Switched_Off_In_The_Edition()
    {
        await SetTenantFeatureAsync(HcmFeatures.ModuleMobile, "false");
        var user = CreateUser();

        await LinkAsync(await InsertEmployeeAsync(), user.Id);

        CountLicences(user.Id, LicenceModule.Mobile).ShouldBe(0);
    }
}
