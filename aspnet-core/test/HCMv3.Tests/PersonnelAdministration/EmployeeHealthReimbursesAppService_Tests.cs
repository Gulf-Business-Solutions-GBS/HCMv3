using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeHealthReimbursesAppService_Tests : AppTestBase
{
    private readonly IEmployeeHealthReimbursesAppService _appService;

    public EmployeeHealthReimbursesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeHealthReimbursesAppService>();
    }

    private Employee CreateTestEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
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
                TenantId = AbpSession.TenantId ?? 1,
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
        });
        return employee;
    }

    /// <summary>
    /// Gives the employee a salary profile carrying <paramref name="amount"/> of health-reimbursement
    /// entitlement. Required now that the quota is enforced on the HR path as well as the self-service one --
    /// legacy checked it in <c>OnSaving</c>, i.e. on every save whoever made it, and this port restores that.
    /// </summary>
    private void GiveEntitlement(int employeeId, double amount)
    {
        UsingDbContext(context =>
        {
            var group = context.Set<EmployeeGroup>().Add(new EmployeeGroup
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "G",
                NameAr = "G"
            }).Entity;

            var dependantType = context.Set<DependantType>().Add(new DependantType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Self",
                NameAr = "Self",
                IsEmployee = true
            }).Entity;

            context.SaveChanges();

            var subGroup = context.Set<EmployeeSubGroup>().Add(new EmployeeSubGroup
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeGroupId = group.Id,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "SG",
                NameAr = "SG",
                HealthInsurancePeriod = CustomStartMonth.None
            }).Entity;

            context.SaveChanges();

            context.Set<EmployeeSubGroupHealthReimburse>().Add(new EmployeeSubGroupHealthReimburse
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeSubGroupId = subGroup.Id,
                DependantTypeId = dependantType.Id,
                StartDate = new DateTime(2000, 1, 1),
                EndDate = new DateTime(2100, 12, 31),
                Amount = amount
            });

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2100, 12, 31),
                EmployeeGroupId = group.Id,
                EmployeeSubGroupId = subGroup.Id
            });
        });
    }

    [Fact]
    public async Task Should_Create_EmployeeHealthReimburse()
    {
        //Arrange
        var employee = CreateTestEmployee("EHR100");
        GiveEntitlement(employee.Id, 500);

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeHealthReimburseDto
        {
            EmployeeId = employee.Id,
            PaymentDate = new DateTime(2024, 3, 15),
            Amount = 100
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeHealthReimbursesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_Amount_Is_Zero()
    {
        //Arrange
        var employee = CreateTestEmployee("EHR200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeHealthReimburseDto
            {
                EmployeeId = employee.Id,
                PaymentDate = new DateTime(2024, 3, 15),
                Amount = 0
            }));
    }

    [Fact]
    public async Task Should_Refuse_An_HR_Entered_Claim_That_Exceeds_The_Balance()
    {
        // The quota gate is on BOTH write paths. HR entering a claim on an employee's behalf is a normal
        // thing to do and must not be the back door that skips the entitlement check.
        var employee = CreateTestEmployee("EHR300");
        GiveEntitlement(employee.Id, 500);

        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeHealthReimburseDto
            {
                EmployeeId = employee.Id,
                PaymentDate = new DateTime(2024, 3, 15),
                Amount = 600
            }));
    }
}
