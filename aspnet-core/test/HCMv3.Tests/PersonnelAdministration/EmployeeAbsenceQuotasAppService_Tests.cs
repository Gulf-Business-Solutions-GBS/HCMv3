using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeAbsenceQuotasAppService_Tests : AppTestBase
{
    private readonly IEmployeeAbsenceQuotasAppService _appService;

    public EmployeeAbsenceQuotasAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeAbsenceQuotasAppService>();
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

    private AbsenceType CreateTestAbsenceType(string code)
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return absenceType;
    }

    [Fact]
    public async Task Should_Create_Quota_And_Compute_Balance()
    {
        //Arrange
        var employee = CreateTestEmployee("EQ100");
        var absenceType = CreateTestAbsenceType("AQT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsenceQuotaDto
        {
            EmployeeId = employee.Id,
            AbsenceTypeId = absenceType.Id,
            EntitlementFrom = new DateTime(2024, 1, 1),
            EntitlementTo = new DateTime(9999, 12, 31),
            DeductionFrom = new DateTime(2024, 1, 1),
            DeductionTo = new DateTime(9999, 12, 31),
            QuotaAccrued = 21,
            QuotaUsed = 5,
            QuotaAdjusted = 1,
            QuotaForfeited = 0
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeAbsenceQuotasInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].QuotaBalance.ShouldBe(15);
    }

    [Fact]
    public async Task Should_Throw_When_EntitlementTo_Earlier_Than_EntitlementFrom()
    {
        //Arrange
        var employee = CreateTestEmployee("EQ200");
        var absenceType = CreateTestAbsenceType("AQT200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsenceQuotaDto
            {
                EmployeeId = employee.Id,
                AbsenceTypeId = absenceType.Id,
                EntitlementFrom = new DateTime(2024, 5, 1),
                EntitlementTo = new DateTime(2024, 1, 1),
                DeductionFrom = new DateTime(2024, 5, 1),
                DeductionTo = new DateTime(2024, 1, 1)
            }));
    }

    // ---------------------------------------------------------------------------------------------------
    // Legacy OnSaving calculations: UpdateQuotaAccrued (ActualQuotaAccrued + MaxLimit cap) and
    // UpdateNotificationStatus.
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Set_ActualQuotaAccrued_To_QuotaAccrued_On_Create()
    {
        //Arrange
        var employee = CreateTestEmployee("EQ300");
        var absenceType = CreateTestAbsenceType("AQT300");

        //Act
        await _appService.CreateOrEdit(NewQuota(employee.Id, absenceType.Id, accrued: 12.5, used: 2));

        //Assert
        var row = await SingleQuotaAsync(employee.Id);
        row.QuotaAccrued.ShouldBe(12.5);
        row.ActualQuotaAccrued.ShouldBe(12.5);
    }

    [Fact]
    public async Task Should_Add_QuotaAccrued_Change_To_ActualQuotaAccrued_On_Edit()
    {
        //Arrange
        var employee = CreateTestEmployee("EQ400");
        var absenceType = CreateTestAbsenceType("AQT400");
        await _appService.CreateOrEdit(NewQuota(employee.Id, absenceType.Id, accrued: 10, used: 0));
        var created = await SingleQuotaAsync(employee.Id);
        UsingDbContext(context => context.Set<EmployeeAbsenceQuota>().Find(created.Id).ActualQuotaAccrued = 40);

        //Act
        var edit = NewQuota(employee.Id, absenceType.Id, accrued: 13, used: 0);
        edit.Id = created.Id;
        await _appService.CreateOrEdit(edit);

        //Assert
        var row = await SingleQuotaAsync(employee.Id);
        row.QuotaAccrued.ShouldBe(13);
        row.ActualQuotaAccrued.ShouldBe(43);
    }

    [Fact]
    public async Task Should_Cap_QuotaAccrued_At_SubGroup_MaxLimit_When_ForfeitRule_None()
    {
        //Arrange
        var employee = CreateTestEmployee("EQ500");
        var absenceType = CreateTestAbsenceType("AQT500");
        AssignToSubGroupWithLimits(employee.Id, absenceType.Id, maxLimit: 30, notificationDays: 0);

        //Act
        await _appService.CreateOrEdit(NewQuota(employee.Id, absenceType.Id, accrued: 50, used: 5));

        //Assert
        var row = await SingleQuotaAsync(employee.Id);
        row.QuotaAccrued.ShouldBe(35);
        row.QuotaBalance.ShouldBe(30);
        row.ActualQuotaAccrued.ShouldBe(50);
    }

    [Fact]
    public async Task Should_Not_Cap_QuotaAccrued_When_Balance_Within_MaxLimit()
    {
        //Arrange
        var employee = CreateTestEmployee("EQ600");
        var absenceType = CreateTestAbsenceType("AQT600");
        AssignToSubGroupWithLimits(employee.Id, absenceType.Id, maxLimit: 30, notificationDays: 0);

        //Act
        await _appService.CreateOrEdit(NewQuota(employee.Id, absenceType.Id, accrued: 30, used: 2));

        //Assert
        (await SingleQuotaAsync(employee.Id)).QuotaAccrued.ShouldBe(30);
    }

    [Fact]
    public async Task Should_Not_Cap_QuotaAccrued_When_ForfeitRule_Is_Monthly()
    {
        //Arrange
        var employee = CreateTestEmployee("EQ700");
        var absenceType = CreateTestAbsenceType("AQT700");
        AssignToSubGroupWithLimits(employee.Id, absenceType.Id, maxLimit: 30, notificationDays: 0);
        await _appService.CreateOrEdit(NewQuota(employee.Id, absenceType.Id, accrued: 10, used: 0));
        var created = await SingleQuotaAsync(employee.Id);
        UsingDbContext(context => context.Set<ManageTimeSettings>().ToList().ForEach(s => s.ForfeitRule = ForfeitRule.Monthly));

        //Act
        var edit = NewQuota(employee.Id, absenceType.Id, accrued: 50, used: 0);
        edit.Id = created.Id;
        await _appService.CreateOrEdit(edit);

        //Assert
        (await SingleQuotaAsync(employee.Id)).QuotaAccrued.ShouldBe(50);
    }

    [Fact]
    public async Task Should_Reset_NotificationSent_When_Balance_Below_Notification_Days()
    {
        //Arrange
        var employee = CreateTestEmployee("EQ800");
        var absenceType = CreateTestAbsenceType("AQT800");
        AssignToSubGroupWithLimits(employee.Id, absenceType.Id, maxLimit: 0, notificationDays: 20);
        CreateMaxLimitNotification(isActive: true);
        var quotaId = CreateQuotaRow(employee.Id, absenceType.Id, accrued: 25, notificationSent: true);

        //Act
        var edit = NewQuota(employee.Id, absenceType.Id, accrued: 25, used: 10);
        edit.Id = quotaId;
        await _appService.CreateOrEdit(edit);

        //Assert
        (await SingleQuotaAsync(employee.Id)).NotificationSent.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Keep_NotificationSent_When_Notification_Inactive()
    {
        //Arrange
        var employee = CreateTestEmployee("EQ900");
        var absenceType = CreateTestAbsenceType("AQT900");
        AssignToSubGroupWithLimits(employee.Id, absenceType.Id, maxLimit: 0, notificationDays: 20);
        CreateMaxLimitNotification(isActive: false);
        var quotaId = CreateQuotaRow(employee.Id, absenceType.Id, accrued: 25, notificationSent: true);

        //Act
        var edit = NewQuota(employee.Id, absenceType.Id, accrued: 25, used: 10);
        edit.Id = quotaId;
        await _appService.CreateOrEdit(edit);

        //Assert
        (await SingleQuotaAsync(employee.Id)).NotificationSent.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Overwrite_NotificationSent_From_Input()
    {
        //Arrange
        var employee = CreateTestEmployee("EQ1000");
        var absenceType = CreateTestAbsenceType("AQT1000");
        var quotaId = CreateQuotaRow(employee.Id, absenceType.Id, accrued: 25, notificationSent: true);

        //Act
        var edit = NewQuota(employee.Id, absenceType.Id, accrued: 25, used: 0);
        edit.Id = quotaId;
        edit.NotificationSent = false;
        await _appService.CreateOrEdit(edit);

        //Assert
        (await SingleQuotaAsync(employee.Id)).NotificationSent.ShouldBeTrue();
    }

    private static CreateOrEditEmployeeAbsenceQuotaDto NewQuota(int employeeId, int absenceTypeId, double accrued, double used)
    {
        return new CreateOrEditEmployeeAbsenceQuotaDto
        {
            EmployeeId = employeeId,
            AbsenceTypeId = absenceTypeId,
            EntitlementFrom = new DateTime(2024, 1, 1),
            EntitlementTo = new DateTime(9999, 12, 31),
            DeductionFrom = new DateTime(2024, 1, 1),
            DeductionTo = new DateTime(9999, 12, 31),
            QuotaAccrued = accrued,
            QuotaUsed = used
        };
    }

    private async Task<EmployeeAbsenceQuotaDto> SingleQuotaAsync(int employeeId)
    {
        var result = await _appService.GetAll(new GetAllEmployeeAbsenceQuotasInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        return result.Items[0];
    }

    private int CreateQuotaRow(int employeeId, int absenceTypeId, double accrued, bool notificationSent)
    {
        return UsingDbContext(context =>
        {
            var quota = context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                EntitlementFrom = new DateTime(2024, 1, 1),
                EntitlementTo = new DateTime(9999, 12, 31),
                DeductionFrom = new DateTime(2024, 1, 1),
                DeductionTo = new DateTime(9999, 12, 31),
                QuotaAccrued = accrued,
                ActualQuotaAccrued = accrued,
                NotificationSent = notificationSent
            }).Entity;
            context.SaveChanges();
            return quota.Id;
        });
    }

    private void CreateMaxLimitNotification(bool isActive)
    {
        UsingDbContext(context =>
        {
            context.Set<SystemNotification>().Add(new SystemNotification
            {
                TenantId = AbpSession.TenantId ?? 1,
                NotificationName = EmployeeAbsenceQuotasAppService.AnnualLeaveMaxLimitNotificationName,
                IsActive = isActive
            });
        });
    }

    private void AssignToSubGroupWithLimits(int employeeId, int absenceTypeId, int maxLimit, int notificationDays)
    {
        var tenantId = AbpSession.TenantId ?? 1;
        UsingDbContext(context =>
        {
            var group = context.Set<EmployeeGroup>().Add(new EmployeeGroup { TenantId = tenantId, NameEn = "G", NameAr = "G" }).Entity;
            context.SaveChanges();

            var subGroup = context.Set<EmployeeSubGroup>().Add(new EmployeeSubGroup { TenantId = tenantId, EmployeeGroupId = group.Id, NameEn = "SG", NameAr = "SG" }).Entity;
            context.SaveChanges();

            context.Set<EmployeeSubGroupAbsence>().Add(new EmployeeSubGroupAbsence
            {
                TenantId = tenantId,
                EmployeeSubGroupId = subGroup.Id,
                AbsenceTypeId = absenceTypeId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(9999, 12, 31),
                TotalDays = 30,
                MaxLimit = maxLimit,
                MaxLimitReachNotificationInDays = notificationDays
            });

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = tenantId,
                EmployeeId = employeeId,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = new DateTime(9999, 12, 31),
                EmployeeSubGroupId = subGroup.Id
            });
            context.SaveChanges();
        });
    }
}
