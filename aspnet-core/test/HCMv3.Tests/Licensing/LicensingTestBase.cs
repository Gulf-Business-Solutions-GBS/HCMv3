using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization.Users;
using HCMv3.Authorization.Users;
using HCMv3.GeneralMasterInfo;
using HCMv3.Licensing;
using HCMv3.MultiTenancy;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Tests.Licensing;

/// <summary>Tenant feature values, plain users with chosen permissions, licences and employees for the licensing tests.</summary>
public abstract class LicensingTestBase : AppTestBase
{
    protected LicensingTestBase()
    {
        LoginAsDefaultTenantAdmin();
    }

    protected int TenantId => AbpSession.TenantId ?? 1;

    protected async Task SetTenantFeatureAsync(string name, string value)
    {
        var tenantId = TenantId;
        await WithUnitOfWorkAsync(async () =>
            await Resolve<TenantManager>().SetFeatureValueAsync(tenantId, name, value));
    }

    /// <summary>A user with no role (so not an administrator) holding exactly <paramref name="grantedPermissions"/>.</summary>
    protected User CreateUser(bool isActive = true, params string[] grantedPermissions)
    {
        User user = null;

        UsingDbContext(context =>
        {
            var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
            user = new User
            {
                TenantId = TenantId,
                UserName = "lic" + unique,
                Name = "Licence",
                Surname = "Tester",
                EmailAddress = "lic" + unique + "@testdomain.com",
                IsEmailConfirmed = true,
                IsActive = isActive,
                Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw=="
            };
            user.SetNormalizedNames();

            context.Users.Add(user);
            context.SaveChanges();

            foreach (var permission in grantedPermissions)
            {
                context.UserPermissions.Add(new UserPermissionSetting
                {
                    TenantId = TenantId,
                    UserId = user.Id,
                    Name = permission,
                    IsGranted = true
                });
            }
        });

        return user;
    }

    /// <summary>Writes a licence row directly, bypassing the seat rules -- for setting up a state, not for testing assignment.</summary>
    protected void GiveLicence(long userId, LicenceModule module)
    {
        UsingDbContext(context =>
        {
            context.Set<UserModuleLicence>().Add(new UserModuleLicence
            {
                TenantId = TenantId,
                UserId = userId,
                Module = module
            });
        });
    }

    protected int CountLicences(long userId, LicenceModule module)
    {
        return UsingDbContext(context => context.Set<UserModuleLicence>()
            .Count(l => l.UserId == userId && l.Module == module && !l.IsDeleted));
    }

    protected int CountActiveEmployees()
    {
        var tenantId = TenantId;
        return UsingDbContext(context => context.Set<Employee>()
            .Count(e => e.TenantId == tenantId && e.Status == EmployeeStatus.Active && !e.IsDeleted));
    }

    protected Employee NewEmployee(EmployeeStatus status, long? userId = null)
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

        var code = Guid.NewGuid().ToString("N").Substring(0, 8);
        return new Employee
        {
            TenantId = TenantId,
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
            NationalityId = nationality.Id,
            UserId = userId,
            Status = status
        };
    }
}
