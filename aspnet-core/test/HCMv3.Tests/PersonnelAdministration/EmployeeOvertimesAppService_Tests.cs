using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeOvertimesAppService_Tests : AppTestBase
{
    private readonly IEmployeeOvertimesAppService _appService;

    public EmployeeOvertimesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeOvertimesAppService>();
    }

    /// <param name="payOvertime">
    /// The sub group's Pay Overtime on the employee's org assignment; null gives no org assignment at all.
    /// </param>
    private Employee CreateTestEmployee(string code, bool? payOvertime = true)
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

        if (payOvertime.HasValue)
        {
            AssignSubGroup(employee.Id, payOvertime.Value, withSubGroup: true);
        }

        return employee;
    }

    /// <summary>Org assignment for all of 2020 onwards, with or without a sub group that does or does not pay overtime.</summary>
    private void AssignSubGroup(int employeeId, bool payOvertime, bool withSubGroup)
    {
        var tenantId = AbpSession.TenantId ?? 1;
        UsingDbContext(context =>
        {
            int? subGroupId = null;
            if (withSubGroup)
            {
                var group = context.Set<EmployeeGroup>().Add(new EmployeeGroup
                {
                    TenantId = tenantId,
                    Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                    NameEn = "Group",
                    NameAr = "Group"
                }).Entity;
                context.SaveChanges();

                var subGroup = context.Set<EmployeeSubGroup>().Add(new EmployeeSubGroup
                {
                    TenantId = tenantId,
                    EmployeeGroupId = group.Id,
                    Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                    NameEn = "Sub group",
                    NameAr = "Sub group",
                    PayOvertime = payOvertime
                }).Entity;
                context.SaveChanges();
                subGroupId = subGroup.Id;
            }

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = tenantId,
                EmployeeId = employeeId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(9999, 12, 31),
                EmployeeSubGroupId = subGroupId
            });
        });
    }

    private OvertimeType CreateTestOvertimeType(string code)
    {
        OvertimeType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<OvertimeType>().Add(new OvertimeType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code,
                OvertimeRate = 1.5
            }).Entity;
        });
        return type;
    }

    [Fact]
    public async Task Should_Create_EmployeeOvertime()
    {
        //Arrange
        var employee = CreateTestEmployee("EOT100");
        var type = CreateTestOvertimeType("OT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOvertimeDto
        {
            EmployeeId = employee.Id,
            OvertimeTypeId = type.Id,
            EffectiveDate = new DateTime(2024, 3, 15),
            ApprovedOvertimeHours = 4
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeOvertimesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_EffectiveDate()
    {
        //Arrange
        var employee = CreateTestEmployee("EOT200");
        var type = CreateTestOvertimeType("OT200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeOvertimeDto
        {
            EmployeeId = employee.Id,
            OvertimeTypeId = type.Id,
            EffectiveDate = new DateTime(2024, 3, 15),
            ApprovedOvertimeHours = 4
        });

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeOvertimeDto
            {
                EmployeeId = employee.Id,
                OvertimeTypeId = type.Id,
                EffectiveDate = new DateTime(2024, 3, 15),
                ApprovedOvertimeHours = 2
            }));
    }

    /// <summary>
    /// Overtime type with a pay wage type and a Fixed rate of <paramref name="fixedRate"/> per hour for all of
    /// 2024, plus FixedHours payroll settings (8 h) so the legacy hours-basis gate passes
    /// (legacy <c>EmployeeOvertime.cs</c> lines 208, 217-219, 235, 248-250).
    /// </summary>
    private OvertimeType CreatePayableOvertimeType(string code, double overtimeRate, double fixedRate)
    {
        OvertimeType type = null;
        UsingDbContext(context =>
        {
            context.Set<HCMv3.SystemSettings.ManagePayrollSettings>().Add(new HCMv3.SystemSettings.ManagePayrollSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                OvertimeHoursBasis = HCMv3.SystemSettings.OvertimeHoursBasis.FixedHours,
                DefaultWorkingHours = 8
            });

            var wageType = context.Set<HCMv3.PayrollManagement.WageType>().Add(new HCMv3.PayrollManagement.WageType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "OT Pay",
                NameAr = "OT Pay",
                PayrollOrder = 1
            }).Entity;
            context.SaveChanges();

            type = context.Set<OvertimeType>().Add(new OvertimeType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code,
                OvertimeRate = overtimeRate,
                WageTypeId = wageType.Id
            }).Entity;
            context.SaveChanges();

            context.Set<OvertimeTypeDetail>().Add(new OvertimeTypeDetail
            {
                TenantId = AbpSession.TenantId ?? 1,
                OvertimeTypeId = type.Id,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 12, 31),
                OvertimeRateType = OvertimeRateType.Fixed,
                Rate = fixedRate
            });
        });
        return type;
    }

    [Fact]
    public async Task Should_Return_Expected_Overtime_Amount_In_The_List()
    {
        // Legacy EmployeeOvertime.cs lines 206, 250, 294: 4 approved h x 1.5 = 6 h, x fixed rate 2.5 = 15.
        var employee = CreateTestEmployee("EOT300");
        var type = CreatePayableOvertimeType("OT300", overtimeRate: 1.5, fixedRate: 2.5);
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOvertimeDto
        {
            EmployeeId = employee.Id,
            OvertimeTypeId = type.Id,
            EffectiveDate = new DateTime(2024, 3, 15),
            ApprovedOvertimeHours = 4
        });

        var result = await _appService.GetAll(new GetAllEmployeeOvertimesInput { EmployeeId = employee.Id });

        result.Items[0].ExpectedOvertimeAmount.ShouldBe(15);
    }

    [Fact]
    public async Task Should_Return_Expected_Overtime_Amount_With_The_Edit_Output()
    {
        var employee = CreateTestEmployee("EOT301");
        var type = CreatePayableOvertimeType("OT301", overtimeRate: 2, fixedRate: 1.25);
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOvertimeDto
        {
            EmployeeId = employee.Id,
            OvertimeTypeId = type.Id,
            EffectiveDate = new DateTime(2024, 3, 15),
            ApprovedOvertimeHours = 3
        });
        var row = (await _appService.GetAll(new GetAllEmployeeOvertimesInput { EmployeeId = employee.Id })).Items[0];

        var output = await _appService.GetEmployeeOvertimeForEdit(new Abp.Application.Services.Dto.EntityDto(row.Id));

        // 3 h x 2 = 6 h, x 1.25 = 7.5.
        output.ExpectedOvertimeAmount.ShouldBe(7.5);
    }

    [Fact]
    public async Task Should_Calculate_Expected_Overtime_Amount_For_An_Unsaved_Form()
    {
        // Arrange: 5 approved h x 1.5 = 7.5 h, x fixed rate 2 = 15.
        var employee = CreateTestEmployee("EOT400");
        var type = CreatePayableOvertimeType("OT400", overtimeRate: 1.5, fixedRate: 2);

        // Act
        var amount = await _appService.GetExpectedOvertimeAmount(new GetExpectedOvertimeAmountInput
        {
            EmployeeId = employee.Id,
            OvertimeTypeId = type.Id,
            EffectiveDate = new DateTime(2024, 3, 15),
            ApprovedOvertimeHours = 5
        });

        // Assert
        amount.ShouldBe(15);
        (await _appService.GetAll(new GetAllEmployeeOvertimesInput { EmployeeId = employee.Id })).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_Zero_Expected_Overtime_Amount_When_No_Approved_Hours()
    {
        // Arrange
        var employee = CreateTestEmployee("EOT401");
        var type = CreatePayableOvertimeType("OT401", overtimeRate: 1.5, fixedRate: 2);

        // Act
        var amount = await _appService.GetExpectedOvertimeAmount(new GetExpectedOvertimeAmountInput
        {
            EmployeeId = employee.Id,
            OvertimeTypeId = type.Id,
            EffectiveDate = new DateTime(2024, 3, 15),
            ApprovedOvertimeHours = 0
        });

        // Assert
        amount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Filter_The_List_By_Employee_Code()
    {
        // Arrange
        var match = CreateTestEmployee("EOTFLT1");
        var other = CreateTestEmployee("EOTZZZ2");
        var type = CreateTestOvertimeType("OT600");
        foreach (var employee in new[] { match, other })
        {
            await _appService.CreateOrEdit(new CreateOrEditEmployeeOvertimeDto
            {
                EmployeeId = employee.Id,
                OvertimeTypeId = type.Id,
                EffectiveDate = new DateTime(2024, 3, 15),
                ApprovedOvertimeHours = 1
            });
        }

        // Act
        var result = await _appService.GetAll(new GetAllEmployeeOvertimesInput { Filter = "EOTFLT" });

        // Assert
        result.Items.ShouldAllBe(r => r.EmployeeId == match.Id);
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Return_Employees_For_Lookup_Matching_The_Filter()
    {
        // Arrange
        var employee = CreateTestEmployee("EOTLKP1", payOvertime: null);

        // Act
        var result = await _appService.GetEmployeesForLookup(new GetEmployeeOvertimeEmployeesInput { Filter = "EOTLKP" });

        // Assert
        result.Items.ShouldContain(e => e.Id == employee.Id && e.Code == "EOTLKP1");
        result.Items.ShouldAllBe(e => e.Code.Contains("EOTLKP") || e.NameEn.Contains("EOTLKP") || e.NameAr.Contains("EOTLKP"));
    }

    [Fact]
    public async Task Should_Return_The_Employee_With_The_Edit_Output()
    {
        // Arrange
        var employee = CreateTestEmployee("EOT700");
        var type = CreateTestOvertimeType("OT700");
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOvertimeDto
        {
            EmployeeId = employee.Id,
            OvertimeTypeId = type.Id,
            EffectiveDate = new DateTime(2024, 3, 15),
            ApprovedOvertimeHours = 1
        });
        var row = (await _appService.GetAll(new GetAllEmployeeOvertimesInput { EmployeeId = employee.Id })).Items[0];

        // Act
        var output = await _appService.GetEmployeeOvertimeForEdit(new Abp.Application.Services.Dto.EntityDto(row.Id));

        // Assert
        output.EmployeeCode.ShouldBe("EOT700");
        output.EmployeeFullNameEn.ShouldBe("F L");
    }

    [Fact]
    public async Task Should_Throw_When_No_Org_Assignment_On_The_Date()
    {
        // Arrange
        var employee = CreateTestEmployee("EOT500", payOvertime: null);
        var type = CreateTestOvertimeType("OT500");

        // Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(() => _appService.CreateOrEdit(new CreateOrEditEmployeeOvertimeDto
        {
            EmployeeId = employee.Id,
            OvertimeTypeId = type.Id,
            EffectiveDate = new DateTime(2024, 3, 15),
            ApprovedOvertimeHours = 4
        }));
    }

    [Fact]
    public async Task Should_Throw_When_Org_Assignment_Has_No_Sub_Group()
    {
        // Arrange
        var employee = CreateTestEmployee("EOT501", payOvertime: null);
        AssignSubGroup(employee.Id, payOvertime: true, withSubGroup: false);
        var type = CreateTestOvertimeType("OT501");

        // Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(() => _appService.CreateOrEdit(new CreateOrEditEmployeeOvertimeDto
        {
            EmployeeId = employee.Id,
            OvertimeTypeId = type.Id,
            EffectiveDate = new DateTime(2024, 3, 15),
            ApprovedOvertimeHours = 4
        }));
    }

    [Fact]
    public async Task Should_Throw_When_Sub_Group_Does_Not_Pay_Overtime()
    {
        // Arrange
        var employee = CreateTestEmployee("EOT502", payOvertime: false);
        var type = CreateTestOvertimeType("OT502");

        // Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(() => _appService.CreateOrEdit(new CreateOrEditEmployeeOvertimeDto
        {
            EmployeeId = employee.Id,
            OvertimeTypeId = type.Id,
            EffectiveDate = new DateTime(2024, 3, 15),
            ApprovedOvertimeHours = 4
        }));
    }
}
