using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeSeparationsAppService_Tests : AppTestBase
{
    private readonly IEmployeeSeparationsAppService _appService;

    public EmployeeSeparationsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeSeparationsAppService>();
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

    private SeparationReason CreateTestSeparationReason(string code)
    {
        SeparationReason reason = null;
        UsingDbContext(context =>
        {
            reason = context.Set<SeparationReason>().Add(new SeparationReason
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return reason;
    }

    [Fact]
    public async Task Should_Create_Separation()
    {
        //Arrange
        var employee = CreateTestEmployee("S100");
        var reason = CreateTestSeparationReason("SR100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeSeparationDto
        {
            EmployeeId = employee.Id,
            SubmissionDate = new DateTime(2023, 1, 1),
            LastDayInService = new DateTime(2023, 2, 1),
            SeparationType = SeparationType.Resignation,
            SeparationReasonId = reason.Id
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeSeparationsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    private void SetNoticePeriodExcludesSubmissionDate(bool exclude)
    {
        UsingDbContext(context =>
        {
            var tenantId = AbpSession.TenantId ?? 1;
            var settings = context.Set<HCMv3.SystemSettings.ManageGeneralSettings>().FirstOrDefault(s => s.TenantId == tenantId)
                           ?? context.Set<HCMv3.SystemSettings.ManageGeneralSettings>()
                               .Add(new HCMv3.SystemSettings.ManageGeneralSettings { TenantId = tenantId }).Entity;
            settings.NoticePeriodDaysExcludingSubmissionDate = exclude;
        });
    }

    /// <summary>Legacy <c>RecomputeNoticePeriod</c>: 1 Jan to 31 Jan is 31 days counting the submission day, 30 without it.</summary>
    [Theory]
    [InlineData(false, 31)]
    [InlineData(true, 30)]
    public async Task Should_Compute_The_Notice_Period_From_The_Dates_Per_General_Settings(bool excludeSubmissionDate, int expectedDays)
    {
        var employee = CreateTestEmployee("S300" + (excludeSubmissionDate ? "X" : "I"));
        var reason = CreateTestSeparationReason("SR300" + (excludeSubmissionDate ? "X" : "I"));
        SetNoticePeriodExcludesSubmissionDate(excludeSubmissionDate);

        await _appService.CreateOrEdit(new CreateOrEditEmployeeSeparationDto
        {
            EmployeeId = employee.Id,
            SubmissionDate = new DateTime(2023, 1, 1),
            LastDayInService = new DateTime(2023, 1, 31),
            NoticePeriodDays = 999, // whatever the screen sent, the dates decide
            SeparationType = SeparationType.Resignation,
            SeparationReasonId = reason.Id
        });

        var result = await _appService.GetAll(new GetAllEmployeeSeparationsInput { EmployeeId = employee.Id });
        result.Items[0].NoticePeriodDays.ShouldBe(expectedDays);
    }

    [Fact]
    public async Task Should_Throw_When_LastDayInService_Earlier_Than_SubmissionDate()
    {
        //Arrange
        var employee = CreateTestEmployee("S200");
        var reason = CreateTestSeparationReason("SR200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeSeparationDto
            {
                EmployeeId = employee.Id,
                SubmissionDate = new DateTime(2023, 2, 1),
                LastDayInService = new DateTime(2023, 1, 1),
                SeparationType = SeparationType.Resignation,
                SeparationReasonId = reason.Id
            }));
    }
}
