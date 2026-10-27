using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.Workflow;
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

    private EmployeeSubGroup CreateTestSubGroup(string code)
    {
        EmployeeSubGroup subGroup = null;
        UsingDbContext(context =>
        {
            var group = context.Set<EmployeeGroup>().Add(new EmployeeGroup
            {
                TenantId = AbpSession.TenantId ?? 1, Code = "G" + code, NameEn = "G" + code, NameAr = "G" + code
            }).Entity;
            context.SaveChanges();

            subGroup = context.Set<EmployeeSubGroup>().Add(new EmployeeSubGroup
            {
                TenantId = AbpSession.TenantId ?? 1, EmployeeGroupId = group.Id, Code = code, NameEn = code, NameAr = code
            }).Entity;
        });
        return subGroup;
    }

    private PayrollArea CreateTestPayrollArea(string code)
    {
        PayrollArea payrollArea = null;
        UsingDbContext(context =>
        {
            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = AbpSession.TenantId ?? 1, Code = "C" + code, NameEn = "C" + code, NameAr = "C" + code, DecimalCount = 3
            }).Entity;
            var period = context.Set<PayrollPeriod>().Add(new PayrollPeriod
            {
                TenantId = AbpSession.TenantId ?? 1, Code = "P" + code, NameEn = "P" + code, NameAr = "P" + code
            }).Entity;
            context.SaveChanges();

            payrollArea = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                Name = code,
                PayrollPeriodId = period.Id,
                CurrencyId = currency.Id,
                CutOffDay = 25
            }).Entity;
        });
        return payrollArea;
    }

    private WorkflowGroup CreateTestWorkflowGroup(string code)
    {
        WorkflowGroup workflowGroup = null;
        UsingDbContext(context =>
        {
            workflowGroup = context.Set<WorkflowGroup>().Add(new WorkflowGroup
            {
                TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code
            }).Entity;
        });
        return workflowGroup;
    }

    /// <summary>An organization unit whose profile carries <paramref name="payrollAreaId"/>.</summary>
    private long CreateTestOrganizationUnit(string code, int? payrollAreaId)
    {
        long organizationUnitId = 0;
        UsingDbContext(context =>
        {
            var unit = context.Set<Abp.Organizations.OrganizationUnit>().Add(new Abp.Organizations.OrganizationUnit
            {
                TenantId = AbpSession.TenantId, Code = code, DisplayName = code
            }).Entity;
            var unitType = context.Set<OrganizationUnitType>().Add(new OrganizationUnitType
            {
                TenantId = AbpSession.TenantId ?? 1, Code = "T" + code, NameEn = "T" + code, NameAr = "T" + code
            }).Entity;
            context.SaveChanges();

            context.Set<OrgUnitProfile>().Add(new OrgUnitProfile
            {
                TenantId = AbpSession.TenantId ?? 1,
                OrganizationUnitId = unit.Id,
                NameAr = code,
                OrganizationUnitTypeId = unitType.Id,
                PayrollAreaId = payrollAreaId
            });
            organizationUnitId = unit.Id;
        });
        return organizationUnitId;
    }

    /// <summary>Written straight to the database, the way Go-Live creates an employee's first assignment.</summary>
    private void SeedAssignment(int employeeId, DateTime startDate, Action<EmployeeOrgAssignment> configure)
    {
        UsingDbContext(context =>
        {
            var assignment = new EmployeeOrgAssignment
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                StartDate = startDate,
                EndDate = new DateTime(9999, 12, 31)
            };
            configure(assignment);
            context.Set<EmployeeOrgAssignment>().Add(assignment);
        });
    }

    private EmployeeOrgAssignment GetAssignment(int employeeId, DateTime startDate)
    {
        EmployeeOrgAssignment assignment = null;
        UsingDbContext(context =>
        {
            assignment = context.Set<EmployeeOrgAssignment>()
                .Single(a => a.EmployeeId == employeeId && a.StartDate == startDate);
        });
        return assignment;
    }

    [Fact]
    public async Task Should_Keep_EmployeeSubGroup_On_A_Later_Assignment_Saved_Through_CreateOrEdit()
    {
        //Arrange - an assignment that holds a group and sub group, as Go-Live creates them
        var employee = CreateTestEmployee("O400");
        var subGroup = CreateTestSubGroup("SG400");
        SeedAssignment(employee.Id, new DateTime(2020, 1, 1), a =>
        {
            a.EmployeeGroupId = subGroup.EmployeeGroupId;
            a.EmployeeSubGroupId = subGroup.Id;
        });

        //Act - a transfer saved from the admin form
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2021, 1, 1)
        });

        //Assert
        var later = GetAssignment(employee.Id, new DateTime(2021, 1, 1));
        later.EmployeeGroupId.ShouldBe(subGroup.EmployeeGroupId);
        later.EmployeeSubGroupId.ShouldBe(subGroup.Id);
    }

    [Fact]
    public async Task Should_Carry_PayrollArea_And_WorkflowGroup_From_Previous_Assignment()
    {
        //Arrange
        var employee = CreateTestEmployee("O500");
        var payrollArea = CreateTestPayrollArea("PA500");
        var workflowGroup = CreateTestWorkflowGroup("WG500");
        SeedAssignment(employee.Id, new DateTime(2020, 1, 1), a =>
        {
            a.PayrollAreaId = payrollArea.Id;
            a.WorkflowGroupId = workflowGroup.Id;
        });

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2021, 1, 1)
        });

        //Assert
        var later = GetAssignment(employee.Id, new DateTime(2021, 1, 1));
        later.PayrollAreaId.ShouldBe(payrollArea.Id);
        later.WorkflowGroupId.ShouldBe(workflowGroup.Id);
    }

    [Fact]
    public async Task Should_Save_The_Four_Fields_Sent_On_Create()
    {
        //Arrange
        var employee = CreateTestEmployee("O600");
        var subGroup = CreateTestSubGroup("SG600");
        var payrollArea = CreateTestPayrollArea("PA600");
        var workflowGroup = CreateTestWorkflowGroup("WG600");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EmployeeGroupId = subGroup.EmployeeGroupId,
            EmployeeSubGroupId = subGroup.Id,
            PayrollAreaId = payrollArea.Id,
            WorkflowGroupId = workflowGroup.Id
        });

        //Assert
        var saved = GetAssignment(employee.Id, new DateTime(2020, 1, 1));
        saved.EmployeeGroupId.ShouldBe(subGroup.EmployeeGroupId);
        saved.EmployeeSubGroupId.ShouldBe(subGroup.Id);
        saved.PayrollAreaId.ShouldBe(payrollArea.Id);
        saved.WorkflowGroupId.ShouldBe(workflowGroup.Id);
    }

    [Fact]
    public async Task Should_Return_The_Four_Fields_For_Edit()
    {
        //Arrange
        var employee = CreateTestEmployee("O700");
        var subGroup = CreateTestSubGroup("SG700");
        var payrollArea = CreateTestPayrollArea("PA700");
        var workflowGroup = CreateTestWorkflowGroup("WG700");
        SeedAssignment(employee.Id, new DateTime(2020, 1, 1), a =>
        {
            a.EmployeeGroupId = subGroup.EmployeeGroupId;
            a.EmployeeSubGroupId = subGroup.Id;
            a.PayrollAreaId = payrollArea.Id;
            a.WorkflowGroupId = workflowGroup.Id;
        });
        var id = GetAssignment(employee.Id, new DateTime(2020, 1, 1)).Id;

        //Act
        var output = await _appService.GetEmployeeOrgAssignmentForEdit(new EntityDto(id));

        //Assert
        output.EmployeeOrgAssignment.EmployeeGroupId.ShouldBe(subGroup.EmployeeGroupId);
        output.EmployeeOrgAssignment.EmployeeSubGroupId.ShouldBe(subGroup.Id);
        output.EmployeeOrgAssignment.PayrollAreaId.ShouldBe(payrollArea.Id);
        output.EmployeeOrgAssignment.WorkflowGroupId.ShouldBe(workflowGroup.Id);
    }

    [Fact]
    public async Task Should_Return_The_Four_Names_In_The_List()
    {
        //Arrange
        var employee = CreateTestEmployee("O750");
        var subGroup = CreateTestSubGroup("SG750");
        var payrollArea = CreateTestPayrollArea("PA750");
        var workflowGroup = CreateTestWorkflowGroup("WG750");
        SeedAssignment(employee.Id, new DateTime(2020, 1, 1), a =>
        {
            a.EmployeeGroupId = subGroup.EmployeeGroupId;
            a.EmployeeSubGroupId = subGroup.Id;
            a.PayrollAreaId = payrollArea.Id;
            a.WorkflowGroupId = workflowGroup.Id;
        });

        //Act
        var result = await _appService.GetAll(new GetAllEmployeeOrgAssignmentsInput { EmployeeId = employee.Id, MaxResultCount = 10 });

        //Assert
        var item = result.Items.Single();
        item.EmployeeGroupName.ShouldBe("GSG750");
        item.EmployeeSubGroupName.ShouldBe("SG750");
        item.PayrollAreaName.ShouldBe("PA750");
        item.WorkflowGroupName.ShouldBe("WG750");
    }

    [Fact]
    public async Task Should_Update_The_Four_Fields()
    {
        //Arrange
        var employee = CreateTestEmployee("O800");
        var subGroup = CreateTestSubGroup("SG800");
        var payrollArea = CreateTestPayrollArea("PA800");
        var workflowGroup = CreateTestWorkflowGroup("WG800");
        SeedAssignment(employee.Id, new DateTime(2020, 1, 1), _ => { });
        var id = GetAssignment(employee.Id, new DateTime(2020, 1, 1)).Id;

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            Id = id,
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EmployeeGroupId = subGroup.EmployeeGroupId,
            EmployeeSubGroupId = subGroup.Id,
            PayrollAreaId = payrollArea.Id,
            WorkflowGroupId = workflowGroup.Id
        });

        //Assert
        var saved = GetAssignment(employee.Id, new DateTime(2020, 1, 1));
        saved.EmployeeGroupId.ShouldBe(subGroup.EmployeeGroupId);
        saved.EmployeeSubGroupId.ShouldBe(subGroup.Id);
        saved.PayrollAreaId.ShouldBe(payrollArea.Id);
        saved.WorkflowGroupId.ShouldBe(workflowGroup.Id);
    }

    [Fact]
    public async Task Should_Not_Carry_Old_SubGroup_When_A_Different_Group_Is_Chosen()
    {
        //Arrange
        var employee = CreateTestEmployee("O900");
        var oldSubGroup = CreateTestSubGroup("SG900");
        var newSubGroup = CreateTestSubGroup("SG901");
        SeedAssignment(employee.Id, new DateTime(2020, 1, 1), a =>
        {
            a.EmployeeGroupId = oldSubGroup.EmployeeGroupId;
            a.EmployeeSubGroupId = oldSubGroup.Id;
        });

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2021, 1, 1),
            EmployeeGroupId = newSubGroup.EmployeeGroupId
        });

        //Assert
        var later = GetAssignment(employee.Id, new DateTime(2021, 1, 1));
        later.EmployeeGroupId.ShouldBe(newSubGroup.EmployeeGroupId);
        later.EmployeeSubGroupId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Carry_Old_SubGroup_When_The_Same_Group_Is_Chosen()
    {
        //Arrange
        var employee = CreateTestEmployee("O950");
        var subGroup = CreateTestSubGroup("SG950");
        SeedAssignment(employee.Id, new DateTime(2020, 1, 1), a =>
        {
            a.EmployeeGroupId = subGroup.EmployeeGroupId;
            a.EmployeeSubGroupId = subGroup.Id;
        });

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2021, 1, 1),
            EmployeeGroupId = subGroup.EmployeeGroupId
        });

        //Assert
        GetAssignment(employee.Id, new DateTime(2021, 1, 1)).EmployeeSubGroupId.ShouldBe(subGroup.Id);
    }

    [Fact]
    public async Task Should_Throw_When_SubGroup_Belongs_To_Another_Group()
    {
        //Arrange
        var employee = CreateTestEmployee("O1000");
        var subGroup = CreateTestSubGroup("SG1000");
        var otherSubGroup = CreateTestSubGroup("SG1001");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
            {
                EmployeeId = employee.Id,
                StartDate = new DateTime(2020, 1, 1),
                EmployeeGroupId = otherSubGroup.EmployeeGroupId,
                EmployeeSubGroupId = subGroup.Id
            }));
    }

    [Fact]
    public async Task Should_Fill_Group_From_SubGroup_When_Group_Is_Blank()
    {
        //Arrange
        var employee = CreateTestEmployee("O1100");
        var subGroup = CreateTestSubGroup("SG1100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EmployeeSubGroupId = subGroup.Id
        });

        //Assert
        GetAssignment(employee.Id, new DateTime(2020, 1, 1)).EmployeeGroupId.ShouldBe(subGroup.EmployeeGroupId);
    }

    [Fact]
    public async Task Should_Default_PayrollArea_From_OrganizationUnit_When_There_Is_No_Previous_Assignment()
    {
        //Arrange
        var employee = CreateTestEmployee("O1200");
        var payrollArea = CreateTestPayrollArea("PA1200");
        var organizationUnitId = CreateTestOrganizationUnit("00120", payrollArea.Id);

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            OrganizationUnitId = organizationUnitId
        });

        //Assert
        GetAssignment(employee.Id, new DateTime(2020, 1, 1)).PayrollAreaId.ShouldBe(payrollArea.Id);
    }

    [Fact]
    public async Task Should_Prefer_Previous_PayrollArea_Over_OrganizationUnit()
    {
        //Arrange
        var employee = CreateTestEmployee("O1300");
        var previousArea = CreateTestPayrollArea("PA1300");
        var unitArea = CreateTestPayrollArea("PA1301");
        var organizationUnitId = CreateTestOrganizationUnit("00130", unitArea.Id);
        SeedAssignment(employee.Id, new DateTime(2020, 1, 1), a => a.PayrollAreaId = previousArea.Id);

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2021, 1, 1),
            OrganizationUnitId = organizationUnitId
        });

        //Assert
        GetAssignment(employee.Id, new DateTime(2021, 1, 1)).PayrollAreaId.ShouldBe(previousArea.Id);
    }

    [Fact]
    public async Task Should_Keep_PayrollArea_Sent_On_Create_Over_OrganizationUnit()
    {
        //Arrange
        var employee = CreateTestEmployee("O1400");
        var chosenArea = CreateTestPayrollArea("PA1400");
        var unitArea = CreateTestPayrollArea("PA1401");
        var organizationUnitId = CreateTestOrganizationUnit("00140", unitArea.Id);

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            OrganizationUnitId = organizationUnitId,
            PayrollAreaId = chosenArea.Id
        });

        //Assert
        GetAssignment(employee.Id, new DateTime(2020, 1, 1)).PayrollAreaId.ShouldBe(chosenArea.Id);
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
