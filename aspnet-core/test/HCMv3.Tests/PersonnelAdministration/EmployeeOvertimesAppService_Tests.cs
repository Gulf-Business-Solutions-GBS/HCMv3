using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
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
}
