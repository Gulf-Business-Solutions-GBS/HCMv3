using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeOrgAssignmentsAppService_Tests : AppTestBase
{
    private readonly IEmployeeOrgAssignmentsAppService _appService;

    public EmployeeOrgAssignmentsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeOrgAssignmentsAppService>();
    }

    private Employee CreateTestEmployee(string code)
    {
        Nationality nationality = null;
        Employee employee = null;
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

    private JobTitle CreateTestJobTitle(string code)
    {
        JobTitle jobTitle = null;
        UsingDbContext(context =>
        {
            jobTitle = context.Set<JobTitle>().Add(new JobTitle
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return jobTitle;
    }

    [Fact]
    public async Task Should_Create_OrgAssignment_And_Prefill_From_Previous()
    {
        //Arrange
        var employee = CreateTestEmployee("O100");
        var jobTitle = CreateTestJobTitle("JT100");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            JobTitleId = jobTitle.Id
        });

        //Act - a later record with no JobTitle specified should prefill from previous
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2021, 1, 1)
        });

        //Assert
        UsingDbContext(context =>
        {
            var records = context.Set<EmployeeOrgAssignment>()
                .Where(a => a.EmployeeId == employee.Id)
                .OrderBy(a => a.StartDate)
                .ToList();

            records.Count.ShouldBe(2);
            records[1].JobTitleId.ShouldBe(jobTitle.Id);
        });
    }

    [Fact]
    public async Task Should_Auto_Chain_EndDate_When_New_OrgAssignment_Record_Starts()
    {
        //Arrange
        var employee = CreateTestEmployee("O200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1)
        });

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2021, 1, 1)
        });

        //Assert
        UsingDbContext(context =>
        {
            var records = context.Set<EmployeeOrgAssignment>()
                .Where(a => a.EmployeeId == employee.Id)
                .OrderBy(a => a.StartDate)
                .ToList();

            records.Count.ShouldBe(2);
            records[0].EndDate.ShouldBe(new DateTime(2020, 12, 31));
            records[1].EndDate.ShouldBe(new DateTime(9999, 12, 31));
        });
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_StartDate_For_Same_Employee()
    {
        //Arrange
        var employee = CreateTestEmployee("O300");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1)
        });

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
            {
                EmployeeId = employee.Id,
                StartDate = new DateTime(2020, 1, 1)
            }));
    }
}
