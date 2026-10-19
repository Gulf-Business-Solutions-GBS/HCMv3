using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.Licensing;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Licensing;

/// <summary>The Max Active Employees limit as the Employee screen meets it: hire, reactivate, and save an active employee.</summary>
// ReSharper disable once InconsistentNaming
public class EmployeesAppService_ActiveEmployeeLimit_Tests : LicensingTestBase
{
    private readonly IEmployeesAppService _employeesAppService;

    public EmployeesAppService_ActiveEmployeeLimit_Tests()
    {
        _employeesAppService = Resolve<IEmployeesAppService>();
    }

    private async Task<int> HireAsync(long civilId)
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

        await _employeesAppService.CreateOrEdit(new CreateOrEditEmployeeDto
        {
            Code = "L" + civilId.ToString().Substring(civilId.ToString().Length - 6),
            FirstNameEn = "John",
            LastNameEn = "Doe",
            FirstNameAr = "جون",
            LastNameAr = "دو",
            BirthDate = new DateTime(1990, 1, 1),
            JoiningDate = new DateTime(2020, 1, 1),
            ProbationDays = 90,
            NationalityId = nationality.Id,
            CivilID = civilId
        });

        return UsingDbContext(context => context.Set<Employee>().Single(e => e.CivilID == civilId).Id);
    }

    private async Task SaveWithStatusAsync(int employeeId, EmployeeStatus status)
    {
        var forEdit = await _employeesAppService.GetEmployeeForEdit(new EntityDto(employeeId));
        forEdit.Employee.Status = status;
        await _employeesAppService.CreateOrEdit(forEdit.Employee);
    }

    [Fact]
    public async Task Should_Refuse_A_New_Hire_At_The_Limit()
    {
        await HireAsync(111111111111);
        await SetTenantFeatureAsync(HcmFeatures.MaxActiveEmployees, CountActiveEmployees().ToString());

        await Should.ThrowAsync<UserFriendlyException>(() => HireAsync(222222222222));
    }

    [Fact]
    public async Task Should_Refuse_To_Reactivate_An_Employee_At_The_Limit()
    {
        var leaver = await HireAsync(111111111111);
        await HireAsync(222222222222);
        await SaveWithStatusAsync(leaver, EmployeeStatus.Inactive);
        await SetTenantFeatureAsync(HcmFeatures.MaxActiveEmployees, CountActiveEmployees().ToString());

        await Should.ThrowAsync<UserFriendlyException>(() => SaveWithStatusAsync(leaver, EmployeeStatus.Active));
    }

    [Fact]
    public async Task Should_Always_Save_An_Employee_Who_Is_Already_Active()
    {
        var employee = await HireAsync(111111111111);
        await HireAsync(222222222222);

        // The host lowered the limit below what the tenant already has.
        await SetTenantFeatureAsync(HcmFeatures.MaxActiveEmployees, "1");

        await SaveWithStatusAsync(employee, EmployeeStatus.Active);
    }
}
